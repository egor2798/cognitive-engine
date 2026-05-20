using System;
using System.Collections.Generic;
using UnityEngine;

public class PresetExerciseGenerator : MonoBehaviour
{
    [Header("Preset Names")]
    [SerializeField] private string waveProjectName = "Smooth_Wave";
    [SerializeField] private string circleProjectName = "Smooth_Circle";
    [SerializeField] private string figureEightProjectName = "Smooth_Figure_Eight";

    [Header("Common Exercise Settings")]
    [SerializeField] private int durationMs = 60000;
    [SerializeField] private float lineThickness = 6f;
    [SerializeField] private float corridorWidthCm = 4f;
    [SerializeField] private float hitRadiusCm = 5f;
    [SerializeField] private float defaultSpeed = 300f;
    [SerializeField] private bool usePacemaker = true;
    [SerializeField] private bool showTrack = true;

    [Header("Wave Settings")]
    [SerializeField] private int waveSamples = 48;
    [SerializeField] private float waveWidth = 6.5f;
    [SerializeField] private float waveAmplitude = 1.3f;
    [SerializeField] private float waveCycles = 2f;

    [Header("Circle Settings")]
    [SerializeField] private int circleSamples = 64;
    [SerializeField] private float circleRadius = 2.0f;

    [Header("Figure Eight Settings")]
    [SerializeField] private int figureEightSamples = 80;
    [SerializeField] private float figureEightWidth = 2.8f;
    [SerializeField] private float figureEightHeight = 1.5f;

    [ContextMenu("Generate All Preset Exercises")]
    public void GenerateAllPresetExercises()
    {
        GenerateWavePreset();
        GenerateCirclePreset();
        GenerateFigureEightPreset();
    }

    [ContextMenu("Generate Smooth Wave Preset")]
    public void GenerateWavePreset()
    {
        ExerciseData exercise = CreateExercise(waveProjectName);
        PathData path = CreatePath();

        List<Vector2> points = new List<Vector2>();
        int samples = Mathf.Max(4, waveSamples);

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float x = Mathf.Lerp(-waveWidth * 0.5f, waveWidth * 0.5f, t);
            float y = Mathf.Sin(t * Mathf.PI * 2f * waveCycles) * waveAmplitude;
            points.Add(new Vector2(x, y));
        }

        FillPathWithLineSegments(path, points);
        exercise.paths.Add(path);
        AddDefaultPacemaker(exercise, path.id);
        SavePreset(exercise, waveProjectName);
    }

    [ContextMenu("Generate Smooth Circle Preset")]
    public void GenerateCirclePreset()
    {
        ExerciseData exercise = CreateExercise(circleProjectName);
        PathData path = CreatePath();

        List<Vector2> points = new List<Vector2>();
        int samples = Mathf.Max(8, circleSamples);

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float angle = t * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * circleRadius;
            float y = Mathf.Sin(angle) * circleRadius;
            points.Add(new Vector2(x, y));
        }

        FillPathWithLineSegments(path, points);
        exercise.paths.Add(path);
        AddDefaultPacemaker(exercise, path.id);
        SavePreset(exercise, circleProjectName);
    }

    [ContextMenu("Generate Smooth Figure Eight Preset")]
    public void GenerateFigureEightPreset()
    {
        ExerciseData exercise = CreateExercise(figureEightProjectName);
        PathData path = CreatePath();

        List<Vector2> points = new List<Vector2>();
        int samples = Mathf.Max(12, figureEightSamples);

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float angle = t * Mathf.PI * 2f;

            // Lemniscate-like path: smooth horizontal figure eight.
            float x = Mathf.Sin(angle) * figureEightWidth;
            float y = Mathf.Sin(angle * 2f) * figureEightHeight;
            points.Add(new Vector2(x, y));
        }

        FillPathWithLineSegments(path, points);
        exercise.paths.Add(path);
        AddDefaultPacemaker(exercise, path.id);
        SavePreset(exercise, figureEightProjectName);
    }

    private ExerciseData CreateExercise(string exerciseName)
    {
        ExerciseData exercise = new ExerciseData();
        exercise.id = Guid.NewGuid().ToString();
        exercise.name = exerciseName.Replace('_', ' ');
        exercise.shapes = new List<ShapeData>();
        exercise.paths = new List<PathData>();
        exercise.settings = CreateSettings();
        return exercise;
    }

    private ExerciseSettingsData CreateSettings()
    {
        ExerciseSettingsData settings = new ExerciseSettingsData();
        settings.usePacemaker = usePacemaker;
        settings.allowWithoutPacemaker = true;
        settings.defaultSpeed = defaultSpeed;
        settings.durationMs = durationMs;
        settings.lineThickness = lineThickness;
        settings.showTrack = showTrack;
        settings.useSensor = false;
        settings.sensorSensitivity = 1f;
        settings.corridorWidthCm = corridorWidthCm;
        settings.hitRadiusCm = hitRadiusCm;
        settings.workspaceWidthMm = 800f;
        settings.workspaceHeightMm = 600f;
        settings.trackDeviation = true;
        settings.saveSessionResult = true;
        settings.pacemakers = new List<PacemakerSettingsData>();
        return settings;
    }

    private PathData CreatePath()
    {
        PathData path = new PathData();
        path.id = Guid.NewGuid().ToString();
        path.segments = new List<SegmentData>();
        return path;
    }

    private void FillPathWithLineSegments(PathData path, List<Vector2> points)
    {
        if (path == null || points == null || points.Count < 2)
            return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            SegmentData segment = new SegmentData();
            segment.id = Guid.NewGuid().ToString();
            segment.segmentType = SegmentType.Line;
            segment.startPoint = points[i];
            segment.endPoint = points[i + 1];
            segment.controlPoint1 = points[i];
            segment.controlPoint2 = points[i + 1];
            path.segments.Add(segment);
        }
    }

    private void AddDefaultPacemaker(ExerciseData exercise, string targetPathId)
    {
        if (exercise == null || exercise.settings == null || string.IsNullOrWhiteSpace(targetPathId))
            return;

        if (exercise.settings.pacemakers == null)
            exercise.settings.pacemakers = new List<PacemakerSettingsData>();

        exercise.settings.pacemakers.Clear();

        if (!exercise.settings.usePacemaker)
            return;

        PacemakerSettingsData pacemaker = new PacemakerSettingsData();
        pacemaker.id = Guid.NewGuid().ToString();
        pacemaker.enabled = true;
        pacemaker.speed = exercise.settings.defaultSpeed;
        pacemaker.loopMode = exercise.settings.defaultPacemakerLoopMode;
        pacemaker.targetIds = new List<string> { targetPathId };

        exercise.settings.pacemakers.Add(pacemaker);
    }

    private void SavePreset(ExerciseData exercise, string projectName)
    {
        string savedPath = ProjectStorage.SaveProject(exercise, projectName, true);
        Debug.Log("PresetExerciseGenerator: saved preset '" + projectName + "' to " + savedPath);
    }
}
