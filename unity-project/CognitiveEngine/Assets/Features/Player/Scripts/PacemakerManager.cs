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

        foreach (var p in exercise.settings.pacemakers)
        {
            if (p == null || !p.enabled)
                continue;

            var targets = Resolve(p.targetIds);
            if (targets.Count == 0)
                continue;

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

    private List<PacemakerTarget> Resolve(List<string> ids)
    {
        List<PacemakerTarget> result = new();

        if (ids == null || exercise == null)
            return result;

        foreach (var id in ids)
        {
            if (exercise.paths != null)
            {
                foreach (var p in exercise.paths)
                {
                    if (p != null && p.id == id)
                        result.Add(new PacemakerTarget(id, p));
                }
            }

            if (exercise.shapes != null)
            {
                foreach (var s in exercise.shapes)
                {
                    if (s != null && s.id == id)
                        result.Add(new PacemakerTarget(id, s, PacemakerTargetType.Square));
                }
            }
        }

        return result;
    }
}
