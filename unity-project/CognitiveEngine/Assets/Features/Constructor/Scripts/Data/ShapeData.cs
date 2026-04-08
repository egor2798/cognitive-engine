using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShapeData
{
    public string id;
    public ShapeType shapeType;

    public Vector2 center;
    public float width = 1f;
    public float height = 1f;
    public float radius = 0.5f;

    public List<Vector2> points = new();
}