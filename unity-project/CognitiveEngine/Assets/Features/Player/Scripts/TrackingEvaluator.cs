using System.Collections.Generic;
using UnityEngine;

public class TrackingEvaluator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointerTracker pointer;
    [SerializeField] private PacemakerManager manager;
    [SerializeField] private PlayerButtons playerButtons;
    [SerializeField] private AudioFeedbackController audioFeedback;
    [SerializeField] private BodyRayUdpReceiver bodyRayReceiver;

    [Header("Evaluation Geometry")]
    [Tooltip("If enabled, deviation is measured to the track centerline, not to the pacemaker marker.")]
    [SerializeField] private bool evaluateAgainstTrackGeometry = true;

    [Tooltip("If enabled, allowed zone is half of corridorWidthCm. If disabled, hitRadiusCm is used as before.")]
    [SerializeField] private bool useCorridorWidthForAllowedDeviation = true;

    [Tooltip("If enabled, visual line thickness is included in allowed deviation. Example: lineThickness=100 counts as a wide physical track, not only as visual style.")]
    [SerializeField] private bool includeLineThicknessInAllowedDeviation = true;

    [Tooltip("Must match ShapeElement/SegmentElement ThicknessToWorldWidth = 0.01.")]
    [SerializeField] private float lineThicknessToWorldWidth = 0.01f;

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

    [Header("Session Samples")]
    [SerializeField] private bool recordSamples = true;
    [SerializeField] private float sampleIntervalSec = 0.05f;
    [SerializeField] private int maxSamplesPerStream = 5000;

    [Tooltip("Do not write repeated initial (0,0) cursor samples before real cursor movement/data appears.")]
    [SerializeField] private bool ignoreInitialZeroCursorSamples = true;
    [SerializeField] private float initialZeroCursorToleranceWorld = 0.0001f;

    [Tooltip("After the first non-zero cursor signal, skip a few unstable samples before writing cursorSamples.")]
    [SerializeField] private int skipCursorSamplesAfterSignalStart = 5;

    [Tooltip("Ignore cursor samples with unrealistic deviation. This removes first-frame pointer jumps.")]
    [SerializeField] private bool discardUnreasonableCursorSamples = true;
    [SerializeField] private float maxAcceptedCursorDeviationMm = 500f;

    private SessionResult result;
    private ExerciseData currentExercise;

    private readonly List<TrackGeometry> exerciseGeometries = new();

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

    private float sampleTimerSec;
    private int cursorSampleIndex;
    private int pacerSampleIndex;
    private bool cursorSignalStarted;
    private int cursorWarmupSamplesLeft;
    private bool cursorOutlierReported;
    private bool cursorSamplesStartedReported;

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

        ApplyPointerInputSourceFromExercise(result.inputSource);

        ApplyExerciseBodyPointInfoToResult(exercise);
        if (audioFeedback != null && exercise != null) audioFeedback.ApplySettings(exercise.settings);
        if (audioFeedback != null) audioFeedback.BeginSession();
        AddSessionEvent("session_started", "Сессия начата");
        if (audioFeedback != null) audioFeedback.PlaySessionStarted();
        if (result.usedPacemaker)
        {
            AddSessionEvent("pacer_started", "Пейсмейкер включён");
            if (audioFeedback != null) audioFeedback.PlayPacerStarted();
        }

        BuildExerciseGeometries();

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
        sampleTimerSec = 0f;
        cursorSampleIndex = 0;
        pacerSampleIndex = 0;
        cursorSignalStarted = false;
        cursorWarmupSamplesLeft = 0;
        cursorOutlierReported = false;
        cursorSamplesStartedReported = false;
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
            AddSessionEventOnce("session_completed", "Сессия завершена");
            if (audioFeedback != null) audioFeedback.PlaySessionCompleted();
            if (audioFeedback != null) audioFeedback.EndSession();

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

        if (pointer == null)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector2 pointerPos = pointer.GetPointerPosition();
        float worldToMm = cachedWorldToMmScale > 0f ? cachedWorldToMmScale : GetWorldToMmScale();

        result.totalTimeSec += dt;
        result.activeTimeSec += dt;

        float durationSec = currentExercise.settings.durationMs / 1000f;
        if (durationSec > 0f && result.totalTimeSec >= durationSec)
        {
            FinishSession();
            return;
        }

        if (!TryFindEvaluationReference(
                pointerPos,
                out PacemakerRunner nearestRunner,
                out TrackGeometry nearestGeometry,
                out ClosestPointInfo closestOnTrack,
                out Vector2 referencePacemakerPos,
                out float nearestDistanceWorld))
        {
            return;
        }

        float deviationMm = nearestDistanceWorld * worldToMm;
        float allowedDeviationMm = GetAllowedDeviationMm();
        bool isOutside = deviationMm > allowedDeviationMm;

        if (!isOutside)
        {
            result.timeInsideSec += dt;
            if (nearestRunner != null)
                nearestRunner.SetColor(Color.green);
        }
        else
        {
            result.timeOutsideSec += dt;
            if (nearestRunner != null)
                nearestRunner.SetColor(Color.red);
        }

        sumDeviationMm += deviationMm * dt;
        sumDeviationSqMm += deviationMm * deviationMm * dt;

        if (deviationMm > result.maxDeviationMm)
            result.maxDeviationMm = deviationMm;

        RecordSamples(pointerPos, referencePacemakerPos, deviationMm, isOutside, worldToMm, dt);

        UpdateOutsideEpisodes(isOutside);

        if (nearestRunner != null && nearestGeometry != null && nearestGeometry.IsValid())
            UpdateLagLeadByTrackProgress(nearestRunner, nearestGeometry, closestOnTrack, dt, worldToMm);

        UpdateMovementMetrics(pointerPos, referencePacemakerPos, dt, worldToMm);
        UpdateStops();
    }

    private void BuildExerciseGeometries()
    {
        exerciseGeometries.Clear();

        if (currentExercise == null)
            return;

        if (currentExercise.paths != null)
        {
            for (int i = 0; i < currentExercise.paths.Count; i++)
            {
                TrackGeometry geometry = TrackGeometryFactory.FromPath(currentExercise.paths[i]);
                if (geometry != null && geometry.IsValid())
                    exerciseGeometries.Add(geometry);
            }
        }

        if (currentExercise.shapes != null)
        {
            for (int i = 0; i < currentExercise.shapes.Count; i++)
            {
                TrackGeometry geometry = TrackGeometryFactory.FromShape(currentExercise.shapes[i]);
                if (geometry != null && geometry.IsValid())
                    exerciseGeometries.Add(geometry);
            }
        }

        Debug.Log("TrackingEvaluator: evaluation geometries = " + exerciseGeometries.Count);
    }

    private bool TryFindEvaluationReference(
        Vector2 pointerPos,
        out PacemakerRunner nearestRunner,
        out TrackGeometry nearestGeometry,
        out ClosestPointInfo closestOnTrack,
        out Vector2 referencePacemakerPos,
        out float nearestDistanceWorld)
    {
        nearestRunner = null;
        nearestGeometry = null;
        closestOnTrack = new ClosestPointInfo { distance = float.MaxValue, segmentIndex = -1 };
        referencePacemakerPos = Vector2.zero;
        nearestDistanceWorld = float.MaxValue;

        if (evaluateAgainstTrackGeometry)
        {
            List<PacemakerRunner> runners = manager != null ? manager.GetRunners() : null;

            if (runners != null)
            {
                for (int i = 0; i < runners.Count; i++)
                {
                    PacemakerRunner runner = runners[i];
                    if (runner == null)
                        continue;

                    TrackGeometry geometry = runner.GetCurrentGeometry();
                    if (geometry == null || !geometry.IsValid())
                        continue;

                    ClosestPointInfo closest = geometry.GetClosestPoint(pointerPos);

                    if (closest.distance < nearestDistanceWorld)
                    {
                        nearestDistanceWorld = closest.distance;
                        nearestRunner = runner;
                        nearestGeometry = geometry;
                        closestOnTrack = closest;
                        referencePacemakerPos = runner.GetCurrentPosition();
                    }
                }
            }

            // If there is no pacemaker, we still can evaluate the pointer against the exercise track itself.
            if (nearestRunner == null && exerciseGeometries.Count > 0)
            {
                for (int i = 0; i < exerciseGeometries.Count; i++)
                {
                    TrackGeometry geometry = exerciseGeometries[i];
                    if (geometry == null || !geometry.IsValid())
                        continue;

                    ClosestPointInfo closest = geometry.GetClosestPoint(pointerPos);

                    if (closest.distance < nearestDistanceWorld)
                    {
                        nearestDistanceWorld = closest.distance;
                        nearestGeometry = geometry;
                        closestOnTrack = closest;
                        referencePacemakerPos = closest.point;
                    }
                }
            }

            if (nearestGeometry != null && nearestDistanceWorld < float.MaxValue)
                return true;
        }

        // Fallback to previous behavior: evaluate against nearest pacemaker marker.
        List<PacemakerRunner> fallbackRunners = manager != null ? manager.GetRunners() : null;
        if (fallbackRunners == null || fallbackRunners.Count == 0)
            return false;

        for (int i = 0; i < fallbackRunners.Count; i++)
        {
            PacemakerRunner runner = fallbackRunners[i];
            if (runner == null)
                continue;

            Vector2 pacemakerPos = runner.GetCurrentPosition();
            float distance = Vector2.Distance(pointerPos, pacemakerPos);

            if (distance < nearestDistanceWorld)
            {
                nearestDistanceWorld = distance;
                nearestRunner = runner;
                nearestGeometry = runner.GetCurrentGeometry();
                referencePacemakerPos = pacemakerPos;
            }
        }

        return nearestRunner != null;
    }

    private float GetAllowedDeviationMm()
    {
        if (currentExercise == null || currentExercise.settings == null)
            return 1f;

        float baseAllowedMm;

        if (useCorridorWidthForAllowedDeviation)
        {
            // corridorWidthCm is the full corridor width. Distance from centerline is allowed only to half of it.
            float corridorWidthCm = Mathf.Max(0.01f, currentExercise.settings.corridorWidthCm);
            baseAllowedMm = corridorWidthCm * 10f * 0.5f;
        }
        else
        {
            baseAllowedMm = Mathf.Max(0.01f, currentExercise.settings.hitRadiusCm) * 10f;
        }

        if (!includeLineThicknessInAllowedDeviation)
            return baseAllowedMm;

        // Важно:
        // ShapeElement/SegmentElement рисуют LineRenderer шириной:
        // lineThickness * 0.01 world units.
        // Оценка отклонения считается до центральной линии трека.
        // Поэтому к допустимой зоне нужно добавить половину визуальной толщины линии.
        float worldToMm = cachedWorldToMmScale > 0f ? cachedWorldToMmScale : GetWorldToMmScale();
        float visualLineWidthWorld = Mathf.Max(0f, currentExercise.settings.lineThickness) * Mathf.Max(0f, lineThicknessToWorldWidth);
        float visualLineHalfWidthMm = visualLineWidthWorld * worldToMm * 0.5f;

        return baseAllowedMm + visualLineHalfWidthMm;
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
                AddSessionEvent("corridor_exit", "Выход за коридор");
        if (audioFeedback != null) audioFeedback.PlayCorridorExit();
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

    private void UpdateLagLeadByTrackProgress(
        PacemakerRunner nearestRunner,
        TrackGeometry geometry,
        ClosestPointInfo pointerClosest,
        float dt,
        float worldToMm)
    {
        if (nearestRunner == null || geometry == null || !geometry.IsValid())
            return;

        float pointerProgress = pointerClosest.progress;
        float pacemakerProgress = nearestRunner.GetCurrentProgress();
        float deltaProgress = pacemakerProgress - pointerProgress;

        if (geometry.IsClosed)
        {
            if (deltaProgress > 0.5f)
                deltaProgress -= 1f;
            else if (deltaProgress < -0.5f)
                deltaProgress += 1f;
        }

        float deltaMm = deltaProgress * geometry.TotalLength * worldToMm;
        float thresholdMm = Mathf.Max(0.01f, currentExercise.settings.lagThresholdCm) * 10f;

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


    private void ApplyPointerInputSourceFromExercise(PointerInputSource source)
    {
        if (pointer == null)
        {
            Debug.LogWarning("TrackingEvaluator: pointer == null, cannot apply input source from exercise.");
            return;
        }

        pointer.Configure(source);

        Debug.Log("TrackingEvaluator: pointer input source applied from exercise: " + source);
    }

    private void ApplyExerciseBodyPointInfoToResult(ExerciseData exercise)
    {
        if (result == null || exercise == null || exercise.settings == null)
            return;

        ExerciseSettingsData settings = exercise.settings;

        result.activeBodyPointId = settings.activeBodyPointId;
        result.activeBodyPointZoneId = settings.activeBodyPointZoneId;

        if (result.bodyPointBindings == null)
            result.bodyPointBindings = new List<BodyPointBindingData>();

        result.bodyPointBindings.Clear();

        if (settings.bodyPointBindings != null)
        {
            for (int i = 0; i < settings.bodyPointBindings.Count; i++)
            {
                BodyPointBindingData source = settings.bodyPointBindings[i];
                if (source == null)
                    continue;

                result.bodyPointBindings.Add(new BodyPointBindingData
                {
                    bodyPointId = source.bodyPointId,
                    bodyPointZoneId = source.bodyPointZoneId,
                    trackerId = source.trackerId,
                    cursorId = source.cursorId
                });
            }
        }

        if (result.bodyPointBindings.Count > 0 && result.bodyPointBindings[0] != null)
        {
            BodyPointBindingData primary = result.bodyPointBindings[0];

            result.activeBodyPointId = primary.bodyPointId;
            result.activeBodyPointZoneId = primary.bodyPointZoneId;
            result.activeTrackerId = primary.trackerId;
            result.activeCursorId = primary.cursorId;
        }
    }

    private void AddSessionEvent(string eventType, string details = "")
    {
        if (result == null)
            return;

        if (result.events == null)
            result.events = new List<SessionEventData>();

        result.events.Add(new SessionEventData
        {
            eventType = eventType,
            timeSec = result.totalTimeSec,
            bodyPointId = result.activeBodyPointId,
            bodyPointZoneId = result.activeBodyPointZoneId,
            trackerId = result.activeTrackerId,
            cursorId = result.activeCursorId,
            details = details
        });
    }



    private void RecordSamples(Vector2 cursorWorldPos, Vector2 pacerWorldPos, float deviationMm, bool outsideCorridor, float worldToMm, float dt)
    {
        if (!recordSamples || result == null)
            return;

        sampleTimerSec += dt;

        if (sampleTimerSec < Mathf.Max(0.001f, sampleIntervalSec))
            return;

        sampleTimerSec = 0f;

        if (result.cursorSamples == null)
            result.cursorSamples = new List<SessionSampleData>();

        if (result.pacerSamples == null)
            result.pacerSamples = new List<SessionSampleData>();

        bool cursorLooksZero =
            Mathf.Abs(cursorWorldPos.x) <= initialZeroCursorToleranceWorld &&
            Mathf.Abs(cursorWorldPos.y) <= initialZeroCursorToleranceWorld;

        if (!cursorSignalStarted && !cursorLooksZero)
        {
            cursorSignalStarted = true;
            cursorWarmupSamplesLeft = Mathf.Max(0, skipCursorSamplesAfterSignalStart);
            AddSessionEvent("cursor_signal_started", "Получены первые ненулевые координаты курсора");
        }

        bool canWriteCursorSample = true;

        if (ignoreInitialZeroCursorSamples && !cursorSignalStarted && cursorLooksZero)
            canWriteCursorSample = false;

        if (canWriteCursorSample && cursorWarmupSamplesLeft > 0)
        {
            cursorWarmupSamplesLeft--;
            canWriteCursorSample = false;
        }

        if (canWriteCursorSample &&
            discardUnreasonableCursorSamples &&
            maxAcceptedCursorDeviationMm > 0f &&
            deviationMm > maxAcceptedCursorDeviationMm)
        {
            if (!cursorOutlierReported)
            {
                cursorOutlierReported = true;
                AddSessionEvent("cursor_sample_outlier_discarded", "Отброшен стартовый выброс координат курсора");
            }

            canWriteCursorSample = false;
        }

        if (canWriteCursorSample && !cursorSamplesStartedReported)
        {
            cursorSamplesStartedReported = true;
            AddSessionEvent("cursor_samples_started", "Начата запись стабильных samples курсора");
        }

        if (canWriteCursorSample && result.cursorSamples.Count < maxSamplesPerStream)
        {
            result.cursorSamples.Add(CreateSample(
                "cursor",
                cursorSampleIndex++,
                cursorWorldPos,
                deviationMm,
                outsideCorridor,
                worldToMm,
                cursorSignalStarted ? "ok" : "initial_zero"));
        }

        if (result.pacerSamples.Count < maxSamplesPerStream)
        {
            result.pacerSamples.Add(CreateSample(
                "pacer",
                pacerSampleIndex++,
                pacerWorldPos,
                0f,
                false,
                worldToMm,
                "ok"));
        }
    }

    private SessionSampleData CreateSample(string sampleType, int sampleIndex, Vector2 worldPos, float deviationMm, bool outsideCorridor, float worldToMm, string signalStatus)
    {
        float xMm = worldPos.x * worldToMm;
        float yMm = worldPos.y * worldToMm;

        BodyRayUnityPacket bodyRayPacket = null;
        bool hasBodyRayPacket =
            sampleType == "cursor" &&
            bodyRayReceiver != null &&
            bodyRayReceiver.TryGetLatestPacket(out bodyRayPacket);

        float rawWorldX = hasBodyRayPacket ? bodyRayPacket.rawWorldX : worldPos.x;
        float rawWorldY = hasBodyRayPacket ? bodyRayPacket.rawWorldY : worldPos.y;
        float correctedWorldX = hasBodyRayPacket ? bodyRayPacket.correctedWorldX : worldPos.x;
        float correctedWorldY = hasBodyRayPacket ? bodyRayPacket.correctedWorldY : worldPos.y;

        float rawXMm = hasBodyRayPacket ? bodyRayPacket.rawXMm : rawWorldX * worldToMm;
        float rawYMm = hasBodyRayPacket ? bodyRayPacket.rawYMm : rawWorldY * worldToMm;
        float correctedXMm = hasBodyRayPacket ? bodyRayPacket.correctedXMm : correctedWorldX * worldToMm;
        float correctedYMm = hasBodyRayPacket ? bodyRayPacket.correctedYMm : correctedWorldY * worldToMm;

        string packetTrackingMode = hasBodyRayPacket && !string.IsNullOrEmpty(bodyRayPacket.trackingMode)
            ? bodyRayPacket.trackingMode
            : GetBodyPointTrackingModeLabel();

        string packetAnchorId = hasBodyRayPacket ? bodyRayPacket.anchorId : "";
        string packetCalibrationStatus = hasBodyRayPacket && !string.IsNullOrEmpty(bodyRayPacket.calibrationStatus)
            ? bodyRayPacket.calibrationStatus
            : "not_applied";

        return new SessionSampleData
        {
            sampleType = sampleType,
            sampleIndex = sampleIndex,
            timeSec = result != null ? result.totalTimeSec : 0f,

            trackingMode = packetTrackingMode,
            bodyPointId = result != null ? result.activeBodyPointId : BodyPointId.RightForearm,
            bodyPointZoneId = result != null ? result.activeBodyPointZoneId : "zone_right_forearm",
            trackerId = result != null ? result.activeTrackerId : "WT901_01",
            cursorId = result != null ? result.activeCursorId : "cursor_1",

            // Backward-compatible coordinates are the corrected cursor used by the evaluator.
            worldX = correctedWorldX,
            worldY = correctedWorldY,
            xMm = correctedXMm,
            yMm = correctedYMm,

            rawWorldX = rawWorldX,
            rawWorldY = rawWorldY,
            rawXMm = rawXMm,
            rawYMm = rawYMm,

            correctedWorldX = correctedWorldX,
            correctedWorldY = correctedWorldY,
            correctedXMm = correctedXMm,
            correctedYMm = correctedYMm,

            correctionMm = hasBodyRayPacket ? bodyRayPacket.correctionMm : 0f,
            confidence = hasBodyRayPacket ? bodyRayPacket.confidence : (signalStatus == "ok" ? 1f : 0f),
            anchorId = packetAnchorId,
            loopClosureErrorMm = hasBodyRayPacket ? bodyRayPacket.loopClosureErrorMm : 0f,
            calibrationStatus = packetCalibrationStatus,

            deviationMm = deviationMm,
            outsideCorridor = outsideCorridor,
            signalStatus = hasBodyRayPacket && !string.IsNullOrEmpty(bodyRayPacket.signalStatus)
                ? bodyRayPacket.signalStatus
                : signalStatus
        };
    }

    private string GetBodyPointTrackingModeLabel()
    {
        int count = 0;

        if (result != null && result.bodyPointBindings != null)
            count = result.bodyPointBindings.Count;

        if (count <= 1)
            return "single";

        if (count == 2)
            return "pair";

        return "triad";
    }

    private void AddSessionEventOnce(string eventType, string details = "")
    {
        if (result != null && result.events != null)
        {
            for (int i = 0; i < result.events.Count; i++)
            {
                if (result.events[i] != null && result.events[i].eventType == eventType)
                    return;
            }
        }

        AddSessionEvent(eventType, details);
    }

    public SessionResult GetCurrentResult()
    {
        return result;
    }
}
