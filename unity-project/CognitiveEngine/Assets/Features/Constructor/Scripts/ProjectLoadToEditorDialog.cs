using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProjectLoadToEditorDialog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrackEditorProjectLoader editorProjectLoader;

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
    [SerializeField] private Sprite buttonSprite;

    private readonly List<Button> createdButtons = new List<Button>();
    private string selectedProjectName;

    private Color activeColor = new Color32(32, 178, 170, 255);
    private Color idleColor = new Color32(30, 65, 79, 255);
    private Dictionary<string, Image> buttonImages = new Dictionary<string, Image>();

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
        buttonImages.Clear();
        selectedProjectName = null;
        SetSelectedText("Проект не выбран");
        SetStatus("");

        List<ProjectStorage.ProjectInfo> projects = ProjectStorage.GetProjects();

        if (projects == null || projects.Count == 0)
        {
            SetStatus("Проекты не найдены.");
            return;
        }

        for (int i = 0; i < projects.Count; i++)
        {
            CreateProjectButton(projects[i]);
        }

        SetStatus("Найдено проектов: " + projects.Count);
    }

    private void CreateProjectButton(ProjectStorage.ProjectInfo project)
    {
        if (project == null || listRoot == null)
            return;

        Button button;

        if (projectButtonPrefab != null)
        {
            button = Instantiate(projectButtonPrefab, listRoot);
        }
        else
        {
            GameObject buttonObject = new GameObject("ProjectButton_" + project.name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(listRoot, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800f, 50f);

            Image image = buttonObject.GetComponent<Image>(); 
            image.sprite = buttonSprite; 
            image.type = Image.Type.Sliced; 
            image.color = idleColor;

            button = buttonObject.GetComponent<Button>();

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            text.fontSize = 20f;
            text.margin = new Vector4(20, 0, 0, 0);

            text.color = Color.white;
            text.text = project.name;
        }

        Image btnImg = button.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.color = idleColor;
            buttonImages.Add(project.name, btnImg);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = project.name;

        string capturedName = project.name;
        button.onClick.AddListener(() => SelectProject(capturedName));
        createdButtons.Add(button);
    }

    private void SelectProject(string projectName)
    {
        selectedProjectName = projectName;

        foreach (var item in buttonImages)
        {
            item.Value.color = (item.Key == projectName) ? activeColor : idleColor;
        }

        SetSelectedText("Выбран проект: " + projectName);
        SetStatus("Нажмите загрузить, чтобы открыть проект.");
    }

    private void LoadSelectedProject()
    {
        if (string.IsNullOrWhiteSpace(selectedProjectName))
        {
            SetStatus("Сначала выберите проект из списка.");
            return;
        }

        if (editorProjectLoader == null)
        {
            SetStatus("Ошибка: не подключён TrackEditorProjectLoader.");
            return;
        }

        bool ok = editorProjectLoader.LoadProjectToEditor(selectedProjectName);
        if (!ok)
        {
            SetStatus("Не удалось загрузить проект: " + selectedProjectName);
            return;
        }

        SetStatus("Проект загружен: " + selectedProjectName);
        Close();
    }

    private void ClearButtons()
    {
        for (int i = 0; i < createdButtons.Count; i++)
        {
            if (createdButtons[i] != null)
                Destroy(createdButtons[i].gameObject);
        }

        createdButtons.Clear();
    }

    private void SetSelectedText(string message)
    {
        if (selectedProjectText != null)
            selectedProjectText.text = message;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}