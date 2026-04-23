using System;
using System.Collections.Generic;
using UnityEngine;

public class TrackingEvaluator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointerTracker pointer;
    [SerializeField] private PacemakerManager manager;
    [SerializeField] private PlayerButtons playerButtons;

    private SessionResult result;
    private ExerciseData currentExercise;

    private bool isRunning;
    private bool isFinished;

    private float sumDeviationMm;
    private float sumDeviationSqMm;

    private Vector2 previousPointerPos;
    private Vector2 previousPacemakerPos;
    private bool hasPreviousPositions;

    private float previousPointerSpeedMmS;
    private float previousPacemakerSpeedMmS;

    private bool pendingOutside;
    private float pendingOutsideStartSec;
    private bool confirmedOutside;
    private float confirmedOutsideStartSec;

    private bool inStop;
    private float pendingStopStartSec;
    private float confirmedStopStartSec;

    private int trajectorySegmentCounter;
    private float currentSegmentStartSec;

    public void StartEvaluation(ExerciseData exercise)
    {
        currentExercise = exercise;
        result = new SessionResult();

        result.exerciseId = exercise != null ? exercise.id : "";
        result.exerciseName = exercise != null ? exercise.name : "";
        result.usedPacemaker = exercise != null && exercise.settings != null && exercise.settings.usePacemaker;
        result.inputSource = exercise != null && exercise.settings != null
            ? exercise.settings.inputSource
            : PointerInputSource.Mouse;

        isRunning = true;
        isFinished = false;

        sumDeviationMm = 0f;
        sumDeviationSqMm = 0f;

        hasPreviousPositions = false;

        pendingOutside = false;
        confirmedOutside = false;

        inStop = false;
        pendingStopStartSec = 0f;
        confirmedStopStartSec = 0f;

        trajectorySegmentCounter = 0;
        currentSegmentStartSec = 0f;

        result.trajectorySegments.Clear();
    }

    public SessionResult StopEvaluation()
    {
        if (isRunning)
        {
            FinalizeOutsideEpisode();
            FinalizeStopSegment();
            FinalizeMetrics();
        }

        isRunning = false;
        return result;
    }

    private void Update()
    {
        if (!isRunning || currentExercise == null || currentExercise.settings == null)
            return;

        var runners = manager != null ? manager.GetRunners() : null;
        if (pointer == null || runners == null || runners.Count == 0)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector2 pointerPos = pointer.GetPointerPosition();

        PacemakerRunner nearestRunner = null;
        float nearestDistanceWorld = float.MaxValue;
        Vector2 nearestPacemakerPos = Vector2.zero;

        for (int i = 0; i < runners.Count; i++)
        {
            PacemakerRunner runner = runners[i];
            if (runner == null)
                continue;

            Vector2 pacemakerPos = runner.GetCurrentPosition();
            float distance = Vector2.Distance(pointerPos, pacemakerPos);

            if (distance < nearestDistanceWorld)
            {
                nearestDistanceWorld = distance;
                nearestRunner = runner;
                nearestPacemakerPos = pacemakerPos;
            }
        }

        if (nearestRunner == null)
            return;

        float worldToMm = GetWorldToMmScale();
        float deviationMm = nearestDistanceWorld * worldToMm;

        float allowedDeviationMm = currentExercise.settings.hitRadiusCm * 10f;
        bool isOutside = deviationMm > allowedDeviationMm;

        result.totalTimeSec += dt;
        result.activeTimeSec += dt;

        float durationSec = currentExercise.settings.durationMs / 1000f;
        if (result.totalTimeSec >= durationSec)
        {
            FinishSession();
            return;
        }

        if (!isOutside)
        {
            result.timeInsideSec += dt;
            nearestRunner.SetColor(Color.green);
        }
        else
        {
            result.timeOutsideSec += dt;
            nearestRunner.SetColor(Color.red);
        }

        sumDeviationMm += deviationMm * dt;
        sumDeviationSqMm += deviationMm * deviationMm * dt;

        if (deviationMm > result.maxDeviationMm)
            result.maxDeviationMm = deviationMm;

        UpdateOutsideEpisodes(isOutside);
        UpdateLagLead(pointerPos, nearestPacemakerPos, dt, worldToMm);
        UpdateMovementMetrics(pointerPos, nearestPacemakerPos, dt, worldToMm);
        UpdateStops();
    }

    private void FinishSession()
    {
        if (isFinished)
            return;

        isFinished = true;

        Debug.Log("=== AUTO FINISH ===");

        SessionResult finalResult = StopEvaluation();

        Debug.Log("Auto total time: " + finalResult.totalTimeSec);
        Debug.Log("Auto mean deviation: " + finalResult.meanDeviationMm);
        Debug.Log("Auto max deviation: " + finalResult.maxDeviationMm);
        Debug.Log("Auto time outside pct: " + finalResult.timeOutsidePct);

        if (playerButtons != null)
            playerButtons.StopAll();
        else
            Debug.LogWarning("TrackingEvaluator: playerButtons == null");
    }

    private void UpdateOutsideEpisodes(bool isOutside)
    {
        float now = result.totalTimeSec;
        float debounceSec = Mathf.Max(0f, currentExercise.settings.exitDebounceMs / 1000f);

        if (isOutside)
        {
            if (!pendingOutside)
            {
                pendingOutside = true;
                pendingOutsideStartSec = now;
            }

            if (!confirmedOutside && now - pendingOutsideStartSec >= debounceSec)
            {
                confirmedOutside = true;
                confirmedOutsideStartSec = pendingOutsideStartSec;
                result.outsideEpisodesCount++;
            }
        }
        else
        {
            if (confirmedOutside)
            {
                float episodeDuration = now - confirmedOutsideStartSec;
                if (episodeDuration > result.longestOutsideEpisodeSec)
                    result.longestOutsideEpisodeSec = episodeDuration;
            }

            pendingOutside = false;
            confirmedOutside = false;
        }
    }

    private void FinalizeOutsideEpisode()
    {
        if (confirmedOutside)
        {
            float now = result.totalTimeSec;
            float episodeDuration = now - confirmedOutsideStartSec;
            if (episodeDuration > result.longestOutsideEpisodeSec)
                result.longestOutsideEpisodeSec = episodeDuration;
        }
    }

    private void UpdateLagLead(Vector2 pointerPos, Vector2 pacemakerPos, float dt, float worldToMm)
    {
        float deltaMm = (pacemakerPos.x - pointerPos.x) * worldToMm;
        float thresholdMm = currentExercise.settings.lagThresholdCm * 10f;

        if (deltaMm > thresholdMm)
            result.lagTimeSec += dt;
        else if (deltaMm < -thresholdMm)
            result.leadTimeSec += dt;
    }

    private void UpdateMovementMetrics(Vector2 pointerPos, Vector2 pacemakerPos, float dt, float worldToMm)
    {
        if (!hasPreviousPositions)
        {
            previousPointerPos = pointerPos;
            previousPacemakerPos = pacemakerPos;
            hasPreviousPositions = true;
            return;
        }

        float pointerStepMm = Vector2.Distance(pointerPos, previousPointerPos) * worldToMm;
        float pacemakerStepMm = Vector2.Distance(pacemakerPos, previousPacemakerPos) * worldToMm;

        result.pointerPathLengthMm += pointerStepMm;
        result.pacemakerPathLengthMm += pacemakerStepMm;

        float pointerSpeedMmS = pointerStepMm / dt;
        float pacemakerSpeedMmS = pacemakerStepMm / dt;

        result.meanPointerSpeedMmS += pointerSpeedMmS * dt;
        result.meanPacemakerSpeedMmS += pacemakerSpeedMmS * dt;

        if (pointerSpeedMmS > result.maxPointerSpeedMmS)
            result.maxPointerSpeedMmS = pointerSpeedMmS;

        if (pacemakerSpeedMmS > result.maxPacemakerSpeedMmS)
            result.maxPacemakerSpeedMmS = pacemakerSpeedMmS;

        previousPointerSpeedMmS = pointerSpeedMmS;
        previousPacemakerSpeedMmS = pacemakerSpeedMmS;

        previousPointerPos = pointerPos;
        previousPacemakerPos = pacemakerPos;
    }

    private void UpdateStops()
    {
        float now = result.totalTimeSec;
        float threshold = currentExercise.settings.stopSpeedThresholdMmS;
        float confirmSec = Mathf.Max(0f, currentExercise.settings.stopConfirmMs / 1000f);

        bool belowThreshold = previousPointerSpeedMmS < threshold;

        if (belowThreshold)
        {
            if (!inStop && pendingStopStartSec <= 0f)
                pendingStopStartSec = now;

            if (!inStop && pendingStopStartSec > 0f && now - pendingStopStartSec >= confirmSec)
            {
                inStop = true;
                confirmedStopStartSec = pendingStopStartSec;
                result.stopCount++;

                StartNewSegment(now, "stop_detected");
            }
        }
        else
        {
            if (inStop)
            {
                float stopDuration = now - confirmedStopStartSec;
                if (stopDuration > result.longestStopSec)
                    result.longestStopSec = stopDuration;

                inStop = false;
                pendingStopStartSec = 0f;
                confirmedStopStartSec = 0f;

                currentSegmentStartSec = now;
            }
            else
            {
                pendingStopStartSec = 0f;
            }
        }
    }

    private void StartNewSegment(float now, string reason)
    {
        if (now > currentSegmentStartSec)
        {
            result.trajectorySegments.Add(new TrajectorySegmentInfo
            {
                segmentId = trajectorySegmentCounter++,
                startTimeSec = currentSegmentStartSec,
                endTimeSec = now,
                reason = reason
            });
        }
    }

    private void FinalizeStopSegment()
    {
        float now = result.totalTimeSec;

        if (now > currentSegmentStartSec)
        {
            result.trajectorySegments.Add(new TrajectorySegmentInfo
            {
                segmentId = trajectorySegmentCounter++,
                startTimeSec = currentSegmentStartSec,
                endTimeSec = now,
                reason = "session_end"
            });
        }
    }

    private void FinalizeMetrics()
    {
        float total = Mathf.Max(0.0001f, result.totalTimeSec);

        result.timeInsidePct = 100f * result.timeInsideSec / total;
        result.timeOutsidePct = 100f * result.timeOutsideSec / total;

        result.meanDeviationMm = sumDeviationMm / total;
        result.rmseDeviationMm = Mathf.Sqrt(sumDeviationSqMm / total);

        result.lagTimePct = 100f * result.lagTimeSec / total;
        result.leadTimePct = 100f * result.leadTimeSec / total;

        result.meanPointerSpeedMmS /= total;
        result.meanPacemakerSpeedMmS /= total;
    }

    private float GetWorldToMmScale()
    {
        float widthWorld = 10f;
        float widthMm = currentExercise.settings.workspaceWidthMm;

        if (Camera.main != null && Camera.main.orthographic)
            widthWorld = Camera.main.orthographicSize * 2f * Camera.main.aspect;

        if (widthWorld <= 0.0001f)
            return 1f;

        return widthMm / widthWorld;
    }

    public SessionResult GetCurrentResult()
    {
        return result;
    }
}