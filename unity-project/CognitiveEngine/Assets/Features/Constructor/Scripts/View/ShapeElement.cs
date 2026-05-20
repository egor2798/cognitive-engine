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
    private readonly List<LineRenderer> auxiliaryLineRenderers = new();
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
        ClearAuxiliaryLineRenderers();
        points.Clear();

        switch (data.shapeType)
        {
            case ShapeType.Circle:
                BuildCircle();
                break;
            case ShapeType.Triangle:
                BuildRegularPolygon(3, -90f);
                break;
            case ShapeType.Diamond:
                BuildDiamond();
                break;
            case ShapeType.Pentagon:
                BuildRegularPolygon(5, -90f);
                break;
            case ShapeType.Hexagon:
                BuildRegularPolygon(6, -90f);
                break;
            case ShapeType.Star:
                BuildStar(5);
                break;
            case ShapeType.Star8:
                BuildStar(8);
                break;
            case ShapeType.Arc:
                BuildArc();
                break;
            case ShapeType.Zigzag:
                BuildZigzag();
                break;
            case ShapeType.FigureEight:
                BuildFigureEight();
                break;
            case ShapeType.Spiral:
                BuildSpiral();
                break;
            case ShapeType.Target:
                BuildTarget();
                break;
            case ShapeType.Tricycle:
                BuildTricycle();
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
                    data.width = data.radius * 2f;
                    data.height = data.radius * 2f;
                    break;
                }

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
        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;

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

            case ShapeType.Target:
            case ShapeType.Tricycle:
                return new Vector3(data.center.x + Mathf.Max(0.05f, data.width) * 0.5f, data.center.y, 0f);

            default:
                return new Vector3(
                    data.center.x + Mathf.Max(0.05f, data.width) * 0.5f,
                    data.center.y + Mathf.Max(0.05f, data.height) * 0.5f,
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

        SetClosedPoints(new List<Vector3>
        {
            new Vector3(data.center.x - halfW, data.center.y + halfH, 0f),
            new Vector3(data.center.x + halfW, data.center.y + halfH, 0f),
            new Vector3(data.center.x + halfW, data.center.y - halfH, 0f),
            new Vector3(data.center.x - halfW, data.center.y - halfH, 0f)
        });
    }

    private void BuildCircle()
    {
        data.radius = Mathf.Max(0.05f, data.radius);
        data.width = data.radius * 2f;
        data.height = data.radius * 2f;

        int segments = Mathf.Max(24, circleSegments);
        List<Vector3> generated = new List<Vector3>();

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;

            generated.Add(new Vector3(
                data.center.x + Mathf.Cos(angle) * data.radius,
                data.center.y + Mathf.Sin(angle) * data.radius,
                0f
            ));
        }

        SetClosedPoints(generated);
    }

    private void BuildRegularPolygon(int sides, float rotationDegrees)
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        sides = Mathf.Max(3, sides);
        float rotation = rotationDegrees * Mathf.Deg2Rad;

        List<Vector3> generated = new List<Vector3>();
        for (int i = 0; i < sides; i++)
        {
            float t = i / (float)sides;
            float angle = t * Mathf.PI * 2f + rotation;

            generated.Add(new Vector3(
                data.center.x + Mathf.Cos(angle) * data.width * 0.5f,
                data.center.y + Mathf.Sin(angle) * data.height * 0.5f,
                0f
            ));
        }

        SetClosedPoints(generated);
    }

    private void BuildDiamond()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        float halfW = data.width * 0.5f;
        float halfH = data.height * 0.5f;

        SetClosedPoints(new List<Vector3>
        {
            new Vector3(data.center.x, data.center.y + halfH, 0f),
            new Vector3(data.center.x + halfW, data.center.y, 0f),
            new Vector3(data.center.x, data.center.y - halfH, 0f),
            new Vector3(data.center.x - halfW, data.center.y, 0f)
        });
    }

    private void BuildStar(int rays)
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        rays = Mathf.Max(3, rays);
        int vertexCount = rays * 2;
        float innerScale = 0.45f;
        float rotation = -90f * Mathf.Deg2Rad;

        List<Vector3> generated = new List<Vector3>();
        for (int i = 0; i < vertexCount; i++)
        {
            bool outer = i % 2 == 0;
            float scale = outer ? 1f : innerScale;
            float angle = i / (float)vertexCount * Mathf.PI * 2f + rotation;

            generated.Add(new Vector3(
                data.center.x + Mathf.Cos(angle) * data.width * 0.5f * scale,
                data.center.y + Mathf.Sin(angle) * data.height * 0.5f * scale,
                0f
            ));
        }

        SetClosedPoints(generated);
    }

    private void BuildArc()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        int segments = Mathf.Max(12, circleSegments / 2);
        float startAngle = data.arcStartAngle;
        float endAngle = data.arcEndAngle;

        // Если углы случайно совпали, делаем видимую дугу 180 градусов.
        if (Mathf.Abs(Mathf.DeltaAngle(startAngle, endAngle)) < 1f)
            endAngle = startAngle + 180f;

        List<Vector3> generated = new List<Vector3>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angleDeg = Mathf.LerpAngle(startAngle, endAngle, t);
            float angle = angleDeg * Mathf.Deg2Rad;

            generated.Add(new Vector3(
                data.center.x + Mathf.Cos(angle) * data.width * 0.5f,
                data.center.y + Mathf.Sin(angle) * data.height * 0.5f,
                0f
            ));
        }

        SetOpenPoints(generated);
    }

    private void BuildZigzag()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        // Зигзаг — открытая траектория.
        // Количество переломов фиксируем для стабильного первого варианта.
        // Позже можно вынести этот параметр в настройки фигуры.
        const int pointsCount = 7;
        float halfW = data.width * 0.5f;
        float halfH = data.height * 0.5f;

        List<Vector3> generated = new List<Vector3>();

        for (int i = 0; i < pointsCount; i++)
        {
            float t = i / (float)(pointsCount - 1);
            float x = data.center.x - halfW + data.width * t;
            float y = data.center.y + (i % 2 == 0 ? halfH : -halfH);

            generated.Add(new Vector3(x, y, 0f));
        }

        SetOpenPoints(generated);
    }

    private void BuildFigureEight()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        const int segments = 128;
        List<Vector3> generated = new List<Vector3>();

        // Лемниската / восьмёрка. Точка старта в центре, далее плавный
        // замкнутый маршрут по двум петлям. Масштабируется через width/height.
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;

            float x = Mathf.Sin(t);
            float y = Mathf.Sin(t) * Mathf.Cos(t);

            generated.Add(new Vector3(
                data.center.x + x * data.width * 0.5f,
                data.center.y + y * data.height * 0.5f,
                0f
            ));
        }

        SetClosedPoints(generated);
    }


    private void BuildSpiral()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        const int segments = 160;
        float turns = Mathf.Clamp(data.spiralTurns <= 0f ? 3f : data.spiralTurns, 1f, 8f);

        List<Vector3> generated = new List<Vector3>();

        // Спираль Архимеда: r растёт линейно от центра к внешней границе.
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f * turns;
            float radius01 = t;

            float x = Mathf.Cos(angle) * radius01 * data.width * 0.5f;
            float y = Mathf.Sin(angle) * radius01 * data.height * 0.5f;

            generated.Add(new Vector3(data.center.x + x, data.center.y + y, 0f));
        }

        SetOpenPoints(generated);
    }

    private void BuildTarget()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);

        // Мишень — это не маршрут для обводки, а зона удержания.
        // Поэтому визуально отделяем её от трицикла: центральная зона + кольца + перекрестие.
        // Основной LineRenderer показывает центральную зону удержания.
        const int segments = 96;

        float centerScale = 0.18f;
        List<Vector3> centerRing = BuildEllipseRingPoints(centerScale, segments);
        SetClosedPoints(centerRing);

        // Дополнительные кольца мишени. Они нужны как визуальные уровни отклонения от центра.
        CreateAuxiliaryLineRenderer("AuxLine_TargetRing_Middle", BuildEllipseRingPoints(0.45f, segments), true);
        CreateAuxiliaryLineRenderer("AuxLine_TargetRing_Outer", BuildEllipseRingPoints(0.75f, segments), true);
        CreateAuxiliaryLineRenderer("AuxLine_TargetRing_Max", BuildEllipseRingPoints(1.00f, segments), true);

        // Перекрестие делает мишень визуально отличимой от трицикла.
        float halfW = data.width * 0.5f;
        float halfH = data.height * 0.5f;

        CreateAuxiliaryLineRenderer("AuxLine_TargetHorizontal", new List<Vector3>
        {
            new Vector3(data.center.x - halfW, data.center.y, 0f),
            new Vector3(data.center.x + halfW, data.center.y, 0f)
        }, false);

        CreateAuxiliaryLineRenderer("AuxLine_TargetVertical", new List<Vector3>
        {
            new Vector3(data.center.x, data.center.y - halfH, 0f),
            new Vector3(data.center.x, data.center.y + halfH, 0f)
        }, false);
    }

    private void BuildTricycle()
    {
        data.width = Mathf.Max(0.05f, data.width);
        data.height = Mathf.Max(0.05f, data.height);
        data.ringCount = 3;

        // Трицикл — три вложенные окружности. Визуально показываем все 3 кольца.
        // Маршрут пейсмейкера строится отдельно в TrackGeometryFactory: среднее → внешнее → внутреннее.
        List<List<Vector3>> rings = BuildConcentricRingPoints(3);

        // Основное кольцо — среднее, как в описании протокола.
        SetClosedPoints(rings[1]);
        CreateAuxiliaryLineRenderer("AuxLine_TricycleOuter", rings[2], true);
        CreateAuxiliaryLineRenderer("AuxLine_TricycleInner", rings[0], true);
    }

    private List<Vector3> BuildEllipseRingPoints(float scale, int segments)
    {
        scale = Mathf.Clamp(scale, 0.02f, 1f);
        segments = Mathf.Max(24, segments);

        float radiusX = data.width * 0.5f * scale;
        float radiusY = data.height * 0.5f * scale;

        List<Vector3> generated = new List<Vector3>();

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;

            generated.Add(new Vector3(
                data.center.x + Mathf.Cos(angle) * radiusX,
                data.center.y + Mathf.Sin(angle) * radiusY,
                0f
            ));
        }

        return generated;
    }

    private List<List<Vector3>> BuildConcentricRingPoints(int ringCount)
    {
        ringCount = Mathf.Max(1, ringCount);
        int segments = Mathf.Max(48, circleSegments);

        float maxRadiusX = data.width * 0.5f;
        float maxRadiusY = data.height * 0.5f;

        List<List<Vector3>> rings = new List<List<Vector3>>();

        for (int ring = 1; ring <= ringCount; ring++)
        {
            float scale = ring / (float)ringCount;
            List<Vector3> generated = new List<Vector3>();

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float angle = t * Mathf.PI * 2f;

                generated.Add(new Vector3(
                    data.center.x + Mathf.Cos(angle) * maxRadiusX * scale,
                    data.center.y + Mathf.Sin(angle) * maxRadiusY * scale,
                    0f
                ));
            }

            rings.Add(generated);
        }

        return rings;
    }

    private void CreateAuxiliaryLineRenderer(string objectName, List<Vector3> generated, bool loop)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);

        LineRenderer aux = child.AddComponent<LineRenderer>();
        aux.useWorldSpace = true;
        aux.alignment = LineAlignment.View;
        aux.textureMode = LineTextureMode.Stretch;
        aux.numCapVertices = 8;
        aux.numCornerVertices = 8;
        aux.loop = loop;

        if (lineRenderer != null && lineRenderer.sharedMaterial != null)
            aux.sharedMaterial = lineRenderer.sharedMaterial;
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                aux.sharedMaterial = new Material(shader);
        }

        aux.positionCount = generated.Count;
        for (int i = 0; i < generated.Count; i++)
            aux.SetPosition(i, generated[i]);

        auxiliaryLineRenderers.Add(aux);
        ApplyStyleToRenderer(aux);
    }

    private void ClearAuxiliaryLineRenderers()
    {
        auxiliaryLineRenderers.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name.StartsWith("AuxLine_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    private void SetOpenPoints(List<Vector3> generated)
    {
        points.Clear();
        points.AddRange(generated);

        lineRenderer.loop = false;
        lineRenderer.positionCount = generated.Count;

        for (int i = 0; i < generated.Count; i++)
            lineRenderer.SetPosition(i, generated[i]);
    }

    private void SetClosedPoints(List<Vector3> generated)
    {
        points.Clear();
        points.AddRange(generated);

        lineRenderer.loop = true;
        lineRenderer.positionCount = generated.Count;

        for (int i = 0; i < generated.Count; i++)
            lineRenderer.SetPosition(i, generated[i]);
    }

    private void ApplyStyle()
    {
        if (lineRenderer == null)
            return;

        ApplyStyleToRenderer(lineRenderer);

        for (int i = 0; i < auxiliaryLineRenderers.Count; i++)
        {
            if (auxiliaryLineRenderers[i] != null)
                ApplyStyleToRenderer(auxiliaryLineRenderers[i]);
        }
    }

    private void ApplyStyleToRenderer(LineRenderer renderer)
    {
        if (renderer == null)
            return;

        float baseWidth = Mathf.Max(0.001f, lineThickness * ThicknessToWorldWidth);
        float widthValue = isSelected ? baseWidth * SelectedWidthMultiplier : baseWidth;

        Color normalColor = new Color32(19, 78, 92, 255);
        Color selectedColor = new Color32(39, 199, 217, 255);
        Color finalColor = isSelected ? selectedColor : normalColor;

        renderer.enabled = showTrack;
        renderer.startWidth = widthValue;
        renderer.endWidth = widthValue;
        renderer.startColor = finalColor;
        renderer.endColor = finalColor;
    }
}
