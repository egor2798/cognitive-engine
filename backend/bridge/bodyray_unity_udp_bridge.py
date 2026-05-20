#!/usr/bin/env python3
"""
BodyRay -> Unity UDP bridge.

Today-use modes:
1) Simulator mode:
   python bodyray_unity_udp_bridge.py --mode simulator --tracking-mode pair

2) STDIN mode for real WITMOTION SDK integration:
   Your SDK process writes JSON lines like:
   {"WT901_01":{"AccX":0,"AccY":0,"AccZ":1,"AsX":0,"AsY":0,"AsZ":0,"Q0":1,"Q1":0,"Q2":0,"Q3":0}}

   Run:
   python bodyray_unity_udp_bridge.py --mode stdin --config examples/exercise_pair_chest_forearm.json

This bridge sends Unity-friendly UDP packets to 127.0.0.1:50555.
"""

from __future__ import annotations

import argparse
import json
import math
import socket
import sys
import time
from pathlib import Path
from typing import Dict, Any, Optional


def try_import_bodyray():
    try:
        from body_pointer_math_v2 import SensorFrame, load_config_from_dict, BodyRayPointerRuntime
        from witmotion_integration_adapter_v2 import WitMotionFrameAdapter
        return SensorFrame, load_config_from_dict, BodyRayPointerRuntime, WitMotionFrameAdapter
    except Exception as exc:
        print("[WARN] BodyRay modules are not available:", exc)
        return None, None, None, None


def load_json(path: str) -> Dict[str, Any]:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def send_packet(sock: socket.socket, host: str, port: int, packet: Dict[str, Any]) -> None:
    payload = json.dumps(packet, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    sock.sendto(payload, (host, port))


def make_packet(
    *,
    tracking_mode: str,
    body_point_id: int,
    body_point_zone_id: str,
    tracker_id: str,
    cursor_id: str,
    raw_world_x: float,
    raw_world_y: float,
    corrected_world_x: Optional[float] = None,
    corrected_world_y: Optional[float] = None,
    world_to_mm: float = 125.0,
    correction_mm: float = 0.0,
    confidence: float = 1.0,
    anchor_id: str = "",
    loop_closure_error_mm: float = 0.0,
    calibration_status: str = "not_applied",
    signal_status: str = "ok",
    x_px: float = 0.0,
    y_px: float = 0.0,
    x_cm: float = 0.0,
    y_cm: float = 0.0,
) -> Dict[str, Any]:
    if corrected_world_x is None:
        corrected_world_x = raw_world_x
    if corrected_world_y is None:
        corrected_world_y = raw_world_y

    return {
        "type": "body_ray_pointer_v2",
        "timeSec": time.monotonic(),
        "trackingMode": tracking_mode,
        "bodyPointId": body_point_id,
        "bodyPointZoneId": body_point_zone_id,
        "trackerId": tracker_id,
        "cursorId": cursor_id,

        "rawWorldX": raw_world_x,
        "rawWorldY": raw_world_y,
        "rawXMm": raw_world_x * world_to_mm,
        "rawYMm": raw_world_y * world_to_mm,

        "correctedWorldX": corrected_world_x,
        "correctedWorldY": corrected_world_y,
        "correctedXMm": corrected_world_x * world_to_mm,
        "correctedYMm": corrected_world_y * world_to_mm,

        "correctionMm": correction_mm,
        "confidence": confidence,
        "anchorId": anchor_id,
        "loopClosureErrorMm": loop_closure_error_mm,
        "calibrationStatus": calibration_status,
        "signalStatus": signal_status,

        "xPx": x_px,
        "yPx": y_px,
        "xCm": x_cm,
        "yCm": y_cm,
    }


def unity_world_from_bodyray_cm(x_cm: float, y_cm: float, screen_width_cm: float, screen_height_cm: float, world_to_mm: float) -> tuple[float, float]:
    # BodyRay screen coordinates are usually from screen origin.
    # Unity workspace currently uses centered world coordinates.
    x_mm = (x_cm - screen_width_cm * 0.5) * 10.0
    y_mm = (y_cm - screen_height_cm * 0.5) * 10.0
    return x_mm / world_to_mm, y_mm / world_to_mm


def run_simulator(args) -> None:
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"[BodyRay UDP] simulator -> {args.host}:{args.port}")
    print("[BodyRay UDP] Press Ctrl+C to stop.")

    t0 = time.monotonic()
    while True:
        t = time.monotonic() - t0
        x = args.sim_radius_x * math.cos(t * args.sim_speed)
        y = args.sim_radius_y * math.sin(t * args.sim_speed)

        packet = make_packet(
            tracking_mode=args.tracking_mode,
            body_point_id=args.body_point_id,
            body_point_zone_id=args.body_point_zone_id,
            tracker_id=args.tracker_id,
            cursor_id=args.cursor_id,
            raw_world_x=x,
            raw_world_y=y,
            world_to_mm=args.world_to_mm,
            confidence=1.0,
            calibration_status="simulator",
        )

        send_packet(sock, args.host, args.port, packet)
        time.sleep(1.0 / args.hz)


