using System;

[Serializable]
public class BodyRayUnityPacket
{
    public string type = "body_ray_pointer_v2";
    public double timeSec;

    public string trackingMode = "single";
    public int bodyPointId = 5;
    public string bodyPointZoneId = "zone_right_forearm";
    public string trackerId = "WT901_01";
    public string cursorId = "cursor_1";

    public float rawWorldX;
    public float rawWorldY;
    public float rawXMm;
    public float rawYMm;

    public float correctedWorldX;
    public float correctedWorldY;
    public float correctedXMm;
    public float correctedYMm;

    public float correctionMm;
    public float confidence = 1f;
    public string anchorId = "";
    public float loopClosureErrorMm;
    public string calibrationStatus = "not_applied";
    public string signalStatus = "ok";

    public float xPx;
    public float yPx;
    public float xCm;
    public float yCm;
}
