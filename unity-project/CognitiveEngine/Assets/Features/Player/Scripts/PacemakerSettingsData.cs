using System;
using System.Collections.Generic;

[Serializable]
public class PacemakerSettingsData
{
    public string id;
    public bool enabled = true;

    // Единица измерения по ТЗ: процент длины трека в минуту.
    // 100 = весь маршрут за 60 секунд, 300 = за 20 секунд, 600 = за 10 секунд.
    public float speed = 300f;

    public PacemakerLoopMode loopMode = PacemakerLoopMode.Loop;

    public List<string> targetIds = new();
}
