using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStartPanel : MonoBehaviour
{
    // UI speed unit: percent of full route per minute.
    // 100 = full route in 60 seconds, 300 = full route in 20 seconds.
    private const float LegacyInternalSpeedMax = 0.02f;
    private const float LegacyInternalToPercentPerMinute = 100000f;

    private const string SelectedProjectPrefsKey = "selectedProjectName";
    private const string SelectedExercisePrefsKey = "selectedExerciseName";

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
        Debug.Log("OpenPanel called");
        gameObject.SetActive(true);

        if (loadedExercise == null)
            LoadExerciseSettings();
        else
            RefreshUIFromLoadedExercise();
    }

    public void SetLoadedExercise(ExerciseData exercise)
    {
        if (exercise == null)
        {
            Debug.LogWarning("PlayerStartPanel: cannot set null exercise");
            return;
        }

        loadedExercise = exercise;
        RefreshUIFromLoadedExercise();

        Debug.Log("PlayerStartPanel: project exercise selected: " + loadedExercise.name);
    }

    private void LoadExerciseSettings()
    {
        string selectedProjectName = PlayerPrefs.GetString(SelectedProjectPrefsKey, "");

        if (!string.IsNullOrWhiteSpace(selectedProjectName))
        {
            loadedExercise = ProjectStorage.LoadProject(selectedProjectName);

            if (loadedExercise != null)
                Debug.Log("PlayerStartPanel: loaded selected project: " + selectedProjectName);
            else
                Debug.LogWarning("PlayerStartPanel: selected project was not found, fallback to last/legacy: " + selectedProjectName);
        }

        if (loadedExercise == null)
            loadedExercise = ProjectStorage.LoadLastProjectOrLegacy();

        if (loadedExercise == null || loadedExercise.settings == null)
        {
            Debug.LogError("PlayerStartPanel: failed to load exercise settings");
            return;
        }

        RefreshUIFromLoadedExercise();
        Debug.Log("PlayerStartPanel: settings loaded");
    }

    private void RefreshUIFromLoadedExercise()
    {
        if (loadedExercise == null || loadedExercise.settings == null)
            return;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(loadedExercise.name)
                ? "Упражнение"
                : loadedExercise.name;
        }

        if (speedInput != null)
            speedInput.text = NormalizeSpeedPercentPerMinute(loadedExercise.settings.defaultSpeed).ToString("0.##");

        if (pacemakerToggle != null)
            pacemakerToggle.isOn = loadedExercise.settings.usePacemaker;

        if (durationDropdown != null)
        {
            switch (loadedExercise.settings.durationMs)
            {
                case 60000: durationDropdown.value = 0; break;
                case 120000: durationDropdown.value = 1; break;
                case 180000: durationDropdown.value = 2; break;
                case 240000: durationDropdown.value = 3; break;
                case 300000: durationDropdown.value = 4; break;
                default: durationDropdown.value = 0; break;
            }

            durationDropdown.RefreshShownValue();
        }
    }

    private float NormalizeSpeedPercentPerMinute(float speed)
    {
        if (speed <= 0f)
            return 0f;

        // Compatibility with old saved exercises: 0.003 -> 300 %/min.
        if (speed <= LegacyInternalSpeedMax)
            return speed * LegacyInternalToPercentPerMinute;

        return speed;
    }

    public void StartExercise()
    {
        Debug.Log("StartExercise clicked");

        if (loadedExercise == null)
        {
            Debug.LogWarning("PlayerStartPanel: exercise not loaded, trying to load last project or legacy exercise");
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

        if (speedInput != null && float.TryParse(speedInput.text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float speed))
        {
            speed = Mathf.Max(0f, speed);
            loadedExercise.settings.defaultSpeed = speed;

            if (loadedExercise.settings.pacemakers != null)
            {
                foreach (var pacemaker in loadedExercise.settings.pacemakers)
                {
                    if (pacemaker != null)
                        pacemaker.speed = speed;
                }
            }
        }

        if (pacemakerToggle != null)
            loadedExercise.settings.usePacemaker = pacemakerToggle.isOn;

        if (durationDropdown != null)
        {
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
}
