using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShapeData
{
    public string id;
    public ShapeType shapeType;

    public BodyPointId bodyPointId = BodyPointId.RightForearm;
    public string bodyPointZoneId = "zone_right_forearm";
    public string trackerId = "WT901_01";
    public string cursorId = "cursor_1";

    public Vector2 center;
    public float width = 1f;
    public float height = 1f;
    public float radius = 0.5f;

    // Для открытых дуг: углы в градусах.
    // 0 градусов = вправо, 90 = вверх.
    public float arcStartAngle = 210f;
    public float arcEndAngle = -30f;

    // Для спирали Архимеда.
    public float spiralTurns = 3f;

    // Для мишени/трицикла: количество концентрических окружностей.
    public int ringCount = 3;

    public List<Vector2> points = new();
}
