import asyncio
import json
import math
import socket
import sys
import threading
import time

if sys.platform == "win32":
    sys.coinit_flags = 0

from bleak import BleakClient, BleakScanner

DEVICE_NAME_HINT = "WT901BLE"
DATA_CHAR_UUID = "0000ffe4-0000-1000-8000-00805f9a34fb"

# Старый канал, который у тебя уже работал раньше.
LEGACY_UDP_IP = "127.0.0.1"
LEGACY_UDP_PORT = 5005
SEND_LEGACY_CSV = True

# Новый канал для BodyRayUdpReceiver.cs из Unity.
BODYRAY_UDP_IP = "127.0.0.1"
BODYRAY_UDP_PORT = 50555
SEND_BODYRAY_JSON = True

# Включи True, если нужно посмотреть сырые BLE-пакеты.
RAW_DEBUG = False
RAW_DEBUG_LIMIT_PER_SEC = 5

# Частота UDP-отправки в Unity. Даже если BLE приходит рывками,
# Unity будет получать последнее актуальное состояние стабильно.
UDP_SEND_HZ = 100
PRINT_SEND_HZ = 5
STATS_PRINT_INTERVAL_SEC = 1.0

# Пока используем только рабочую логику наклона.
# X берём из roll, Y берём из pitch.
# Если оси в Unity будут перепутаны, поменяй BODYRAY_USE_ROLL_FOR_X / BODYRAY_USE_PITCH_FOR_Y
# или поставь BODYRAY_INVERT_X / BODYRAY_INVERT_Y.
BODYRAY_TRACKING_MODE = "single"       # single / pair / triad
BODYRAY_BODY_POINT_ID = 5              # 5 = RightForearm
BODYRAY_BODY_POINT_ZONE_ID = "zone_right_forearm"
BODYRAY_TRACKER_ID = "WT901_01"
BODYRAY_CURSOR_ID = "cursor_1"

BODYRAY_USE_ROLL_FOR_X = True
BODYRAY_USE_PITCH_FOR_Y = True
BODYRAY_INVERT_X = False
BODYRAY_INVERT_Y = False

# Насколько градусов наклона соответствует одной world-единице Unity.
# Например 20 градусов / 1 world unit. Меньше число = курсор быстрее.
DEGREES_PER_WORLD_UNIT_X = 20.0
DEGREES_PER_WORLD_UNIT_Y = 20.0

# Ограничение координат, чтобы курсор не улетал за рабочую область.
MAX_WORLD_X = 3.2
MAX_WORLD_Y = 2.0

# Мёртвая зона в градусах около центра.
TILT_DEADZONE_DEG = 0.6

# Сглаживание координат. 0 = без сглаживания, 0.85 = сильное сглаживание.
SMOOTHING = 0.75

# Автокалибровка центра по первым стабильным данным.
AUTO_CALIBRATE_ON_START = True
CALIBRATION_SECONDS = 1.5

latest_values = {
    "roll": 0.0,
    "pitch": 0.0,
    "yaw": 0.0,
    "accX": 0.0,
    "accY": 0.0,
    "accZ": 0.0,
    "gyroX": 0.0,
    "gyroY": 0.0,
    "gyroZ": 0.0,
}

has_any_data = False
ble_error = None
ble_status = "Подключение..."
packet_count = 0
stop_event = threading.Event()

legacy_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
bodyray_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

_stream_buffer = bytearray()
_last_raw_print = 0.0
_last_send_print = 0.0
_last_stats_print = 0.0
_ble_packets_this_sec = 0
_udp_packets_this_sec = 0
_last_ble_packet_time = 0.0
_sample_seq = 0

calibration_started_at = None
calibration_done = False
calibration_roll_sum = 0.0
calibration_pitch_sum = 0.0
calibration_count = 0
center_roll = 0.0
center_pitch = 0.0

smoothed_world_x = 0.0
smoothed_world_y = 0.0
has_smoothed_value = False


def _short_le(data, start):
    return int.from_bytes(data[start:start + 2], byteorder="little", signed=True)


