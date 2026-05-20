using System;
using System.Collections.Generic;

[Serializable]
public class PathData
{
    public string id;

    public BodyPointId bodyPointId = BodyPointId.RightForearm;
    public string bodyPointZoneId = "zone_right_forearm";
    public string trackerId = "WT901_01";
    public string cursorId = "cursor_1";
    public List<SegmentData> segments = new();
}