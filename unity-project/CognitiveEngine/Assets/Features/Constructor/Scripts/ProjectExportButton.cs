using System.IO;
using TMPro;
using UnityEngine;

public class ProjectExportButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExerciseSaver exerciseSaver;

    [Header("Options")]
    [SerializeField] private bool openFolderAfterExport = true;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text statusText;

    public void ExportExercise()
    {
        if (exerciseSaver == null)
        {
            SetStatus("ExerciseSaver не подключён");
            return;
        }

        exerciseSaver.SaveLegacyJson();

        string path = ProjectStorage.LegacyExercisePath;
        SetStatus("Экспортировано: " + path);

        if (openFolderAfterExport)
            OpenExportFolder();
    }

    public void OpenExportFolder()
    {
        string folder = Application.persistentDataPath;

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        Application.OpenURL(folder);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("ProjectExportButton: " + message);
    }
}