def parse_wit_11_frame(frame):
    """
    Обычный WIT/WitMotion формат:
    55 51 ... checksum — acceleration
    55 52 ... checksum — gyro
    55 53 ... checksum — angles
    """
    if len(frame) != 11 or frame[0] != 0x55:
        return False

    frame_type = frame[1]

    # checksum в BLE иногда может отличаться из-за склейки пакетов,
    # поэтому не делаем его жёстким условием.
    values = [_short_le(frame, i) for i in range(2, 8, 2)]

    if frame_type == 0x51:
        latest_values["accX"] = values[0] / 32768.0 * 16.0
        latest_values["accY"] = values[1] / 32768.0 * 16.0
        latest_values["accZ"] = values[2] / 32768.0 * 16.0
        return True

    if frame_type == 0x52:
        latest_values["gyroX"] = values[0] / 32768.0 * 2000.0
        latest_values["gyroY"] = values[1] / 32768.0 * 2000.0
        latest_values["gyroZ"] = values[2] / 32768.0 * 2000.0
        return True

    if frame_type == 0x53:
        latest_values["roll"] = values[0] / 32768.0 * 180.0
        latest_values["pitch"] = values[1] / 32768.0 * 180.0
        latest_values["yaw"] = values[2] / 32768.0 * 180.0
        return True

    return False


def parse_combined_20_packet(data):
    """
    Формат, который у тебя уже работал раньше:
    data[0] == 0x55, дальше 9 signed short:
      0..2 — acceleration
      3..5 — gyro
      6..8 — roll/pitch/yaw
    """
    if len(data) < 20 or data[0] != 0x55:
        return False

    decoded = [_short_le(data, i) for i in range(2, 20, 2)]
    if len(decoded) < 9:
        return False

    latest_values["accX"] = decoded[0] / 32768.0 * 16.0
    latest_values["accY"] = decoded[1] / 32768.0 * 16.0
    latest_values["accZ"] = decoded[2] / 32768.0 * 16.0

    latest_values["gyroX"] = decoded[3] / 32768.0 * 2000.0
    latest_values["gyroY"] = decoded[4] / 32768.0 * 2000.0
    latest_values["gyroZ"] = decoded[5] / 32768.0 * 2000.0

    latest_values["roll"] = decoded[6] / 32768.0 * 180.0
    latest_values["pitch"] = decoded[7] / 32768.0 * 180.0
    latest_values["yaw"] = decoded[8] / 32768.0 * 180.0
    return True


def parse_stream_frames(data):
    """
    Парсер потока 11-байтных WIT-кадров.
    BLE может присылать несколько кадров склеенными или разрезанными.
    """
    global _stream_buffer

    parsed_any = False
    _stream_buffer.extend(data)

    while True:
        try:
            start = _stream_buffer.index(0x55)
        except ValueError:
            _stream_buffer.clear()
            break

        if start > 0:
            del _stream_buffer[:start]

        if len(_stream_buffer) < 11:
            break

        frame_type = _stream_buffer[1]
        if frame_type not in (0x51, 0x52, 0x53):
            del _stream_buffer[0]
            continue

        frame = bytes(_stream_buffer[:11])
        del _stream_buffer[:11]

        if parse_wit_11_frame(frame):
            parsed_any = True

    return parsed_any


def parse_sensor_data(data):
    if parse_combined_20_packet(data):
        return True

    return parse_stream_frames(data)


def make_legacy_udp_message():
    return (
        f"{latest_values['roll']:.4f},"
        f"{latest_values['pitch']:.4f},"
        f"{latest_values['yaw']:.4f},"
        f"{latest_values['accX']:.5f},"
        f"{latest_values['accY']:.5f},"
        f"{latest_values['accZ']:.5f},"
        f"{latest_values['gyroX']:.4f},"
        f"{latest_values['gyroY']:.4f},"
        f"{latest_values['gyroZ']:.4f},"
        f"{_sample_seq}"
    )


