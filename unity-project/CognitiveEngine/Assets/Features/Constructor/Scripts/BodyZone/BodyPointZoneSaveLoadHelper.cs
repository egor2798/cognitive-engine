using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BodyPointZoneCollectionData
{
    public BodyPointId selectedBodyPoint;
    public List<BodyPointZoneData> bodyPointZones = new List<BodyPointZoneData>();
}

public class BodyPointZoneSaveLoadHelper : MonoBehaviour
{
    [SerializeField] private BodyPointZoneManager zoneManager;

    public BodyPointZoneCollectionData Capture()
    {
        BodyPointZoneCollectionData result = new BodyPointZoneCollectionData();

        if (zoneManager == null)
            return result;

        result.selectedBodyPoint = zoneManager.SelectedBodyPoint;
        result.bodyPointZones = new List<BodyPointZoneData>();

        foreach (BodyPointZoneData zone in zoneManager.Zones)
        {
            BodyPointZoneData copy = new BodyPointZoneData();
            copy.zoneId = zone.zoneId;
            copy.bodyPointId = zone.bodyPointId;
            copy.macroSection = zone.macroSection;
            copy.x = zone.x;
            copy.y = zone.y;
            copy.width = zone.width;
            copy.height = zone.height;
            copy.active = zone.active;
            copy.visibleForDoctor = zone.visibleForDoctor;
            copy.visibleForPatient = zone.visibleForPatient;
            copy.cursorId = zone.cursorId;
            copy.trackerId = zone.trackerId;
            copy.trackId = zone.trackId;
            copy.corridorId = zone.corridorId;
            copy.pacerId = zone.pacerId;
            result.bodyPointZones.Add(copy);
        }

        return result;
    }

    public string CaptureJsonPretty()
    {
        return JsonUtility.ToJson(Capture(), true);
    }
}
