using UnityEngine;

public static class TrackIntersectionUtility
{
    public static bool TrySegmentIntersection(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d,
        out Vector2 intersection,
        out float tOnAB,
        out float tOnCD)
    {
        intersection = Vector2.zero;
        tOnAB = 0f;
        tOnCD = 0f;

        Vector2 r = b - a;
        Vector2 s = d - c;
        float denominator = Cross(r, s);

        if (Mathf.Abs(denominator) < 0.000001f)
            return false;

        Vector2 ca = c - a;
        float t = Cross(ca, s) / denominator;
        float u = Cross(ca, r) / denominator;

        // Важно: пересечение на самом конце сегмента тоже является переходом.
        // Раньше endpoint-пересечения отбрасывались, из-за этого составной трек
        // мог уйти в fallback и нарисовать диагональный соединитель.
        const float eps = 0.001f;
        if (t < -eps || t > 1f + eps || u < -eps || u > 1f + eps)
            return false;

        tOnAB = Mathf.Clamp01(t);
        tOnCD = Mathf.Clamp01(u);
        intersection = a + r * tOnAB;
        return true;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
}
