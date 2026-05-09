using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProjectSaveDialog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExerciseSaver exerciseSaver;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField projectNameInput;
    [SerializeField] private TMP_Text pathText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button openFolderButton;

    private void Awake()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveProject);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        if (openFolderButton != null)
            openFolderButton.onClick.AddListener(ProjectStorage.OpenProjectsFolder);

        if (projectNameInput != null)
            projectNameInput.onValueChanged.AddListener(_ => RefreshPathText());

        Close();
    }

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        string suggestedName = "New_Project";

        if (exerciseSaver != null && exerciseSaver.CurrentExercise != null && !string.IsNullOrWhiteSpace(exerciseSaver.CurrentExercise.name))
            suggestedName = exerciseSaver.CurrentExercise.name;

        if (projectNameInput != null && string.IsNullOrWhiteSpace(projectNameInput.text))
            projectNameInput.text = ProjectStorage.SanitizeProjectName(suggestedName);

        if (statusText != null)
            statusText.text = "";

        RefreshPathText();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void SaveProject()
    {
        if (exerciseSaver == null)
        {
            SetStatus("ExerciseSaver не подключён");
            return;
        }

        string projectName = projectNameInput != null ? projectNameInput.text : "New_Project";
        string path = exerciseSaver.SaveProject(projectName);

        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Ошибка сохранения проекта");
            return;
        }

        SetStatus("Сохранено: " + path);
        RefreshPathText();
    }

    private void RefreshPathText()
    {
        if (pathText == null)
            return;

        string projectName = projectNameInput != null ? projectNameInput.text : "New_Project";
        string safeName = ProjectStorage.SanitizeProjectName(projectName);
        pathText.text = ProjectStorage.GetProjectExercisePath(safeName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("ProjectSaveDialog: " + message);
    }
}
