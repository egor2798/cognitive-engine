using System.Collections.Generic;

public class PacemakerTarget
{
    public string id;
    public PacemakerTargetType targetType;

    public PathData pathData;
    public ShapeData shapeData;
    public CompositeTrackData compositeData;
    public List<ShapeData> compositeShapes;

    public PacemakerTarget(string id, PathData path)
    {
        this.id = id;
        pathData = path;
        targetType = PacemakerTargetType.Path;
    }

    public PacemakerTarget(string id, ShapeData shape, PacemakerTargetType type)
    {
        this.id = id;
        shapeData = shape;
        targetType = type;
    }

    public PacemakerTarget(string id, CompositeTrackData composite, List<ShapeData> shapes)
    {
        this.id = id;
        compositeData = composite;
        compositeShapes = shapes;
        targetType = PacemakerTargetType.Composite;
    }
}

