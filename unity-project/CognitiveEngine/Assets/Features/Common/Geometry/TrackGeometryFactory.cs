using System.Collections.Generic;
using UnityEngine;

public static class TrackGeometryFactory
{
    private const int CircleSegments = 96;
    private const int BezierSegments = 32;

    public static TrackGeometry FromShape(ShapeData shape)
    {
        if (shape == null)
            return new TrackGeometry(null, false);

        switch (shape.shapeType)
        {
            case ShapeType.Circle:
                return FromCircle(shape);
            case ShapeType.Triangle:
                return FromRegularPolygon(shape, 3, -90f);
            case ShapeType.Diamond:
                return FromDiamond(shape);
            case ShapeType.Pentagon:
                return FromRegularPolygon(shape, 5, -90f);
            case ShapeType.Hexagon:
                return FromRegularPolygon(shape, 6, -90f);
            case ShapeType.Star:
                return FromStar(shape, 5);
            case ShapeType.Star8:
                return FromStar(shape, 8);
            case ShapeType.Arc:
                return FromArc(shape);
            case ShapeType.Zigzag:
                return FromZigzag(shape);
            case ShapeType.FigureEight:
                return FromFigureEight(shape);
            case ShapeType.Spiral:
                return FromSpiral(shape);
            case ShapeType.Target:
                return FromTarget(shape);
            case ShapeType.Tricycle:
                return FromTricycle(shape);
            case ShapeType.Polyline:
                return new TrackGeometry(shape.points, false);
            case ShapeType.Square:
            default:
                return FromRectangle(shape);
        }
    }


