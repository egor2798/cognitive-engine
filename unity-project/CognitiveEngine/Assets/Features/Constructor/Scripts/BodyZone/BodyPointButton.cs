using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BodyPointButton : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BodyPointId bodyPointId;

    [Header("References")]
    [SerializeField] private BodyPointZoneManager zoneManager;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text labelText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color32(23, 54, 67, 255);
    [SerializeField] private Color selectedColor = new Color32(39, 199, 217, 255);
    [SerializeField] private Color activeColor = new Color32(35, 110, 130, 255);

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClicked);

        if (labelText != null)
            labelText.text = BodyPointCatalog.GetDisplayName(bodyPointId);
    }

    private void OnEnable()
    {
        if (zoneManager != null)
        {
            zoneManager.OnSelectedBodyPointChanged += HandleSelectedChanged;
            zoneManager.OnZonesChanged += RefreshVisual;
        }

        RefreshVisual();
    }

    private void OnDisable()
    {
        if (zoneManager != null)
        {
            zoneManager.OnSelectedBodyPointChanged -= HandleSelectedChanged;
            zoneManager.OnZonesChanged -= RefreshVisual;
        }
    }

    private void OnClicked()
    {
        if (zoneManager == null)
            return;

        zoneManager.SelectBodyPoint(bodyPointId);
        RefreshVisual();
    }

    private void HandleSelectedChanged(BodyPointId selected)
    {
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (zoneManager == null || backgroundImage == null)
            return;

        BodyPointZoneData zone = zoneManager.GetZone(bodyPointId);
        bool isSelected = zoneManager.SelectedBodyPoint == bodyPointId;
        bool isActive = zone != null && zone.active;

        if (isSelected)
            backgroundImage.color = selectedColor;
        else if (isActive)
            backgroundImage.color = activeColor;
        else
            backgroundImage.color = normalColor;
    }
}
