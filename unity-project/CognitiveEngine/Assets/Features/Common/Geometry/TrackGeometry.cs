using System.Collections.Generic;
using UnityEngine;

public sealed class TrackGeometry
{
    private readonly List<Vector2> points = new();
    private readonly List<float> segmentLengths = new();

    public IReadOnlyList<Vector2> Points => points;
    public bool IsClosed { get; private set; }
    public float TotalLength { get; private set; }

    public TrackGeometry(IEnumerable<Vector2> sourcePoints, bool isClosed)
    {
        IsClosed = isClosed;

        if (sourcePoints != null)
        {
            foreach (Vector2 point in sourcePoints)
                points.Add(point);
        }

        RemoveTooClosePoints();
        RebuildLengthCache();
    }

    public bool IsValid()
    {
        return points.Count >= 2 && TotalLength > 0.0001f;
    }

    public Vector2 GetPointAtProgress(float progress)
    {
        if (!IsValid())
            return Vector2.zero;

        progress = Mathf.Repeat(progress, 1f);

        float targetDistance = progress * TotalLength;
        return GetPointAtDistance(targetDistance);
    }

    public Vector2 GetPointAtDistance(float distance)
    {
        if (!IsValid())
            return Vector2.zero;

        if (IsClosed)
            distance = Mathf.Repeat(distance, TotalLength);
        else
            distance = Mathf.Clamp(distance, 0f, TotalLength);

        float accumulated = 0f;

        int segmentCount = GetSegmentCount();

        for (int i = 0; i < segmentCount; i++)
        {
            float length = segmentLengths[i];

            if (length <= 0.0001f)
                continue;

            if (accumulated + length >= distance)
            {
                float localT = (distance - accumulated) / length;
                GetSegment(i, out Vector2 a, out Vector2 b);
                return Vector2.Lerp(a, b, localT);
            }

            accumulated += length;
        }

        return points[^1];
    }

    public ClosestPointInfo GetClosestPoint(Vector2 point)
    {
        ClosestPointInfo best = new ClosestPointInfo
        {
            point = Vector2.zero,
            distance = float.MaxValue,
            progress = 0f,
            distanceAlongTrack = 0f,
            segmentIndex = -1
        };

        if (!IsValid())
            return best;

        float accumulated = 0f;
        int segmentCount = GetSegmentCount();

        for (int i = 0; i < segmentCount; i++)
        {
            GetSegment(i, out Vector2 a, out Vector2 b);

            Vector2 ab = b - a;
            float abSqr = ab.sqrMagnitude;
            float segmentLength = segmentLengths[i];

            if (abSqr <= 0.000001f || segmentLength <= 0.0001f)
                continue;

            float t = Vector2.Dot(point - a, ab) / abSqr;
            t = Mathf.Clamp01(t);

            Vector2 projection = a + ab * t;
            float distance = Vector2.Distance(point, projection);

            if (distance < best.distance)
            {
                float distanceAlong = accumulated + segmentLength * t;

                best.point = projection;
                best.distance = distance;
                best.distanceAlongTrack = distanceAlong;
                best.progress = TotalLength > 0.0001f ? distanceAlong / TotalLength : 0f;
                best.segmentIndex = i;
            }

            accumulated += segmentLength;
        }

        return best;
    }

    public bool IsInsideCorridor(Vector2 point, float corridorWidth)
    {
        if (!IsValid())
            return false;

        float halfWidth = Mathf.Max(0.001f, corridorWidth * 0.5f);
        ClosestPointInfo closest = GetClosestPoint(point);

        return closest.distance <= halfWidth;
    }

    private void RebuildLengthCache()
    {
        segmentLengths.Clear();
        TotalLength = 0f;

        int segmentCount = GetSegmentCount();

        for (int i = 0; i < segmentCount; i++)
        {
            GetSegment(i, out Vector2 a, out Vector2 b);

            float length = Vector2.Distance(a, b);
            segmentLengths.Add(length);
            TotalLength += length;
        }
    }

    private int GetSegmentCount()
    {
        if (points.Count < 2)
            return 0;

        return IsClosed ? points.Count : points.Count - 1;
    }

    private void GetSegment(int index, out Vector2 a, out Vector2 b)
    {
        a = points[index];

        int nextIndex = index + 1;
        if (nextIndex >= points.Count)
            nextIndex = 0;

        b = points[nextIndex];
    }

    private void RemoveTooClosePoints()
    {
        if (points.Count < 2)
            return;

        for (int i = points.Count - 1; i > 0; i--)
        {
            if (Vector2.Distance(points[i], points[i - 1]) < 0.0001f)
                points.RemoveAt(i);
        }

        if (IsClosed && points.Count > 2)
        {
            if (Vector2.Distance(points[0], points[^1]) < 0.0001f)
                points.RemoveAt(points.Count - 1);
        }
    }
}

public struct ClosestPointInfo
{
    public Vector2 point;
    public float distance;
    public float progress;
    public float distanceAlongTrack;
    public int segmentIndex;
}