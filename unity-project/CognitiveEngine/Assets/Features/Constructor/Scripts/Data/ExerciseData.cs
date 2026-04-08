using System;
using System.Collections.Generic;

[Serializable]
public class ExerciseData
{
    public string id;
    public string name;

    public List<ShapeData> shapes = new();
    public List<PathData> paths = new();
}