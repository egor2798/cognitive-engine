using System.Collections.Generic;
using UnityEngine;

public static class CompositeTrackGeometryBuilder
{
    private const float IntersectionMergeDistance = 0.035f;
    private const float MinRoutePointDistance = 0.001f;

    private sealed class IntersectionPair
    {
        public Vector2 point;
        public int segmentA;
        public float tA;
        public int segmentB;
        public float tB;
    }

    private readonly struct PathMarker
    {
        public readonly Vector2 point;
        public readonly int segmentIndex;
        public readonly float t;

        public PathMarker(Vector2 point, int segmentIndex, float t)
        {
            this.point = point;
            this.segmentIndex = segmentIndex;
            this.t = Mathf.Clamp01(t);
        }
    }

    public static TrackGeometry Build(CompositeTrackData data, List<ShapeData> shapes)
    {
        if (shapes == null || shapes.Count == 0)
            return new TrackGeometry(null, false);

        if (shapes.Count == 1)
            return TrackGeometryFactory.FromShape(shapes[0]);

        if (shapes.Count != 2)
        {
            Debug.LogWarning("CompositeTrackGeometryBuilder: сейчас корректно поддерживаются 2 фигуры. Для 3+ нужен порядок соединений.");
            return new TrackGeometry(null, false);
        }

        TrackGeometry first = TrackGeometryFactory.FromShape(shapes[0]);
        TrackGeometry second = TrackGeometryFactory.FromShape(shapes[1]);

        if (first == null || second == null || !first.IsValid() || !second.IsValid())
        {
            Debug.LogWarning("CompositeTrackGeometryBuilder: одна из фигур не имеет корректной геометрии.");
            return new TrackGeometry(null, false);
        }

        List<IntersectionPair> intersections = FindIntersections(first, second);
        if (intersections.Count == 0)
        {
            Debug.LogWarning("CompositeTrackGeometryBuilder: центральные линии фигур не пересекаются. Составной трек не построен.");
            return new TrackGeometry(null, false);
        }

        IntersectionPair transition = ChoosePrimaryIntersection(intersections, shapes[0].center, shapes[1].center);
        if (transition == null)
            return new TrackGeometry(null, false);

        PathMarker firstMarker = new PathMarker(transition.point, transition.segmentA, transition.tA);
        PathMarker secondMarker = new PathMarker(transition.point, transition.segmentB, transition.tB);

        bool firstForward = GetDirection(data, shapeIndex: 0);
        bool secondForward = GetDirection(data, shapeIndex: 1);

        // Final composite-track model:
        // one stable transition point + full loop of each shape.
        // Random affects only traversal direction, not transition point selection.
        List<Vector2> route = WalkFullLoopDirectional(first, firstMarker, firstForward);
        AppendPoints(route, WalkFullLoopDirectional(second, secondMarker, secondForward), skipFirstIfSame: true);
        CleanRoute(route);

        if (route.Count < 4)
        {
            Debug.LogWarning("CompositeTrackGeometryBuilder: построенный маршрут слишком короткий.");
            return new TrackGeometry(null, false);
        }

        Debug.Log("CompositeTrackGeometryBuilder: one-transition full-loop route. mode=" +
                  (data != null ? data.transitionMode.ToString() : "null") +
                  ", intersections=" + intersections.Count +
                  ", transition=" + transition.point +
                  ", firstForward=" + firstForward +
                  ", secondForward=" + secondForward +
                  ", points=" + route.Count +
                  ", length=" + CalculatePathLength(route).ToString("F3"));

        return new TrackGeometry(route, true);
    }

    private static bool GetDirection(CompositeTrackData data, int shapeIndex)
    {
        if (data == null)
            return true;

        switch (data.transitionMode)
        {
            case CompositeTransitionMode.RandomSideOnStart:
            case CompositeTransitionMode.RandomSideAtEachIntersection:
                return UnityEngine.Random.value >= 0.5f;

            case CompositeTransitionMode.FixedRoute:
            case CompositeTransitionMode.Sequential:
            default:
                return true;
        }
    }

    private static List<IntersectionPair> FindIntersections(TrackGeometry a, TrackGeometry b)
    {
        List<IntersectionPair> result = new List<IntersectionPair>();
        int countA = GetSegmentCount(a);
        int countB = GetSegmentCount(b);

        for (int i = 0; i < countA; i++)
        {
            GetSegment(a, i, out Vector2 a0, out Vector2 a1);

            for (int j = 0; j < countB; j++)
            {
                GetSegment(b, j, out Vector2 b0, out Vector2 b1);

                if (!TrackIntersectionUtility.TrySegmentIntersection(a0, a1, b0, b1,
                        out Vector2 intersection, out float tA, out float tB))
                    continue;

                if (IsDuplicateIntersection(result, intersection))
                    continue;

                result.Add(new IntersectionPair
                {
                    point = intersection,
                    segmentA = i,
                    tA = tA,
                    segmentB = j,
                    tB = tB
                });
            }
        }

        return result;
    }

