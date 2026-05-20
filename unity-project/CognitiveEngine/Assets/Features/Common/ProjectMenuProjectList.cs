using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProjectMenuProjectList : MonoBehaviour
{
    private const string SelectedProjectPrefsKey = "selectedProjectName";
    private const string SelectedExercisePrefsKey = "selectedExerciseName";

    [Header("Scene")]
    [SerializeField] private string playerSceneName = "PlayerScene";
    [SerializeField] private string constructorSceneName = "ConstructorScene";

    [Header("Role / Permissions")]
    [SerializeField] private bool readRoleFromPlayerPrefs = true;
    [SerializeField] private bool doctorModeInEditor = false;

    [Header("UI")]
    [SerializeField] private RectTransform listContent;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button openFolderButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Optional Prefab")]
    [SerializeField] private Button projectRowPrefab;

    [Header("Project Actions Menu")]
    [SerializeField] private GameObject actionsPanel;
    [SerializeField] private Text actionsTitleText;
    [SerializeField] private Button editButton;
    [SerializeField] private Button duplicateButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button closeActionsButton;

    [Header("Actions Menu Placement")]
    [SerializeField] private Vector2 actionsMenuSize = new Vector2(220f, 220f);
    [SerializeField] private Vector2 actionsMenuOffset = new Vector2(125f, -15f);
    [SerializeField] private bool preferActionsMenuRightSide = true;

    [Header("Auto Row Layout")]
    [SerializeField] private bool autoConfigureListContent = true;
    [SerializeField] private float rowHeight = 48f;
    [SerializeField] private float rowSpacing = 8f;
    [SerializeField] private float moreButtonWidth = 42f;
    [SerializeField] private float projectTextFontSize = 18f;
    [SerializeField] private float moreButtonFontSize = 24f;

    [Header("Delete Confirmation")]
    [SerializeField] private GameObject confirmDeletePanel;
    [SerializeField] private TMP_Text confirmDeleteText;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;

    [Header("Auto")]
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool openFirstProjectIfOnlyOne = false;

    private string pendingDeleteProjectName;
    private float pendingDeleteStartedAt;
    private string selectedActionProjectName;

    private readonly Color rowColor = new Color(0.086f, 0.165f, 0.192f, 1f);
    private readonly Color transparentColor = new Color(0f, 0f, 0f, 0f);
    private readonly Color accentColor = new Color(0.125f, 0.698f, 0.667f, 1f);

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
            SetLayerRecursive(obj.transform.GetChild(i).gameObject, layer);
    }

    private void Awake()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(RefreshList);
        if (openFolderButton != null) openFolderButton.onClick.AddListener(ProjectStorage.OpenProjectsFolder);
        if (confirmDeleteButton != null) confirmDeleteButton.onClick.AddListener(ConfirmDeleteProject);
        if (cancelDeleteButton != null) cancelDeleteButton.onClick.AddListener(CancelDeleteProject);
        if (editButton != null) editButton.onClick.AddListener(EditSelectedProject);
        if (duplicateButton != null) duplicateButton.onClick.AddListener(DuplicateSelectedProject);
        if (deleteButton != null) deleteButton.onClick.AddListener(DeleteSelectedProject);
        if (closeActionsButton != null) closeActionsButton.onClick.AddListener(CloseActionsMenu);
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        if (actionsPanel != null) actionsPanel.SetActive(false);
        if (autoConfigureListContent) ConfigureListContent();
    }

    private void Start()
    {
        if (refreshOnStart) RefreshList();
    }

    public void RefreshList()
    {
        if (autoConfigureListContent) ConfigureListContent();
        ClearList();
        List<ProjectStorage.ProjectInfo> projects = ProjectStorage.GetProjects();
        if (projects.Count == 0)
        {
            SetStatus("ПРОЕКТЫ НЕ НАЙДЕНЫ");
            return;
        }
        for (int i = 0; i < projects.Count; i++)
            CreateProjectRow(projects[i]);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        SetStatus("ПРОЕКТОВ: " + projects.Count);
        if (openFirstProjectIfOnlyOne && projects.Count == 1)
            OpenProject(projects[0].name);
    }

    private void ConfigureListContent()
    {
        if (listContent == null) return;
        VerticalLayoutGroup vertical = listContent.GetComponent<VerticalLayoutGroup>() ?? listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(8, 8, 8, 8);
        vertical.spacing = rowSpacing;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        ContentSizeFitter fitter = listContent.GetComponent<ContentSizeFitter>() ?? listContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ClearList()
    {
        if (listContent == null) return;
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);
    }

    private void CreateProjectRow(ProjectStorage.ProjectInfo project)
    {
        if (listContent == null || project == null) return;
        string upperName = project.name.ToUpper();
        if (projectRowPrefab != null)
        {
            Button rowButton = Instantiate(projectRowPrefab, listContent);
            rowButton.gameObject.SetActive(true);
            TMP_Text tmpText = rowButton.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) tmpText.text = upperName;
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => OpenProject(project.name));
            return;
        }
        GameObject row = new GameObject("ProjectRow_" + project.name, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(listContent, false);
        row.GetComponent<Image>().color = rowColor;
        HorizontalLayoutGroup horizontal = row.GetComponent<HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(12, 6, 0, 0);
        horizontal.spacing = 4f;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;
        Button openButton = CreateButton("OpenButton", upperName, row.transform, TextAnchor.MiddleLeft, transparentColor, projectTextFontSize);
        LayoutElement nameLayout = openButton.gameObject.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1f;
        openButton.onClick.AddListener(() => OpenProject(project.name));
        Button moreButton = CreateButton("MoreButton", "⋮", row.transform, TextAnchor.MiddleCenter, transparentColor, moreButtonFontSize);
        LayoutElement moreLayout = moreButton.gameObject.AddComponent<LayoutElement>();
        moreLayout.preferredWidth = moreButtonWidth;
        moreLayout.flexibleWidth = 0f;
        RectTransform moreRect = moreButton.GetComponent<RectTransform>();
        moreButton.onClick.AddListener(() => RequestProjectActions(project.name, moreRect));
    }

    private Button CreateButton(string objName, string label, Transform parent, TextAnchor align, Color color, float size)
    {
        GameObject btnObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        btnObj.GetComponent<Image>().color = color;
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        Text text = txtObj.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = Mathf.RoundToInt(size);
        text.color = Color.white;
        text.alignment = align;
        text.raycastTarget = false;
        return btnObj.GetComponent<Button>();
    }

    public void OpenProject(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return;
        PlayerPrefs.SetString(SelectedProjectPrefsKey, projectName);
        PlayerPrefs.SetString(SelectedExercisePrefsKey, projectName.Replace('_', ' ').ToUpper());
        PlayerPrefs.SetString(ProjectStorage.LastProjectNamePrefsKey, projectName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(playerSceneName);
    }

    public void RequestProjectActions(string projectName, RectTransform sourceButton)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return;
        selectedActionProjectName = projectName;
        EnsureActionsPanelExists();
        if (actionsTitleText != null) actionsTitleText.text = projectName.ToUpper();
        if (actionsPanel != null)
        {
            actionsPanel.SetActive(true);
            PlaceActionsMenuNearButton(sourceButton);
        }
    }

    private void EnsureActionsPanelExists()
    {
        if (actionsPanel != null) return;
        Canvas canvas = listContent != null ? listContent.GetComponentInParent<Canvas>() : null;
        Transform parent = canvas != null ? canvas.transform : transform;
        actionsPanel = new GameObject("ProjectActionsPanel", typeof(RectTransform), typeof(Image));
        actionsPanel.transform.SetParent(parent, false);
        RectTransform panelRect = actionsPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = actionsMenuSize;
        Image panelImage = actionsPanel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.98f);
        actionsTitleText = CreateLegacyText("Title", actionsPanel.transform, "ДЕЙСТВИЯ", 14, TextAnchor.MiddleCenter, new Vector2(0, -20), new Vector2(actionsMenuSize.x - 20, 30));
        editButton = CreateActionButton("Edit", "РЕДАКТИРОВАТЬ", new Vector2(0, -55));
        duplicateButton = CreateActionButton("Duplicate", "ДУБЛИРОВАТЬ", new Vector2(0, -95));
        deleteButton = CreateActionButton("Delete", "УДАЛИТЬ", new Vector2(0, -135));
        closeActionsButton = CreateActionButton("Close", "ЗАКРЫТЬ", new Vector2(0, -175));
        editButton.onClick.AddListener(EditSelectedProject);
        duplicateButton.onClick.AddListener(DuplicateSelectedProject);
        deleteButton.onClick.AddListener(DeleteSelectedProject);
        closeActionsButton.onClick.AddListener(CloseActionsMenu);
    }

    private Button CreateActionButton(string objName, string label, Vector2 pos)
    {
        GameObject btnObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(actionsPanel.transform, false);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(actionsMenuSize.x - 30, 32);
        btnObj.GetComponent<Image>().color = new Color(0.12f, 0.25f, 0.31f, 1f);
        CreateLegacyText("Label", btnObj.transform, label, 12, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta);
        return btnObj.GetComponent<Button>();
    }

    private Text CreateLegacyText(string objName, Transform parent, string label, int size, TextAnchor align, Vector2 pos, Vector2 delta)
    {
        GameObject txtObj = new GameObject(objName, typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(parent, false);
        RectTransform rect = txtObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = delta;
        Text t = txtObj.GetComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = align;
        return t;
    }

    private void PlaceActionsMenuNearButton(RectTransform source)
    {
        if (actionsPanel == null || source == null) return;
        RectTransform panelRect = actionsPanel.GetComponent<RectTransform>();
        panelRect.pivot = new Vector2(0, 1);
        panelRect.position = source.position;
    }

    private void EditSelectedProject()
    {
        if (string.IsNullOrWhiteSpace(selectedActionProjectName)) return;
        PlayerPrefs.SetString(SelectedProjectPrefsKey, selectedActionProjectName);
        PlayerPrefs.SetString("openProjectInConstructor", selectedActionProjectName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(constructorSceneName);
    }

    private void DuplicateSelectedProject()
    {
        if (string.IsNullOrWhiteSpace(selectedActionProjectName)) return;
        ProjectStorage.DuplicateProject(selectedActionProjectName);
        CloseActionsMenu();
        RefreshList();
    }

    private void DeleteSelectedProject()
    {
        if (string.IsNullOrWhiteSpace(selectedActionProjectName)) return;
        pendingDeleteProjectName = selectedActionProjectName;
        if (confirmDeletePanel != null)
        {
            if (confirmDeleteText != null) confirmDeleteText.text = "УДАЛИТЬ \"" + pendingDeleteProjectName.ToUpper() + "\"?";
            confirmDeletePanel.SetActive(true);
        }
        else ConfirmDeleteProject();
        CloseActionsMenu();
    }

    public void ConfirmDeleteProject()
    {
        if (!string.IsNullOrWhiteSpace(pendingDeleteProjectName)) ProjectStorage.DeleteProject(pendingDeleteProjectName);
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        pendingDeleteProjectName = null;
        RefreshList();
    }

    public void CancelDeleteProject()
    {
        pendingDeleteProjectName = null;
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
    }

    private void CloseActionsMenu() => actionsPanel?.SetActive(false);

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message.ToUpper();
    }

    private void Update()
    {
        if (confirmDeletePanel != null && confirmDeletePanel.activeSelf) return;
        if (!string.IsNullOrWhiteSpace(pendingDeleteProjectName) && Time.unscaledTime - pendingDeleteStartedAt > 5f)
            pendingDeleteProjectName = null;
    }
}