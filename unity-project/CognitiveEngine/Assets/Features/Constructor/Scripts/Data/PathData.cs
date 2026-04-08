using System;
using System.Collections.Generic;

[Serializable]
public class PathData
{
    public string id;
    public List<SegmentData> segments = new();
}