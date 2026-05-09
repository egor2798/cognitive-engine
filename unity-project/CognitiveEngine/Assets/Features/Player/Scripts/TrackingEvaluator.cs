using System.Collections.Generic;
using UnityEngine;

public class TrackingEvaluator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointerTracker pointer;
    [SerializeField] private PacemakerManager manager;
    [SerializeField] private PlayerButtons playerButtons;

    [Header("World To Millimeters Scale")]
    [Tooltip("Direct conversion from Unity world units to millimeters. For X -4..4 = 1000 mm use 125.")]
    [SerializeField] private bool useManualWorldToMmScale = true;
    [SerializeField] private float manualWorldToMmScale = 125f;

    [Tooltip("Use fixed workspace size instead of camera width. This is more stable for constructor/player coordinates.")]
    [SerializeField] private bool useFixedWorkspaceScale = true;
    [Tooltip("How many Unity world units correspond to full workspace width. For PointerTracker world X -4..4 use 8.")]
    [SerializeField] private float workspaceWorldWidthUnits = 8f;
    [Tooltip("How many Unity world units correspond to full workspace height. Reserved for future vertical scaling checks.")]
    [SerializeField] private float workspaceWorldHeightUnits = 6f;

    [Header("Movement Metrics Filtering")]
    [SerializeField] private float speedWarmupSec = 0.25f;
    [SerializeField] private float minDeltaTimeForSpeedSec = 0.003f;
    [SerializeField] private float maxReasonablePointerSpeedMmS = 2000f;
    [SerializeField] private float maxReasonablePacemakerSpeedMmS = 5000f;
    [SerializeField] private float maxReasonablePointerStepMm = 250f;
    [SerializeField] private float maxReasonablePacemakerStepMm = 500f;

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

    private float pointerMovementTimeSec;
    private float pacemakerMovementTimeSec;
    private int skippedMovementSamples;
    private float cachedWorldToMmScale;

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
        previousPointerSpeedMmS = 0f;
        previousPacemakerSpeedMmS = 0f;
        pointerMovementTimeSec = 0f;
        pacemakerMovementTimeSec = 0f;
        skippedMovementSamples = 0;
        cachedWorldToMmScale = GetWorldToMmScale();
        Debug.Log("TrackingEvaluator: worldToMmScale = " + cachedWorldToMmScale);

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

        float worldToMm = cachedWorldToMmScale > 0f ? cachedWorldToMmScale : GetWorldToMmScale();
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
        Debug.Log("Auto pointer path mm: " + finalResult.pointerPathLengthMm);
        Debug.Log("Auto pacemaker path mm: " + finalResult.pacemakerPathLengthMm);

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

        previousPointerPos = pointerPos;
        previousPacemakerPos = pacemakerPos;

        if (result.totalTimeSec < speedWarmupSec || dt < minDeltaTimeForSpeedSec)
        {
            skippedMovementSamples++;
            return;
        }

        float pointerSpeedMmS = pointerStepMm / dt;
        float pacemakerSpeedMmS = pacemakerStepMm / dt;

        bool pointerSampleOk = pointerStepMm <= maxReasonablePointerStepMm && pointerSpeedMmS <= maxReasonablePointerSpeedMmS;
        bool pacemakerSampleOk = pacemakerStepMm <= maxReasonablePacemakerStepMm && pacemakerSpeedMmS <= maxReasonablePacemakerSpeedMmS;

        // Path length should not collapse to zero just because one frame contains a technical spike.
        // For the integral path metric we use a capped step: normal samples are counted as-is,
        // and rare spikes are clipped to a reasonable physical maximum instead of being discarded.
        float pointerStepForPathMm = pointerSampleOk ? pointerStepMm : Mathf.Min(pointerStepMm, maxReasonablePointerStepMm);
        float pacemakerStepForPathMm = pacemakerSampleOk ? pacemakerStepMm : Mathf.Min(pacemakerStepMm, maxReasonablePacemakerStepMm);

        result.pointerPathLengthMm += pointerStepForPathMm;
        result.pacemakerPathLengthMm += pacemakerStepForPathMm;
        pointerMovementTimeSec += dt;
        pacemakerMovementTimeSec += dt;

        float pointerSpeedForStopMmS = pointerStepForPathMm / dt;
        previousPointerSpeedMmS = pointerSpeedForStopMmS;

        if (pointerSampleOk)
        {
            if (pointerSpeedMmS > result.maxPointerSpeedMmS)
                result.maxPointerSpeedMmS = pointerSpeedMmS;
        }
        else
        {
            skippedMovementSamples++;
        }

        if (pacemakerSampleOk)
        {
            if (pacemakerSpeedMmS > result.maxPacemakerSpeedMmS)
                result.maxPacemakerSpeedMmS = pacemakerSpeedMmS;

            previousPacemakerSpeedMmS = pacemakerSpeedMmS;
        }
        else
        {
            skippedMovementSamples++;
        }
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

        float pointerTime = Mathf.Max(0.0001f, pointerMovementTimeSec);
        float pacemakerTime = Mathf.Max(0.0001f, pacemakerMovementTimeSec);

        // Mean speed is derived from the already converted path length in millimeters.
        // This keeps path length and speed in the same units and avoids unit mismatch.
        result.meanPointerSpeedMmS = result.pointerPathLengthMm / pointerTime;
        result.meanPacemakerSpeedMmS = result.pacemakerPathLengthMm / pacemakerTime;

        if (skippedMovementSamples > 0)
            Debug.Log("TrackingEvaluator: skipped movement samples = " + skippedMovementSamples);
    }

    private float GetWorldToMmScale()
    {
        // Most stable mode for this project: explicit conversion from Unity units to millimeters.
        // PointerTracker currently uses approximately X -4..4 for 1000 mm workspace width,
        // therefore 1 Unity unit = 125 mm.
        if (useManualWorldToMmScale)
            return Mathf.Max(0.0001f, manualWorldToMmScale);

        if (currentExercise == null || currentExercise.settings == null)
            return 1f;

        float widthMm = Mathf.Max(1f, currentExercise.settings.workspaceWidthMm);

        if (useFixedWorkspaceScale)
        {
            float widthWorld = Mathf.Max(0.0001f, workspaceWorldWidthUnits);
            return widthMm / widthWorld;
        }

        float cameraWidthWorld = 10f;

        if (Camera.main != null && Camera.main.orthographic)
            cameraWidthWorld = Camera.main.orthographicSize * 2f * Camera.main.aspect;

        if (cameraWidthWorld <= 0.0001f)
            return 1f;

        return widthMm / cameraWidthWorld;
    }

    public SessionResult GetCurrentResult()
    {
        return result;
    }
}
