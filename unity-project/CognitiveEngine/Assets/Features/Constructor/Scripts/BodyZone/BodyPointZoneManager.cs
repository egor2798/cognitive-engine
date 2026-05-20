using System;
using System.Collections.Generic;
using UnityEngine;

public class BodyPointZoneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform workingPlane;
    [SerializeField] private BodyPointZoneView zoneViewPrefab;

    [Header("Optional")]
    [Tooltip("Если указать сюда окно настроек, зоны будут скрываться, пока окно открыто.")]
    [SerializeField] private GameObject settingsWindow;

    [Header("Rules")]
    [SerializeField] private int maxActiveZones = 3;
    [SerializeField] private bool showZonesInConstructor = true;

    [Header("Layering")]
    [SerializeField] private bool keepZonesBehindObjects = true;

    private readonly List<BodyPointZoneData> zones = new List<BodyPointZoneData>();
    private readonly Dictionary<BodyPointId, BodyPointZoneView> views = new Dictionary<BodyPointId, BodyPointZoneView>();

    public IReadOnlyList<BodyPointZoneData> Zones => zones;
    public BodyPointId SelectedBodyPoint { get; private set; } = BodyPointId.RightForearm;

    public event Action<BodyPointId> OnSelectedBodyPointChanged;
    public event Action OnZonesChanged;

    private void Awake()
    {
        CreateDefaultZones();
    }

    private void Start()
    {
        RebuildViews();
        SetBodyPointActive(BodyPointId.RightForearm, true);
        SelectBodyPoint(BodyPointId.RightForearm);
    }

    private void LateUpdate()
    {
        bool settingsOpened = settingsWindow != null && settingsWindow.activeInHierarchy;

        foreach (var pair in views)
        {
            if (pair.Value == null) continue;

            pair.Value.gameObject.SetActive(showZonesInConstructor && !settingsOpened);

            if (keepZonesBehindObjects)
                pair.Value.transform.SetAsFirstSibling();
        }
    }

    public void CreateDefaultZones()
    {
        zones.Clear();

        zones.Add(new BodyPointZoneData(BodyPointId.Head,          0.38f, 0.03f, 0.24f, 0.12f));
        zones.Add(new BodyPointZoneData(BodyPointId.Chest,         0.38f, 0.16f, 0.24f, 0.15f));
        zones.Add(new BodyPointZoneData(BodyPointId.BodyCore,      0.38f, 0.32f, 0.24f, 0.20f));

        zones.Add(new BodyPointZoneData(BodyPointId.LeftShoulder,  0.06f, 0.08f, 0.28f, 0.17f));
        zones.Add(new BodyPointZoneData(BodyPointId.LeftForearm,   0.06f, 0.26f, 0.28f, 0.22f));
        zones.Add(new BodyPointZoneData(BodyPointId.LeftThigh,     0.06f, 0.56f, 0.28f, 0.17f));
        zones.Add(new BodyPointZoneData(BodyPointId.LeftShin,      0.06f, 0.74f, 0.28f, 0.20f));

        zones.Add(new BodyPointZoneData(BodyPointId.RightShoulder, 0.66f, 0.08f, 0.28f, 0.17f));
        zones.Add(new BodyPointZoneData(BodyPointId.RightForearm,  0.66f, 0.26f, 0.28f, 0.22f));
        zones.Add(new BodyPointZoneData(BodyPointId.RightThigh,    0.66f, 0.56f, 0.28f, 0.17f));
        zones.Add(new BodyPointZoneData(BodyPointId.RightShin,     0.66f, 0.74f, 0.28f, 0.20f));
    }

    public void RebuildViews()
    {
        if (workingPlane == null || zoneViewPrefab == null)
        {
            Debug.LogWarning("BodyPointZoneManager: workingPlane or zoneViewPrefab is not assigned.");
            return;
        }

        foreach (Transform child in workingPlane)
        {
            BodyPointZoneView existing = child.GetComponent<BodyPointZoneView>();
            if (existing != null)
                Destroy(child.gameObject);
        }

        views.Clear();

        foreach (BodyPointZoneData zone in zones)
        {
            BodyPointZoneView view = Instantiate(zoneViewPrefab, workingPlane);
            view.name = "Zone_" + BodyPointCatalog.GetStableCode(zone.bodyPointId);
            view.Bind(zone, workingPlane);

            if (keepZonesBehindObjects)
                view.transform.SetAsFirstSibling();

            view.gameObject.SetActive(showZonesInConstructor);
            views[zone.bodyPointId] = view;
        }

        RefreshViews();
    }

    public void SelectBodyPointFromInt(int bodyPointInt)
    {
        SelectBodyPoint((BodyPointId)bodyPointInt);
    }

    public void SelectBodyPoint(BodyPointId bodyPointId)
    {
        if (bodyPointId == BodyPointId.None)
            return;

        SelectedBodyPoint = bodyPointId;
        SetBodyPointActive(bodyPointId, true);

        RefreshViews();
        OnSelectedBodyPointChanged?.Invoke(bodyPointId);

        Debug.Log("Selected body point: " + BodyPointCatalog.GetDisplayName(bodyPointId));
    }

    public bool SetBodyPointActive(BodyPointId bodyPointId, bool active)
    {
        BodyPointZoneData zone = GetZone(bodyPointId);
        if (zone == null)
            return false;

        if (active && !zone.active && CountActiveZones() >= maxActiveZones)
        {
            Debug.LogWarning("Cannot activate body point: max active zones = " + maxActiveZones);
            return false;
        }

        zone.active = active;
        RefreshViews();
        OnZonesChanged?.Invoke();
        return true;
    }

    public BodyPointZoneData GetSelectedZone()
    {
        return GetZone(SelectedBodyPoint);
    }

    public BodyPointZoneData GetZone(BodyPointId bodyPointId)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i].bodyPointId == bodyPointId)
                return zones[i];
        }

        return null;
    }

    public List<BodyPointZoneData> GetActiveZones()
    {
        List<BodyPointZoneData> result = new List<BodyPointZoneData>();

        foreach (BodyPointZoneData zone in zones)
        {
            if (zone.active)
                result.Add(zone);
        }

        return result;
    }

    public int CountActiveZones()
    {
        int count = 0;

        foreach (BodyPointZoneData zone in zones)
        {
            if (zone.active)
                count++;
        }

        return count;
    }

    public void SetZonesVisible(bool visible)
    {
        showZonesInConstructor = visible;

        foreach (var pair in views)
        {
            if (pair.Value != null)
                pair.Value.gameObject.SetActive(visible);
        }
    }

    private void RefreshViews()
    {
        foreach (var pair in views)
        {
            bool selected = pair.Key == SelectedBodyPoint;
            if (pair.Value != null)
                pair.Value.Refresh(selected);
        }
    }
}
