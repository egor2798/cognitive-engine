using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BodyRayActivePointConfigV2
{
    public string role;
    public string body_point;
    public string sensor_id;
    public string parent_body_point;
    public float expected_distance_to_parent_m;
}

[Serializable]
public class BodyRayExerciseRuntimeConfigV2
{
    public string version = "2.0";
    public string tracking_mode = "single";
    public int max_active_points = 3;
    public string pointer_sensor_id = "WT901_01";
    public List<BodyRayActivePointConfigV2> active_points = new();
}

public static class BodyRayUnityConfigMapperV2
{
    public static BodyRayExerciseRuntimeConfigV2 FromExercise(ExerciseData exercise)
    {
        BodyRayExerciseRuntimeConfigV2 config = new BodyRayExerciseRuntimeConfigV2();

        if (exercise == null || exercise.settings == null || exercise.settings.bodyPointBindings == null)
            return config;

        List<BodyPointBindingData> bindings = exercise.settings.bodyPointBindings;
        int count = Mathf.Clamp(bindings.Count, 0, 3);

        config.tracking_mode = count <= 1 ? "single" : count == 2 ? "pair" : "triad";
        config.max_active_points = 3;

        for (int i = 0; i < count; i++)
        {
            BodyPointBindingData binding = bindings[i];
            if (binding == null)
                continue;

            string bodyPoint = BodyPointToBodyRayName(binding.bodyPointId);
            string role = ResolveRole(i, count);

            if (role == "pointer")
                config.pointer_sensor_id = binding.trackerId;

            config.active_points.Add(new BodyRayActivePointConfigV2
            {
                role = role,
                body_point = bodyPoint,
                sensor_id = binding.trackerId,
                parent_body_point = ResolveParent(i, count, config.active_points),
                expected_distance_to_parent_m = ResolveExpectedDistance(i, count)
            });
        }

        return config;
    }

    private static string ResolveRole(int index, int count)
    {
        if (count <= 1)
            return "pointer";

        if (count == 2)
            return index == 0 ? "base" : "pointer";

        if (index == 0)
            return "base";

        if (index == 1)
            return "joint_reference";

        return "pointer";
    }

    private static string ResolveParent(int index, int count, List<BodyRayActivePointConfigV2> existing)
    {
        if (index <= 0 || existing == null || existing.Count == 0)
            return "";

        return existing[existing.Count - 1].body_point;
    }

    private static float ResolveExpectedDistance(int index, int count)
    {
        if (index <= 0)
            return 0f;

        if (count >= 3 && index == 1)
            return 0.28f;

        return 0.45f;
    }

    public static string BodyPointToBodyRayName(BodyPointId bodyPointId)
    {
        switch (bodyPointId)
        {
            case BodyPointId.Head:
                return "head";
            case BodyPointId.Chest:
                return "chest";
            case BodyPointId.BodyCore:
                return "body_core";
            case BodyPointId.RightShoulder:
                return "right_shoulder";
            case BodyPointId.RightForearm:
                return "right_forearm";
            case BodyPointId.RightThigh:
                return "right_thigh";
            case BodyPointId.RightShin:
                return "right_shin";
            case BodyPointId.LeftShoulder:
                return "left_shoulder";
            case BodyPointId.LeftForearm:
                return "left_forearm";
            case BodyPointId.LeftThigh:
                return "left_thigh";
            case BodyPointId.LeftShin:
                return "left_shin";
            default:
                return bodyPointId.ToString();
        }
    }
}
