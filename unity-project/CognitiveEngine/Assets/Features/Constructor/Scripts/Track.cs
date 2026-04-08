using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private readonly List<Vector3> points = new();

    public IReadOnlyList<Vector3> Points => points;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;

        RefreshView();
    }

    public void AddPoint(Vector3 worldPoint)
    {
        worldPoint.z = 0f;
        points.Add(worldPoint);
        RefreshView();
    }

    public void ClearTrack()
    {
        points.Clear();
        RefreshView();
    }

    public void RefreshView()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }
    }
}