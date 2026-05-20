using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ProjectStorage
{
    public const string ExerciseFileName = "exercise.json";
    public const string ProjectsFolderName = "projects";
    public const string LastProjectNamePrefsKey = "lastProjectName";

    public static string ProjectsRootPath
    {
        get
        {
            string path = Path.Combine(Application.persistentDataPath, ProjectsFolderName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }
    }

    public static string LegacyExercisePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, ExerciseFileName);
        }
    }

    public static string SanitizeProjectName(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            projectName = "New_Project";

        projectName = projectName.Trim();

        foreach (char c in Path.GetInvalidFileNameChars())
            projectName = projectName.Replace(c, '_');

        projectName = projectName.Replace(' ', '_');

        while (projectName.Contains("__"))
            projectName = projectName.Replace("__", "_");

        if (string.IsNullOrWhiteSpace(projectName))
            projectName = "New_Project";

        return projectName;
    }

    public static string GetProjectFolderPath(string projectName)
    {
        string safeName = SanitizeProjectName(projectName);
        return Path.Combine(ProjectsRootPath, safeName);
    }

    public static string GetProjectExercisePath(string projectName)
    {
        return Path.Combine(GetProjectFolderPath(projectName), ExerciseFileName);
    }

    public static string SaveProject(ExerciseData exercise, string projectName, bool alsoSaveLegacyCopy = true)
    {
        if (exercise == null)
        {
            Debug.LogError("ProjectStorage: exercise is null");
            return null;
        }

        string safeName = SanitizeProjectName(projectName);

        if (string.IsNullOrWhiteSpace(exercise.name) || exercise.name == "New Exercise")
            exercise.name = safeName.Replace('_', ' ');

        string folderPath = GetProjectFolderPath(safeName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string json = JsonUtility.ToJson(exercise, true);
        string exercisePath = Path.Combine(folderPath, ExerciseFileName);
        File.WriteAllText(exercisePath, json);

        if (alsoSaveLegacyCopy)
            File.WriteAllText(LegacyExercisePath, json);

        PlayerPrefs.SetString(LastProjectNamePrefsKey, safeName);
        PlayerPrefs.Save();

        Debug.Log("ProjectStorage: saved project '" + safeName + "' to " + exercisePath);
        return exercisePath;
    }

    public static ExerciseData LoadProject(string projectName)
    {
        string path = GetProjectExercisePath(projectName);
        return LoadExerciseFromPath(path);
    }

    public static ExerciseData LoadLegacyExercise()
    {
        return LoadExerciseFromPath(LegacyExercisePath);
    }

    public static ExerciseData LoadLastProjectOrLegacy()
    {
        string lastProjectName = PlayerPrefs.GetString(LastProjectNamePrefsKey, "");

        if (!string.IsNullOrWhiteSpace(lastProjectName))
        {
            string projectPath = GetProjectExercisePath(lastProjectName);

            if (File.Exists(projectPath))
                return LoadExerciseFromPath(projectPath);
        }

        return LoadLegacyExercise();
    }

    public static ExerciseData LoadExerciseFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Debug.LogError("ProjectStorage: exercise file not found: " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        ExerciseData exercise = JsonUtility.FromJson<ExerciseData>(json);

        if (exercise == null)
        {
            Debug.LogError("ProjectStorage: failed to parse exercise: " + path);
            return null;
        }

        Debug.Log("ProjectStorage: loaded exercise from " + path);
        return exercise;
    }

    public static List<ProjectInfo> GetProjects()
    {
        List<ProjectInfo> projects = new List<ProjectInfo>();
        string root = ProjectsRootPath;

        if (!Directory.Exists(root))
            return projects;

        string[] folders = Directory.GetDirectories(root);

        for (int i = 0; i < folders.Length; i++)
        {
            string folder = folders[i];
            string exercisePath = Path.Combine(folder, ExerciseFileName);

            if (!File.Exists(exercisePath))
                continue;

            FileInfo fileInfo = new FileInfo(exercisePath);
            string name = Path.GetFileName(folder);

            projects.Add(new ProjectInfo
            {
                name = name,
                folderPath = folder,
                exercisePath = exercisePath,
                lastWriteTime = fileInfo.LastWriteTime
            });
        }

        projects.Sort((a, b) => b.lastWriteTime.CompareTo(a.lastWriteTime));
        return projects;
    }

    public static bool ProjectExists(string projectName)
    {
        return File.Exists(GetProjectExercisePath(projectName));
    }


    public static string DuplicateProject(string projectName)
    {
        string safeName = SanitizeProjectName(projectName);
        string sourcePath = GetProjectExercisePath(safeName);

        if (!File.Exists(sourcePath))
        {
            Debug.LogError("ProjectStorage: cannot duplicate, exercise file not found: " + sourcePath);
            return null;
        }

        string baseCopyName = safeName + "_Copy";
        string copyName = baseCopyName;
        int index = 2;

        while (ProjectExists(copyName))
        {
            copyName = baseCopyName + "_" + index;
            index++;
        }

        try
        {
            string targetFolder = GetProjectFolderPath(copyName);
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            string targetPath = Path.Combine(targetFolder, ExerciseFileName);
            File.Copy(sourcePath, targetPath, false);

            ExerciseData copiedExercise = LoadExerciseFromPath(targetPath);
            if (copiedExercise != null)
            {
                copiedExercise.name = copyName.Replace('_', ' ');
                string json = JsonUtility.ToJson(copiedExercise, true);
                File.WriteAllText(targetPath, json);
            }

            Debug.Log("ProjectStorage: duplicated project '" + safeName + "' as '" + copyName + "'");
            return copyName;
        }
        catch (Exception exception)
        {
            Debug.LogError("ProjectStorage: failed to duplicate project '" + safeName + "': " + exception.Message);
            return null;
        }
    }


    public static bool DeleteProject(string projectName)
    {
        string safeName = SanitizeProjectName(projectName);
        string folderPath = GetProjectFolderPath(safeName);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning("ProjectStorage: project folder not found: " + folderPath);
            return false;
        }

        try
        {
            Directory.Delete(folderPath, true);

            string lastProjectName = PlayerPrefs.GetString(LastProjectNamePrefsKey, "");
            if (lastProjectName == safeName)
            {
                PlayerPrefs.DeleteKey(LastProjectNamePrefsKey);
                PlayerPrefs.DeleteKey("selectedProjectName");
                PlayerPrefs.DeleteKey("selectedExerciseName");
                PlayerPrefs.Save();
            }

            Debug.Log("ProjectStorage: deleted project '" + safeName + "'");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("ProjectStorage: failed to delete project '" + safeName + "': " + exception.Message);
            return false;
        }
    }

    public static void OpenProjectsFolder()
    {
        string path = ProjectsRootPath;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        Application.OpenURL(path);
    }

    [Serializable]
    public class ProjectInfo
    {
        public string name;
        public string folderPath;
        public string exercisePath;
        public DateTime lastWriteTime;
    }
}
