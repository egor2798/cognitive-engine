using System.IO;
using UnityEngine;

public class ExerciseLoader : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform shapesRoot;
    [SerializeField] private Transform pathsRoot;
    [SerializeField] private ShapeElement shapePrefab;
    [SerializeField] private PathElement pathPrefab;
    [SerializeField] private SegmentElement segmentPrefab;

    [Header("Options")]
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool loadLastProjectFirst = true;

    private ExerciseData loadedExercise;

    public ExerciseData LoadedExercise => loadedExercise;

    private void Start()
    {
        if (loadOnStart)
            LoadFromJson();
    }

    public void LoadFromJson()
    {
        ExerciseData exercise = loadLastProjectFirst
            ? ProjectStorage.LoadLastProjectOrLegacy()
            : ProjectStorage.LoadLegacyExercise();

        if (exercise == null)
            return;

        LoadFromExerciseData(exercise);
    }

    public void LoadFromLegacyJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "exercise.json");

        if (!File.Exists(path))
        {
            Debug.LogError("ExerciseLoader: file not found: " + path);
            return;
        }

        ExerciseData exercise = ProjectStorage.LoadExerciseFromPath(path);

        if (exercise == null)
            return;

        LoadFromExerciseData(exercise);
    }

    public void LoadProject(string projectName)
    {
        ExerciseData exercise = ProjectStorage.LoadProject(projectName);

        if (exercise == null)
            return;

        LoadFromExerciseData(exercise);
    }

    public void LoadFromExerciseData(ExerciseData exercise)
    {
        if (exercise == null)
        {
            Debug.LogError("ExerciseLoader: exercise is null");
            return;
        }

        loadedExercise = exercise;

        ClearScene();
        BuildExercise(loadedExercise);

        Debug.Log("ExerciseLoader: exercise loaded: " + loadedExercise.name);
    }

    public void StopExercise()
    {
        ClearScene();
    }

    private void BuildExercise(ExerciseData exercise)
    {
        if (exercise == null)
            return;

        ExerciseSettingsData settings = exercise.settings ?? new ExerciseSettingsData();
        float lineThickness = settings.lineThickness;
        bool showTrack = settings.showTrack;

        if (exercise.shapes != null)
        {
            for (int i = 0; i < exercise.shapes.Count; i++)
            {
                ShapeData shapeData = exercise.shapes[i];

                if (shapeData == null)
                    continue;

                ShapeElement shapeInstance = Instantiate(shapePrefab, shapesRoot);
                shapeInstance.name = $"LoadedShape_{i + 1}";
                shapeInstance.Initialize(shapeData, lineThickness, showTrack);
            }
        }

        if (exercise.paths != null)
        {
            for (int i = 0; i < exercise.paths.Count; i++)
            {
                PathData pathData = exercise.paths[i];

                if (pathData == null)
                    continue;

                PathElement pathInstance = Instantiate(pathPrefab, pathsRoot);
                pathInstance.name = $"LoadedPath_{i + 1}";
                pathInstance.Initialize(pathData);

                if (pathData.segments == null)
                    continue;

                for (int j = 0; j < pathData.segments.Count; j++)
                {
                    SegmentData segmentData = pathData.segments[j];

                    if (segmentData == null)
                        continue;

                    SegmentElement segmentInstance = Instantiate(segmentPrefab, pathInstance.transform);
                    segmentInstance.name = $"LoadedSegment_{j + 1}";
                    segmentInstance.Initialize(segmentData, lineThickness, showTrack);

                    pathInstance.AddSegmentElement(segmentInstance);
                }
            }
        }
    }

    private void ClearScene()
    {
        if (shapesRoot != null)
        {
            for (int i = shapesRoot.childCount - 1; i >= 0; i--)
                Destroy(shapesRoot.GetChild(i).gameObject);
        }

        if (pathsRoot != null)
        {
            for (int i = pathsRoot.childCount - 1; i >= 0; i--)
                Destroy(pathsRoot.GetChild(i).gameObject);
        }
    }
}
