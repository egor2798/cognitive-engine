using System.Collections.Generic;
using UnityEngine;

public class PacemakerController : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private float pacemakerSize = 0.2f;
    [SerializeField] private Color pacemakerColor = Color.red;

    private ExerciseData currentExercise;
    private PathData currentPath;
    private ShapeData currentShape;
    private bool useShapePerimeter;

    private GameObject pacemakerVisual;
    private readonly List<float> segmentLengths = new();
    private float totalLength;
    private float progress01;
    private float elapsedTime;
    private bool isRunning;

    private void Update()
    {
        if (!isRunning || currentExercise == null)
            return;

        if (!useShapePerimeter && (currentPath == null || totalLength <= 0f))
            return;

        if (useShapePerimeter && currentShape == null)
            return;

        elapsedTime += Time.deltaTime;

        if (currentExercise.settings != null && currentExercise.settings.durationMs > 0)
        {
            if (elapsedTime * 1000f >= currentExercise.settings.durationMs)
            {
                StopPacemaker();
                return;
            }
        }

        float speed01PerSecond = GetProgressSpeedPerSecond();
        progress01 += speed01PerSecond * Time.deltaTime;

        if (progress01 > 1f)
            progress01 -= 1f;

        Vector2 point = useShapePerimeter
            ? GetSquarePointAtProgress(currentShape, progress01)
            : GetPointAtProgress(progress01);

        pacemakerVisual.transform.position = new Vector3(point.x, point.y, -1f);
    }

    public void StartPacemaker(ExerciseData exercise)
    {
        Debug.Log("PacemakerController: StartPacemaker вызван");
        Debug.Log("PacemakerController: paths count = " +
            (exercise != null && exercise.paths != null ? exercise.paths.Count : -1));

        currentExercise = exercise;
        currentPath = null;
        currentShape = null;
        useShapePerimeter = false;
        totalLength = 0f;
        segmentLengths.Clear();

        if (currentExercise == null)
        {
            Debug.LogWarning("PacemakerController: exercise == null");
            StopPacemaker();
            return;
        }

        if (currentExercise.settings != null && !currentExercise.settings.usePacemaker)
        {
            StopPacemaker();
            return;
        }

        if (currentExercise.paths != null && currentExercise.paths.Count > 0)
        {
            currentPath = currentExercise.paths[0];

            if (currentPath != null && currentPath.segments != null && currentPath.segments.Count > 0)
            {
                BuildSegmentLengths();
                EnsureVisual();

                progress01 = 0f;
                elapsedTime = 0f;
                isRunning = true;

                pacemakerVisual.SetActive(true);

                Vector2 startPoint = GetPointAtProgress(0f);
                pacemakerVisual.transform.position = new Vector3(startPoint.x, startPoint.y, -1f);
                return;
            }
        }

        if (currentExercise.shapes != null && currentExercise.shapes.Count > 0)
        {
            for (int i = 0; i < currentExercise.shapes.Count; i++)
            {
                ShapeData shape = currentExercise.shapes[i];

                if (shape != null && shape.shapeType == ShapeType.Square)
                {
                    currentShape = shape;
                    useShapePerimeter = true;

                    EnsureVisual();

                    progress01 = 0f;
                    elapsedTime = 0f;
                    isRunning = true;

                    pacemakerVisual.SetActive(true);

                    Vector2 startPoint = GetSquarePointAtProgress(currentShape, 0f);
                    pacemakerVisual.transform.position = new Vector3(startPoint.x, startPoint.y, -1f);
                    return;
                }
            }
        }

        Debug.LogWarning("PacemakerController: нет пути и нет квадрата для движения");
        StopPacemaker();
    }

    public void StopPacemaker()
    {
        isRunning = false;

        if (pacemakerVisual != null)
            pacemakerVisual.SetActive(false);
    }

    private void EnsureVisual()
    {
        if (pacemakerVisual != null)
            return;

        pacemakerVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pacemakerVisual.name = "PacemakerVisual";
        pacemakerVisual.transform.SetParent(transform, false);
        pacemakerVisual.transform.localScale = new Vector3(pacemakerSize, pacemakerSize, 1f);
        pacemakerVisual.transform.position = new Vector3(0f, 0f, -1f);

        Collider collider = pacemakerVisual.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        MeshRenderer renderer = pacemakerVisual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = pacemakerColor;
            renderer.material = material;
        }
    }

    private float GetProgressSpeedPerSecond()
    {
        float baseSpeed = 0.003f;

        if (currentExercise != null && currentExercise.settings != null)
            baseSpeed = currentExercise.settings.defaultSpeed;

        return Mathf.Max(0.0001f, baseSpeed * 60f);
    }

    private void BuildSegmentLengths()
    {
        segmentLengths.Clear();
        totalLength = 0f;

        foreach (SegmentData segment in currentPath.segments)
        {
            float length = GetSegmentLength(segment);
            segmentLengths.Add(length);
            totalLength += length;
        }
    }

    private float GetSegmentLength(SegmentData segment)
    {
        if (segment == null)
            return 0f;

        if (segment.segmentType == SegmentType.Bezier)
            return ApproximateBezierLength(segment, 20);

        return Vector2.Distance(segment.startPoint, segment.endPoint);
    }

    private Vector2 GetPointAtProgress(float progress)
    {
        if (currentPath == null || currentPath.segments == null || currentPath.segments.Count == 0)
            return Vector2.zero;

        float targetDistance = Mathf.Clamp01(progress) * totalLength;
        float accumulated = 0f;

        for (int i = 0; i < currentPath.segments.Count; i++)
        {
            SegmentData segment = currentPath.segments[i];
            float length = segmentLengths[i];

            if (length <= 0f)
                continue;

            if (accumulated + length >= targetDistance)
            {
                float localDistance = targetDistance - accumulated;
                float t = localDistance / length;
                return EvaluateSegment(segment, t);
            }

            accumulated += length;
        }

        SegmentData last = currentPath.segments[currentPath.segments.Count - 1];
        return EvaluateSegment(last, 1f);
    }

    private Vector2 EvaluateSegment(SegmentData segment, float t)
    {
        t = Mathf.Clamp01(t);

        if (segment.segmentType == SegmentType.Bezier)
        {
            return EvaluateBezier(
                segment.startPoint,
                segment.controlPoint1,
                segment.controlPoint2,
                segment.endPoint,
                t
            );
        }

        return Vector2.Lerp(segment.startPoint, segment.endPoint, t);
    }

    private Vector2 EvaluateBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return
            u * u * u * p0 +
            3f * u * u * t * p1 +
            3f * u * t * t * p2 +
            t * t * t * p3;
    }

    private float ApproximateBezierLength(SegmentData segment, int steps)
    {
        float length = 0f;
        Vector2 previous = segment.startPoint;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = EvaluateBezier(
                segment.startPoint,
                segment.controlPoint1,
                segment.controlPoint2,
                segment.endPoint,
                t
            );

            length += Vector2.Distance(previous, point);
            previous = point;
        }

        return length;
    }

    private Vector2 GetSquarePointAtProgress(ShapeData square, float progress)
    {
        if (square == null)
            return Vector2.zero;

        float halfW = square.width * 0.5f;
        float halfH = square.height * 0.5f;

        Vector2 topLeft = new Vector2(square.center.x - halfW, square.center.y + halfH);
        Vector2 topRight = new Vector2(square.center.x + halfW, square.center.y + halfH);
        Vector2 bottomRight = new Vector2(square.center.x + halfW, square.center.y - halfH);
        Vector2 bottomLeft = new Vector2(square.center.x - halfW, square.center.y - halfH);

        float sideProgress = Mathf.Clamp01(progress) * 4f;

        if (sideProgress < 1f)
            return Vector2.Lerp(topLeft, topRight, sideProgress);

        if (sideProgress < 2f)
            return Vector2.Lerp(topRight, bottomRight, sideProgress - 1f);

        if (sideProgress < 3f)
            return Vector2.Lerp(bottomRight, bottomLeft, sideProgress - 2f);

        return Vector2.Lerp(bottomLeft, topLeft, sideProgress - 3f);
    }
}