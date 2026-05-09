using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text saveStatusText;
    [SerializeField] private GameObject resultPanel;

    [Header("Optional Navigation")]
    [Tooltip("Optional. If assigned, closing the result window will reopen the start panel.")]
    [SerializeField] private PlayerStartPanel startPanel;

    [Header("Result Buttons")]
    [Tooltip("If enabled, buttons will be created automatically inside ResultPanel.")]
    [SerializeField] private bool autoCreateButtons = true;
    [SerializeField] private bool showRestartButton = true;
    [SerializeField] private bool showOpenFolderButton = true;
    [SerializeField] private bool showLoadLatestButton = false;

    [SerializeField] private Vector2 buttonSize = new Vector2(150f, 44f);
    [SerializeField] private Vector2 buttonsOffset = new Vector2(-28f, 26f);
    [SerializeField] private float buttonsSpacing = 12f;

    [Header("Saving")]
    [SerializeField] private bool autoSaveResult = true;

    [Header("Style")]
    [SerializeField] private bool applyStyleOnAwake = true;
    [SerializeField] private Color panelColor = new Color32(5, 17, 28, 245);
    [SerializeField] private Color buttonColor = new Color32(28, 72, 88, 255);
    [SerializeField] private Color primaryButtonColor = new Color32(38, 178, 170, 255);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color mutedTextColor = new Color32(170, 184, 194, 255);
    [SerializeField] private Color accentTextColor = new Color32(38, 206, 198, 255);
    [SerializeField] private Vector2 panelSize = new Vector2(760f, 520f);
    [SerializeField] private Vector2 textPadding = new Vector2(34f, 34f);
    [SerializeField] private float bottomReservedForButtons = 88f;
    [SerializeField] private float resultFontSize = 21f;
    [SerializeField] private float saveStatusFontSize = 14f;

    private Button closeButton;
    private Button restartButton;
    private Button openFolderButton;
    private Button loadLatestButton;

    private SessionResult lastResult;
    private string lastSavedPath;

    private void Awake()
    {
        ApplyStyle();
        EnsureButtons();
    }

    private void Start()
    {
        Hide();
    }

    public void ShowResult(SessionResult result)
    {
        lastResult = result;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        ApplyStyle();
        EnsureButtons();
        RenderResult(result, "Результаты");

        if (autoSaveResult && result != null)
        {
            lastSavedPath = SessionResultStorage.Save(result);
            UpdateSaveStatus(lastSavedPath);
        }
    }

    public void LoadLatestResult()
    {
        SessionResultStorage.SavedSessionResult saved = SessionResultStorage.LoadLatest();

        if (saved == null || saved.result == null)
        {
            if (resultPanel != null)
                resultPanel.SetActive(true);

            ApplyStyle();
            EnsureButtons();

            if (resultText != null)
                resultText.text = "<size=34><b>Результаты</b></size>\n\nСохранённых результатов пока нет";

            if (saveStatusText != null)
                saveStatusText.text = "";

            return;
        }

        lastResult = saved.result;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        ApplyStyle();
        EnsureButtons();
        RenderResult(saved.result, "Последний сохранённый результат");

        if (saveStatusText != null)
            saveStatusText.text = "Загружен результат: " + saved.savedAtLocal;
    }

    public void Hide()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultText != null)
            resultText.text = "";

        if (saveStatusText != null)
            saveStatusText.text = "";
    }

    public void OnCloseResultClick()
    {
        Hide();

        if (startPanel != null)
            startPanel.OpenPanel();
    }

    public void OnRestartClick()
    {
        Hide();

        if (startPanel != null)
        {
            startPanel.OpenPanel();
            startPanel.StartExercise();
        }
        else
        {
            Debug.LogWarning("SessionResultUI: startPanel is not assigned, cannot restart automatically.");
        }
    }

    public void OpenResultsFolder()
    {
        SessionResultStorage.OpenResultsFolder();
    }

    private void RenderResult(SessionResult result, string title)
    {
        if (resultText == null || result == null)
            return;

        string exerciseName = string.IsNullOrWhiteSpace(result.exerciseName) ? "Без названия" : result.exerciseName;

        resultText.richText = true;
        resultText.text =
            $"<size=34><b>{title}</b></size>\n" +
            $"<color=#{ToHex(mutedTextColor)}>Упражнение:</color> <b>{Escape(exerciseName)}</b>    " +
            $"<color=#{ToHex(mutedTextColor)}>Время:</color> <b>{result.totalTimeSec:F1} с</b>\n\n" +

            $"<color=#{ToHex(accentTextColor)}><b>Точность</b></color>\n" +
            $"  Среднее отклонение: <b>{result.meanDeviationMm:F2} мм</b>\n" +
            $"  Максимальное отклонение: <b>{result.maxDeviationMm:F2} мм</b>\n" +
            $"  RMSE отклонения: <b>{result.rmseDeviationMm:F2} мм</b>\n\n" +

            $"<color=#{ToHex(accentTextColor)}><b>Зона</b></color>\n" +
            $"  Время вне зоны: <b>{result.timeOutsideSec:F2} с</b>\n" +
            $"  Процент вне зоны: <b>{result.timeOutsidePct:F2}%</b>\n" +
            $"  Количество выходов: <b>{result.outsideEpisodesCount}</b>\n" +
            $"  Самый длинный выход: <b>{result.longestOutsideEpisodeSec:F2} с</b>\n\n" +

            $"<color=#{ToHex(accentTextColor)}><b>Пейсмейкер</b></color>\n" +
            $"  Отставание: <b>{result.lagTimeSec:F2} с</b> ({result.lagTimePct:F2}%)\n" +
            $"  Опережение: <b>{result.leadTimeSec:F2} с</b> ({result.leadTimePct:F2}%)\n\n" +

            $"<color=#{ToHex(accentTextColor)}><b>Скорость</b></color>\n" +
            $"  Средняя скорость: <b>{result.meanPointerSpeedMmS:F2} мм/с</b>\n" +
            $"  Максимальная скорость: <b>{result.maxPointerSpeedMmS:F2} мм/с</b>";
    }

    private void UpdateSaveStatus(string path)
    {
        if (saveStatusText == null)
            return;

        saveStatusText.richText = true;
        saveStatusText.fontSize = saveStatusFontSize;
        saveStatusText.color = mutedTextColor;

        if (string.IsNullOrWhiteSpace(path))
            saveStatusText.text = "Результат не сохранён";
        else
            saveStatusText.text = "Результат сохранён: " + System.IO.Path.GetFileName(path);
    }

    private void ApplyStyle()
    {
        if (!applyStyleOnAwake || resultPanel == null)
            return;

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = panelSize;
            panelRect.localScale = Vector3.one;
        }

        Image panelImage = resultPanel.GetComponent<Image>();
        if (panelImage == null)
            panelImage = resultPanel.AddComponent<Image>();

        panelImage.color = panelColor;

        if (resultText != null)
        {
            RectTransform textRect = resultText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.offsetMin = new Vector2(textPadding.x, textPadding.y + bottomReservedForButtons);
                textRect.offsetMax = new Vector2(-textPadding.x, -textPadding.y);
            }

            resultText.color = textColor;
            resultText.fontSize = resultFontSize;
            resultText.alignment = TextAlignmentOptions.TopLeft;
            resultText.enableWordWrapping = true;
            resultText.overflowMode = TextOverflowModes.Overflow;
            resultText.lineSpacing = 2f;
        }

        if (saveStatusText != null)
        {
            RectTransform statusRect = saveStatusText.GetComponent<RectTransform>();
            if (statusRect != null && resultPanel != null && saveStatusText.transform.IsChildOf(resultPanel.transform))
            {
                statusRect.anchorMin = new Vector2(0f, 0f);
                statusRect.anchorMax = new Vector2(1f, 0f);
                statusRect.pivot = new Vector2(0.5f, 0f);
                statusRect.offsetMin = new Vector2(textPadding.x, 70f);
                statusRect.offsetMax = new Vector2(-textPadding.x, 94f);
            }

            saveStatusText.color = mutedTextColor;
            saveStatusText.fontSize = saveStatusFontSize;
            saveStatusText.alignment = TextAlignmentOptions.Left;
        }
    }

    private void EnsureButtons()
    {
        if (!autoCreateButtons || resultPanel == null)
            return;

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        if (closeButton == null)
        {
            closeButton = FindOrCreateButton("CloseResultButton", "Закрыть", panelRect);
            closeButton.onClick.RemoveListener(OnCloseResultClick);
            closeButton.onClick.AddListener(OnCloseResultClick);
        }

        if (showRestartButton && restartButton == null)
        {
            restartButton = FindOrCreateButton("RestartResultButton", "Повторить", panelRect);
            restartButton.onClick.RemoveListener(OnRestartClick);
            restartButton.onClick.AddListener(OnRestartClick);
        }

        if (showOpenFolderButton && openFolderButton == null)
        {
            openFolderButton = FindOrCreateButton("OpenResultsFolderButton", "Папка", panelRect);
            openFolderButton.onClick.RemoveListener(OpenResultsFolder);
            openFolderButton.onClick.AddListener(OpenResultsFolder);
        }

        // Кнопку "Последний" в окне результата больше не показываем.
        // Метод LoadLatestResult оставлен на случай, если он где-то ещё используется вручную.
        if (loadLatestButton == null)
        {
            Transform latestButtonTransform = resultPanel.transform.Find("LoadLatestResultButton");
            if (latestButtonTransform != null && latestButtonTransform.TryGetComponent(out Button existingLatestButton))
                loadLatestButton = existingLatestButton;
        }

        if (loadLatestButton != null)
            loadLatestButton.gameObject.SetActive(false);

        LayoutButtons();
    }

    private Button FindOrCreateButton(string objectName, string label, RectTransform panelRect)
    {
        Transform existing = resultPanel.transform.Find(objectName);
        if (existing != null && existing.TryGetComponent(out Button existingButton))
        {
            StyleButton(existingButton, objectName == "RestartResultButton");
            return existingButton;
        }

        return CreateButton(objectName, label, buttonSize, panelRect, objectName == "RestartResultButton");
    }

    private Button CreateButton(string objectName, string label, Vector2 size, RectTransform panelRect, bool primary)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(resultPanel.transform, false);
        buttonObject.layer = resultPanel.layer;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.sizeDelta = size;

        Button button = buttonObject.GetComponent<Button>();
        StyleButton(button, primary);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        textObject.layer = buttonObject.layer;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return button;
    }

    private void StyleButton(Button button, bool primary)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();

        Color baseColor = primary ? primaryButtonColor : buttonColor;
        image.color = baseColor;

        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = LerpColor(baseColor, Color.white, 0.12f);
        colors.pressedColor = LerpColor(baseColor, Color.black, 0.15f);
        colors.selectedColor = baseColor;
        colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private void LayoutButtons()
    {
        Button[] orderedButtons = new Button[]
        {
            closeButton,
            openFolderButton,
            restartButton
        };

        int visibleIndex = 0;

        for (int i = 0; i < orderedButtons.Length; i++)
        {
            Button button = orderedButtons[i];
            if (button == null)
                continue;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = buttonSize;
            rect.anchoredPosition = new Vector2(
                buttonsOffset.x - visibleIndex * (buttonSize.x + buttonsSpacing),
                buttonsOffset.y
            );

            visibleIndex++;
        }
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static string ToHex(Color color)
    {
        Color32 c = color;
        return c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            Mathf.Lerp(a.r, b.r, t),
            Mathf.Lerp(a.g, b.g, t),
            Mathf.Lerp(a.b, b.b, t),
            Mathf.Lerp(a.a, b.a, t)
        );
    }
}
