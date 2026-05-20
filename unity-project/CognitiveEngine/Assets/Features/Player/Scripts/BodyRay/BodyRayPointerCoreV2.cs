// BodyRayPointerCoreV2.cs
// Минимальное ядро проекции "луча от тела" на экран для Unity/C#.
// Этот файл не содержит BLE-драйвер. Данные WT901BLECL должны приходить из интеграционного слоя.

using UnityEngine;

public enum TrackingModeV2
{
    Single, // 1 активная точка
    Pair,   // 2 активные точки: база + указатель
    Triad   // 3 активные точки: база + опорная точка + указатель
}

[System.Serializable]
public class ScreenPlaneV2
{
    public Vector3 origin = Vector3.zero;
    public Vector3 xAxis = Vector3.right;
    public Vector3 yAxis = Vector3.up;
    public Vector3 normal = Vector3.forward;
    public float widthMeters = 2.20f;
    public float heightMeters = 1.24f;
    public int widthPixels = 3840;
    public int heightPixels = 2160;
}

[System.Serializable]
public class SegmentPoseV2
{
    public string bodyPoint;
    public Vector3 positionMeters;
    public Quaternion rotation;
    public Vector3 velocityMetersPerSecond;
    public float confidence = 1.0f;
}

public static class BodyRayPointerCoreV2
{
    // Пересечение луча с плоскостью экрана.
    public static bool RayPlaneIntersection(Vector3 origin, Vector3 direction, ScreenPlaneV2 screen, out Vector3 hit)
    {
        direction.Normalize();
        Vector3 n = screen.normal.normalized;
        float denom = Vector3.Dot(direction, n);
        if (Mathf.Abs(denom) < 0.000001f)
        {
            hit = Vector3.zero;
            return false;
        }

        float lambda = Vector3.Dot(screen.origin - origin, n) / denom;
        if (lambda < 0f)
        {
            hit = Vector3.zero;
            return false;
        }

        hit = origin + lambda * direction;
        return true;
    }

    // Перевод точки на плоскости экрана в пиксели.
    public static Vector2 HitToPixels(Vector3 hit, ScreenPlaneV2 screen)
    {
        Vector3 rel = hit - screen.origin;
        float xMeters = Vector3.Dot(rel, screen.xAxis.normalized);
        float yMeters = Vector3.Dot(rel, screen.yAxis.normalized);

        float xPx = xMeters / screen.widthMeters * screen.widthPixels;
        float yPx = yMeters / screen.heightMeters * screen.heightPixels;
        return new Vector2(xPx, yPx);
    }

    // Главная функция: сегмент тела -> курсор на экране.
    public static bool SegmentToCursor(
        SegmentPoseV2 pointerPose,
        Vector3 mountOffsetLocal,
        Vector3 laserVectorLocal,
        ScreenPlaneV2 screen,
        out Vector2 cursorPx)
    {
        Vector3 origin = pointerPose.positionMeters + pointerPose.rotation * mountOffsetLocal;
        Vector3 direction = pointerPose.rotation * laserVectorLocal.normalized;

        if (!RayPlaneIntersection(origin, direction, screen, out Vector3 hit))
        {
            cursorPx = Vector2.zero;
            return false;
        }

        cursorPx = HitToPixels(hit, screen);
        return true;
    }

    // Мягкое ограничение расстояния между двумя точками тела.
    // Пример: плечо-предплечье не должны "растянуться" из-за дрейфа IMU.
    public static Vector3 EnforceDistance(Vector3 parent, Vector3 child, float expectedDistanceMeters)
    {
        Vector3 d = child - parent;
        if (d.sqrMagnitude < 0.000001f)
        {
            d = Vector3.right;
        }
        return parent + d.normalized * expectedDistanceMeters;
    }
}