def clamp(value, min_value, max_value):
    return max(min_value, min(max_value, value))


def apply_deadzone(value, deadzone):
    if abs(value) < deadzone:
        return 0.0

    if value > 0:
        return value - deadzone

    return value + deadzone


def update_calibration():
    global calibration_started_at, calibration_done, calibration_roll_sum, calibration_pitch_sum
    global calibration_count, center_roll, center_pitch

    if not AUTO_CALIBRATE_ON_START:
        calibration_done = True
        return

    now = time.time()

    if calibration_done:
        return

    if calibration_started_at is None:
        calibration_started_at = now
        calibration_roll_sum = 0.0
        calibration_pitch_sum = 0.0
        calibration_count = 0
        print(f"CALIBRATION: держи датчик в нейтральном положении {CALIBRATION_SECONDS:.1f} сек...")

    calibration_roll_sum += latest_values["roll"]
    calibration_pitch_sum += latest_values["pitch"]
    calibration_count += 1

    if now - calibration_started_at >= CALIBRATION_SECONDS:
        if calibration_count > 0:
            center_roll = calibration_roll_sum / calibration_count
            center_pitch = calibration_pitch_sum / calibration_count

        calibration_done = True
        print(f"CALIBRATION DONE: center_roll={center_roll:.3f}, center_pitch={center_pitch:.3f}")


def calibrate_now():
    global calibration_started_at, calibration_done, calibration_roll_sum, calibration_pitch_sum, calibration_count
    calibration_started_at = None
    calibration_done = False
    calibration_roll_sum = 0.0
    calibration_pitch_sum = 0.0
    calibration_count = 0
    print("CALIBRATION RESET: новая калибровка центра запущена.")


def compute_world_from_tilt():
    global smoothed_world_x, smoothed_world_y, has_smoothed_value

    update_calibration()

    raw_x_angle = latest_values["roll"] if BODYRAY_USE_ROLL_FOR_X else latest_values["pitch"]
    raw_y_angle = latest_values["pitch"] if BODYRAY_USE_PITCH_FOR_Y else latest_values["roll"]

    center_x_angle = center_roll if BODYRAY_USE_ROLL_FOR_X else center_pitch
    center_y_angle = center_pitch if BODYRAY_USE_PITCH_FOR_Y else center_roll

    dx_deg = raw_x_angle - center_x_angle
    dy_deg = raw_y_angle - center_y_angle

    dx_deg = apply_deadzone(dx_deg, TILT_DEADZONE_DEG)
    dy_deg = apply_deadzone(dy_deg, TILT_DEADZONE_DEG)

    if BODYRAY_INVERT_X:
        dx_deg = -dx_deg

    if BODYRAY_INVERT_Y:
        dy_deg = -dy_deg

    raw_world_x = clamp(dx_deg / DEGREES_PER_WORLD_UNIT_X, -MAX_WORLD_X, MAX_WORLD_X)
    raw_world_y = clamp(dy_deg / DEGREES_PER_WORLD_UNIT_Y, -MAX_WORLD_Y, MAX_WORLD_Y)

    if not has_smoothed_value:
        smoothed_world_x = raw_world_x
        smoothed_world_y = raw_world_y
        has_smoothed_value = True
    else:
        smoothed_world_x = smoothed_world_x * SMOOTHING + raw_world_x * (1.0 - SMOOTHING)
        smoothed_world_y = smoothed_world_y * SMOOTHING + raw_world_y * (1.0 - SMOOTHING)

    return raw_world_x, raw_world_y, smoothed_world_x, smoothed_world_y


