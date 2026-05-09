using System.Collections.Generic;
using UnityEngine;

public class PacemakerRunner : MonoBehaviour
{
    // Speed unit according to the technical specification:
    // percent of the full track length per minute.
    // 100 = one full route per 60 seconds, 300 = one full route per 20 seconds.
    private const float PercentToRoute = 1f / 100f;
    private const float SecondsPerMinute = 60f;
    private const float LegacyInternalSpeedMax = 0.02f;
    private const float LegacyInternalToPercentPerMinute = 100000f;

    [SerializeField] private float size = 0.2f;
    [SerializeField] private Color color = Color.red;

    private PacemakerSettingsData settings;
    private List<PacemakerTarget> targets = new();

    private GameObject visual;

    private int currentTargetIndex;
    private bool forward = true;
    private float progress;

    private readonly List<float> segmentLengths = new();
    private float totalLength;

    private bool isRunning;

    public void Configure(PacemakerSettingsData s, List<PacemakerTarget> t)
    {
        settings = s;
        targets = t ?? new List<PacemakerTarget>();

        currentTargetIndex = 0;
        progress = 0f;
        forward = true;

        BuildCache();
        CreateVisual();
    }

    public void StartRunner()
    {
        if (targets.Count == 0)
            return;

        isRunning = true;
        if (visual != null)
            visual.SetActive(true);
    }

    public void StopRunner()
    {
        isRunning = false;
        if (visual != null)
            visual.SetActive(false);
    }

    private void Update()
    {
        if (!isRunning || settings == null || targets.Count == 0)
            return;

        float speedPercentPerMinute = NormalizeSpeedPercentPerMinute(settings.speed);
        float routePerSecond = speedPercentPerMinute * PercentToRoute / SecondsPerMinute;
        float delta = routePerSecond * Time.deltaTime;

        if (settings.loopMode == PacemakerLoopMode.PingPong)
        {
            progress += forward ? delta : -delta;

            if (progress >= 1f)
            {
                progress = 1f;
                forward = false;
            }
            else if (progress <= 0f)
            {
                progress = 0f;
                forward = true;
            }
        }
        else
        {
            progress += delta;

            if (progress > 1f)
            {
                progress = 0f;
                NextTarget();
            }
        }

        Vector2 pos = Evaluate(progress);
        if (visual != null)
            visual.transform.position = new Vector3(pos.x, pos.y, -1f);
    }

    private float NormalizeSpeedPercentPerMinute(float speed)
    {
        if (speed <= 0f)
            return 0f;

        // Old projects stored pacemaker speed as a tiny internal value, e.g. 0.003.
        // In the new system this corresponds to 300 %/min.
        if (speed <= LegacyInternalSpeedMax)
            return speed * LegacyInternalToPercentPerMinute;

        return speed;
    }

    private void NextTarget()
    {
        if (targets.Count == 1)
            return;

        currentTargetIndex = (currentTargetIndex + 1) % targets.Count;
        BuildCache();
    }

    private void CreateVisual()
    {
        if (visual != null)
            return;

        visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.transform.SetParent(transform);
        visual.transform.localScale = Vector3.one * size;

        Destroy(visual.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        visual.GetComponent<MeshRenderer>().material = mat;
    }

    public Vector2 GetCurrentPosition()
    {
        return visual != null ? (Vector2)visual.transform.position : Vector2.zero;
    }

    public void SetColor(Color c)
    {
        if (visual != null)
            visual.GetComponent<MeshRenderer>().material.color = c;
    }

    private void BuildCache()
    {
        segmentLengths.Clear();
        totalLength = 0f;

        if (targets.Count == 0)
            return;

        var target = targets[currentTargetIndex];

        if (target.targetType != PacemakerTargetType.Path || target.pathData == null || target.pathData.segments == null)
            return;

        foreach (var seg in target.pathData.segments)
        {
            if (seg == null)
                continue;

            float len = Vector2.Distance(seg.startPoint, seg.endPoint);
            segmentLengths.Add(len);
            totalLength += len;
        }
    }

    private Vector2 Evaluate(float t)
    {
        var target = targets[currentTargetIndex];

        if (target.targetType == PacemakerTargetType.Square)
            return Square(target.shapeData, t);

        return Path(target.pathData, t);
    }

    private Vector2 Path(PathData path, float t)
    {
        if (path == null || path.segments == null || path.segments.Count == 0 || totalLength <= 0f)
            return Vector2.zero;

        float dist = t * totalLength;
        float acc = 0;

        for (int i = 0; i < path.segments.Count; i++)
        {
            var seg = path.segments[i];
            float len = i < segmentLengths.Count ? segmentLengths[i] : Vector2.Distance(seg.startPoint, seg.endPoint);

            if (len <= 0.0001f)
                continue;

            if (acc + len >= dist)
            {
                float local = (dist - acc) / len;
                return Vector2.Lerp(seg.startPoint, seg.endPoint, local);
            }

            acc += len;
        }

        return path.segments[^1].endPoint;
    }

    private Vector2 Square(ShapeData s, float t)
    {
        if (s == null)
            return Vector2.zero;

        if (s.shapeType == ShapeType.Circle)
            return Circle(s, t);

        float hw = s.width / 2f;
        float hh = s.height / 2f;

        Vector2 tl = new(s.center.x - hw, s.center.y + hh);
        Vector2 tr = new(s.center.x + hw, s.center.y + hh);
        Vector2 br = new(s.center.x + hw, s.center.y - hh);
        Vector2 bl = new(s.center.x - hw, s.center.y - hh);

        float p = t * 4f;

        if (p < 1f) return Vector2.Lerp(tl, tr, p);
        if (p < 2f) return Vector2.Lerp(tr, br, p - 1f);
        if (p < 3f) return Vector2.Lerp(br, bl, p - 2f);
        return Vector2.Lerp(bl, tl, p - 3f);
    }

    private Vector2 Circle(ShapeData s, float t)
    {
        float radius = Mathf.Max(0.01f, s.radius);
        float angle = t * Mathf.PI * 2f;

        return new Vector2(
            s.center.x + Mathf.Cos(angle) * radius,
            s.center.y + Mathf.Sin(angle) * radius
        );
    }
}
