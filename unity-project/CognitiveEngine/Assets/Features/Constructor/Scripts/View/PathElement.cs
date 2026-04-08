using System.Collections.Generic;
using UnityEngine;

public class PathElement : MonoBehaviour
{
    private readonly List<SegmentElement> segmentElements = new();

    public PathData Data { get; private set; }
    public IReadOnlyList<SegmentElement> SegmentElements => segmentElements;

    public void Initialize(PathData data)
    {
        Data = data;
    }

    public void AddSegmentElement(SegmentElement segmentElement)
    {
        segmentElements.Add(segmentElement);
    }

    public void RemoveSegmentElement(SegmentElement segmentElement)
    {
        segmentElements.Remove(segmentElement);
    }

    public void ClearSegmentCache()
    {
        segmentElements.Clear();
    }
}