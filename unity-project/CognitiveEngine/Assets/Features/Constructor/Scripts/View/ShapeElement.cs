using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ShapeElement : MonoBehaviour
{
    private const float DefaultLineThickness = 6f;
    private const float ThicknessToWorldWidth = 0.01f;
    private const float SelectedWidthMultiplier = 1.55f;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private ShapeData data = new ShapeData();

    [Header("Runtime Style")]
    [SerializeField] private float lineThickness = DefaultLineThickness;
    [SerializeField] private bool showTrack = true;

    [Header("Resize Handle")]
    [SerializeField] private bool hideResizeHandleObjects = true;

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

        SetupLineRenderer();
        HideResizeHandleObjects();
        Rebuild();
    }

    public void Initialize(ShapeData newData)
    {
        data = newData;
        Rebuild();
    }

    public void Initialize(ShapeData newData, float settingsLineThickness, bool settingsShowTrack)
    {
        data = newData;
        lineThickness = settingsLineThickness;
        showTrack = settingsShowTrack;
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

    public void Rebuild()
    {
        if (data == null || lineRenderer == null)
            return;

        SetupLineRenderer();
        HideResizeHandleObjects();
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
        // Resize через мышь оставляем, но визуальный белый handle скрываем.
        // Тянуть можно за правый верхний угол квадрата или за правую точку круга.
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


    private void HideResizeHandleObjects()
    {
        if (!hideResizeHandleObjects)
            return;

        // На старом prefab могла остаться отдельная белая точка/квадрат для resize.
        // Размер теперь редактируется через правую панель "Свойства", поэтому такие
        // дочерние объекты отключаем, чтобы их не путали с началом координат.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            string n = child.name.ToLowerInvariant();
            bool looksLikeHandle =
                n.Contains("handle") ||
                n.Contains("resize") ||
                n.Contains("corner") ||
                n.Contains("point") ||
                n.Contains("marker");

            // Если у ShapeElement есть дочерний SpriteRenderer/Image без понятного имени,
            // это почти наверняка старый resize-handle. LineRenderer самой фигуры находится
            // на этом объекте, не на дочернем.
            bool hasVisual =
                child.GetComponent<SpriteRenderer>() != null ||
                child.GetComponent<UnityEngine.UI.Image>() != null ||
                child.GetComponent<Renderer>() != null;

            if (looksLikeHandle || hasVisual)
                child.gameObject.SetActive(false);
        }
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lineRenderer.sharedMaterial = new Material(shader);
        }
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

        float baseWidth = Mathf.Max(0.001f, lineThickness * ThicknessToWorldWidth);
        float widthValue = isSelected ? baseWidth * SelectedWidthMultiplier : baseWidth;

        Color normalColor = new Color32(19, 78, 92, 255);
        Color selectedColor = new Color32(39, 199, 217, 255);
        Color finalColor = isSelected ? selectedColor : normalColor;

        lineRenderer.enabled = showTrack;
        lineRenderer.startWidth = widthValue;
        lineRenderer.endWidth = widthValue;
        lineRenderer.startColor = finalColor;
        lineRenderer.endColor = finalColor;
    }
}
