using System.Collections.Generic;
using UnityEngine;

public class WorkspaceGrid : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int halfCellsX = 20;
    [SerializeField] private int halfCellsY = 20;
    [SerializeField] private float cellSize = 1f;

    [Header("Style")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.10f);
    [SerializeField] private Color axisColor = new Color(1f, 1f, 1f, 0.28f);

    private readonly List<LineRenderer> lines = new();

    private void Start()
    {
        RebuildGrid();
    }

    public void RebuildGrid()
    {
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

        for (int x = -halfCellsX; x <= halfCellsX; x++)
        {
            float xPos = x * cellSize;
            bool isAxis = x == 0;

            CreateLine(
                new Vector3(xPos, minY, 0f),
                new Vector3(xPos, maxY, 0f),
                isAxis ? axisColor : gridColor
            );
        }

        for (int y = -halfCellsY; y <= halfCellsY; y++)
        {
            float yPos = y * cellSize;
            bool isAxis = y == 0;

            CreateLine(
                new Vector3(minX, yPos, 0f),
                new Vector3(maxX, yPos, 0f),
                isAxis ? axisColor : gridColor
            );
        }
    }

    private void CreateLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject go = new GameObject("GridLine");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.loop = false;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.material = lineMaterial;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = -100;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        lr.SetPosition(0, transform.position + start);
        lr.SetPosition(1, transform.position + end);

        lines.Add(lr);
    }

    private void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        lines.Clear();
    }
}