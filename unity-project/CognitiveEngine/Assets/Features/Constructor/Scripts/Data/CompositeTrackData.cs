using System;
using System.Collections.Generic;

[Serializable]
public class CompositeTrackData
{
    public string id;
    public string name;

    public BodyPointId bodyPointId = BodyPointId.RightForearm;
    public string bodyPointZoneId = "zone_right_forearm";
    public string trackerId = "WT901_01";
    public string cursorId = "cursor_1";
    public List<string> shapeIds = new List<string>();

    // ВАЖНО: значения enum зафиксированы явно, чтобы JSON сохранял и загружал режимы стабильно.
    // 0 — фиксированный маршрут.
    // 1 — случайная сторона при запуске.
    // 2 — случайная сторона на каждом переходе.
    // 3 — последовательный режим, зарезервирован для следующих этапов.
    public CompositeTransitionMode transitionMode = CompositeTransitionMode.RandomSideOnStart;

    public int randomSeed = 0;
    public int routeSteps = 700;
}

public enum CompositeTransitionMode
{
    FixedRoute = 0,
    RandomSideOnStart = 1,
    RandomSideAtEachIntersection = 2,
    Sequential = 3
}