    private static TrackGeometry FromSpiral(ShapeData shape)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);
        float turns = Mathf.Clamp(shape.spiralTurns <= 0f ? 3f : shape.spiralTurns, 1f, 8f);

        const int segments = 160;
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f * turns;
            float radius01 = t;

            float x = Mathf.Cos(angle) * radius01 * width * 0.5f;
            float y = Mathf.Sin(angle) * radius01 * height * 0.5f;

            points.Add(new Vector2(shape.center.x + x, shape.center.y + y));
        }

        return new TrackGeometry(points, false);
    }

    private static TrackGeometry FromFigureEight(ShapeData shape)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);

        const int segments = 128;
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;

            float x = Mathf.Sin(t);
            float y = Mathf.Sin(t) * Mathf.Cos(t);

            points.Add(new Vector2(
                shape.center.x + x * width * 0.5f,
                shape.center.y + y * height * 0.5f
            ));
        }

        return new TrackGeometry(points, true);
    }

    private static TrackGeometry FromZigzag(ShapeData shape)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        const int pointsCount = 7;
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < pointsCount; i++)
        {
            float t = i / (float)(pointsCount - 1);
            float x = shape.center.x - halfW + width * t;
            float y = shape.center.y + (i % 2 == 0 ? halfH : -halfH);

            points.Add(new Vector2(x, y));
        }

        return new TrackGeometry(points, false);
    }

    private static TrackGeometry FromTarget(ShapeData shape)
    {
        // Мишень — статическая зона удержания, а не маршрут для обводки.
        // Для общей TrackGeometry представляем её как маленькую центральную окружность.
        // Так TrackingEvaluator сможет считать отклонение от центра мишени,
        // а не заставлять пользователя двигаться по внешним кольцам.
        return FromEllipseRing(shape, 0.18f);
    }

    private static TrackGeometry FromTricycle(ShapeData shape)
    {
        // Трицикл: среднее кольцо → внешнее кольцо → внутреннее кольцо.
        // Переходы между кольцами идут через правую точку окружности.
        // Позже этот маршрут можно заменить на граф с дискретными переходами.
        List<Vector2> points = new List<Vector2>();

        AddEllipseRing(points, shape, 2f / 3f, true);
        AddEllipseRing(points, shape, 1f, true);
        AddEllipseRing(points, shape, 1f / 3f, true);

        return new TrackGeometry(points, false);
    }

    private static TrackGeometry FromEllipseRing(ShapeData shape, float scale)
    {
        List<Vector2> points = new List<Vector2>();
        AddEllipseRing(points, shape, scale, false);
        return new TrackGeometry(points, true);
    }

    private static void AddEllipseRing(List<Vector2> points, ShapeData shape, float scale, bool includeClosingPoint)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);
        scale = Mathf.Clamp(scale, 0.05f, 1f);

        int segments = CircleSegments;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;

            points.Add(new Vector2(
                shape.center.x + Mathf.Cos(angle) * width * 0.5f * scale,
                shape.center.y + Mathf.Sin(angle) * height * 0.5f * scale
            ));
        }

        if (includeClosingPoint && points.Count > 0)
        {
            points.Add(new Vector2(
                shape.center.x + width * 0.5f * scale,
                shape.center.y
            ));
        }
    }

    public static TrackGeometry FromPath(PathData path)
    {
        List<Vector2> points = new List<Vector2>();

        if (path == null || path.segments == null)
            return new TrackGeometry(points, false);

        for (int i = 0; i < path.segments.Count; i++)
        {
            SegmentData segment = path.segments[i];
            if (segment == null)
                continue;

            List<Vector2> segmentPoints = BuildSegmentPoints(segment);

            for (int p = 0; p < segmentPoints.Count; p++)
            {
                if (points.Count > 0 && p == 0)
                {
                    if (Vector2.Distance(points[points.Count - 1], segmentPoints[p]) < 0.0001f)
                        continue;
                }

                points.Add(segmentPoints[p]);
            }
        }

        return new TrackGeometry(points, false);
    }

    private static TrackGeometry FromRectangle(ShapeData shape)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        List<Vector2> points = new List<Vector2>
        {
            new Vector2(shape.center.x - halfW, shape.center.y + halfH),
            new Vector2(shape.center.x + halfW, shape.center.y + halfH),
            new Vector2(shape.center.x + halfW, shape.center.y - halfH),
            new Vector2(shape.center.x - halfW, shape.center.y - halfH)
        };

        return new TrackGeometry(points, true);
    }

    private static TrackGeometry FromCircle(ShapeData shape)
    {
        float radius = Mathf.Max(0.05f, shape.radius);
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < CircleSegments; i++)
        {
            float t = i / (float)CircleSegments;
            float angle = t * Mathf.PI * 2f;

            points.Add(new Vector2(
                shape.center.x + Mathf.Cos(angle) * radius,
                shape.center.y + Mathf.Sin(angle) * radius
            ));
        }

        return new TrackGeometry(points, true);
    }

    private static TrackGeometry FromRegularPolygon(ShapeData shape, int sides, float rotationDegrees)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);
        float rotation = rotationDegrees * Mathf.Deg2Rad;

        sides = Mathf.Max(3, sides);
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < sides; i++)
        {
            float t = i / (float)sides;
            float angle = t * Mathf.PI * 2f + rotation;

            points.Add(new Vector2(
                shape.center.x + Mathf.Cos(angle) * width * 0.5f,
                shape.center.y + Mathf.Sin(angle) * height * 0.5f
            ));
        }

        return new TrackGeometry(points, true);
    }

    private static TrackGeometry FromDiamond(ShapeData shape)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        List<Vector2> points = new List<Vector2>
        {
            new Vector2(shape.center.x, shape.center.y + halfH),
            new Vector2(shape.center.x + halfW, shape.center.y),
            new Vector2(shape.center.x, shape.center.y - halfH),
            new Vector2(shape.center.x - halfW, shape.center.y)
        };

        return new TrackGeometry(points, true);
    }

    private static TrackGeometry FromStar(ShapeData shape, int rays)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);
        float innerScale = 0.45f;
        float rotation = -90f * Mathf.Deg2Rad;

        rays = Mathf.Max(3, rays);
        int vertexCount = rays * 2;
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < vertexCount; i++)
        {
            bool outer = i % 2 == 0;
            float scale = outer ? 1f : innerScale;
            float angle = i / (float)vertexCount * Mathf.PI * 2f + rotation;

            points.Add(new Vector2(
                shape.center.x + Mathf.Cos(angle) * width * 0.5f * scale,
                shape.center.y + Mathf.Sin(angle) * height * 0.5f * scale
            ));
        }

        return new TrackGeometry(points, true);
    }

    private static TrackGeometry FromArc(ShapeData shape)
    {
        float width = Mathf.Max(0.05f, shape.width);
        float height = Mathf.Max(0.05f, shape.height);

        int segments = 48;
        float startAngle = shape.arcStartAngle;
        float endAngle = shape.arcEndAngle;

        if (Mathf.Abs(Mathf.DeltaAngle(startAngle, endAngle)) < 1f)
            endAngle = startAngle + 180f;

        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angleDeg = Mathf.LerpAngle(startAngle, endAngle, t);
            float angle = angleDeg * Mathf.Deg2Rad;

            points.Add(new Vector2(
                shape.center.x + Mathf.Cos(angle) * width * 0.5f,
                shape.center.y + Mathf.Sin(angle) * height * 0.5f
            ));
        }

        return new TrackGeometry(points, false);
    }

    private static List<Vector2> BuildSegmentPoints(SegmentData segment)
    {
        List<Vector2> points = new List<Vector2>();

        switch (segment.segmentType)
        {
            case SegmentType.Line:
                points.Add(segment.startPoint);
                points.Add(segment.endPoint);
                break;

            case SegmentType.Bezier:
                for (int i = 0; i <= BezierSegments; i++)
                {
                    float t = i / (float)BezierSegments;
                    points.Add(CalculateCubicBezier(
                        segment.startPoint,
                        segment.controlPoint1,
                        segment.controlPoint2,
                        segment.endPoint,
                        t
                    ));
                }
                break;
        }

        return points;
    }

    private static Vector2 CalculateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;

        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }
}
