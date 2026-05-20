using UnityEngine;

public enum BodyPointId
{
    None = 0,
    Head = 1,
    Chest = 2,
    BodyCore = 3,
    RightShoulder = 4,
    RightForearm = 5,
    RightThigh = 6,
    RightShin = 7,
    LeftShoulder = 8,
    LeftForearm = 9,
    LeftThigh = 10,
    LeftShin = 11
}

public enum BodyMacroSection
{
    None = 0,
    Center = 1,
    RightArm = 2,
    RightLeg = 3,
    LeftArm = 4,
    LeftLeg = 5
}

public static class BodyPointCatalog
{
    public static string GetDisplayName(BodyPointId id)
    {
        switch (id)
        {
            case BodyPointId.Head: return "Голова";
            case BodyPointId.Chest: return "Грудь";
            case BodyPointId.BodyCore: return "Корпус / живот";
            case BodyPointId.RightShoulder: return "Правое плечо";
            case BodyPointId.RightForearm: return "Правое предплечье / кисть";
            case BodyPointId.RightThigh: return "Правое бедро";
            case BodyPointId.RightShin: return "Правая голень";
            case BodyPointId.LeftShoulder: return "Левое плечо";
            case BodyPointId.LeftForearm: return "Левое предплечье / кисть";
            case BodyPointId.LeftThigh: return "Левое бедро";
            case BodyPointId.LeftShin: return "Левая голень";
            default: return "Не выбрано";
        }
    }

    public static string GetStableCode(BodyPointId id)
    {
        switch (id)
        {
            case BodyPointId.Head: return "head";
            case BodyPointId.Chest: return "chest";
            case BodyPointId.BodyCore: return "body_core";
            case BodyPointId.RightShoulder: return "right_shoulder";
            case BodyPointId.RightForearm: return "right_forearm";
            case BodyPointId.RightThigh: return "right_thigh";
            case BodyPointId.RightShin: return "right_shin";
            case BodyPointId.LeftShoulder: return "left_shoulder";
            case BodyPointId.LeftForearm: return "left_forearm";
            case BodyPointId.LeftThigh: return "left_thigh";
            case BodyPointId.LeftShin: return "left_shin";
            default: return "none";
        }
    }

    public static BodyMacroSection GetMacroSection(BodyPointId id)
    {
        switch (id)
        {
            case BodyPointId.Head:
            case BodyPointId.Chest:
            case BodyPointId.BodyCore:
                return BodyMacroSection.Center;

            case BodyPointId.RightShoulder:
            case BodyPointId.RightForearm:
                return BodyMacroSection.RightArm;

            case BodyPointId.RightThigh:
            case BodyPointId.RightShin:
                return BodyMacroSection.RightLeg;

            case BodyPointId.LeftShoulder:
            case BodyPointId.LeftForearm:
                return BodyMacroSection.LeftArm;

            case BodyPointId.LeftThigh:
            case BodyPointId.LeftShin:
                return BodyMacroSection.LeftLeg;

            default:
                return BodyMacroSection.None;
        }
    }
}
