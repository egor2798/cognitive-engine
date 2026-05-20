using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BodyPointBindingData
{
    public BodyPointId bodyPointId;
    public string bodyPointZoneId;
    public string trackerId;
    public string cursorId;
}

public class BodyPointBindingsPanel : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown bodyPointDropdown;
    [SerializeField] private TMP_Dropdown trackerDropdown;
    [SerializeField] private TMP_Dropdown cursorDropdown;

    [Header("Buttons")]
    [SerializeField] private Button addBindingButton;
    [SerializeField] private Button clearBindingsButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text activeBindingsText;
    [SerializeField] private TMP_Text statusText;

    [Header("Limits")]
    [SerializeField] private int maxBindings = 3;

    private readonly List<BodyPointBindingData> bindings = new List<BodyPointBindingData>();

    public IReadOnlyList<BodyPointBindingData> Bindings => bindings;

    private void Awake()
    {
        BuildDropdowns();

        if (addBindingButton != null)
            addBindingButton.onClick.AddListener(AddCurrentBinding);

        if (clearBindingsButton != null)
            clearBindingsButton.onClick.AddListener(ClearBindings);

        SetStatus("Статус: -");
        RefreshListText();
    }

    private void BuildDropdowns()
    {
        BuildBodyPointDropdown();
        BuildTrackerDropdown();
        BuildCursorDropdown();
    }

    private void BuildBodyPointDropdown()
    {
        if (bodyPointDropdown == null)
            return;

        bodyPointDropdown.ClearOptions();

        List<string> options = new List<string>
        {
            BodyPointCatalog.GetDisplayName(BodyPointId.Head),
            BodyPointCatalog.GetDisplayName(BodyPointId.Chest),
            BodyPointCatalog.GetDisplayName(BodyPointId.BodyCore),

            BodyPointCatalog.GetDisplayName(BodyPointId.RightShoulder),
            BodyPointCatalog.GetDisplayName(BodyPointId.RightForearm),
            BodyPointCatalog.GetDisplayName(BodyPointId.RightThigh),
            BodyPointCatalog.GetDisplayName(BodyPointId.RightShin),

            BodyPointCatalog.GetDisplayName(BodyPointId.LeftShoulder),
            BodyPointCatalog.GetDisplayName(BodyPointId.LeftForearm),
            BodyPointCatalog.GetDisplayName(BodyPointId.LeftThigh),
            BodyPointCatalog.GetDisplayName(BodyPointId.LeftShin)
        };

        bodyPointDropdown.AddOptions(options);
        bodyPointDropdown.value = BodyPointToDropdownIndex(BodyPointId.RightForearm);
        bodyPointDropdown.RefreshShownValue();
    }

    private void BuildTrackerDropdown()
    {
        if (trackerDropdown == null)
            return;

        trackerDropdown.ClearOptions();

        trackerDropdown.AddOptions(new List<string>
        {
            "WT901_01",
            "WT901_02",
            "WT901_03"
        });

        trackerDropdown.value = 0;
        trackerDropdown.RefreshShownValue();
    }

    private void BuildCursorDropdown()
    {
        if (cursorDropdown == null)
            return;

        cursorDropdown.ClearOptions();

        cursorDropdown.AddOptions(new List<string>
        {
            "Курсор 1",
            "Курсор 2",
            "Курсор 3"
        });

        cursorDropdown.value = 0;
        cursorDropdown.RefreshShownValue();
    }

    public void AddCurrentBinding()
    {
        if (bindings.Count >= maxBindings)
        {
            SetStatus("Статус: можно добавить максимум 3 активные точки.");
            return;
        }

        BodyPointId bodyPointId = DropdownIndexToBodyPoint(bodyPointDropdown != null ? bodyPointDropdown.value : 0);
        string zoneId = "zone_" + BodyPointCatalog.GetStableCode(bodyPointId);
        string trackerId = GetDropdownText(trackerDropdown);
        string cursorId = CursorDropdownToId(cursorDropdown != null ? cursorDropdown.value : 0);

        if (HasBodyPoint(bodyPointId))
        {
            SetStatus("Статус: эта точка тела уже добавлена.");
            return;
        }

        if (HasTracker(trackerId))
        {
            SetStatus("Статус: этот датчик уже используется.");
            return;
        }

        if (HasCursor(cursorId))
        {
            SetStatus("Статус: этот целеуказатель уже используется.");
            return;
        }

        BodyPointBindingData binding = new BodyPointBindingData
        {
            bodyPointId = bodyPointId,
            bodyPointZoneId = zoneId,
            trackerId = trackerId,
            cursorId = cursorId
        };

        bindings.Add(binding);

        SetStatus("Статус: добавлено — " + BodyPointCatalog.GetDisplayName(bodyPointId));
        RefreshListText();
    }

    public void ClearBindings()
    {
        bindings.Clear();
        SetStatus("Статус: список активных точек очищен.");
        RefreshListText();
    }

    public List<BodyPointBindingData> GetBindingsCopy()
    {
        List<BodyPointBindingData> result = new List<BodyPointBindingData>();

        foreach (BodyPointBindingData binding in bindings)
        {
            result.Add(new BodyPointBindingData
            {
                bodyPointId = binding.bodyPointId,
                bodyPointZoneId = binding.bodyPointZoneId,
                trackerId = binding.trackerId,
                cursorId = binding.cursorId
            });
        }

        return result;
    }

    public void SetBindings(List<BodyPointBindingData> source)
    {
        bindings.Clear();

        if (source != null)
        {
            foreach (BodyPointBindingData binding in source)
            {
                if (binding == null)
                    continue;

                BodyPointId bodyPointId = binding.bodyPointId == BodyPointId.None
                    ? BodyPointId.RightForearm
                    : binding.bodyPointId;

                bindings.Add(new BodyPointBindingData
                {
                    bodyPointId = bodyPointId,
                    bodyPointZoneId = string.IsNullOrEmpty(binding.bodyPointZoneId)
                        ? "zone_" + BodyPointCatalog.GetStableCode(bodyPointId)
                        : binding.bodyPointZoneId,
                    trackerId = string.IsNullOrEmpty(binding.trackerId) ? "WT901_01" : binding.trackerId,
                    cursorId = string.IsNullOrEmpty(binding.cursorId) ? "cursor_1" : binding.cursorId
                });
            }
        }

        RefreshListText();
        SetStatus("Статус: данные точек тела загружены.");
    }

    public BodyPointBindingData GetPrimaryBinding()
    {
        if (bindings.Count > 0)
            return bindings[0];

        return new BodyPointBindingData
        {
            bodyPointId = BodyPointId.RightForearm,
            bodyPointZoneId = "zone_right_forearm",
            trackerId = "WT901_01",
            cursorId = "cursor_1"
        };
    }

    private void RefreshListText()
    {
        if (activeBindingsText == null)
            return;

        if (bindings.Count == 0)
        {
            activeBindingsText.text = "пока ничего не добавлено";
            return;
        }

        string text = "";

        for (int i = 0; i < bindings.Count; i++)
        {
            BodyPointBindingData b = bindings[i];

            text += $"{i + 1}. {BodyPointCatalog.GetDisplayName(b.bodyPointId)}\n";
            text += $"   Датчик: {b.trackerId}    Целеуказатель: {CursorIdToDisplayName(b.cursorId)}\n";
            text += $"   Зона: {b.bodyPointZoneId}";

            if (i < bindings.Count - 1)
                text += "\n\n";
        }

        activeBindingsText.text = text;
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;

        Debug.Log("BodyPointBindingsPanel: " + text);
    }

    private bool HasBodyPoint(BodyPointId bodyPointId)
    {
        foreach (BodyPointBindingData binding in bindings)
        {
            if (binding.bodyPointId == bodyPointId)
                return true;
        }

        return false;
    }

    private bool HasTracker(string trackerId)
    {
        foreach (BodyPointBindingData binding in bindings)
        {
            if (binding.trackerId == trackerId)
                return true;
        }

        return false;
    }

    private bool HasCursor(string cursorId)
    {
        foreach (BodyPointBindingData binding in bindings)
        {
            if (binding.cursorId == cursorId)
                return true;
        }

        return false;
    }

    private string GetDropdownText(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
            return "";

        int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
        return dropdown.options[index].text;
    }

    private BodyPointId DropdownIndexToBodyPoint(int index)
    {
        switch (index)
        {
            case 0: return BodyPointId.Head;
            case 1: return BodyPointId.Chest;
            case 2: return BodyPointId.BodyCore;

            case 3: return BodyPointId.RightShoulder;
            case 4: return BodyPointId.RightForearm;
            case 5: return BodyPointId.RightThigh;
            case 6: return BodyPointId.RightShin;

            case 7: return BodyPointId.LeftShoulder;
            case 8: return BodyPointId.LeftForearm;
            case 9: return BodyPointId.LeftThigh;
            case 10: return BodyPointId.LeftShin;

            default: return BodyPointId.RightForearm;
        }
    }

    private int BodyPointToDropdownIndex(BodyPointId id)
    {
        switch (id)
        {
            case BodyPointId.Head: return 0;
            case BodyPointId.Chest: return 1;
            case BodyPointId.BodyCore: return 2;

            case BodyPointId.RightShoulder: return 3;
            case BodyPointId.RightForearm: return 4;
            case BodyPointId.RightThigh: return 5;
            case BodyPointId.RightShin: return 6;

            case BodyPointId.LeftShoulder: return 7;
            case BodyPointId.LeftForearm: return 8;
            case BodyPointId.LeftThigh: return 9;
            case BodyPointId.LeftShin: return 10;

            default: return 4;
        }
    }

    private string CursorDropdownToId(int index)
    {
        switch (index)
        {
            case 0: return "cursor_1";
            case 1: return "cursor_2";
            case 2: return "cursor_3";
            default: return "cursor_1";
        }
    }

    private string CursorIdToDisplayName(string cursorId)
    {
        switch (cursorId)
        {
            case "cursor_1": return "Курсор 1";
            case "cursor_2": return "Курсор 2";
            case "cursor_3": return "Курсор 3";
            default: return cursorId;
        }
    }
}
