using System.Collections.Generic;
using UnityEngine;

public class WorkspaceGrid : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int halfCellsX = 20;
    [SerializeField] private int halfCellsY = 20;
    [SerializeField] private float cellSize = 1f;

    [Header("Coordinate Origin")]
    [Tooltip("If enabled, the grid origin is Unity world (0, 0), so figure coordinates X=0/Y=0 match the grid center.")]
    [SerializeField] private bool useWorldZeroAsOrigin = true;
    [SerializeField] private Vector2 customOrigin = Vector2.zero;

    [Header("Style")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.10f);
    [SerializeField] private Color axisColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("Origin Marker")]
    [SerializeField] private bool showOriginMarker = true;
    [SerializeField] private float originMarkerSize = 0.35f;
    [SerializeField] private float originMarkerLineWidth = 0.04f;
    [SerializeField] private Color originMarkerColor = new Color(1f, 0.95f, 0.25f, 1f);

    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private bool hasBuiltGrid;

    private void Start()
    {
        RebuildGrid();
    }

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        ApplyOriginTransformPolicy();
        ClearGrid();

        if (lineMaterial == null)
        {
            Debug.LogWarning("WorkspaceGrid: lineMaterial is not assigned.");
            return;
        }

        float minX = -halfCellsX * cellSize;
        float maxX = halfCellsX * cellSize;
        float minY = -halfCellsY * cellSize;
        float maxY = halfCellsY * cellSize;

        Vector3 origin = GetOrigin();

        for (int x = -halfCellsX; x <= halfCellsX; x++)
        {
            float xPos = x * cellSize;
            bool isAxis = x == 0;

            CreateLine(
                origin + new Vector3(xPos, minY, 0f),
                origin + new Vector3(xPos, maxY, 0f),
                isAxis ? axisColor : gridColor,
                isAxis ? 10 : 0
            );
        }

        for (int y = -halfCellsY; y <= halfCellsY; y++)
        {
            float yPos = y * cellSize;
            bool isAxis = y == 0;

            CreateLine(
                origin + new Vector3(minX, yPos, 0f),
                origin + new Vector3(maxX, yPos, 0f),
                isAxis ? axisColor : gridColor,
                isAxis ? 10 : 0
            );
        }

        if (showOriginMarker)
            CreateOriginMarker(origin);

        hasBuiltGrid = true;
    }

    private void ApplyOriginTransformPolicy()
    {
        if (!useWorldZeroAsOrigin)
            return;

        // Do not do this in Awake/OnValidate. Unity can show warnings if transforms
        // are changed while it is checking consistency or importing assets.
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private Vector3 GetOrigin()
    {
        if (useWorldZeroAsOrigin)
            return Vector3.zero;

        return new Vector3(customOrigin.x, customOrigin.y, 0f);
    }

    private void CreateOriginMarker(Vector3 origin)
    {
        float size = Mathf.Max(0.05f, originMarkerSize);

        CreateLine(
            origin + new Vector3(-size, 0f, 0f),
            origin + new Vector3(size, 0f, 0f),
            originMarkerColor,
            50,
            originMarkerLineWidth
        );

        CreateLine(
            origin + new Vector3(0f, -size, 0f),
            origin + new Vector3(0f, size, 0f),
            originMarkerColor,
            50,
            originMarkerLineWidth
        );
    }

    private void CreateLine(Vector3 start, Vector3 end, Color color, int sortingOrderOffset)
    {
        CreateLine(start, end, color, sortingOrderOffset, lineWidth);
    }

    private void CreateLine(Vector3 start, Vector3 end, Color color, int sortingOrderOffset, float width)
    {
        GameObject go = new GameObject("GridLine");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.loop = false;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.material = lineMaterial;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = -100 + sortingOrderOffset;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lines.Add(lr);
    }

    private void ClearGrid()
    {
        lines.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        halfCellsX = Mathf.Max(1, halfCellsX);
        halfCellsY = Mathf.Max(1, halfCellsY);
        cellSize = Mathf.Max(0.01f, cellSize);
        lineWidth = Mathf.Max(0.001f, lineWidth);
        originMarkerSize = Mathf.Max(0.05f, originMarkerSize);
        originMarkerLineWidth = Mathf.Max(0.001f, originMarkerLineWidth);

        // Important: do not rebuild the grid here.
        // Creating children, SetParent, and AddComponent inside OnValidate/Awake
        // causes Unity warnings like:
        // "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate".
        // The grid is rebuilt safely in Start(), or manually via ContextMenu.
    }
#endif
}
