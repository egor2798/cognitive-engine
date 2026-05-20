using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public class SessionSampleData
{
    public string sampleType; // cursor / pacer
    public int sampleIndex;
    public float timeSec;

    public string trackingMode; // single / pair / triad
    public string bodyPointZoneId;
    public BodyPointId bodyPointId;
    public string trackerId;
    public string cursorId;

    // Backward-compatible coordinates used by the current Unity evaluator.
    // For now these equal correctedWorldX/Y.
    public float worldX;
    public float worldY;
    public float xMm;
    public float yMm;

    // Methodical diagnostic fields requested for IMU/BodyRay integration.
    public float rawWorldX;
    public float rawWorldY;
    public float rawXMm;
    public float rawYMm;

    public float correctedWorldX;
    public float correctedWorldY;
    public float correctedXMm;
    public float correctedYMm;

    public float correctionMm;
    public float confidence;
    public string anchorId;
    public float loopClosureErrorMm;
    public string calibrationStatus;

    public float deviationMm;
    public bool outsideCorridor;
    public string signalStatus;
}

[Serializable]
public class SessionEventData
{
    public string eventType;
    public float timeSec;
    public string bodyPointZoneId;
    public BodyPointId bodyPointId;
    public string trackerId;
    public string cursorId;
    public string details;
}

[Serializable]
public class SessionResult
{
    public string exerciseId;
    public string exerciseName;

    public bool usedPacemaker;
    public PointerInputSource inputSource;

    public BodyPointId activeBodyPointId = BodyPointId.RightForearm;
    public string activeBodyPointZoneId = "zone_right_forearm";
    public string activeTrackerId = "WT901_01";
    public string activeCursorId = "cursor_1";
    public List<BodyPointBindingData> bodyPointBindings = new();
    public List<SessionEventData> events = new();
    public List<SessionSampleData> cursorSamples = new();
    public List<SessionSampleData> pacerSamples = new();

    public float totalTimeSec;
    public float activeTimeSec;

    public float timeInsideSec;
    public float timeOutsideSec;
    public float timeInsidePct;
    public float timeOutsidePct;

    public float meanDeviationMm;
    public float maxDeviationMm;
    public float rmseDeviationMm;

    public int outsideEpisodesCount;
    public float longestOutsideEpisodeSec;

    public float lagTimeSec;
    public float lagTimePct;
    public float leadTimeSec;
    public float leadTimePct;

    public float pointerPathLengthMm;
    public float pacemakerPathLengthMm;

    public float meanPointerSpeedMmS;
    public float maxPointerSpeedMmS;

    public float meanPacemakerSpeedMmS;
    public float maxPacemakerSpeedMmS;

    public int stopCount;
    public float longestStopSec;

    public List<TrajectorySegmentInfo> trajectorySegments = new();
}