using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class TrackEditor : MonoBehaviour
{
    public enum ToolMode
    {
        Select,
        AddPath,
        AddSquare,
        AddCircle,
        AddTriangle,
        AddDiamond,
        AddPentagon,
        AddHexagon,
        AddStar,
        AddStar8,
        AddArc,
        AddZigzag,
        AddFigureEight,
        AddSpiral,
        AddTarget,
        AddTricycle
    }

    [Header("Scene References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform shapesRoot;
    [SerializeField] private Transform pathsRoot;
    [SerializeField] private ShapeElement shapePrefab;
    [SerializeField] private PathElement pathPrefab;
    [SerializeField] private SegmentElement segmentPrefab;
    [SerializeField] private RectTransform workspacePanel;
    [SerializeField] private RectTransform workspaceView;
    [SerializeField] private PropertiesPanel propertiesPanel;
    [SerializeField] private TMP_Text modeText;

    [Header("Default Shape Sizes")]
    [SerializeField] private float defaultSquareWidth = 1f;
    [SerializeField] private float defaultSquareHeight = 1f;
    [SerializeField] private float defaultCircleRadius = 0.5f;

    [Header("Selection")]
    [SerializeField] private float selectionThreshold = 0.3f;
    [SerializeField] private float segmentSelectionThreshold = 0.2f;

    [Header("Navigation")]
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minOrthoSize = 2f;
    [SerializeField] private float maxOrthoSize = 200f;

    [Header("Composite Track")]
    [SerializeField] private CompositeTransitionMode defaultCompositeTransitionMode = CompositeTransitionMode.RandomSideOnStart;

    public ToolMode CurrentTool { get; private set; } = ToolMode.Select;
    public ExerciseData CurrentExercise { get; private set; }

    private ShapeElement selectedShape;
    private SegmentElement selectedSegment;
    private PathElement selectedPath;

    private PathElement activePathElement;
    private PathData activePathData;
    private Vector2? pendingPathStartPoint;

    private readonly List<ShapeElement> createdShapes = new();
    private readonly List<PathElement> createdPaths = new();

    private bool isDraggingShape;
    private bool isDraggingSegment;
    private Vector2 dragOffset;
    private bool isResizingShape;
    private bool isPanningWorkspace;
    private Vector3 lastPanScreenPosition;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        CreateNewExercise();
        UpdateModeText();
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        HandleHotkeys();
        HandleWorkspacePan();
        HandleWorkspaceZoom();

        Vector3 worldPoint3 = GetMouseWorldPoint();
        Vector2 worldPoint = new Vector2(worldPoint3.x, worldPoint3.y);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            if (CurrentTool == ToolMode.Select)
            {
                if (TryStartResize(worldPoint))
                    return;

                if (TryStartDrag(worldPoint))
                    return;
            }

            HandleLeftClick(worldPoint3);
        }

        if (Mouse.current.leftButton.isPressed)
        {
            if (isResizingShape && selectedShape != null)
            {
                selectedShape.UpdateResizeFromWorldPoint(new Vector3(worldPoint.x, worldPoint.y, 0f));
            }
            else if (isDraggingShape && selectedShape != null)
            {
                DragSelectedShape(worldPoint);
            }
            else if (isDraggingSegment && selectedSegment != null)
            {
                DragSelectedSegment(worldPoint);
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDraggingShape = false;
            isDraggingSegment = false;
            isResizingShape = false;
        }

        if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            DeleteSelection();
        }
    }

    private void HandleHotkeys()
    {
        if (Keyboard.current == null)
            return;

        bool ctrlPressed =
            Keyboard.current.leftCtrlKey.isPressed ||
            Keyboard.current.rightCtrlKey.isPressed;

        if (ctrlPressed && Keyboard.current.zKey.wasPressedThisFrame)
        {
            UndoLastCreatedObject();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            FinishActivePath();
            SetTool(ToolMode.Select);
        }
    }

    private void HandleWorkspaceZoom()
    {
        if (mainCamera == null || Mouse.current == null || Keyboard.current == null)
            return;

        if (!IsPointerInsideWorkspace())
            return;

        bool ctrlPressed =
            Keyboard.current.leftCtrlKey.isPressed ||
            Keyboard.current.rightCtrlKey.isPressed;

        if (!ctrlPressed)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        float newSize = mainCamera.orthographicSize - scroll * zoomSpeed * 0.01f;
        newSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);

        mainCamera.orthographicSize = newSize;
    }

    private void HandleWorkspacePan()
    {
        if (mainCamera == null || Mouse.current == null)
            return;

        if (!IsPointerInsideWorkspace())
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isPanningWorkspace = true;
            lastPanScreenPosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.isPressed && isPanningWorkspace)
        {
            Vector3 currentScreenPosition = Mouse.current.position.ReadValue();

            Vector3 previousWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(lastPanScreenPosition.x, lastPanScreenPosition.y, -mainCamera.transform.position.z)
            );

            Vector3 currentWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(currentScreenPosition.x, currentScreenPosition.y, -mainCamera.transform.position.z)
            );

            Vector3 delta = previousWorld - currentWorld;
            delta.z = 0f;

            mainCamera.transform.position += delta;
            lastPanScreenPosition = currentScreenPosition;
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isPanningWorkspace = false;
        }
    }

    private bool IsPointerInsideWorkspace()
    {
        if (workspaceView == null || Mouse.current == null)
            return false;

        Vector2 mouse = Mouse.current.position.ReadValue();

        Rect rect = workspaceView.rect;

        Vector2 min = workspaceView.TransformPoint(rect.min);
        Vector2 max = workspaceView.TransformPoint(rect.max);

        return mouse.x >= min.x &&
               mouse.x <= max.x &&
               mouse.y >= min.y &&
               mouse.y <= max.y;
    }

    public void SetTool(ToolMode toolMode)
    {
        if (CurrentTool == ToolMode.AddPath && toolMode != ToolMode.AddPath)
            FinishActivePath();

        isDraggingShape = false;
        isDraggingSegment = false;
        isResizingShape = false;
        CurrentTool = toolMode;
        UpdateModeText();
    }

    public void SetSelectTool()
    {
        SetTool(ToolMode.Select);
    }

    public void SetLineTool()
    {
        SetTool(ToolMode.AddPath);
    }

    public void SetSquareTool()
    {
        SetTool(ToolMode.AddSquare);
    }

    public void SetCircleTool()
    {
        SetTool(ToolMode.AddCircle);
    }

    public void SetTriangleTool()
    {
        SetTool(ToolMode.AddTriangle);
    }

    public void SetDiamondTool()
    {
        SetTool(ToolMode.AddDiamond);
    }

    public void SetPentagonTool()
    {
        SetTool(ToolMode.AddPentagon);
    }

    public void SetHexagonTool()
    {
        SetTool(ToolMode.AddHexagon);
    }

    public void SetStarTool()
    {
        SetTool(ToolMode.AddStar);
    }

    public void SetStar8Tool()
    {
        SetTool(ToolMode.AddStar8);
    }

    public void SetArcTool()
    {
        SetTool(ToolMode.AddArc);
    }

    public void SetZigzagTool()
    {
        SetTool(ToolMode.AddZigzag);
    }

    public void SetFigureEightTool()
    {
        SetTool(ToolMode.AddFigureEight);
    }

    public void SetSpiralTool()
    {
        SetTool(ToolMode.AddSpiral);
    }

    public void SetTargetTool()
    {
        SetTool(ToolMode.AddTarget);
    }

    public void SetTricycleTool()
    {
        SetTool(ToolMode.AddTricycle);
    }

    private void UpdateModeText()
    {
        if (modeText == null)
            return;

        modeText.text = "Режим: " + GetToolLabel(CurrentTool);
    }

    private string GetToolLabel(ToolMode toolMode)
    {
        switch (toolMode)
        {
            case ToolMode.Select:
                return "Select";
            case ToolMode.AddPath:
                return "Line";
            case ToolMode.AddSquare:
                return "Square";
            case ToolMode.AddCircle:
                return "Circle";
            case ToolMode.AddTriangle:
                return "Triangle";
            case ToolMode.AddDiamond:
                return "Diamond";
            case ToolMode.AddPentagon:
                return "Pentagon";
            case ToolMode.AddHexagon:
                return "Hexagon";
            case ToolMode.AddStar:
                return "Star";
            case ToolMode.AddStar8:
                return "Star 8";
            case ToolMode.AddArc:
                return "Arc";
            case ToolMode.AddZigzag:
                return "Zigzag";
            case ToolMode.AddFigureEight:
                return "Figure 8";
            case ToolMode.AddSpiral:
                return "Spiral";
            case ToolMode.AddTarget:
                return "Target";
            case ToolMode.AddTricycle:
                return "Tricycle";
            default:
                return toolMode.ToString();
        }
    }

    public void ClearAllShapes()
    {
        ClearSelection();
        FinishActivePath();

        for (int i = shapesRoot.childCount - 1; i >= 0; i--)
            Destroy(shapesRoot.GetChild(i).gameObject);

        for (int i = pathsRoot.childCount - 1; i >= 0; i--)
            Destroy(pathsRoot.GetChild(i).gameObject);

        CurrentExercise.shapes.Clear();
        CurrentExercise.paths.Clear();

        createdShapes.Clear();
        createdPaths.Clear();
    }

    public void DeleteSelection()
    {
        if (selectedSegment != null && selectedPath != null)
        {
            DeleteSelectedSegment();
            return;
        }

        if (selectedShape != null)
        {
            DeleteSelectedShape();
        }
    }

    public void DeleteSelectedShape()
    {
        if (selectedShape == null || selectedShape.Data == null)
            return;

        CurrentExercise.shapes.Remove(selectedShape.Data);
        createdShapes.Remove(selectedShape);

        Destroy(selectedShape.gameObject);
        selectedShape = null;

        if (propertiesPanel != null)
            propertiesPanel.ShowEmpty();
    }

    private void DeleteSelectedSegment()
    {
        if (selectedSegment == null || selectedPath == null || selectedPath.Data == null || selectedSegment.Data == null)
            return;

        selectedPath.Data.segments.Remove(selectedSegment.Data);
        selectedPath.RemoveSegmentElement(selectedSegment);

        Destroy(selectedSegment.gameObject);

        selectedSegment = null;

        if (propertiesPanel != null)
            propertiesPanel.ShowEmpty();

        if (selectedPath.Data.segments.Count == 0)
        {
            CurrentExercise.paths.Remove(selectedPath.Data);
            createdPaths.Remove(selectedPath);

            Destroy(selectedPath.gameObject);
            selectedPath = null;
        }
    }

    public void UndoLastCreatedObject()
    {
        if (activePathData != null && activePathData.segments.Count > 0)
        {
            UndoLastPathSegment();
            return;
        }

        if (createdShapes.Count > 0)
        {
            ShapeElement lastShape = createdShapes[^1];
            createdShapes.RemoveAt(createdShapes.Count - 1);

            if (lastShape != null)
            {
                if (selectedShape == lastShape)
                    selectedShape = null;

                if (lastShape.Data != null)
                    CurrentExercise.shapes.Remove(lastShape.Data);

                Destroy(lastShape.gameObject);
            }

            return;
        }

        if (createdPaths.Count > 0)
        {
            PathElement lastPath = createdPaths[^1];
            createdPaths.RemoveAt(createdPaths.Count - 1);

            if (lastPath != null && lastPath.Data != null)
                CurrentExercise.paths.Remove(lastPath.Data);

            if (lastPath != null)
                Destroy(lastPath.gameObject);
        }
    }

    private void UndoLastPathSegment()
    {
        if (activePathData == null || activePathElement == null)
            return;

        if (activePathData.segments.Count == 0)
            return;

        int lastIndex = activePathData.segments.Count - 1;
        activePathData.segments.RemoveAt(lastIndex);

        var segmentElements = activePathElement.SegmentElements;
        if (segmentElements.Count > 0)
        {
            SegmentElement lastSegment = segmentElements[segmentElements.Count - 1];
            activePathElement.RemoveSegmentElement(lastSegment);
            Destroy(lastSegment.gameObject);
        }

        if (activePathData.segments.Count == 0)
        {
            CurrentExercise.paths.Remove(activePathData);
            createdPaths.Remove(activePathElement);
            Destroy(activePathElement.gameObject);

            activePathData = null;
            activePathElement = null;
            pendingPathStartPoint = null;
        }
        else
        {
            SegmentData lastSegment = activePathData.segments[^1];
            pendingPathStartPoint = lastSegment.endPoint;
        }
    }

    public void CreateNewExercise()
    {
        CurrentExercise = new ExerciseData
        {
            id = Guid.NewGuid().ToString(),
            name = "New Exercise",
            shapes = new List<ShapeData>(),
            paths = new List<PathData>()
        };

        ClearSelection();
        activePathData = null;
        activePathElement = null;
        pendingPathStartPoint = null;
    }

    private void HandleLeftClick(Vector3 worldPoint)
    {
        switch (CurrentTool)
        {
            case ToolMode.Select:
                FinishActivePath();
                TrySelect(worldPoint);
                break;

            case ToolMode.AddPath:
                AddPathPoint(worldPoint);
                break;

            case ToolMode.AddSquare:
                FinishActivePath();
                CreateSquare(worldPoint);
                break;

            case ToolMode.AddCircle:
                FinishActivePath();
                CreateCircle(worldPoint);
                break;

            case ToolMode.AddTriangle:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Triangle, "Triangle");
                break;

            case ToolMode.AddDiamond:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Diamond, "Diamond");
                break;

            case ToolMode.AddPentagon:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Pentagon, "Pentagon");
                break;

            case ToolMode.AddHexagon:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Hexagon, "Hexagon");
                break;

            case ToolMode.AddStar:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Star, "Star");
                break;

            case ToolMode.AddStar8:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Star8, "Star8");
                break;

            case ToolMode.AddArc:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Arc, "Arc");
                break;

            case ToolMode.AddZigzag:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Zigzag, "Zigzag");
                break;

            case ToolMode.AddFigureEight:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.FigureEight, "FigureEight");
                break;

            case ToolMode.AddSpiral:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Spiral, "Spiral");
                break;

            case ToolMode.AddTarget:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Target, "Target");
                break;

            case ToolMode.AddTricycle:
                FinishActivePath();
                CreateShapeByType(worldPoint, ShapeType.Tricycle, "Tricycle");
                break;
        }
    }

    private void CreateSquare(Vector3 centerPoint)
    {
        ShapeData data = new ShapeData
        {
            id = Guid.NewGuid().ToString(),
            shapeType = ShapeType.Square,
            center = new Vector2(centerPoint.x, centerPoint.y),
            width = defaultSquareWidth,
            height = defaultSquareHeight
        };

        CreateShape(data, "Square");
    }

    private void CreateCircle(Vector3 centerPoint)
    {
        ShapeData data = new ShapeData
        {
            id = Guid.NewGuid().ToString(),
            shapeType = ShapeType.Circle,
            center = new Vector2(centerPoint.x, centerPoint.y),
            radius = defaultCircleRadius,
            width = defaultCircleRadius * 2f,
            height = defaultCircleRadius * 2f
        };

        CreateShape(data, "Circle");
    }

    private void CreateShapeByType(Vector3 centerPoint, ShapeType shapeType, string prefix)
    {
        ShapeData data = new ShapeData
        {
            id = Guid.NewGuid().ToString(),
            shapeType = shapeType,
            center = new Vector2(centerPoint.x, centerPoint.y),
            width = defaultSquareWidth,
            height = defaultSquareHeight,
            radius = Mathf.Min(defaultSquareWidth, defaultSquareHeight) * 0.5f
        };

        CreateShape(data, prefix);
    }

    private void CreateShape(ShapeData data, string prefix)
    {
        ApplyBodyPointBindingToShape(data);

        CurrentExercise.shapes.Add(data);

        ShapeElement instance = Instantiate(shapePrefab, shapesRoot);
        instance.name = $"{prefix}_{CurrentExercise.shapes.Count}";
        instance.Initialize(data);

        createdShapes.Add(instance);

        // После создания сразу выбираем фигуру и показываем её реальные размеры справа.
        ClearSelection();
        selectedShape = instance;
        selectedShape.SetSelected(true);

        if (propertiesPanel != null)
            propertiesPanel.ShowShapeProperties(selectedShape);
    }

    private void AddPathPoint(Vector3 point)
    {
        Vector2 point2 = new Vector2(point.x, point.y);

        if (activePathData == null || activePathElement == null)
        {
            StartNewPath();
            pendingPathStartPoint = point2;
            return;
        }

        if (!pendingPathStartPoint.HasValue)
        {
            pendingPathStartPoint = point2;
            return;
        }

        Vector2 start = pendingPathStartPoint.Value;
        Vector2 end = point2;

        if (Vector2.Distance(start, end) < 0.001f)
            return;

        SegmentData segmentData = new SegmentData
        {
            id = Guid.NewGuid().ToString(),
            segmentType = SegmentType.Line,
            startPoint = start,
            endPoint = end
        };

        activePathData.segments.Add(segmentData);

        SegmentElement segmentInstance = Instantiate(segmentPrefab, activePathElement.transform);
        segmentInstance.name = $"Segment_{activePathData.segments.Count}";
        segmentInstance.Initialize(segmentData);

        activePathElement.AddSegmentElement(segmentInstance);

        pendingPathStartPoint = end;
    }

    private void StartNewPath()
    {
        PathData pathData = new PathData
        {
            id = Guid.NewGuid().ToString(),
            segments = new List<SegmentData>()
        };

        ApplyBodyPointBindingToPath(pathData);

        CurrentExercise.paths.Add(pathData);

        PathElement pathInstance = Instantiate(pathPrefab, pathsRoot);
        pathInstance.name = $"Path_{CurrentExercise.paths.Count}";
        pathInstance.Initialize(pathData);

        activePathData = pathData;
        activePathElement = pathInstance;

        createdPaths.Add(pathInstance);
    }

    private void FinishActivePath()
    {
        if (activePathData != null && activePathData.segments.Count == 0)
        {
            CurrentExercise.paths.Remove(activePathData);

            if (activePathElement != null)
            {
                createdPaths.Remove(activePathElement);
                Destroy(activePathElement.gameObject);
            }
        }

        activePathData = null;
        activePathElement = null;
        pendingPathStartPoint = null;
    }

    private void TrySelect(Vector3 worldPoint)
    {
        ClearSelection();

        SegmentElement nearestSegment = null;
        PathElement nearestSegmentPath = null;
        float minSegmentDistance = float.MaxValue;

        for (int i = 0; i < pathsRoot.childCount; i++)
        {
            PathElement path = pathsRoot.GetChild(i).GetComponent<PathElement>();
            if (path == null)
                continue;

            var segments = path.SegmentElements;
            for (int j = 0; j < segments.Count; j++)
            {
                SegmentElement segment = segments[j];
                if (segment == null)
                    continue;

                float distance = segment.GetDistanceToPoint(new Vector2(worldPoint.x, worldPoint.y));
                if (distance < minSegmentDistance)
                {
                    minSegmentDistance = distance;
                    nearestSegment = segment;
                    nearestSegmentPath = path;
                }
            }
        }

        if (nearestSegment != null && minSegmentDistance <= segmentSelectionThreshold)
        {
            selectedSegment = nearestSegment;
            selectedPath = nearestSegmentPath;
            selectedSegment.SetSelected(true);

            if (propertiesPanel != null)
                propertiesPanel.ShowSegmentProperties(selectedSegment);

            return;
        }

        ShapeElement nearestShape = null;
        float minShapeDistance = float.MaxValue;

        for (int i = 0; i < shapesRoot.childCount; i++)
        {
            ShapeElement shape = shapesRoot.GetChild(i).GetComponent<ShapeElement>();
            if (shape == null)
                continue;

            float distance = GetDistanceToShape(shape, worldPoint);
            if (distance < minShapeDistance)
            {
                minShapeDistance = distance;
                nearestShape = shape;
            }
        }

        if (nearestShape != null && minShapeDistance <= selectionThreshold)
        {
            selectedShape = nearestShape;
            selectedShape.SetSelected(true);

            if (propertiesPanel != null)
                propertiesPanel.ShowShapeProperties(selectedShape);
        }
        else if (propertiesPanel != null)
        {
            propertiesPanel.ShowEmpty();
        }
    }

    private bool TryStartResize(Vector2 worldPoint)
    {
        if (selectedShape == null)
            return false;

        if (!selectedShape.SupportsResizeHandle())
            return false;

        if (!selectedShape.IsPointNearResizeHandle(new Vector3(worldPoint.x, worldPoint.y, 0f), 0.2f))
            return false;

        isResizingShape = true;
        isDraggingShape = false;
        isDraggingSegment = false;
        return true;
    }

    private bool TryStartDrag(Vector2 worldPoint)
    {
        ClearSelection();
        TrySelect(new Vector3(worldPoint.x, worldPoint.y, 0f));

        if (selectedShape != null && selectedShape.Data != null)
        {
            switch (selectedShape.Data.shapeType)
            {
                case ShapeType.Square:
                case ShapeType.Circle:
                case ShapeType.Triangle:
                case ShapeType.Diamond:
                case ShapeType.Pentagon:
                case ShapeType.Hexagon:
                case ShapeType.Star:
                case ShapeType.Star8:
                case ShapeType.Arc:
                case ShapeType.Zigzag:
                case ShapeType.FigureEight:
                case ShapeType.Spiral:
                case ShapeType.Target:
                case ShapeType.Tricycle:
                    dragOffset = worldPoint - selectedShape.Data.center;
                    isDraggingShape = true;
                    isDraggingSegment = false;
                    isResizingShape = false;
                    return true;
            }
        }

        if (selectedSegment != null && selectedSegment.Data != null)
        {
            Vector2 center = GetSegmentCenter(selectedSegment.Data);
            dragOffset = worldPoint - center;
            isDraggingSegment = true;
            isDraggingShape = false;
            isResizingShape = false;
            return true;
        }

        return false;
    }

    private void DragSelectedShape(Vector2 worldPoint)
    {
        if (selectedShape == null || selectedShape.Data == null)
            return;

        Vector2 targetCenter = worldPoint - dragOffset;
        selectedShape.Data.center = targetCenter;
        selectedShape.Rebuild();

        if (propertiesPanel != null)
            propertiesPanel.ShowShapeProperties(selectedShape);
    }

    private void DragSelectedSegment(Vector2 worldPoint)
    {
        if (selectedSegment == null || selectedSegment.Data == null)
            return;

        Vector2 oldCenter = GetSegmentCenter(selectedSegment.Data);
        Vector2 newCenter = worldPoint - dragOffset;
        Vector2 delta = newCenter - oldCenter;

        selectedSegment.Data.startPoint += delta;
        selectedSegment.Data.endPoint += delta;

        if (selectedSegment.Data.segmentType == SegmentType.Bezier)
        {
            selectedSegment.Data.controlPoint1 += delta;
            selectedSegment.Data.controlPoint2 += delta;
        }

        selectedSegment.Rebuild();

        if (propertiesPanel != null)
            propertiesPanel.ShowSegmentProperties(selectedSegment);
    }

    private Vector2 GetSegmentCenter(SegmentData data)
    {
        return (data.startPoint + data.endPoint) * 0.5f;
    }

    private void ClearSelection()
    {
        if (selectedShape != null)
            selectedShape.SetSelected(false);

        if (selectedSegment != null)
            selectedSegment.SetSelected(false);

        selectedShape = null;
        selectedSegment = null;
        selectedPath = null;

        isDraggingShape = false;
        isDraggingSegment = false;
        isResizingShape = false;

        if (propertiesPanel != null)
            propertiesPanel.ShowEmpty();
    }

    public void ApplyVisualSettingsFromCurrentExercise()
    {
        if (CurrentExercise == null || CurrentExercise.settings == null)
            return;

        for (int i = 0; i < shapesRoot.childCount; i++)
        {
            Transform child = shapesRoot.GetChild(i);
            if (child != null)
                child.gameObject.SendMessage("ApplyExerciseVisualSettings", CurrentExercise.settings, SendMessageOptions.DontRequireReceiver);
        }

        for (int i = 0; i < pathsRoot.childCount; i++)
        {
            PathElement path = pathsRoot.GetChild(i).GetComponent<PathElement>();
            if (path == null)
                continue;

            var segments = path.SegmentElements;
            for (int j = 0; j < segments.Count; j++)
            {
                SegmentElement segment = segments[j];
                if (segment != null)
                    segment.gameObject.SendMessage("ApplyExerciseVisualSettings", CurrentExercise.settings, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private float GetDistanceToShape(ShapeElement shape, Vector3 point)
    {
        if (shape == null || shape.Data == null)
            return float.MaxValue;

        Vector2 p = new Vector2(point.x, point.y);

        TrackGeometry geometry = TrackGeometryFactory.FromShape(shape.Data);
        if (!geometry.IsValid())
            return float.MaxValue;

        return geometry.GetClosestPoint(p).distance;
    }

    private float GetDistanceToSquare(ShapeData data, Vector2 point)
    {
        float halfW = data.width / 2f;
        float halfH = data.height / 2f;

        float minX = data.center.x - halfW;
        float maxX = data.center.x + halfW;
        float minY = data.center.y - halfH;
        float maxY = data.center.y + halfH;

        bool inside = point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        if (inside)
            return 0f;

        float clampedX = Mathf.Clamp(point.x, minX, maxX);
        float clampedY = Mathf.Clamp(point.y, minY, maxY);

        return Vector2.Distance(point, new Vector2(clampedX, clampedY));
    }

    private float GetDistanceToCircle(ShapeData data, Vector2 point)
    {
        float distanceToCenter = Vector2.Distance(point, data.center);

        if (distanceToCenter <= data.radius)
            return 0f;

        return distanceToCenter - data.radius;
    }

    private float GetDistanceToPolyline(IReadOnlyList<Vector3> points, Vector2 point)
    {
        if (points == null || points.Count == 0)
            return float.MaxValue;

        if (points.Count == 1)
            return Vector2.Distance(new Vector2(points[0].x, points[0].y), point);

        float minDistance = float.MaxValue;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = new Vector2(points[i].x, points[i].y);
            Vector2 b = new Vector2(points[i + 1].x, points[i + 1].y);

            float distance = DistancePointToSegment(point, a, b);
            if (distance < minDistance)
                minDistance = distance;
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


    public void CreateCompositeTrackFromAllShapes()
    {
        FinishActivePath();

        if (CurrentExercise == null)
            return;

        if (CurrentExercise.shapes == null || CurrentExercise.shapes.Count < 2)
        {
            Debug.LogWarning("Объединение: нужно минимум 2 фигуры.");
            return;
        }

        List<ShapeData> sourceShapes = new List<ShapeData>();
        for (int i = 0; i < CurrentExercise.shapes.Count; i++)
        {
            ShapeData shape = CurrentExercise.shapes[i];
            if (shape != null)
                sourceShapes.Add(shape);
        }

        if (sourceShapes.Count < 2)
        {
            Debug.LogWarning("Объединение: не найдено достаточно фигур.");
            return;
        }

        CompositeTrackData composite = new CompositeTrackData
        {
            id = Guid.NewGuid().ToString(),
            name = "Composite Track",
            transitionMode = defaultCompositeTransitionMode,
            randomSeed = 0,
            routeSteps = 700
        };

        ApplyBodyPointBindingToComposite(composite);

        for (int i = 0; i < sourceShapes.Count; i++)
        {
            if (!string.IsNullOrEmpty(sourceShapes[i].id))
                composite.shapeIds.Add(sourceShapes[i].id);
        }

        TrackGeometry geometry = CompositeTrackGeometryBuilder.Build(composite, sourceShapes);
        if (geometry == null || !geometry.IsValid() || geometry.Points == null || geometry.Points.Count < 2)
        {
            Debug.LogWarning("Объединение: не удалось построить общий маршрут. Исходные фигуры оставлены без изменений.");
            return;
        }

        int touchedShapeCount = CountShapesTouchedByRoute(geometry, sourceShapes);
        if (touchedShapeCount < 2)
        {
            Debug.LogWarning("Объединение отменено: построенный маршрут относится только к одной фигуре. Исходные фигуры оставлены без изменений.");
            return;
        }

        if (CurrentExercise.compositeTracks == null)
            CurrentExercise.compositeTracks = new List<CompositeTrackData>();

        // Сейчас один активный составной трек на упражнение.
        // Это безопаснее, чем превращать замкнутые фигуры в обычный PathData:
        // исходные фигуры остаются видимыми и не ломаются, а пейсмейкер получает общий маршрут.
        CurrentExercise.compositeTracks.Clear();
        CurrentExercise.compositeTracks.Add(composite);

        AssignCompositeToFirstPacemaker(composite.id);

        ClearSelection();
        SetTool(ToolMode.Select);

        Debug.Log("Объединение выполнено: создан CompositeTrack из фигур: " + sourceShapes.Count);
    }

    private int CountShapesTouchedByRoute(TrackGeometry routeGeometry, List<ShapeData> sourceShapes)
    {
        if (routeGeometry == null || sourceShapes == null || sourceShapes.Count == 0)
            return 0;

        IReadOnlyList<Vector2> routePoints = routeGeometry.Points;
        if (routePoints == null || routePoints.Count < 2)
            return 0;

        const float tolerance = 0.04f;
        const float minLengthOnShape = 0.10f;

        int touchedCount = 0;

        for (int shapeIndex = 0; shapeIndex < sourceShapes.Count; shapeIndex++)
        {
            TrackGeometry shapeGeometry = TrackGeometryFactory.FromShape(sourceShapes[shapeIndex]);
            if (shapeGeometry == null || !shapeGeometry.IsValid())
                continue;

            float lengthNearShape = 0f;

            for (int i = 0; i < routePoints.Count - 1; i++)
            {
                Vector2 a = routePoints[i];
                Vector2 b = routePoints[i + 1];
                float segmentLength = Vector2.Distance(a, b);

                if (segmentLength < 0.0001f)
                    continue;

                Vector2 mid = (a + b) * 0.5f;
                ClosestPointInfo closest = shapeGeometry.GetClosestPoint(mid);

                if (closest.distance <= tolerance)
                    lengthNearShape += segmentLength;
            }

            if (lengthNearShape >= minLengthOnShape)
                touchedCount++;
        }

        return touchedCount;
    }

    private PathData CreatePathDataFromGeometry(TrackGeometry geometry)
    {
        PathData path = new PathData
        {
            id = Guid.NewGuid().ToString(),
            segments = new List<SegmentData>()
        };

        IReadOnlyList<Vector2> points = geometry.Points;
        if (points == null || points.Count < 2)
            return path;

        Vector2 previous = points[0];

        for (int i = 1; i < points.Count; i++)
        {
            Vector2 current = points[i];
            if (Vector2.Distance(previous, current) < 0.001f)
                continue;

            path.segments.Add(new SegmentData
            {
                id = Guid.NewGuid().ToString(),
                segmentType = SegmentType.Line,
                startPoint = previous,
                endPoint = current
            });

            previous = current;
        }

        return path;
    }

    private void CreatePathElementFromData(PathData pathData, string prefix)
    {
        if (pathData == null || pathPrefab == null || segmentPrefab == null || pathsRoot == null)
            return;

        PathElement pathInstance = Instantiate(pathPrefab, pathsRoot);
        pathInstance.name = prefix + "_" + CurrentExercise.paths.Count;
        pathInstance.Initialize(pathData);

        for (int i = 0; i < pathData.segments.Count; i++)
        {
            SegmentData segmentData = pathData.segments[i];
            if (segmentData == null)
                continue;

            SegmentElement segmentInstance = Instantiate(segmentPrefab, pathInstance.transform);
            segmentInstance.name = "Segment_" + (i + 1);
            segmentInstance.Initialize(segmentData);
            pathInstance.AddSegmentElement(segmentInstance);
        }

        createdPaths.Add(pathInstance);
    }

    public void SetCompositeTransitionModeFromDropdown(int optionIndex)
    {
        CompositeTransitionMode mode;

        switch (optionIndex)
        {
            case 0:
                mode = CompositeTransitionMode.FixedRoute;
                break;
            case 1:
                mode = CompositeTransitionMode.RandomSideOnStart;
                break;
            case 2:
                mode = CompositeTransitionMode.RandomSideAtEachIntersection;
                break;
            default:
                mode = CompositeTransitionMode.RandomSideOnStart;
                break;
        }

        SetActiveCompositeTransitionMode(mode);
    }

    public void SetCompositeTransitionModeFixedRoute()
    {
        SetActiveCompositeTransitionMode(CompositeTransitionMode.FixedRoute);
    }

    public void SetCompositeTransitionModeRandomSideOnStart()
    {
        SetActiveCompositeTransitionMode(CompositeTransitionMode.RandomSideOnStart);
    }

    public void SetCompositeTransitionModeRandomSideAtEachIntersection()
    {
        SetActiveCompositeTransitionMode(CompositeTransitionMode.RandomSideAtEachIntersection);
    }

    public void SetActiveCompositeTransitionMode(CompositeTransitionMode mode)
    {
        defaultCompositeTransitionMode = mode;

        if (CurrentExercise == null || CurrentExercise.compositeTracks == null || CurrentExercise.compositeTracks.Count == 0)
        {
            Debug.Log("Composite transition mode set as default for next composite track: " + mode);
            return;
        }

        // Сейчас в упражнении используется один активный составной трек.
        CurrentExercise.compositeTracks[0].transitionMode = mode;

        Debug.Log("Composite transition mode updated: " + mode);
    }

    public string GetActiveCompositeTransitionModeLabel()
    {
        CompositeTransitionMode mode = defaultCompositeTransitionMode;

        if (CurrentExercise != null && CurrentExercise.compositeTracks != null && CurrentExercise.compositeTracks.Count > 0)
            mode = CurrentExercise.compositeTracks[0].transitionMode;

        switch (mode)
        {
            case CompositeTransitionMode.FixedRoute:
                return "Фиксированный маршрут";
            case CompositeTransitionMode.RandomSideOnStart:
                return "Случайная сторона при запуске";
            case CompositeTransitionMode.RandomSideAtEachIntersection:
                return "Случайная сторона на каждом переходе";
            case CompositeTransitionMode.Sequential:
                return "Последовательный режим";
            default:
                return mode.ToString();
        }
    }

    private void RemoveSourceShapesAfterComposite()
    {
        if (CurrentExercise != null && CurrentExercise.shapes != null)
            CurrentExercise.shapes.Clear();

        for (int i = shapesRoot.childCount - 1; i >= 0; i--)
            Destroy(shapesRoot.GetChild(i).gameObject);

        createdShapes.Clear();
        selectedShape = null;
    }

    private void AssignCompositeToFirstPacemaker(string compositeId)
    {
        if (string.IsNullOrEmpty(compositeId))
            return;

        if (CurrentExercise.settings == null)
            CurrentExercise.settings = new ExerciseSettingsData();

        CurrentExercise.settings.usePacemaker = true;

        if (CurrentExercise.settings.pacemakers == null)
            CurrentExercise.settings.pacemakers = new List<PacemakerSettingsData>();

        PacemakerSettingsData pacemaker;

        if (CurrentExercise.settings.pacemakers.Count == 0)
        {
            pacemaker = new PacemakerSettingsData
            {
                id = Guid.NewGuid().ToString(),
                enabled = true,
                speed = 300f,
                loopMode = PacemakerLoopMode.Loop,
                targetIds = new List<string>()
            };

            CurrentExercise.settings.pacemakers.Add(pacemaker);
        }
        else
        {
            pacemaker = CurrentExercise.settings.pacemakers[0];
            if (pacemaker.targetIds == null)
                pacemaker.targetIds = new List<string>();
        }

        pacemaker.enabled = true;
        pacemaker.targetIds.Clear();
        pacemaker.targetIds.Add(compositeId);
    }

    private void AssignPathToFirstPacemaker(string pathId)
    {
        if (string.IsNullOrEmpty(pathId))
            return;

        if (CurrentExercise.settings == null)
            CurrentExercise.settings = new ExerciseSettingsData();

        CurrentExercise.settings.usePacemaker = true;

        if (CurrentExercise.settings.pacemakers == null)
            CurrentExercise.settings.pacemakers = new List<PacemakerSettingsData>();

        PacemakerSettingsData pacemaker;

        if (CurrentExercise.settings.pacemakers.Count == 0)
        {
            pacemaker = new PacemakerSettingsData
            {
                id = Guid.NewGuid().ToString(),
                enabled = true,
                speed = 300f,
                loopMode = PacemakerLoopMode.Loop,
                targetIds = new List<string>()
            };

            CurrentExercise.settings.pacemakers.Add(pacemaker);
        }
        else
        {
            pacemaker = CurrentExercise.settings.pacemakers[0];
            if (pacemaker.targetIds == null)
                pacemaker.targetIds = new List<string>();
        }

        pacemaker.enabled = true;
        pacemaker.targetIds.Clear();
        pacemaker.targetIds.Add(pathId);
    }

    private BodyPointBindingData GetPrimaryBodyPointBinding()
    {
        if (CurrentExercise != null &&
            CurrentExercise.settings != null &&
            CurrentExercise.settings.bodyPointBindings != null &&
            CurrentExercise.settings.bodyPointBindings.Count > 0 &&
            CurrentExercise.settings.bodyPointBindings[0] != null)
        {
            return CurrentExercise.settings.bodyPointBindings[0];
        }

        return new BodyPointBindingData
        {
            bodyPointId = BodyPointId.RightForearm,
            bodyPointZoneId = "zone_right_forearm",
            trackerId = "WT901_01",
            cursorId = "cursor_1"
        };
    }

    private void ApplyBodyPointBindingToShape(ShapeData shape)
    {
        if (shape == null)
            return;

        BodyPointBindingData binding = GetPrimaryBodyPointBinding();

        shape.bodyPointId = binding.bodyPointId;
        shape.bodyPointZoneId = binding.bodyPointZoneId;
        shape.trackerId = binding.trackerId;
        shape.cursorId = binding.cursorId;
    }

    private void ApplyBodyPointBindingToPath(PathData path)
    {
        if (path == null)
            return;

        BodyPointBindingData binding = GetPrimaryBodyPointBinding();

        path.bodyPointId = binding.bodyPointId;
        path.bodyPointZoneId = binding.bodyPointZoneId;
        path.trackerId = binding.trackerId;
        path.cursorId = binding.cursorId;
    }

    private void ApplyBodyPointBindingToComposite(CompositeTrackData composite)
    {
        if (composite == null)
            return;

        BodyPointBindingData binding = GetPrimaryBodyPointBinding();

        composite.bodyPointId = binding.bodyPointId;
        composite.bodyPointZoneId = binding.bodyPointZoneId;
        composite.trackerId = binding.trackerId;
        composite.cursorId = binding.cursorId;
    }


    private Vector3 GetMouseWorldPoint()
    {
        if (mainCamera == null || workspaceView == null || Mouse.current == null)
            return Vector3.zero;

        Vector2 mouse = Mouse.current.position.ReadValue();

        Rect rect = workspaceView.rect;

        Vector2 min = workspaceView.TransformPoint(rect.min);
        Vector2 max = workspaceView.TransformPoint(rect.max);

        float width = max.x - min.x;
        float height = max.y - min.y;

        if (width <= 0.001f || height <= 0.001f)
            return Vector3.zero;

        float normalizedX = Mathf.Clamp01((mouse.x - min.x) / width);
        float normalizedY = Mathf.Clamp01((mouse.y - min.y) / height);

        Vector3 world = mainCamera.ViewportToWorldPoint(
            new Vector3(normalizedX, normalizedY, -mainCamera.transform.position.z)
        );

        world.z = 0f;
        return world;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Mouse.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        if (workspaceView == null)
            return results.Count > 0;

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hitObject = results[i].gameObject;
            if (hitObject == null)
                continue;

            Transform hitTransform = hitObject.transform;

            if (hitTransform == workspaceView || hitTransform.IsChildOf(workspaceView))
                continue;

            return true;
        }

        return false;
    }
}