using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime route for CompositeTrack using the final agreed logic:
/// - one fixed transition point between two shapes;
/// - each shape is traversed as a full loop;
/// - random is used only to choose traversal direction of the shape.
///
/// This intentionally does NOT use every geometric intersection as a routing node.
/// </summary>
public sealed class CompositeTrackRuntimeRoute
{
    private const float IntersectionMergeDistance = 0.035f;
    private const float MinRoutePointDistance = 0.001f;
    private const float MaxClosestTransitionDistance = 0.15f;

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

    private readonly struct ClosestMarkerPair
    {
        public readonly PathMarker first;
        public readonly PathMarker second;
        public readonly float distance;

        public ClosestMarkerPair(PathMarker first, PathMarker second, float distance)
        {
            this.first = first;
            this.second = second;
            this.distance = distance;
        }
    }

    private CompositeTrackData data;
    private TrackGeometry[] geometries;
    private PathMarker[] transitionMarkers;
    private bool[] fixedDirections;

    private int currentShapeIndex;
    private TrackGeometry currentLegGeometry;
    private float currentLegDistance;

    public bool IsConfigured { get; private set; }
    public float EstimatedFullRouteLength { get; private set; }
    public TrackGeometry CurrentLegGeometry => currentLegGeometry;

    public bool Configure(CompositeTrackData compositeData, List<ShapeData> shapes)
    {
        IsConfigured = false;
        data = compositeData;
        geometries = null;
        transitionMarkers = null;
        fixedDirections = null;
        currentLegGeometry = null;
        currentLegDistance = 0f;
        EstimatedFullRouteLength = 0f;

        if (data == null || shapes == null || shapes.Count != 2)
        {
            Debug.LogWarning("CompositeTrackRuntimeRoute: runtime mode currently supports exactly 2 source shapes.");
            return false;
        }

        TrackGeometry first = TrackGeometryFactory.FromShape(shapes[0]);
        TrackGeometry second = TrackGeometryFactory.FromShape(shapes[1]);

        if (first == null || second == null || !first.IsValid() || !second.IsValid())
        {
            Debug.LogWarning("CompositeTrackRuntimeRoute: one of source shapes has invalid geometry.");
            return false;
        }

        if (!TryChooseSingleTransition(first, second, shapes[0].center, shapes[1].center, out PathMarker firstMarker, out PathMarker secondMarker, out string transitionInfo))
        {
            Debug.LogWarning("CompositeTrackRuntimeRoute: failed to choose one stable transition point between shapes.");
            return false;
        }

        geometries = new[] { first, second };
        transitionMarkers = new[] { firstMarker, secondMarker };

        fixedDirections = new[]
        {
            UnityEngine.Random.value >= 0.5f,
            UnityEngine.Random.value >= 0.5f
        };

        currentShapeIndex = 0;
        EstimatedFullRouteLength = Mathf.Max(0.001f, first.TotalLength + second.TotalLength);

        BuildCurrentLeg(resetDistance: true);
        IsConfigured = currentLegGeometry != null && currentLegGeometry.IsValid();

        if (IsConfigured)
        {
            Debug.Log("CompositeTrackRuntimeRoute: one-transition runtime route configured. mode=" + data.transitionMode +
                      ", " + transitionInfo +
                      ", estimatedLength=" + EstimatedFullRouteLength.ToString("F3"));
        }

        return IsConfigured;
    }

    public void Advance(float distanceDelta)
    {
        if (!IsConfigured || currentLegGeometry == null || !currentLegGeometry.IsValid())
            return;

        currentLegDistance += Mathf.Max(0f, distanceDelta);

        int guard = 0;
        while (currentLegDistance >= currentLegGeometry.TotalLength && guard < 8)
        {
            currentLegDistance -= currentLegGeometry.TotalLength;
            SwitchToNextShape();
            guard++;

            if (currentLegGeometry == null || !currentLegGeometry.IsValid())
                break;
        }
    }

    public Vector2 GetCurrentPosition()
    {
        if (!IsConfigured || currentLegGeometry == null || !currentLegGeometry.IsValid())
            return Vector2.zero;

        return currentLegGeometry.GetPointAtDistance(currentLegDistance);
    }

    public float GetCurrentLegProgress()
    {
        if (currentLegGeometry == null || !currentLegGeometry.IsValid())
            return 0f;

        return Mathf.Clamp01(currentLegDistance / Mathf.Max(0.001f, currentLegGeometry.TotalLength));
    }

    private void SwitchToNextShape()
    {
        // Mandatory switch to the other shape. There is only one transition point.
        currentShapeIndex = 1 - currentShapeIndex;
        BuildCurrentLeg(resetDistance: false);
    }

