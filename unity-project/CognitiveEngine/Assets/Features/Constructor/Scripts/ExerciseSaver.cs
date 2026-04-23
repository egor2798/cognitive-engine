using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class ExerciseSaver : MonoBehaviour
{
    [SerializeField] private TrackEditor trackEditor;

    public void SaveToJson()
    {
        if (trackEditor == null || trackEditor.CurrentExercise == null)
        {
            Debug.LogError("No exercise to save");
            return;
        }

        PreparePacemakers(trackEditor.CurrentExercise);

        Debug.Log("Shapes count: " + trackEditor.CurrentExercise.shapes.Count);
        Debug.Log("Paths count: " + trackEditor.CurrentExercise.paths.Count);
        Debug.Log("Pacemakers count: " +
            (trackEditor.CurrentExercise.settings != null && trackEditor.CurrentExercise.settings.pacemakers != null
                ? trackEditor.CurrentExercise.settings.pacemakers.Count
                : -1));

        string json = JsonUtility.ToJson(trackEditor.CurrentExercise, true);

        string path = Path.Combine(Application.persistentDataPath, "exercise.json");
        File.WriteAllText(path, json);

        Debug.Log("Saved to: " + path);
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
            loopMode = PacemakerLoopMode.Loop,
            targetIds = new List<string> { targetId }
        };

        exercise.settings.pacemakers.Add(pacemaker);
    }
}