using System;
using UnityEngine;

[Serializable]
public class BodyPointZoneData
{
    public string zoneId;
    public BodyPointId bodyPointId;
    public BodyMacroSection macroSection;

    // Нормализованные координаты внутри рабочей плоскости: 0..1
    public float x;
    public float y;
    public float width;
    public float height;

    public bool active;
    public bool visibleForDoctor = true;
    public bool visibleForPatient = false;

    public string cursorId;
    public string trackerId;
    public string trackId;
    public string corridorId;
    public string pacerId;

    public BodyPointZoneData() { }

    public BodyPointZoneData(BodyPointId bodyPointId, float x, float y, float width, float height)
    {
        this.bodyPointId = bodyPointId;
        this.macroSection = BodyPointCatalog.GetMacroSection(bodyPointId);
        this.zoneId = "zone_" + BodyPointCatalog.GetStableCode(bodyPointId);
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.active = false;
    }

    public Rect GetNormalizedRect()
    {
        return new Rect(x, y, width, height);
    }

    public bool ContainsNormalized(Vector2 p)
    {
        return p.x >= x && p.x <= x + width && p.y >= y && p.y <= y + height;
    }
}