    private void BuildCurrentLeg(bool resetDistance)
    {
        TrackGeometry geometry = geometries[currentShapeIndex];
        PathMarker marker = transitionMarkers[currentShapeIndex];
        bool forward = ChooseDirectionForCurrentShape();

        List<Vector2> points = WalkFullLoopDirectional(geometry, marker, forward);
        CleanRoute(points);

        currentLegGeometry = new TrackGeometry(points, false);

        if (resetDistance)
            currentLegDistance = 0f;
        else if (currentLegGeometry != null && currentLegGeometry.IsValid())
            currentLegDistance = Mathf.Clamp(currentLegDistance, 0f, currentLegGeometry.TotalLength);
        else
            currentLegDistance = 0f;

        Debug.Log("CompositeTrackRuntimeRoute: switched shape. shape=" + currentShapeIndex +
                  ", forward=" + forward +
                  ", points=" + (points != null ? points.Count : 0) +
                  ", length=" + (currentLegGeometry != null ? currentLegGeometry.TotalLength.ToString("F3") : "0"));
    }

    private bool ChooseDirectionForCurrentShape()
    {
        CompositeTransitionMode mode = data != null ? data.transitionMode : CompositeTransitionMode.FixedRoute;

        switch (mode)
        {
            case CompositeTransitionMode.RandomSideAtEachIntersection:
                // Here "at each intersection" means every time pacemaker enters a shape
                // through the single fixed transition point.
                return UnityEngine.Random.value >= 0.5f;

            case CompositeTransitionMode.RandomSideOnStart:
                return fixedDirections != null ? fixedDirections[currentShapeIndex] : true;

            case CompositeTransitionMode.FixedRoute:
            case CompositeTransitionMode.Sequential:
            default:
                return true;
        }
    }

    private static bool TryChooseSingleTransition(
        TrackGeometry first,
        TrackGeometry second,
        Vector2 firstCenter,
        Vector2 secondCenter,
        out PathMarker firstMarker,
        out PathMarker secondMarker,
        out string info)
    {
        firstMarker = default;
        secondMarker = default;
        info = string.Empty;

        List<IntersectionPair> intersections = FindIntersections(first, second);
        if (intersections.Count > 0)
        {
            IntersectionPair primary = ChoosePrimaryIntersection(intersections, firstCenter, secondCenter);
            firstMarker = new PathMarker(primary.point, primary.segmentA, primary.tA);
            secondMarker = new PathMarker(primary.point, primary.segmentB, primary.tB);
            info = "transition=strict intersection, intersections=" + intersections.Count + ", point=" + primary.point;
            return true;
        }

        ClosestMarkerPair closest = FindClosestMarkerPair(first, second);
        if (closest.distance <= MaxClosestTransitionDistance)
        {
            firstMarker = closest.first;
            secondMarker = closest.second;
            info = "transition=closest points, distance=" + closest.distance.ToString("F3") +
                   ", first=" + closest.first.point + ", second=" + closest.second.point;
            return true;
        }

        info = "no transition, closest distance=" + closest.distance.ToString("F3");
        return false;
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
                {
                    continue;
                }

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

    private static ClosestMarkerPair FindClosestMarkerPair(TrackGeometry first, TrackGeometry second)
    {
        ClosestMarkerPair best = new ClosestMarkerPair(default, default, float.MaxValue);

        int countA = GetSegmentCount(first);
        int countB = GetSegmentCount(second);

        for (int i = 0; i < countA; i++)
        {
            GetSegment(first, i, out Vector2 a0, out Vector2 a1);

            for (int j = 0; j < countB; j++)
            {
                GetSegment(second, j, out Vector2 b0, out Vector2 b1);

                EvaluateEndpointProjection(a0, i, 0f, b0, b1, j, ref best, firstPointIsFromA: true);
                EvaluateEndpointProjection(a1, i, 1f, b0, b1, j, ref best, firstPointIsFromA: true);
                EvaluateEndpointProjection(b0, j, 0f, a0, a1, i, ref best, firstPointIsFromA: false);
                EvaluateEndpointProjection(b1, j, 1f, a0, a1, i, ref best, firstPointIsFromA: false);
            }
        }

        return best;
    }

    private static void EvaluateEndpointProjection(
        Vector2 point,
        int pointSegmentIndex,
        float pointT,
        Vector2 segmentStart,
        Vector2 segmentEnd,
        int segmentIndex,
        ref ClosestMarkerPair best,
        bool firstPointIsFromA)
    {
        Vector2 ab = segmentEnd - segmentStart;
        float abSqr = ab.sqrMagnitude;
        if (abSqr <= 0.000001f)
            return;

        float t = Vector2.Dot(point - segmentStart, ab) / abSqr;
        t = Mathf.Clamp01(t);
        Vector2 projection = segmentStart + ab * t;
        float distance = Vector2.Distance(point, projection);

        if (distance >= best.distance)
            return;

        if (firstPointIsFromA)
        {
            best = new ClosestMarkerPair(
                new PathMarker(point, pointSegmentIndex, pointT),
                new PathMarker(projection, segmentIndex, t),
                distance);
        }
        else
        {
            best = new ClosestMarkerPair(
                new PathMarker(projection, segmentIndex, t),
                new PathMarker(point, pointSegmentIndex, pointT),
                distance);
        }
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

    private static void CleanRoute(List<Vector2> route)
    {
        if (route == null)
            return;

        for (int i = route.Count - 1; i > 0; i--)
        {
            if (Vector2.Distance(route[i], route[i - 1]) < MinRoutePointDistance)
                route.RemoveAt(i);
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

        int next = index + 1;
        if (next >= points.Count)
            next = 0;

        b = points[next];
    }
}
