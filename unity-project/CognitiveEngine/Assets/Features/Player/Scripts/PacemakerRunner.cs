using System.Collections.Generic;
using UnityEngine;

public class PacemakerRunner : MonoBehaviour
{
    [SerializeField] private float size = 0.2f;
    [SerializeField] private Color color = Color.red;

    private PacemakerSettingsData settings;
    private List<PacemakerTarget> targets = new();

    private GameObject visual;

    private int currentTargetIndex;
    private bool forward = true;
    private float progress;

    private List<float> segmentLengths = new();
    private float totalLength;

    private bool isRunning;

    public void Configure(PacemakerSettingsData s, List<PacemakerTarget> t)
    {
        settings = s;
        targets = t;

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
        if (!isRunning)
            return;

        progress += settings.speed * 60f * Time.deltaTime;

        if (progress > 1f)
        {
            progress = 0f;
            NextTarget();
        }

        Vector2 pos = Evaluate(progress);
        visual.transform.position = new Vector3(pos.x, pos.y, -1f);
    }

    private void NextTarget()
    {
        if (targets.Count == 1)
            return;

        if (settings.loopMode == PacemakerLoopMode.Loop)
        {
            currentTargetIndex = (currentTargetIndex + 1) % targets.Count;
        }
        else
        {
            if (forward)
            {
                currentTargetIndex++;
                if (currentTargetIndex >= targets.Count)
                {
                    currentTargetIndex = targets.Count - 1;
                    forward = false;
                }
            }
            else
            {
                currentTargetIndex--;
                if (currentTargetIndex < 0)
                {
                    currentTargetIndex = 0;
                    forward = true;
                }
            }
        }

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

        var target = targets[currentTargetIndex];

        if (target.targetType != PacemakerTargetType.Path)
            return;

        foreach (var seg in target.pathData.segments)
        {
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
        if (path == null || path.segments.Count == 0)
            return Vector2.zero;

        float dist = t * totalLength;
        float acc = 0;

        for (int i = 0; i < path.segments.Count; i++)
        {
            var seg = path.segments[i];
            float len = segmentLengths[i];

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
        float hw = s.width / 2;
        float hh = s.height / 2;

        Vector2 tl = new(s.center.x - hw, s.center.y + hh);
        Vector2 tr = new(s.center.x + hw, s.center.y + hh);
        Vector2 br = new(s.center.x + hw, s.center.y - hh);
        Vector2 bl = new(s.center.x - hw, s.center.y - hh);

        float p = t * 4f;

        if (p < 1) return Vector2.Lerp(tl, tr, p);
        if (p < 2) return Vector2.Lerp(tr, br, p - 1);
        if (p < 3) return Vector2.Lerp(br, bl, p - 2);
        return Vector2.Lerp(bl, tl, p - 3);
    }
}