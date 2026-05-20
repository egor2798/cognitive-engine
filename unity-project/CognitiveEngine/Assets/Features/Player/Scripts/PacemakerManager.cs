using System.Collections.Generic;
using UnityEngine;

public class PacemakerManager : MonoBehaviour
{
    [SerializeField] private PacemakerRunner runnerPrefab;

    private readonly List<PacemakerRunner> runners = new();
    private ExerciseData exercise;

    public void StartPacemakers(ExerciseData ex)
    {
        StopPacemakers();
        exercise = ex;

        if (exercise == null || exercise.settings == null)
            return;

        if (!exercise.settings.usePacemaker)
        {
            Debug.Log("PacemakerManager: pacemaker disabled in settings");
            return;
        }

        if (exercise.settings.pacemakers == null)
            return;

        Debug.Log("PacemakerManager: shapes=" + Count(exercise.shapes) +
                  ", paths=" + Count(exercise.paths) +
                  ", compositeTracks=" + Count(exercise.compositeTracks));

        foreach (var p in exercise.settings.pacemakers)
        {
            if (p == null || !p.enabled)
                continue;

            List<string> effectiveTargetIds = GetEffectiveTargetIds(p.targetIds);
            Debug.Log("PacemakerManager: effective targetIds = " + string.Join(", ", effectiveTargetIds));

            var targets = Resolve(effectiveTargetIds);
            if (targets.Count == 0)
            {
                Debug.LogWarning("PacemakerManager: no targets resolved for pacemaker.");
                continue;
            }

            var r = Instantiate(runnerPrefab, transform);
            r.Configure(p, targets);
            r.StartRunner();
            runners.Add(r);
        }
    }

    public void StopPacemakers()
    {
        foreach (var r in runners)
        {
            if (r != null)
                Destroy(r.gameObject);
        }

        runners.Clear();
    }

    public List<PacemakerRunner> GetRunners() => runners;

    private List<string> GetEffectiveTargetIds(List<string> originalTargetIds)
    {
        // ВАЖНО: если в упражнении есть составной трек, пейсмейкер должен брать именно его.
        // Это защищает от старых/перезаписанных targetIds, где мог остаться id первой фигуры.
        if (exercise != null && exercise.compositeTracks != null && exercise.compositeTracks.Count > 0)
        {
            List<string> compositeIds = new List<string>();

            foreach (CompositeTrackData composite in exercise.compositeTracks)
            {
                if (composite != null && !string.IsNullOrWhiteSpace(composite.id))
                    compositeIds.Add(composite.id);
            }

            if (compositeIds.Count > 0)
                return compositeIds;
        }

        return originalTargetIds != null ? new List<string>(originalTargetIds) : new List<string>();
    }

    private List<PacemakerTarget> Resolve(List<string> ids)
    {
        List<PacemakerTarget> result = new();

        if (ids == null || exercise == null)
            return result;

        foreach (var id in ids)
        {
            bool resolved = false;

            if (exercise.compositeTracks != null)
            {
                foreach (var composite in exercise.compositeTracks)
                {
                    if (composite == null || composite.id != id)
                        continue;

                    List<ShapeData> shapes = ResolveCompositeShapes(composite);
                    if (shapes.Count > 0)
                    {
                        Debug.Log("PacemakerManager: resolved COMPOSITE target " + id + " with shapes=" + shapes.Count);
                        result.Add(new PacemakerTarget(id, composite, shapes));
                        resolved = true;
                    }
                    else
                    {
                        Debug.LogWarning("PacemakerManager: composite target found, but no source shapes resolved: " + id);
                    }
                }
            }

            if (resolved)
                continue;

            if (exercise.paths != null)
            {
                foreach (var p in exercise.paths)
                {
                    if (p != null && p.id == id)
                    {
                        Debug.Log("PacemakerManager: resolved PATH target " + id);
                        result.Add(new PacemakerTarget(id, p));
                        resolved = true;
                    }
                }
            }

            if (resolved)
                continue;

            if (exercise.shapes != null)
            {
                foreach (var s in exercise.shapes)
                {
                    if (s != null && s.id == id)
                    {
                        Debug.Log("PacemakerManager: resolved SHAPE target " + id);
                        result.Add(new PacemakerTarget(id, s, PacemakerTargetType.Square));
                        resolved = true;
                    }
                }
            }

            if (!resolved)
                Debug.LogWarning("PacemakerManager: target id was not resolved: " + id);
        }

        return result;
    }

    private List<ShapeData> ResolveCompositeShapes(CompositeTrackData composite)
    {
        List<ShapeData> shapes = new List<ShapeData>();

        if (composite == null || composite.shapeIds == null || exercise == null || exercise.shapes == null)
            return shapes;

        for (int i = 0; i < composite.shapeIds.Count; i++)
        {
            string shapeId = composite.shapeIds[i];
            if (string.IsNullOrEmpty(shapeId))
                continue;

            for (int j = 0; j < exercise.shapes.Count; j++)
            {
                ShapeData shape = exercise.shapes[j];
                if (shape != null && shape.id == shapeId)
                {
                    shapes.Add(shape);
                    break;
                }
            }
        }

        return shapes;
    }

    private int Count<T>(List<T> list)
    {
        return list != null ? list.Count : 0;
    }
}
