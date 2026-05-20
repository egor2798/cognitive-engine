using UnityEngine;
using UnityEngine.UI;

public class ToolbarButtonState : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrackEditor trackEditor;

    [Header("Tool Button Background Images")]
    [SerializeField] private Image selectImage;
    [SerializeField] private Image pathImage;
    [SerializeField] private Image squareImage;
    [SerializeField] private Image circleImage;
    [SerializeField] private Image triangleImage;
    [SerializeField] private Image diamondImage;
    [SerializeField] private Image pentagonImage;
    [SerializeField] private Image hexagonImage;
    [SerializeField] private Image starImage;
    [SerializeField] private Image star8Image;
    [SerializeField] private Image arcImage;
    [SerializeField] private Image zigzagImage;
    [SerializeField] private Image figureEightImage;
    [SerializeField] private Image spiralImage;
    [SerializeField] private Image targetImage;
    [SerializeField] private Image tricycleImage;

    [Header("Command Button Background Images")]
    [SerializeField] private Image compositeTrackImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color32(23, 54, 67, 255);
    [SerializeField] private Color activeColor = new Color32(39, 199, 217, 255);

    private void Start()
    {
        RefreshVisual();
    }

    private void OnEnable()
    {
        RefreshVisual();
    }

    public void SelectTool()
    {
        SetTool(TrackEditor.ToolMode.Select);
    }

    public void PathTool()
    {
        SetTool(TrackEditor.ToolMode.AddPath);
    }

    public void SquareTool()
    {
        SetTool(TrackEditor.ToolMode.AddSquare);
    }

    public void CircleTool()
    {
        SetTool(TrackEditor.ToolMode.AddCircle);
    }

    public void TriangleTool()
    {
        SetTool(TrackEditor.ToolMode.AddTriangle);
    }

    public void DiamondTool()
    {
        SetTool(TrackEditor.ToolMode.AddDiamond);
    }

    public void PentagonTool()
    {
        SetTool(TrackEditor.ToolMode.AddPentagon);
    }

    public void HexagonTool()
    {
        SetTool(TrackEditor.ToolMode.AddHexagon);
    }

    public void StarTool()
    {
        SetTool(TrackEditor.ToolMode.AddStar);
    }

    public void Star8Tool()
    {
        SetTool(TrackEditor.ToolMode.AddStar8);
    }

    public void ArcTool()
    {
        SetTool(TrackEditor.ToolMode.AddArc);
    }

    public void ZigzagTool()
    {
        SetTool(TrackEditor.ToolMode.AddZigzag);
    }

    public void FigureEightTool()
    {
        SetTool(TrackEditor.ToolMode.AddFigureEight);
    }

    public void SpiralTool()
    {
        SetTool(TrackEditor.ToolMode.AddSpiral);
    }

    public void TargetTool()
    {
        SetTool(TrackEditor.ToolMode.AddTarget);
    }

    public void TricycleTool()
    {
        SetTool(TrackEditor.ToolMode.AddTricycle);
    }

    public void CompositeTrackTool()
    {
        if (trackEditor == null)
        {
            Debug.LogWarning("ToolbarButtonState: TrackEditor не назначен.");
            return;
        }

        trackEditor.CreateCompositeTrackFromAllShapes();
        RefreshVisual();
    }

    private void SetTool(TrackEditor.ToolMode toolMode)
    {
        if (trackEditor == null)
        {
            Debug.LogWarning("ToolbarButtonState: TrackEditor не назначен.");
            return;
        }

        trackEditor.SetTool(toolMode);
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (trackEditor == null)
            return;

        SetState(selectImage, trackEditor.CurrentTool == TrackEditor.ToolMode.Select);
        SetState(pathImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddPath);
        SetState(squareImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddSquare);
        SetState(circleImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddCircle);
        SetState(triangleImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddTriangle);
        SetState(diamondImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddDiamond);
        SetState(pentagonImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddPentagon);
        SetState(hexagonImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddHexagon);
        SetState(starImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddStar);
        SetState(star8Image, trackEditor.CurrentTool == TrackEditor.ToolMode.AddStar8);
        SetState(arcImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddArc);
        SetState(zigzagImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddZigzag);
        SetState(figureEightImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddFigureEight);
        SetState(spiralImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddSpiral);
        SetState(targetImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddTarget);
        SetState(tricycleImage, trackEditor.CurrentTool == TrackEditor.ToolMode.AddTricycle);

        // Это командная кнопка, а не активный режим инструмента.
        // Поэтому она всегда остаётся в обычном цвете.
        SetState(compositeTrackImage, false);
    }

    private void SetState(Image img, bool active)
    {
        if (img == null)
            return;

        img.color = active ? activeColor : normalColor;
    }
}