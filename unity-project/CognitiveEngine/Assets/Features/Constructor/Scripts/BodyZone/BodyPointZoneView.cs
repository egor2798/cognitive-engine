using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BodyPointZoneView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TMP_Text titleText;

    [Header("Colors")]
    [SerializeField] private Color inactiveTint = new Color32(255, 255, 255, 28);
    [SerializeField] private Color activeTint = new Color32(39, 199, 217, 60);
    [SerializeField] private Color selectedTint = new Color32(255, 235, 59, 95);
    [SerializeField] private Color borderColor = new Color32(230, 245, 255, 130);

    public BodyPointZoneData Data { get; private set; }

    private void Awake()
    {
        DisableRaycasts();
    }

    public void Bind(BodyPointZoneData data, RectTransform workingPlane)
    {
        Data = data;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        DisableRaycasts();
        ApplyRect(workingPlane);
        Refresh(false);
    }

    private void DisableRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic g in graphics)
        {
            g.raycastTarget = false;
        }
    }

    public void ApplyRect(RectTransform workingPlane)
    {
        if (Data == null || rectTransform == null || workingPlane == null)
            return;

        Rect parentRect = workingPlane.rect;

        float px = Data.x * parentRect.width;
        float py = Data.y * parentRect.height;
        float pw = Data.width * parentRect.width;
        float ph = Data.height * parentRect.height;

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);

        rectTransform.anchoredPosition = new Vector2(px, -py);
        rectTransform.sizeDelta = new Vector2(pw, ph);
    }

    public void Refresh(bool selected)
    {
        if (Data == null)
            return;

        if (titleText != null)
        {
            string prefix = Data.active ? "● " : "";
            titleText.text = prefix + BodyPointCatalog.GetDisplayName(Data.bodyPointId);
        }

        if (backgroundImage != null)
        {
            if (selected)
                backgroundImage.color = selectedTint;
            else if (Data.active)
                backgroundImage.color = activeTint;
            else
                backgroundImage.color = inactiveTint;
        }

        if (borderImage != null)
            borderImage.color = borderColor;
    }
}
