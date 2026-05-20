using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CommandButtonStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color32(23, 54, 67, 255);
    [SerializeField] private Color hoverColor = new Color32(31, 82, 98, 255);
    [SerializeField] private Color pressedColor = new Color32(39, 199, 217, 255);

    private bool isPointerInside;

    private void Reset()
    {
        backgroundImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        SetNormal();
    }

    private void OnEnable()
    {
        SetNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        SetColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        SetNormal();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetColor(pressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetColor(isPointerInside ? hoverColor : normalColor);
    }

    public void SetNormal()
    {
        SetColor(normalColor);
    }

    private void SetColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }
}