def make_bodyray_udp_packet():
    raw_x, raw_y, corrected_x, corrected_y = compute_world_from_tilt()

    # В текущем Unity world_to_mm примерно 125 мм на 1 world unit.
    world_to_mm = 125.0

    correction_mm = math.sqrt(
        ((corrected_x - raw_x) * world_to_mm) ** 2 +
        ((corrected_y - raw_y) * world_to_mm) ** 2
    )

    data_age = time.time() - _last_ble_packet_time if _last_ble_packet_time > 0 else 999.0
    confidence = 1.0 if data_age < 0.25 and calibration_done else 0.5

    return {
        "type": "body_ray_pointer_v2",
        "timeSec": time.monotonic(),
        "trackingMode": BODYRAY_TRACKING_MODE,
        "bodyPointId": BODYRAY_BODY_POINT_ID,
        "bodyPointZoneId": BODYRAY_BODY_POINT_ZONE_ID,
        "trackerId": BODYRAY_TRACKER_ID,
        "cursorId": BODYRAY_CURSOR_ID,

        "rawWorldX": raw_x,
        "rawWorldY": raw_y,
        "rawXMm": raw_x * world_to_mm,
        "rawYMm": raw_y * world_to_mm,

        "correctedWorldX": corrected_x,
        "correctedWorldY": corrected_y,
        "correctedXMm": corrected_x * world_to_mm,
        "correctedYMm": corrected_y * world_to_mm,

        "correctionMm": correction_mm,
        "confidence": confidence,
        "anchorId": "tilt_center",
        "loopClosureErrorMm": 0.0,
        "calibrationStatus": "tilt_calibrated" if calibration_done else "calibrating",
        "signalStatus": "ok" if data_age < 0.25 else "stale",

        # Дополнительно оставляем исходные углы, вдруг понадобятся для отладки.
        "roll": latest_values["roll"],
        "pitch": latest_values["pitch"],
        "yaw": latest_values["yaw"],
        "accX": latest_values["accX"],
        "accY": latest_values["accY"],
        "accZ": latest_values["accZ"],
        "gyroX": latest_values["gyroX"],
        "gyroY": latest_values["gyroY"],
        "gyroZ": latest_values["gyroZ"],
        "sampleSeq": _sample_seq,
    }


def notification_handler(sender, data):
    global has_any_data, packet_count, _last_raw_print, _ble_packets_this_sec, _last_ble_packet_time, _sample_seq

    now = time.time()
    if RAW_DEBUG and now - _last_raw_print >= 1.0 / RAW_DEBUG_LIMIT_PER_SEC:
        _last_raw_print = now
        print("RAW:", data.hex(" "))

    if parse_sensor_data(bytes(data)):
        _sample_seq = (_sample_seq + 1) % 1000000000
        has_any_data = True
        packet_count += 1
        _ble_packets_this_sec += 1
        _last_ble_packet_time = now


def udp_sender_loop():
    global _last_send_print, _last_stats_print, _ble_packets_this_sec, _udp_packets_this_sec

    interval = 1.0 / UDP_SEND_HZ
    while not stop_event.is_set():
        if has_any_data:
            if SEND_LEGACY_CSV:
                legacy_msg = make_legacy_udp_message()
                legacy_sock.sendto(legacy_msg.encode("utf-8"), (LEGACY_UDP_IP, LEGACY_UDP_PORT))

            if SEND_BODYRAY_JSON:
                packet = make_bodyray_udp_packet()
                bodyray_msg = json.dumps(packet, ensure_ascii=False, separators=(",", ":"))
                bodyray_sock.sendto(bodyray_msg.encode("utf-8"), (BODYRAY_UDP_IP, BODYRAY_UDP_PORT))

            _udp_packets_this_sec += 1

            now = time.time()
            if now - _last_send_print >= 1.0 / PRINT_SEND_HZ:
                _last_send_print = now
                if SEND_LEGACY_CSV:
                    print("SEND LEGACY:", make_legacy_udp_message())
                if SEND_BODYRAY_JSON:
                    print(
                        "SEND BODYRAY:",
                        f"x={packet['correctedWorldX']:.3f}",
                        f"y={packet['correctedWorldY']:.3f}",
                        f"roll={latest_values['roll']:.2f}",
                        f"pitch={latest_values['pitch']:.2f}",
                        f"cal={packet['calibrationStatus']}",
                    )

        now = time.time()
        if now - _last_stats_print >= STATS_PRINT_INTERVAL_SEC:
            _last_stats_print = now
            ble_hz = _ble_packets_this_sec / STATS_PRINT_INTERVAL_SEC
            udp_hz = _udp_packets_this_sec / STATS_PRINT_INTERVAL_SEC
            _ble_packets_this_sec = 0
            _udp_packets_this_sec = 0
            data_age = now - _last_ble_packet_time if _last_ble_packet_time > 0 else -1.0
            print(f"HZ: BLE={ble_hz:.0f}/s, UDP={udp_hz:.0f}/s, data_age={data_age:.3f}s")

        time.sleep(interval)


