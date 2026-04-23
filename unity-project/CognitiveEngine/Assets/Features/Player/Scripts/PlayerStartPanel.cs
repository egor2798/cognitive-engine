using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStartPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExerciseLoader exerciseLoader;
    [SerializeField] private PacemakerManager pacemakerManager;
    [SerializeField] private TrackingEvaluator trackingEvaluator;
    [SerializeField] private SessionTimerUI timerUI;
    [SerializeField] private SessionResultUI resultUI;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_InputField speedInput;
    [SerializeField] private TMP_Dropdown durationDropdown;
    [SerializeField] private Toggle pacemakerToggle;

    private ExerciseData loadedExercise;

    public void OpenPanel()
    {
        Debug.Log("OpenPanel вызван");
        gameObject.SetActive(true);
        LoadExerciseSettings();
    }

    private void LoadExerciseSettings()
    {
        string path = Path.Combine(Application.persistentDataPath, "exercise.json");

        if (!File.Exists(path))
        {
            Debug.LogError("exercise.json не найден");
            return;
        }

        string json = File.ReadAllText(path);
        loadedExercise = JsonUtility.FromJson<ExerciseData>(json);

        if (loadedExercise == null || loadedExercise.settings == null)
        {
            Debug.LogError("Ошибка загрузки настроек");
            return;
        }

        titleText.text = string.IsNullOrWhiteSpace(loadedExercise.name)
            ? "Упражнение"
            : loadedExercise.name;

        speedInput.text = loadedExercise.settings.defaultSpeed.ToString();
        pacemakerToggle.isOn = loadedExercise.settings.usePacemaker;

        switch (loadedExercise.settings.durationMs)
        {
            case 60000: durationDropdown.value = 0; break;
            case 120000: durationDropdown.value = 1; break;
            case 180000: durationDropdown.value = 2; break;
            case 240000: durationDropdown.value = 3; break;
            case 300000: durationDropdown.value = 4; break;
            default: durationDropdown.value = 0; break;
        }

        Debug.Log("Настройки загружены");
    }

    public void StartExercise()
    {
        Debug.Log("StartExercise нажата");

        if (loadedExercise == null)
        {
            Debug.LogWarning("Exercise не загружен, пробуем заново");
            LoadExerciseSettings();

            if (loadedExercise == null)
                return;
        }

        ApplySettingsToExercise();

        if (resultUI != null)
            resultUI.Hide();

        if (exerciseLoader != null)
            exerciseLoader.LoadFromExerciseData(loadedExercise);
        else
            Debug.LogError("exerciseLoader == null");

        if (pacemakerManager != null)
            pacemakerManager.StartPacemakers(loadedExercise);
        else
            Debug.LogError("pacemakerManager == null");

        if (trackingEvaluator != null)
            trackingEvaluator.StartEvaluation(loadedExercise);
        else
            Debug.LogError("trackingEvaluator == null");

        if (timerUI != null)
            timerUI.Init(loadedExercise);

        gameObject.SetActive(false);
    }

    private void ApplySettingsToExercise()
    {
        if (loadedExercise == null || loadedExercise.settings == null)
            return;

        if (float.TryParse(speedInput.text, out float speed))
            loadedExercise.settings.defaultSpeed = speed;

        loadedExercise.settings.usePacemaker = pacemakerToggle.isOn;

        switch (durationDropdown.value)
        {
            case 0: loadedExercise.settings.durationMs = 60000; break;
            case 1: loadedExercise.settings.durationMs = 120000; break;
            case 2: loadedExercise.settings.durationMs = 180000; break;
            case 3: loadedExercise.settings.durationMs = 240000; break;
            case 4: loadedExercise.settings.durationMs = 300000; break;
        }
    }
}