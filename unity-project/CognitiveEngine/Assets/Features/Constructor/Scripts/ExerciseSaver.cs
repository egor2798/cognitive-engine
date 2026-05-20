using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ExerciseSaver : MonoBehaviour
{
    [SerializeField] private TrackEditor trackEditor;

    public ExerciseData CurrentExercise
    {
        get
        {
            if (trackEditor == null)
                return null;

            return trackEditor.CurrentExercise;
        }
    }

    public void SaveToJson()
    {
        SaveLegacyJson();
    }

    public void SaveLegacyJson()
    {
        ExerciseData exercise = GetPreparedExercise();

        if (exercise == null)
            return;

        string json = JsonUtility.ToJson(exercise, true);
        string path = Path.Combine(Application.persistentDataPath, "exercise.json");
        File.WriteAllText(path, json);

        Debug.Log("ExerciseSaver: saved legacy exercise to: " + path);
    }

    public string SaveProject(string projectName)
    {
        ExerciseData exercise = GetPreparedExercise();

        if (exercise == null)
            return null;

        return ProjectStorage.SaveProject(exercise, projectName, true);
    }

    private ExerciseData GetPreparedExercise()
    {
        if (trackEditor == null || trackEditor.CurrentExercise == null)
        {
            Debug.LogError("ExerciseSaver: no exercise to save");
            return null;
        }

        PrepareSettings(trackEditor.CurrentExercise);
        PreparePacemakers(trackEditor.CurrentExercise);

        Debug.Log("Shapes count: " + (trackEditor.CurrentExercise.shapes != null ? trackEditor.CurrentExercise.shapes.Count : -1));
        Debug.Log("Paths count: " + (trackEditor.CurrentExercise.paths != null ? trackEditor.CurrentExercise.paths.Count : -1));
        Debug.Log("Composite tracks count: " + (trackEditor.CurrentExercise.compositeTracks != null ? trackEditor.CurrentExercise.compositeTracks.Count : -1));
        Debug.Log("Pacemakers count: " +
            (trackEditor.CurrentExercise.settings != null && trackEditor.CurrentExercise.settings.pacemakers != null
                ? trackEditor.CurrentExercise.settings.pacemakers.Count
                : -1));

        if (trackEditor.CurrentExercise.settings != null &&
            trackEditor.CurrentExercise.settings.pacemakers != null &&
            trackEditor.CurrentExercise.settings.pacemakers.Count > 0 &&
            trackEditor.CurrentExercise.settings.pacemakers[0] != null &&
            trackEditor.CurrentExercise.settings.pacemakers[0].targetIds != null)
        {
            Debug.Log("Pacemaker target: " + string.Join(", ", trackEditor.CurrentExercise.settings.pacemakers[0].targetIds));
        }

        return trackEditor.CurrentExercise;
    }

    private void PrepareSettings(ExerciseData exercise)
    {
        if (exercise.settings == null)
            exercise.settings = new ExerciseSettingsData();

        exercise.settings.inputSource = exercise.settings.useSensor
            ? PointerInputSource.Sensor
            : PointerInputSource.Mouse;
    }

    private void PreparePacemakers(ExerciseData exercise)
    {
        if (exercise == null)
            return;

        if (exercise.settings == null)
            exercise.settings = new ExerciseSettingsData();

        if (exercise.settings.pacemakers == null)
            exercise.settings.pacemakers = new List<PacemakerSettingsData>();

        exercise.settings.pacemakers.Clear();

        if (!exercise.settings.usePacemaker)
            return;

        string targetId = GetPrimaryPacemakerTargetId(exercise);

        if (string.IsNullOrWhiteSpace(targetId))
        {
            Debug.LogWarning("ExerciseSaver: pacemaker enabled, but no target found.");
            return;
        }

        PacemakerSettingsData pacemaker = new PacemakerSettingsData
        {
            id = System.Guid.NewGuid().ToString(),
            enabled = true,
            speed = exercise.settings.defaultSpeed,
            loopMode = exercise.settings.defaultPacemakerLoopMode,
            targetIds = new List<string> { targetId }
        };

        exercise.settings.pacemakers.Add(pacemaker);
    }

    private string GetPrimaryPacemakerTargetId(ExerciseData exercise)
    {
        // ВАЖНО: составной трек должен иметь приоритет над обычными фигурами.
        // Иначе после нажатия "Объединить" ExerciseSaver перезапишет targetIds
        // на первую shape, и в PlayerScene пейсмейкер снова будет ходить только по одной фигуре.
        if (exercise.compositeTracks != null && exercise.compositeTracks.Count > 0)
        {
            CompositeTrackData composite = exercise.compositeTracks[0];
            if (composite != null && !string.IsNullOrWhiteSpace(composite.id))
                return composite.id;
        }

        if (exercise.paths != null && exercise.paths.Count > 0)
        {
            PathData path = exercise.paths[0];
            if (path != null && !string.IsNullOrWhiteSpace(path.id))
                return path.id;
        }

        if (exercise.shapes != null && exercise.shapes.Count > 0)
        {
            ShapeData shape = exercise.shapes[0];
            if (shape != null && !string.IsNullOrWhiteSpace(shape.id))
                return shape.id;
        }

        return null;
    }
}
