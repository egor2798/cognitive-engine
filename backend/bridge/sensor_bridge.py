import asyncio
import socket
import sys
import threading
import time

if sys.platform == "win32":
    sys.coinit_flags = 0

from bleak import BleakClient, BleakScanner

DEVICE_NAME_HINT = "WT901BLE"
DATA_CHAR_UUID = "0000ffe4-0000-1000-8000-00805f9a34fb"

UDP_IP = "127.0.0.1"
UDP_PORT = 5005

# Включи True, если нужно посмотреть сырые BLE-пакеты.
RAW_DEBUG = False
RAW_DEBUG_LIMIT_PER_SEC = 5

# Частота UDP-отправки в Unity. Даже если BLE приходит рывками,
# Unity будет получать последнее актуальное состояние стабильно.
UDP_SEND_HZ = 100
PRINT_SEND_HZ = 5
STATS_PRINT_INTERVAL_SEC = 1.0

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
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

_stream_buffer = bytearray()
_last_raw_print = 0.0
_last_send_print = 0.0
_last_stats_print = 0.0
_ble_packets_this_sec = 0
_udp_packets_this_sec = 0
_last_ble_packet_time = 0.0
_sample_seq = 0


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
        # acceleration, единицы g, диапазон ±16g
        latest_values["accX"] = values[0] / 32768.0 * 16.0
        latest_values["accY"] = values[1] / 32768.0 * 16.0
        latest_values["accZ"] = values[2] / 32768.0 * 16.0
        return True

    if frame_type == 0x52:
        # angular velocity, deg/s, диапазон ±2000 deg/s
        latest_values["gyroX"] = values[0] / 32768.0 * 2000.0
        latest_values["gyroY"] = values[1] / 32768.0 * 2000.0
        latest_values["gyroZ"] = values[2] / 32768.0 * 2000.0
        return True

    if frame_type == 0x53:
        # angles, degrees, диапазон ±180°
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

    Старый bridge брал только decoded[6..8]. Теперь берём все 9 значений.
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
            # Это может быть не 11-байтный кадр, а combined-пакет.
            # Не съедаем весь буфер, просто сдвигаемся на 1 байт.
            del _stream_buffer[0]
            continue

        frame = bytes(_stream_buffer[:11])
        del _stream_buffer[:11]

        if parse_wit_11_frame(frame):
            parsed_any = True

    return parsed_any


def parse_sensor_data(data):
    """
    Важно: сначала пробуем combined 20-byte формат, потому что именно он
    у тебя раньше давал стабильные roll/pitch. Но теперь из него также
    достаём acceleration/gyro.

    Если пакет не combined, тогда парсим поток 11-byte кадров 0x51/0x52/0x53.
    """
    if parse_combined_20_packet(data):
        return True

    return parse_stream_frames(data)


def make_udp_message():
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
            msg = make_udp_message()
            sock.sendto(msg.encode("utf-8"), (UDP_IP, UDP_PORT))
            _udp_packets_this_sec += 1

            now = time.time()
            if now - _last_send_print >= 1.0 / PRINT_SEND_HZ:
                _last_send_print = now
                print("SEND:", msg)

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
        sock.close()
        if ble_error:
            print("Ошибка подключения:", ble_error)


if __name__ == "__main__":
    main()