def command_loop():
    print("COMMANDS: c = recalibrate center, q = quit")
    while not stop_event.is_set():
        try:
            cmd = input().strip().lower()
        except EOFError:
            return

        if cmd == "c":
            calibrate_now()
        elif cmd == "q":
            stop_event.set()
            return


async def find_witmotion_device():
    global ble_status

    ble_status = "Сканирую BLE-устройства..."
    print(ble_status)
    devices = await BleakScanner.discover(timeout=8.0)

    if not devices:
        raise RuntimeError("BLE-устройства не найдены.")

    print("\nНайденные BLE-устройства:")
    candidates = []

    for index, device in enumerate(devices, start=1):
        name = device.name or "Unknown"
        print(f"{index}. {name} [{device.address}]")
        if DEVICE_NAME_HINT.upper() in name.upper():
            candidates.append(device)

    if not candidates:
        raise RuntimeError(f"Не найден датчик по имени '{DEVICE_NAME_HINT}'")

    if len(candidates) == 1:
        device = candidates[0]
        print(f"\nАвтоматически выбран: {device.name} [{device.address}]")
        return device

    print("\nНайдено несколько похожих устройств:")
    for index, device in enumerate(candidates, start=1):
        print(f"{index}. {device.name} [{device.address}]")

    while True:
        choice = input("Выбери номер устройства: ").strip()
        if choice.isdigit():
            selected_index = int(choice)
            if 1 <= selected_index <= len(candidates):
                return candidates[selected_index - 1]
        print("Нужно ввести номер из списка.")


async def ble_loop():
    global ble_status

    device = await find_witmotion_device()
    ble_status = f"Подключаюсь к {device.name or device.address}..."
    print(ble_status)

    async with BleakClient(device) as client:
        ble_status = "Подключено. Жду данные..."
        print("Подключено! Запускаю уведомления...")

        await client.start_notify(DATA_CHAR_UUID, notification_handler)

        try:
            while not stop_event.is_set():
                await asyncio.sleep(0.05)
        finally:
            ble_status = "Отключаюсь от датчика..."
            print(ble_status)

            try:
                await client.stop_notify(DATA_CHAR_UUID)
            except Exception as e:
                print("stop_notify error:", e)

            try:
                await asyncio.sleep(0.3)
            except Exception:
                pass


def ble_thread_main():
    global ble_error

    if sys.platform == "win32":
        try:
            from bleak.backends.winrt.util import uninitialize_sta
            uninitialize_sta()
        except ImportError:
            pass

    try:
        asyncio.run(ble_loop())
    except Exception as exc:
        ble_error = str(exc)
        print("Ошибка BLE:", ble_error)
        stop_event.set()


def main():
    sender_thread = threading.Thread(target=udp_sender_loop, daemon=True)
    sender_thread.start()

    cmd_thread = threading.Thread(target=command_loop, daemon=True)
    cmd_thread.start()

    ble_thread = threading.Thread(target=ble_thread_main)
    ble_thread.start()

    try:
        while not stop_event.is_set():
            time.sleep(0.5)
    except KeyboardInterrupt:
        print("\nОстановка...")
        stop_event.set()
    finally:
        ble_thread.join(timeout=3)
        legacy_sock.close()
        bodyray_sock.close()
        if ble_error:
            print("Ошибка подключения:", ble_error)


if __name__ == "__main__":
    main()
