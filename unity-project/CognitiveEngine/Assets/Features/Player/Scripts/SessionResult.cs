using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SessionResult
{
    public string exerciseId;
    public string exerciseName;

    public bool usedPacemaker;
    public PointerInputSource inputSource;

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