    private static bool IsDuplicateIntersection(List<IntersectionPair> intersections, Vector2 point)
    {
        for (int i = 0; i < intersections.Count; i++)
        {
            if (Vector2.Distance(intersections[i].point, point) <= IntersectionMergeDistance)
                return true;
        }

        return false;
    }

    private static IntersectionPair ChoosePrimaryIntersection(List<IntersectionPair> intersections, Vector2 firstCenter, Vector2 secondCenter)
    {
        if (intersections == null || intersections.Count == 0)
            return null;

        Vector2 preferredPoint = (firstCenter + secondCenter) * 0.5f;
        IntersectionPair best = intersections[0];
        float bestDistance = Vector2.SqrMagnitude(best.point - preferredPoint);

        for (int i = 1; i < intersections.Count; i++)
        {
            float distance = Vector2.SqrMagnitude(intersections[i].point - preferredPoint);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = intersections[i];
            }
        }

        return best;
    }

    private static List<Vector2> WalkFullLoopDirectional(TrackGeometry geometry, PathMarker marker, bool forward)
    {
        return forward
            ? WalkFullLoopForward(geometry, marker)
            : WalkFullLoopBackward(geometry, marker);
    }

    private static List<Vector2> WalkFullLoopForward(TrackGeometry geometry, PathMarker marker)
    {
        List<Vector2> points = new List<Vector2>();
        IReadOnlyList<Vector2> source = geometry.Points;

        if (source == null || source.Count < 2)
            return points;

        int segmentCount = GetSegmentCount(geometry);
        if (segmentCount <= 0)
            return points;

        AddPoint(points, marker.point);

        int currentSegment = marker.segmentIndex;
        for (int i = 0; i < segmentCount; i++)
        {
            int vertexIndex = currentSegment + 1;
            if (vertexIndex >= source.Count)
                vertexIndex = 0;

            AddPoint(points, source[vertexIndex]);

            currentSegment++;
            if (currentSegment >= segmentCount)
                currentSegment = 0;
        }

        AddPoint(points, marker.point);
        return points;
    }

    private static List<Vector2> WalkFullLoopBackward(TrackGeometry geometry, PathMarker marker)
    {
        List<Vector2> points = new List<Vector2>();
        IReadOnlyList<Vector2> source = geometry.Points;

        if (source == null || source.Count < 2)
            return points;

        int segmentCount = GetSegmentCount(geometry);
        if (segmentCount <= 0)
            return points;

        AddPoint(points, marker.point);

        int currentSegment = marker.segmentIndex;
        for (int i = 0; i < segmentCount; i++)
        {
            int vertexIndex = currentSegment;
            if (vertexIndex < 0)
                vertexIndex = source.Count - 1;

            AddPoint(points, source[vertexIndex]);

            currentSegment--;
            if (currentSegment < 0)
                currentSegment = segmentCount - 1;
        }

        AddPoint(points, marker.point);
        return points;
    }

    private static void AppendPoints(List<Vector2> target, List<Vector2> source, bool skipFirstIfSame)
    {
        if (target == null || source == null || source.Count == 0)
            return;

        int startIndex = 0;
        if (skipFirstIfSame && target.Count > 0 && Vector2.Distance(target[target.Count - 1], source[0]) <= MinRoutePointDistance)
            startIndex = 1;

        for (int i = startIndex; i < source.Count; i++)
            AddPoint(target, source[i]);
    }

    private static float CalculatePathLength(List<Vector2> points)
    {
        if (points == null || points.Count < 2)
            return 0f;

        float length = 0f;
        for (int i = 1; i < points.Count; i++)
            length += Vector2.Distance(points[i - 1], points[i]);

        return length;
    }

    private static void CleanRoute(List<Vector2> points)
    {
        if (points == null)
            return;

        for (int i = points.Count - 1; i > 0; i--)
        {
            if (Vector2.Distance(points[i], points[i - 1]) < MinRoutePointDistance)
                points.RemoveAt(i);
        }
    }

    private static void AddPoint(List<Vector2> points, Vector2 point)
    {
        if (points == null)
            return;

        if (points.Count > 0 && Vector2.Distance(points[points.Count - 1], point) < MinRoutePointDistance)
            return;

        points.Add(point);
    }

    private static int GetSegmentCount(TrackGeometry geometry)
    {
        if (geometry == null || geometry.Points == null || geometry.Points.Count < 2)
            return 0;

        return geometry.IsClosed ? geometry.Points.Count : geometry.Points.Count - 1;
    }

    private static void GetSegment(TrackGeometry geometry, int index, out Vector2 a, out Vector2 b)
    {
        IReadOnlyList<Vector2> points = geometry.Points;
        a = points[index];

        int nextIndex = index + 1;
        if (nextIndex >= points.Count)
            nextIndex = 0;

        b = points[nextIndex];
    }
}
