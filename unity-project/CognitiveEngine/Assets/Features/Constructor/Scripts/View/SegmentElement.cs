using System.Collections.Generic;
using UnityEngine;

public class SegmentElement : MonoBehaviour
{
    private const float DefaultLineThickness = 6f;
    private const float ThicknessToWorldWidth = 0.01f;
    private const float SelectedWidthMultiplier = 1.55f;

    [SerializeField] private LineRenderer lineRenderer;

    [Header("Runtime Style")]
    [SerializeField] private float lineThickness = DefaultLineThickness;
    [SerializeField] private bool showTrack = true;

    private bool isSelected;

    public SegmentData Data { get; private set; }

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        SetupLineRenderer();
    }

    public void Initialize(SegmentData data)
    {
        Data = data;
        SetupLineRenderer();
        Rebuild();
    }

    public void Initialize(SegmentData data, float settingsLineThickness, bool settingsShowTrack)
    {
        Data = data;
        lineThickness = settingsLineThickness;
        showTrack = settingsShowTrack;
        SetupLineRenderer();
        Rebuild();
    }

    public void ApplyExerciseVisualSettings(ExerciseSettingsData settings)
    {
        if (settings == null)
            return;

        SetLineThickness(settings.lineThickness);
        SetTrackVisible(settings.showTrack);
    }

    public void SetLineThickness(float thickness)
    {
        lineThickness = Mathf.Max(0.1f, thickness);
        ApplyStyle();
    }

    public void SetTrackVisible(bool visible)
    {
        showTrack = visible;
        ApplyStyle();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyStyle();
    }

    public void Rebuild()
    {
        if (Data == null || lineRenderer == null)
            return;

        List<Vector3> points = BuildPoints();
        lineRenderer.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
            lineRenderer.SetPosition(i, points[i]);

        ApplyStyle();
    }

    public float GetDistanceToPoint(Vector2 point)
    {
        if (Data == null)
            return float.MaxValue;

        switch (Data.segmentType)
        {
            case SegmentType.Line:
                return DistancePointToSegment(point, Data.startPoint, Data.endPoint);

            case SegmentType.Bezier:
                return GetDistanceToBezier(point);

            default:
                return float.MaxValue;
        }
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.loop = false;

        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lineRenderer.sharedMaterial = new Material(shader);
        }

        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (lineRenderer == null)
            return;

        float baseWidth = Mathf.Max(0.001f, lineThickness * ThicknessToWorldWidth);
        float width = isSelected ? baseWidth * SelectedWidthMultiplier : baseWidth;

        lineRenderer.enabled = showTrack;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        Color normalColor = new Color32(19, 78, 92, 255);
        Color selectedColor = new Color32(39, 199, 217, 255);
        Color finalColor = isSelected ? selectedColor : normalColor;

        lineRenderer.startColor = finalColor;
        lineRenderer.endColor = finalColor;
    }

    private List<Vector3> BuildPoints()
    {
        List<Vector3> points = new();

        switch (Data.segmentType)
        {
            case SegmentType.Line:
                points.Add(new Vector3(Data.startPoint.x, Data.startPoint.y, 0f));
                points.Add(new Vector3(Data.endPoint.x, Data.endPoint.y, 0f));
                break;

            case SegmentType.Bezier:
                const int segments = 24;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector2 p = CalculateCubicBezier(
                        Data.startPoint,
                        Data.controlPoint1,
                        Data.controlPoint2,
                        Data.endPoint,
                        t
                    );
                    points.Add(new Vector3(p.x, p.y, 0f));
                }
                break;
        }

        return points;
    }

    private float GetDistanceToBezier(Vector2 point)
    {
        const int segments = 24;
        float minDistance = float.MaxValue;
        Vector2 prev = Data.startPoint;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 current = CalculateCubicBezier(
                Data.startPoint,
                Data.controlPoint1,
                Data.controlPoint2,
                Data.endPoint,
                t
            );

            float distance = DistancePointToSegment(point, prev, current);
            if (distance < minDistance)
                minDistance = distance;

            prev = current;
        }

        return minDistance;
    }

    private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abSqr = ab.sqrMagnitude;

        if (abSqr < Mathf.Epsilon)
            return Vector2.Distance(p, a);

        float t = Vector2.Dot(p - a, ab) / abSqr;
        t = Mathf.Clamp01(t);

        Vector2 projection = a + t * ab;
        return Vector2.Distance(p, projection);
    }

    private Vector2 CalculateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }
}