def run_stdin(args) -> None:
    SensorFrame, load_config_from_dict, BodyRayPointerRuntime, WitMotionFrameAdapter = try_import_bodyray()

    if SensorFrame is None:
        raise RuntimeError("BodyRay modules not found. Put this script in the same folder as body_pointer_math_v2.py and witmotion_integration_adapter_v2.py")

    config_data = load_json(args.config)
    config = load_config_from_dict(config_data)
    runtime = BodyRayPointerRuntime(config)
    adapter = WitMotionFrameAdapter()

    # Initialize from configured initial positions with neutral frames.
    start_frames = {}
    for point in config.active_points:
        start_frames[point.sensor_id] = SensorFrame.from_wit_order(
            point.sensor_id,
            time.monotonic(),
            [0, 0, 1],
            [0, 0, 0],
            [1, 0, 0, 0],
        )

    runtime.initialize_from_start_pose(start_frames, config.initial_positions_m)

    screen_width_cm = float(config.screen.width_m) * 100.0
    screen_height_cm = float(config.screen.height_m) * 100.0

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"[BodyRay UDP] stdin WITMOTION frames -> {args.host}:{args.port}")
    print("[BodyRay UDP] Waiting for JSON lines on stdin...")

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            sdk_by_sensor = json.loads(line)
            frames = {}

            for sensor_id, values in sdk_by_sensor.items():
                frames[sensor_id] = adapter.from_sdk_values(sensor_id, values)

            cursor = runtime.update(frames)
            raw_world_x, raw_world_y = unity_world_from_bodyray_cm(
                cursor.x_cm,
                cursor.y_cm,
                screen_width_cm,
                screen_height_cm,
                args.world_to_mm,
            )

            packet = make_packet(
                tracking_mode=args.tracking_mode,
                body_point_id=args.body_point_id,
                body_point_zone_id=args.body_point_zone_id,
                tracker_id=args.tracker_id,
                cursor_id=args.cursor_id,
                raw_world_x=raw_world_x,
                raw_world_y=raw_world_y,
                world_to_mm=args.world_to_mm,
                confidence=float(cursor.confidence),
                calibration_status="bodyray_runtime",
                signal_status="ok",
                x_px=float(cursor.x_px),
                y_px=float(cursor.y_px),
                x_cm=float(cursor.x_cm),
                y_cm=float(cursor.y_cm),
            )

            send_packet(sock, args.host, args.port, packet)

        except Exception as exc:
            print("[WARN] bad frame:", exc, file=sys.stderr)


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=["simulator", "stdin"], default="simulator")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=50555)
    parser.add_argument("--hz", type=float, default=60.0)

    parser.add_argument("--config", default="examples/exercise_pair_chest_forearm.json")
    parser.add_argument("--tracking-mode", default="pair", choices=["single", "pair", "triad"])

    parser.add_argument("--body-point-id", type=int, default=5)
    parser.add_argument("--body-point-zone-id", default="zone_right_forearm")
    parser.add_argument("--tracker-id", default="WT901_01")
    parser.add_argument("--cursor-id", default="cursor_1")

    parser.add_argument("--world-to-mm", type=float, default=125.0)
    parser.add_argument("--sim-radius-x", type=float, default=2.5)
    parser.add_argument("--sim-radius-y", type=float, default=2.0)
    parser.add_argument("--sim-speed", type=float, default=1.0)

    return parser.parse_args()


if __name__ == "__main__":
    args = parse_args()

    if args.mode == "simulator":
        run_simulator(args)
    elif args.mode == "stdin":
        run_stdin(args)
