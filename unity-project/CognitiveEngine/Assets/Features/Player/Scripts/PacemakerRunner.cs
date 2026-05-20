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

    private TrackGeometry currentGeometry;
    private CompositeTrackRuntimeRoute runtimeCompositeRoute;
    private bool isRunning;

    public void Configure(PacemakerSettingsData s, List<PacemakerTarget> t)
    {
        settings = s;
        targets = t ?? new List<PacemakerTarget>();

        currentTargetIndex = 0;
        progress = 0f;
        forward = true;
        runtimeCompositeRoute = null;

        BuildCurrentGeometry();
        CreateVisual();
        ApplyVisualSettings();
        MoveVisualToCurrentProgress();
    }

    public void StartRunner()
    {
        if (targets.Count == 0)
            return;

        if ((runtimeCompositeRoute == null || !runtimeCompositeRoute.IsConfigured) &&
            (currentGeometry == null || !currentGeometry.IsValid()))
        {
            return;
        }

        isRunning = true;

        if (visual != null)
            visual.SetActive(true);

        MoveVisualToCurrentProgress();
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

        if (runtimeCompositeRoute != null && runtimeCompositeRoute.IsConfigured)
        {
            UpdateRuntimeCompositeRoute();
            MoveVisualToCurrentProgress();
            return;
        }

        if (currentGeometry == null || !currentGeometry.IsValid())
            return;

        float delta = GetProgressDeltaPerFrame();

        if (settings.loopMode == PacemakerLoopMode.PingPong)
            UpdatePingPong(delta);
        else
            UpdateLoop(delta);

        MoveVisualToCurrentProgress();
    }

    private void UpdateRuntimeCompositeRoute()
    {
        float speedPercentPerMinute = Mathf.Max(0f, settings.speed);
        float routePerSecond = speedPercentPerMinute / 100f / 60f;
        float distanceDelta = routePerSecond * runtimeCompositeRoute.EstimatedFullRouteLength * Time.deltaTime;

        runtimeCompositeRoute.Advance(distanceDelta);
        currentGeometry = runtimeCompositeRoute.CurrentLegGeometry;
        progress = runtimeCompositeRoute.GetCurrentLegProgress();
    }

    private float GetProgressDeltaPerFrame()
    {
        // По ТЗ: скорость пейсмейкера задаётся как процент длины трека в минуту.
        // 100 = полный маршрут за 60 секунд.
        // 300 = полный маршрут за 20 секунд.
        // 600 = полный маршрут за 10 секунд.
        float speedPercentPerMinute = Mathf.Max(0f, settings.speed);
        float routePerSecond = speedPercentPerMinute / 100f / 60f;
        return routePerSecond * Time.deltaTime;
    }

    private void UpdateLoop(float delta)
    {
        progress += delta;

        if (progress >= 1f)
        {
            progress = Mathf.Repeat(progress, 1f);
            NextTarget();
        }
    }

    private void UpdatePingPong(float delta)
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

    private void NextTarget()
    {
        if (targets.Count <= 1)
            return;

        currentTargetIndex = (currentTargetIndex + 1) % targets.Count;
        BuildCurrentGeometry();
    }

    private void BuildCurrentGeometry()
    {
        currentGeometry = null;
        runtimeCompositeRoute = null;

        if (targets == null || targets.Count == 0)
            return;

        if (currentTargetIndex < 0 || currentTargetIndex >= targets.Count)
            currentTargetIndex = 0;

        PacemakerTarget target = targets[currentTargetIndex];

        if (target == null)
            return;

        if (target.targetType == PacemakerTargetType.Path)
        {
            currentGeometry = TrackGeometryFactory.FromPath(target.pathData);
            return;
        }

        if (target.targetType == PacemakerTargetType.Composite)
        {
            if (target.compositeData != null &&
                target.compositeData.transitionMode == CompositeTransitionMode.RandomSideAtEachIntersection)
            {
                runtimeCompositeRoute = new CompositeTrackRuntimeRoute();
                if (runtimeCompositeRoute.Configure(target.compositeData, target.compositeShapes))
                {
                    currentGeometry = runtimeCompositeRoute.CurrentLegGeometry;
                    Debug.Log("PacemakerRunner: using RUNTIME COMPOSITE route. valid=True, mode=" + target.compositeData.transitionMode);
                    return;
                }

                runtimeCompositeRoute = null;
                Debug.LogWarning("PacemakerRunner: runtime composite route failed. Falling back to baked composite geometry.");
            }

            currentGeometry = CompositeTrackGeometryBuilder.Build(target.compositeData, target.compositeShapes);
            Debug.Log("PacemakerRunner: using COMPOSITE geometry. valid=" +
                      (currentGeometry != null && currentGeometry.IsValid()) +
                      ", points=" + (currentGeometry != null && currentGeometry.Points != null ? currentGeometry.Points.Count : 0));
            return;
        }

        if (target.shapeData != null)
        {
            currentGeometry = TrackGeometryFactory.FromShape(target.shapeData);
            return;
        }
    }

    private void CreateVisual()
    {
        if (visual != null)
            return;

        visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "PacemakerVisual";
        visual.transform.SetParent(transform);

        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = color;

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.material = material;
    }

    private void ApplyVisualSettings()
    {
        if (visual == null)
            return;

        visual.transform.localScale = Vector3.one * Mathf.Max(0.01f, size);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null)
            renderer.material.color = color;
    }

    private void MoveVisualToCurrentProgress()
    {
        if (visual == null)
            return;

        if (runtimeCompositeRoute != null && runtimeCompositeRoute.IsConfigured)
        {
            Vector2 runtimePosition = runtimeCompositeRoute.GetCurrentPosition();
            visual.transform.position = new Vector3(runtimePosition.x, runtimePosition.y, -1f);
            return;
        }

        if (currentGeometry == null || !currentGeometry.IsValid())
            return;

        Vector2 position = currentGeometry.GetPointAtProgress(progress);
        visual.transform.position = new Vector3(position.x, position.y, -1f);
    }

    public Vector2 GetCurrentPosition()
    {
        return visual != null ? (Vector2)visual.transform.position : Vector2.zero;
    }

    public float GetCurrentProgress()
    {
        return progress;
    }

    public TrackGeometry GetCurrentGeometry()
    {
        return currentGeometry;
    }

    public void SetColor(Color c)
    {
        color = c;

        if (visual == null)
            return;

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null)
            renderer.material.color = color;
    }
}
