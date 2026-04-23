using System.Collections.Generic;
using UnityEngine;

public class PacemakerManager : MonoBehaviour
{
    [SerializeField] private PacemakerRunner runnerPrefab;

    private List<PacemakerRunner> runners = new();
    private ExerciseData exercise;

    public void StartPacemakers(ExerciseData ex)
    {
        StopPacemakers();
        exercise = ex;

        if (exercise.settings.pacemakers == null)
            return;

        foreach (var p in exercise.settings.pacemakers)
        {
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
            Destroy(r.gameObject);

        runners.Clear();
    }

    public List<PacemakerRunner> GetRunners() => runners;

    private List<PacemakerTarget> Resolve(List<string> ids)
    {
        List<PacemakerTarget> result = new();

        foreach (var id in ids)
        {
            foreach (var p in exercise.paths)
                if (p.id == id)
                    result.Add(new PacemakerTarget(id, p));

            foreach (var s in exercise.shapes)
                if (s.id == id)
                    result.Add(new PacemakerTarget(id, s, PacemakerTargetType.Square));
        }

        return result;
    }
}