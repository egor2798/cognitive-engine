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

        Debug.Log("Shapes count: " + trackEditor.CurrentExercise.shapes.Count);
        Debug.Log("Paths count: " + trackEditor.CurrentExercise.paths.Count);
        Debug.Log("Pacemakers count: " +
            (trackEditor.CurrentExercise.settings != null && trackEditor.CurrentExercise.settings.pacemakers != null
                ? trackEditor.CurrentExercise.settings.pacemakers.Count
                : -1));

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

        string targetId = null;

        if (exercise.paths != null && exercise.paths.Count > 0 && exercise.paths[0] != null)
        {
            targetId = exercise.paths[0].id;
        }
        else if (exercise.shapes != null && exercise.shapes.Count > 0 && exercise.shapes[0] != null)
        {
            targetId = exercise.shapes[0].id;
        }

        if (string.IsNullOrWhiteSpace(targetId))
            return;

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
}
