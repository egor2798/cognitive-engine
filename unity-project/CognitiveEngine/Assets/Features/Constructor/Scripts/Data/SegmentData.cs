using System;
using UnityEngine;

[Serializable]
public class SegmentData
{
    public string id;
    public SegmentType segmentType = SegmentType.Line;

    public Vector2 startPoint;
    public Vector2 endPoint;

    public Vector2 controlPoint1;
    public Vector2 controlPoint2;
}