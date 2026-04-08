using UnityEngine;
using UnityEngine.UI;

public class ToolbarButtonState : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrackEditor trackEditor;

    [SerializeField] private Image selectImage;
    [SerializeField] private Image pathImage;
    [SerializeField] private Image squareImage;
    [SerializeField] private Image circleImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color32(23, 54, 67, 255);
    [SerializeField] private Color activeColor = new Color32(39, 199, 217, 255);

    private void Start()
    {
        RefreshVisual();
    }

    public void SelectTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.Select);
        RefreshVisual();
    }

    public void PathTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.AddPath);
        RefreshVisual();
    }

    public void SquareTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.AddSquare);
        RefreshVisual();
    }

    public void CircleTool()
    {
        if (trackEditor == null) return;
        trackEditor.SetTool(TrackEditor.ToolMode.AddCircle);
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (trackEditor == null) return;

        SetState(selectImage, trackEditor.CurrentTool == TrackEditor.ToolMode.Select);
        SetState(pathImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddPath);
        SetState(squareImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddSquare);
        SetState(circleImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddCircle);
    }

    private void SetState(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? activeColor : normalColor;
    }
}