using System;
using System.Collections.Generic;

[Serializable]
public class ExerciseSettingsData
{
    public bool usePacemaker = true;
    public bool allowWithoutPacemaker = true;

    public float defaultSpeed = 0.003f;
    public int durationMs = 60000;
    public float lineThickness = 6f;

    public bool showTrack = true;

    public bool useSensor = false;
    public float sensorSensitivity = 1f;

    public float corridorWidthCm = 4.0f;
    public float hitRadiusCm = 2.0f;

    public float workspaceWidthMm = 1000f;
    public float workspaceHeightMm = 600f;

    public float lagThresholdCm = 2.0f;
    public int exitDebounceMs = 100;

    public float stopSpeedThresholdMmS = 10f;
    public int stopConfirmMs = 150;

    public bool trackDeviation = true;
    public bool saveSessionResult = true;

    public bool metronomeEnabled = false;
    public float metronomeBpm = 60f;
    public float metronomeVolume01 = 1f;

    public PointerInputSource inputSource = PointerInputSource.Mouse;

    public List<PacemakerSettingsData> pacemakers = new();
}