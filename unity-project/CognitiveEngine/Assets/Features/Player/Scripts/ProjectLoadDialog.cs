using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProjectLoadDialog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExerciseLoader exerciseLoader;
    [SerializeField] private PlayerStartPanel playerStartPanel;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform listRoot;
    [SerializeField] private Button projectButtonPrefab;
    [SerializeField] private TMP_Text selectedProjectText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button openFolderButton;
    [SerializeField] private Button closeButton;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private string selectedProjectName;

    private void Awake()
    {
        if (loadButton != null)
            loadButton.onClick.AddListener(LoadSelectedProject);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshList);

        if (openFolderButton != null)
            openFolderButton.onClick.AddListener(ProjectStorage.OpenProjectsFolder);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Close();
    }

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        RefreshList();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void RefreshList()
    {
        ClearButtons();
        selectedProjectName = null;
        RefreshSelectedText();

        List<ProjectStorage.ProjectInfo> projects = ProjectStorage.GetProjects();

        if (projects.Count == 0)
        {
            SetStatus("Проекты не найдены. Сначала сохраните проект в конструкторе.");
            return;
        }

        SetStatus("Найдено проектов: " + projects.Count);

        for (int i = 0; i < projects.Count; i++)
        {
            ProjectStorage.ProjectInfo project = projects[i];
            Button button = CreateProjectButton(project.name);

            if (button == null)
                continue;

            spawnedButtons.Add(button);
        }
    }

    private Button CreateProjectButton(string projectName)
    {
        Button button = null;

        if (projectButtonPrefab != null && listRoot != null)
        {
            button = Instantiate(projectButtonPrefab, listRoot);
            button.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = new GameObject("ProjectButton_" + projectName, typeof(RectTransform), typeof(Image), typeof(Button));
            if (listRoot != null)
                go.transform.SetParent(listRoot, false);

            button = go.GetComponent<Button>();
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text text = textGo.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 22;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = projectName;

        string capturedName = projectName;
        button.onClick.AddListener(() => SelectProject(capturedName));

        return button;
    }

    private void SelectProject(string projectName)
    {
        selectedProjectName = projectName;
        RefreshSelectedText();
        SetStatus("Выбран проект: " + projectName);
    }

    public void LoadSelectedProject()
    {
        if (string.IsNullOrWhiteSpace(selectedProjectName))
        {
            SetStatus("Выберите проект из списка");
            return;
        }

        ExerciseData exercise = ProjectStorage.LoadProject(selectedProjectName);

        if (exercise == null)
        {
            SetStatus("Не удалось загрузить проект: " + selectedProjectName);
            return;
        }

        PlayerPrefs.SetString(ProjectStorage.LastProjectNamePrefsKey, selectedProjectName);
        PlayerPrefs.Save();

        if (exerciseLoader != null)
            exerciseLoader.LoadFromExerciseData(exercise);

        if (playerStartPanel != null)
            playerStartPanel.SetLoadedExercise(exercise);

        SetStatus("Загружен проект: " + selectedProjectName);
        Close();
    }

    private void RefreshSelectedText()
    {
        if (selectedProjectText == null)
            return;

        selectedProjectText.text = string.IsNullOrWhiteSpace(selectedProjectName)
            ? "Проект не выбран"
            : "Выбран: " + selectedProjectName;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("ProjectLoadDialog: " + message);
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i].gameObject);
        }

        spawnedButtons.Clear();
    }
}
