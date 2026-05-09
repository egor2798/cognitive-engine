using System.Collections.Generic;
using UnityEngine;

public class TrackEditorProjectLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrackEditor trackEditor;
    [SerializeField] private Transform shapesRoot;
    [SerializeField] private Transform pathsRoot;
    [SerializeField] private ShapeElement shapePrefab;
    [SerializeField] private PathElement pathPrefab;
    [SerializeField] private SegmentElement segmentPrefab;

    public bool LoadProjectToEditor(string projectName)
    {
        ExerciseData exercise = ProjectStorage.LoadProject(projectName);
        if (exercise == null)
            return false;

        LoadExerciseToEditor(exercise);
        Debug.Log("TrackEditorProjectLoader: loaded project into editor: " + projectName);
        return true;
    }

    public void LoadExerciseToEditor(ExerciseData exercise)
    {
        if (exercise == null)
        {
            Debug.LogError("TrackEditorProjectLoader: exercise is null");
            return;
        }

        if (trackEditor == null)
        {
            Debug.LogError("TrackEditorProjectLoader: TrackEditor is not assigned");
            return;
        }

        if (shapesRoot == null || pathsRoot == null || shapePrefab == null || pathPrefab == null || segmentPrefab == null)
        {
            Debug.LogError("TrackEditorProjectLoader: one or more references are not assigned");
            return;
        }

        trackEditor.SetTool(TrackEditor.ToolMode.Select);
        trackEditor.ClearAllShapes();
        trackEditor.CreateNewExercise();

        ExerciseData current = trackEditor.CurrentExercise;
        CopyExerciseData(exercise, current);
        BuildEditorObjects(current);
    }

    private void CopyExerciseData(ExerciseData source, ExerciseData target)
    {
        if (source == null || target == null)
            return;

        target.id = string.IsNullOrWhiteSpace(source.id) ? System.Guid.NewGuid().ToString() : source.id;
        target.name = string.IsNullOrWhiteSpace(source.name) ? "Loaded Exercise" : source.name;
        target.settings = source.settings;
        target.shapes = source.shapes ?? new List<ShapeData>();
        target.paths = source.paths ?? new List<PathData>();
    }

    private void BuildEditorObjects(ExerciseData exercise)
    {
        if (exercise.shapes != null)
        {
            for (int i = 0; i < exercise.shapes.Count; i++)
            {
                ShapeData shapeData = exercise.shapes[i];
                if (shapeData == null)
                    continue;

                ShapeElement shapeInstance = Instantiate(shapePrefab, shapesRoot);
                shapeInstance.name = $"Shape_{i + 1}";
                shapeInstance.Initialize(shapeData);
            }
        }

        if (exercise.paths != null)
        {
            for (int i = 0; i < exercise.paths.Count; i++)
            {
                PathData pathData = exercise.paths[i];
                if (pathData == null)
                    continue;

                if (pathData.segments == null)
                    pathData.segments = new List<SegmentData>();

                PathElement pathInstance = Instantiate(pathPrefab, pathsRoot);
                pathInstance.name = $"Path_{i + 1}";
                pathInstance.Initialize(pathData);

                for (int j = 0; j < pathData.segments.Count; j++)
                {
                    SegmentData segmentData = pathData.segments[j];
                    if (segmentData == null)
                        continue;

                    SegmentElement segmentInstance = Instantiate(segmentPrefab, pathInstance.transform);
                    segmentInstance.name = $"Segment_{j + 1}";
                    segmentInstance.Initialize(segmentData);
                    pathInstance.AddSegmentElement(segmentInstance);
                }
            }
        }
    }
}
