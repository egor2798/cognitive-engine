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
        AddCircle
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
            radius = defaultCircleRadius
        };

        CreateShape(data, "Circle");
    }

    private void CreateShape(ShapeData data, string prefix)
    {
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
        ShapeData data = shape.Data;

        switch (data.shapeType)
        {
            case ShapeType.Square:
                return GetDistanceToSquare(data, p);

            case ShapeType.Circle:
                return GetDistanceToCircle(data, p);

            case ShapeType.Polyline:
                return GetDistanceToPolyline(shape.Points, p);

            default:
                return float.MaxValue;
        }
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