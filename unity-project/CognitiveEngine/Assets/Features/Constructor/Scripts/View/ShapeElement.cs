using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ShapeElement : MonoBehaviour
{
    private const float NormalWidth = 0.07f;
    private const float SelectedWidth = 0.11f;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private ShapeData data = new ShapeData();

    private readonly List<Vector3> points = new();
    private bool isSelected;

    public ShapeData Data => data;
    public IReadOnlyList<Vector3> Points => points;

    private void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
        }

        Rebuild();
    }

    public void Initialize(ShapeData newData)
    {
        data = newData;
        Rebuild();
    }

    public void Rebuild()
    {
        if (data == null || lineRenderer == null)
            return;

        points.Clear();

        switch (data.shapeType)
        {
            case ShapeType.Circle:
                BuildCircle();
                break;

            case ShapeType.Square:
            default:
                BuildSquare();
                break;
        }

        ApplyStyle();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyStyle();
    }

    public bool SupportsResizeHandle()
    {
        return true;
    }

    public bool IsPointNearResizeHandle(Vector3 worldPoint)
    {
        return IsPointNearResizeHandle(worldPoint, 0.25f);
    }

    public bool IsPointNearResizeHandle(Vector3 worldPoint, float threshold)
    {
        Vector3 handle = GetClosestResizeHandle();
        return Vector3.Distance(worldPoint, handle) <= threshold;
    }

    public void UpdateResizeFromWorldPoint(Vector3 worldPoint)
    {
        switch (data.shapeType)
        {
            case ShapeType.Circle:
                {
                    Vector2 p = new Vector2(worldPoint.x, worldPoint.y);
                    data.radius = Mathf.Max(0.05f, Vector2.Distance(p, data.center));
                    break;
                }

            case ShapeType.Square:
            default:
                {
                    float halfW = Mathf.Abs(worldPoint.x - data.center.x);
                    float halfH = Mathf.Abs(worldPoint.y - data.center.y);
                    data.width = Mathf.Max(0.05f, halfW * 2f);
                    data.height = Mathf.Max(0.05f, halfH * 2f);
                    break;
                }
        }

        Rebuild();
    }

    private Vector3 GetClosestResizeHandle()
    {
        switch (data.shapeType)
        {
            case ShapeType.Circle:
                return new Vector3(data.center.x + Mathf.Max(0.05f, data.radius), data.center.y, 0f);

            case ShapeType.Square:
            default:
                return new Vector3(
                    data.center.x + data.width * 0.5f,
                    data.center.y + data.height * 0.5f,
                    0f
                );
        }
    }

    private void BuildSquare()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        float halfW = data.width * 0.5f;
        float halfH = data.height * 0.5f;

        Vector3 topLeft = new Vector3(data.center.x - halfW, data.center.y + halfH, 0f);
        Vector3 topRight = new Vector3(data.center.x + halfW, data.center.y + halfH, 0f);
        Vector3 bottomRight = new Vector3(data.center.x + halfW, data.center.y - halfH, 0f);
        Vector3 bottomLeft = new Vector3(data.center.x - halfW, data.center.y - halfH, 0f);

        points.Add(topLeft);
        points.Add(topRight);
        points.Add(bottomRight);
        points.Add(bottomLeft);

        lineRenderer.enabled = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, topLeft);
        lineRenderer.SetPosition(1, topRight);
        lineRenderer.SetPosition(2, bottomRight);
        lineRenderer.SetPosition(3, bottomLeft);
    }

    private void BuildCircle()
    {
        data.radius = Mathf.Max(0.05f, data.radius);

        lineRenderer.enabled = true;
        lineRenderer.loop = true;

        int segments = Mathf.Max(24, circleSegments);
        lineRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;

            Vector3 p = new Vector3(
                data.center.x + Mathf.Cos(angle) * data.radius,
                data.center.y + Mathf.Sin(angle) * data.radius,
                0f
            );

            points.Add(p);
            lineRenderer.SetPosition(i, p);
        }
    }

    private void ApplyStyle()
    {
        if (lineRenderer == null)
            return;

        float widthValue = isSelected ? SelectedWidth : NormalWidth;
        Color normalColor = new Color32(19, 78, 92, 255);
        Color selectedColor = new Color32(39, 199, 217, 255);
        Color finalColor = isSelected ? selectedColor : normalColor;

        lineRenderer.startWidth = widthValue;
        lineRenderer.endWidth = widthValue;
        lineRenderer.startColor = finalColor;
        lineRenderer.endColor = finalColor;
    }
}