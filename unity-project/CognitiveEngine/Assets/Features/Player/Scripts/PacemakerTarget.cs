public class PacemakerTarget
{
    public string id;
    public PacemakerTargetType targetType;

    public PathData pathData;
    public ShapeData shapeData;

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
}