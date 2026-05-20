using System;
using System.IO;
using UnityEngine;

public static class SessionResultStorage
{
    [Serializable]
    public class SavedSessionResult
    {
        public string sessionId;
        public string savedAtLocal;
        public string savedAtUtc;
        public string source;
        public SessionResult result;
    }

    // Backward-compatible flat format name.
    [Serializable]
    public class StoredSessionResult
    {
        public string sessionId;
        public string exerciseId;
        public string exerciseName;
        public string savedAt;
        public string savedAtLocal;
        public string savedAtUtc;

        public float durationSec;
        public float averageDeviationMm;
        public float maxDeviationMm;
        public float rmseDeviationMm;
        public float timeOutsideSec;
        public float outsidePercent;
        public int exitCount;
        public float longestExitSec;
        public float averageSpeedMmS;
        public float maxSpeedMmS;
        public string source;
    }

    public static string ResultsDirectory
    {
        get { return Path.Combine(Application.persistentDataPath, "session_results"); }
    }

    public static string Save(SessionResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("SessionResultStorage: result is null");
            return null;
        }

        SavedSessionResult saved = new SavedSessionResult
        {
            sessionId = Guid.NewGuid().ToString(),
            savedAtLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            savedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            source = "Unity Player",
            result = result
        };

        return Save(saved);
    }

    public static string Save(SessionResult result, ExerciseData exercise)
    {
        if (result != null && exercise != null)
        {
            result.exerciseId = exercise.id;
            result.exerciseName = exercise.name;
            ApplyExerciseBodyPointInfo(result, exercise);
        }

        return Save(result);
    }

    public static string Save(SessionResult result, string exerciseId, string exerciseName)
    {
        if (result != null)
        {
            result.exerciseId = exerciseId;
            result.exerciseName = exerciseName;
        }

        return Save(result);
    }

    public static string Save(SavedSessionResult saved)
    {
        if (saved == null || saved.result == null)
        {
            Debug.LogWarning("SessionResultStorage: saved result is null");
            return null;
        }

        NormalizeMovementFields(saved.result);

        if (!Directory.Exists(ResultsDirectory))
            Directory.CreateDirectory(ResultsDirectory);

        if (string.IsNullOrWhiteSpace(saved.sessionId))
            saved.sessionId = Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(saved.savedAtLocal))
            saved.savedAtLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (string.IsNullOrWhiteSpace(saved.savedAtUtc))
            saved.savedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        if (string.IsNullOrWhiteSpace(saved.source))
            saved.source = "Unity Player";

        string exerciseName = saved.result != null ? saved.result.exerciseName : "exercise";
        string safeExerciseName = MakeSafeFileName(exerciseName);
        if (string.IsNullOrWhiteSpace(safeExerciseName))
            safeExerciseName = "exercise";

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"session_{timestamp}_{safeExerciseName}.json";
        string path = Path.Combine(ResultsDirectory, fileName);

        string json = JsonUtility.ToJson(saved, true);
        File.WriteAllText(path, json);

        Debug.Log("SessionResultStorage: saved result to " + path);
        return path;
    }

    public static string SaveResult(StoredSessionResult stored)
    {
        if (stored == null)
            return null;

        SessionResult result = ConvertStoredToSessionResult(stored);

        SavedSessionResult saved = new SavedSessionResult
        {
            sessionId = string.IsNullOrWhiteSpace(stored.sessionId) ? Guid.NewGuid().ToString() : stored.sessionId,
            savedAtLocal = !string.IsNullOrWhiteSpace(stored.savedAtLocal) ? stored.savedAtLocal : stored.savedAt,
            savedAtUtc = stored.savedAtUtc,
            source = string.IsNullOrWhiteSpace(stored.source) ? "Unity Player" : stored.source,
            result = result
        };

        return Save(saved);
    }

    public static SavedSessionResult LoadLatest()
    {
        if (!Directory.Exists(ResultsDirectory))
        {
            Debug.LogWarning("SessionResultStorage: results directory does not exist");
            return null;
        }

        string[] files = Directory.GetFiles(ResultsDirectory, "*.json");
        if (files == null || files.Length == 0)
        {
            Debug.LogWarning("SessionResultStorage: no result files found");
            return null;
        }

        string latestFile = files[0];
        DateTime latestWriteTime = File.GetLastWriteTime(latestFile);

        for (int i = 1; i < files.Length; i++)
        {
            DateTime writeTime = File.GetLastWriteTime(files[i]);
            if (writeTime > latestWriteTime)
            {
                latestWriteTime = writeTime;
                latestFile = files[i];
            }
        }

        string json = File.ReadAllText(latestFile);
        SavedSessionResult saved = JsonUtility.FromJson<SavedSessionResult>(json);

        // Fallback for old flat JSON files.
        if (saved == null || saved.result == null)
        {
            StoredSessionResult stored = JsonUtility.FromJson<StoredSessionResult>(json);
            if (stored == null)
                return null;

            saved = new SavedSessionResult
            {
                sessionId = stored.sessionId,
                savedAtLocal = !string.IsNullOrWhiteSpace(stored.savedAtLocal) ? stored.savedAtLocal : stored.savedAt,
                savedAtUtc = stored.savedAtUtc,
                source = stored.source,
                result = ConvertStoredToSessionResult(stored)
            };
        }

        Debug.Log("SessionResultStorage: loaded latest result from " + latestFile);
        return saved;
    }

    public static StoredSessionResult LoadLatestResult()
    {
        SavedSessionResult saved = LoadLatest();
        if (saved == null || saved.result == null)
            return null;

        SessionResult result = saved.result;

        return new StoredSessionResult
        {
            sessionId = saved.sessionId,
            exerciseId = result.exerciseId,
            exerciseName = result.exerciseName,
            savedAt = saved.savedAtLocal,
            savedAtLocal = saved.savedAtLocal,
            savedAtUtc = saved.savedAtUtc,
            durationSec = result.totalTimeSec,
            averageDeviationMm = result.meanDeviationMm,
            maxDeviationMm = result.maxDeviationMm,
            rmseDeviationMm = result.rmseDeviationMm,
            timeOutsideSec = result.timeOutsideSec,
            outsidePercent = result.timeOutsidePct,
            exitCount = result.outsideEpisodesCount,
            longestExitSec = result.longestOutsideEpisodeSec,
            averageSpeedMmS = result.meanPointerSpeedMmS,
            maxSpeedMmS = result.maxPointerSpeedMmS,
            source = saved.source
        };
    }

    public static void OpenResultsFolder()
    {
        if (!Directory.Exists(ResultsDirectory))
            Directory.CreateDirectory(ResultsDirectory);

        Application.OpenURL(ResultsDirectory);
    }


    private static void ApplyExerciseBodyPointInfo(SessionResult result, ExerciseData exercise)
    {
        if (result == null || exercise == null || exercise.settings == null)
            return;

        result.activeBodyPointId = exercise.settings.activeBodyPointId;
        result.activeBodyPointZoneId = exercise.settings.activeBodyPointZoneId;

        if (result.bodyPointBindings == null)
            result.bodyPointBindings = new System.Collections.Generic.List<BodyPointBindingData>();

        result.bodyPointBindings.Clear();

        if (exercise.settings.bodyPointBindings != null)
        {
            for (int i = 0; i < exercise.settings.bodyPointBindings.Count; i++)
            {
                BodyPointBindingData b = exercise.settings.bodyPointBindings[i];
                if (b == null)
                    continue;

                result.bodyPointBindings.Add(new BodyPointBindingData
                {
                    bodyPointId = b.bodyPointId,
                    bodyPointZoneId = b.bodyPointZoneId,
                    trackerId = b.trackerId,
                    cursorId = b.cursorId
                });
            }
        }

        BodyPointBindingData primary = null;
        if (result.bodyPointBindings.Count > 0)
            primary = result.bodyPointBindings[0];

        if (primary != null)
        {
            result.activeBodyPointId = primary.bodyPointId;
            result.activeBodyPointZoneId = primary.bodyPointZoneId;
            result.activeTrackerId = primary.trackerId;
            result.activeCursorId = primary.cursorId;
        }
    }

    private static SessionResult ConvertStoredToSessionResult(StoredSessionResult stored)
    {
        return new SessionResult
        {
            exerciseId = stored.exerciseId,
            exerciseName = stored.exerciseName,
            totalTimeSec = stored.durationSec,
            meanDeviationMm = stored.averageDeviationMm,
            maxDeviationMm = stored.maxDeviationMm,
            rmseDeviationMm = stored.rmseDeviationMm,
            timeOutsideSec = stored.timeOutsideSec,
            timeOutsidePct = stored.outsidePercent,
            outsideEpisodesCount = stored.exitCount,
            longestOutsideEpisodeSec = stored.longestExitSec,
            meanPointerSpeedMmS = stored.averageSpeedMmS,
            maxPointerSpeedMmS = stored.maxSpeedMmS
        };
    }

    private static void NormalizeMovementFields(SessionResult result)
    {
        if (result == null)
            return;

        float activeTime = result.activeTimeSec > 0.0001f ? result.activeTimeSec : result.totalTimeSec;
        activeTime = Mathf.Max(0.0001f, activeTime);

        // Some older TrackingEvaluator versions accumulated path length in Unity units
        // while speeds were already converted to mm/s. If path is clearly inconsistent
        // with speed * time, rebuild the saved path length from speed and active time.
        float expectedPointerPathMm = Mathf.Max(0f, result.meanPointerSpeedMmS) * activeTime;
        float expectedPacemakerPathMm = Mathf.Max(0f, result.meanPacemakerSpeedMmS) * activeTime;

        if (expectedPointerPathMm > 0.001f && result.pointerPathLengthMm < expectedPointerPathMm * 0.25f)
            result.pointerPathLengthMm = expectedPointerPathMm;

        if (expectedPacemakerPathMm > 0.001f && result.pacemakerPathLengthMm < expectedPacemakerPathMm * 0.25f)
            result.pacemakerPathLengthMm = expectedPacemakerPathMm;

        // If a path is available but average speed is missing, recover the speed too.
        if (result.pointerPathLengthMm > 0.001f && result.meanPointerSpeedMmS <= 0.001f)
            result.meanPointerSpeedMmS = result.pointerPathLengthMm / activeTime;

        if (result.pacemakerPathLengthMm > 0.001f && result.meanPacemakerSpeedMmS <= 0.001f)
            result.meanPacemakerSpeedMmS = result.pacemakerPathLengthMm / activeTime;
    }

    private static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "exercise";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
            name = name.Replace(c.ToString(), "_");

        name = name.Replace(" ", "_");
        return name;
    }
}
