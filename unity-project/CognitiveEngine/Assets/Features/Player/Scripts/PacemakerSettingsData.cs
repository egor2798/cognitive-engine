using System;
using System.Collections.Generic;

[Serializable]
public class PacemakerSettingsData
{
    public string id;
    public bool enabled = true;

    public float speed = 0.003f;
    public PacemakerLoopMode loopMode = PacemakerLoopMode.Loop;

    public List<string> targetIds = new();
}