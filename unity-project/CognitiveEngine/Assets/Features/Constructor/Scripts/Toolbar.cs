using UnityEngine;

public class Toolbar : MonoBehaviour
{
    [SerializeField] private TrackEditor trackEditor;

    public void SelectTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.Select);
    }

    public void AddPathTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.AddPath);
    }

    public void AddSquareTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.AddSquare);
    }

    public void AddCircleTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.AddCircle);
    }

    public void ClearAllShapes()
    {
        if (trackEditor == null) return;
        trackEditor.ClearAllShapes();
    }
}