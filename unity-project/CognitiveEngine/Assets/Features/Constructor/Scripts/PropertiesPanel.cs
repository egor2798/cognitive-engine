using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesPanel : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;

    [Header("Roots")]
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private GameObject shapeRoot;
    [SerializeField] private GameObject pathRoot;
    [SerializeField] private GameObject segmentRoot;

    [Header("Shape fields")]
    [SerializeField] private TMP_Text shapeTypeText;
    [SerializeField] private TMP_InputField shapeXInput;
    [SerializeField] private TMP_InputField shapeYInput;
    [SerializeField] private TMP_InputField shapeWidthInput;
    [SerializeField] private TMP_InputField shapeHeightInput;
    [SerializeField] private TMP_InputField shapeRadiusInput;
    [SerializeField] private Button applyShapeButton;

    [Header("Path fields")]
    [SerializeField] private TMP_Text pathInfoText;

    [Header("Segment fields")]
    [SerializeField] private TMP_Text segmentTypeText;
    [SerializeField] private TMP_InputField segmentStartXInput;
    [SerializeField] private TMP_InputField segmentStartYInput;
    [SerializeField] private TMP_InputField segmentEndXInput;
    [SerializeField] private TMP_InputField segmentEndYInput;
    [SerializeField] private Button applySegmentButton;

    private ShapeElement currentShape;
    private PathElement currentPath;
    private SegmentElement currentSegment;

    private void Awake()
    {
        if (applyShapeButton != null)
        {
            applyShapeButton.onClick.RemoveListener(ApplyShapeProperties);
            applyShapeButton.onClick.AddListener(ApplyShapeProperties);
        }

        if (applySegmentButton != null)
        {
            applySegmentButton.onClick.RemoveListener(ApplySegmentProperties);
            applySegmentButton.onClick.AddListener(ApplySegmentProperties);
        }
    }

    private void Start()
    {
        ShowEmpty();
    }

    public void ShowEmpty()
    {
        currentShape = null;
        currentPath = null;
        currentSegment = null;

        if (titleText != null)
            titleText.text = "Свойства";

        if (infoText != null)
        {
            infoText.text = "Ничего не выбрано";
            infoText.gameObject.SetActive(true);
        }

        SetRoots(true, false, false, false);
    }

    // Старое имя оставлено на случай, если где-то уже был вызов ShowShape(...)
    public void ShowShape(ShapeElement shape)
    {
        ShowShapeProperties(shape);
    }

    public void ShowShapeProperties(ShapeElement shape)
    {
        if (shape == null || shape.Data == null)
        {
            ShowEmpty();
            return;
        }

        currentShape = shape;
        currentPath = null;
        currentSegment = null;

        ShapeData data = shape.Data;

        if (titleText != null)
            titleText.text = "Свойства фигуры";

        if (infoText != null)
            infoText.gameObject.SetActive(false);

        if (shapeTypeText != null)
            shapeTypeText.text = "Тип: " + GetShapeLabel(data.shapeType);

        SetInput(shapeXInput, data.center.x);
        SetInput(shapeYInput, data.center.y);
        SetInput(shapeWidthInput, data.width);
        SetInput(shapeHeightInput, data.height);
        SetInput(shapeRadiusInput, data.radius);

        bool isCircle = data.shapeType == ShapeType.Circle;

        if (shapeWidthInput != null)
            shapeWidthInput.interactable = !isCircle;
        if (shapeHeightInput != null)
            shapeHeightInput.interactable = !isCircle;
        if (shapeRadiusInput != null)
            shapeRadiusInput.interactable = isCircle;

        SetRoots(false, true, false, false);
        ForceChildrenActive(shapeRoot);

        Debug.Log("PropertiesPanel: ShowShapeProperties -> " + shape.name + ", shapeRoot active=" + (shapeRoot != null && shapeRoot.activeInHierarchy));
    }

    public void ShowPathProperties(PathElement path)
    {
        if (path == null || path.Data == null)
        {
            ShowEmpty();
            return;
        }

        currentShape = null;
        currentPath = path;
        currentSegment = null;

        if (titleText != null)
            titleText.text = "Свойства траектории";

        if (infoText != null)
            infoText.gameObject.SetActive(false);

        if (pathInfoText != null)
        {
            int count = path.Data.segments != null ? path.Data.segments.Count : 0;
            pathInfoText.text = "Сегментов: " + count;
        }

        SetRoots(false, false, true, false);
    }

    public void ShowSegmentProperties(SegmentElement segment)
    {
        if (segment == null || segment.Data == null)
        {
            ShowEmpty();
            return;
        }

        currentShape = null;
        currentPath = null;
        currentSegment = segment;

        SegmentData data = segment.Data;

        if (titleText != null)
            titleText.text = "Свойства сегмента";

        if (infoText != null)
            infoText.gameObject.SetActive(false);

        if (segmentTypeText != null)
            segmentTypeText.text = data.segmentType == SegmentType.Bezier ? "Тип: Bezier" : "Тип: линия";

        SetInput(segmentStartXInput, data.startPoint.x);
        SetInput(segmentStartYInput, data.startPoint.y);
        SetInput(segmentEndXInput, data.endPoint.x);
        SetInput(segmentEndYInput, data.endPoint.y);

        SetRoots(false, false, false, true);
    }

    public void ApplyShapeProperties()
    {
        if (currentShape == null || currentShape.Data == null)
            return;

        ShapeData data = currentShape.Data;

        data.center = new Vector2(
            ParseFloat(shapeXInput, data.center.x),
            ParseFloat(shapeYInput, data.center.y)
        );

        if (data.shapeType == ShapeType.Circle)
        {
            data.radius = Mathf.Max(0.05f, ParseFloat(shapeRadiusInput, data.radius));
        }
        else
        {
            data.width = Mathf.Max(0.05f, ParseFloat(shapeWidthInput, data.width));
            data.height = Mathf.Max(0.05f, ParseFloat(shapeHeightInput, data.height));
        }

        currentShape.Rebuild();
        ShowShapeProperties(currentShape);
    }

    public void ApplySegmentProperties()
    {
        if (currentSegment == null || currentSegment.Data == null)
            return;

        SegmentData data = currentSegment.Data;

        data.startPoint = new Vector2(
            ParseFloat(segmentStartXInput, data.startPoint.x),
            ParseFloat(segmentStartYInput, data.startPoint.y)
        );

        data.endPoint = new Vector2(
            ParseFloat(segmentEndXInput, data.endPoint.x),
            ParseFloat(segmentEndYInput, data.endPoint.y)
        );

        currentSegment.Rebuild();
        ShowSegmentProperties(currentSegment);
    }


    private string GetShapeLabel(ShapeType shapeType)
    {
        switch (shapeType)
        {
            case ShapeType.Circle:
                return "круг";
            case ShapeType.Square:
                return "квадрат";
            case ShapeType.Triangle:
                return "треугольник";
            case ShapeType.Diamond:
                return "ромб";
            case ShapeType.Pentagon:
                return "пятиугольник";
            case ShapeType.Hexagon:
                return "шестиугольник";
            case ShapeType.Star:
                return "звезда";
            case ShapeType.Star8:
                return "звезда 8 лучей";
            case ShapeType.Arc:
                return "дуга";
            case ShapeType.Zigzag:
                return "зигзаг";
            case ShapeType.FigureEight:
                return "восьмёрка";
            case ShapeType.Spiral:
                return "спираль Архимеда";
            case ShapeType.Target:
                return "мишень";
            case ShapeType.Tricycle:
                return "трицикл";
            default:
                return shapeType.ToString();
        }
    }

    private void SetRoots(bool empty, bool shape, bool path, bool segment)
    {
        if (emptyRoot != null)
            emptyRoot.SetActive(empty);

        if (shapeRoot != null)
            shapeRoot.SetActive(shape);

        if (pathRoot != null)
            pathRoot.SetActive(path);

        if (segmentRoot != null)
            segmentRoot.SetActive(segment);
    }

    private void ForceChildrenActive(GameObject root)
    {
        if (root == null)
            return;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (child != null)
                child.gameObject.SetActive(true);
        }
    }

    private void SetInput(TMP_InputField input, float value)
    {
        if (input != null)
            input.text = value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    private float ParseFloat(TMP_InputField input, float fallback)
    {
        if (input == null)
            return fallback;

        string text = input.text.Replace(',', '.');
        if (float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
            return value;

        return fallback;
    }
}
