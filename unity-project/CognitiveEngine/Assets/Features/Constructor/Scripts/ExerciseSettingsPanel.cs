using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExerciseSettingsPanel : MonoBehaviour
{
    // UI speed unit according to the technical specification:
    // percent of full track length per minute.
    // 100 = full route in 60 seconds, 300 = full route in 20 seconds.
    private const float LegacyInternalSpeedMax = 0.02f;
    private const float LegacyInternalToPercentPerMinute = 100000f;

    private enum SettingsTab
    {
        Basic,
        Pacemaker,
        Sensor,
        Metrics
    }

    [Header("References")]
    [SerializeField] private TrackEditor trackEditor;

    [Header("Tabs")]
    [SerializeField] private GameObject basicTab;
    [SerializeField] private GameObject pacemakerTab;
    [SerializeField] private GameObject sensorTab;
    [SerializeField] private GameObject metricsTab;

    [Header("Tab Buttons")]
    [SerializeField] private Button basicTabButton;
    [SerializeField] private Button pacemakerTabButton;
    [SerializeField] private Button sensorTabButton;
    [SerializeField] private Button metricsTabButton;

    [Header("Basic")]
    [SerializeField] private Toggle showTrackToggle;
    [SerializeField] private TMP_Dropdown durationDropdown;
    [SerializeField] private TMP_InputField thicknessInput;
    [SerializeField] private TMP_InputField corridorWidthInput;
    [SerializeField] private TMP_InputField hitRadiusInput;

    [Header("Pacemaker")]
    [SerializeField] private Toggle usePacemakerToggle;
    [SerializeField] private Toggle allowWithoutPacemakerToggle;
    [SerializeField] private TMP_InputField speedInput;
    [SerializeField] private TMP_Dropdown pacemakerLoopModeDropdown;

    [Header("Sensor")]
    [SerializeField] private Toggle useSensorToggle;
    [SerializeField] private TMP_Dropdown inputSourceDropdown;
    [SerializeField] private TMP_InputField sensitivityInput;

    [Header("Metrics")]
    [SerializeField] private Toggle trackDeviationToggle;
    [SerializeField] private Toggle saveSessionResultToggle;
    [SerializeField] private TMP_InputField lagThresholdInput;
    [SerializeField] private TMP_InputField exitDebounceInput;
    [SerializeField] private TMP_InputField stopSpeedThresholdInput;
    [SerializeField] private TMP_InputField stopConfirmInput;

    [Header("Speed UI")]
    [Tooltip("Speed is shown and saved as percent of full track length per minute. Example: 300 = full route in 20 seconds.")]
    [SerializeField] private bool useDisplaySpeedUnits = true;
    [SerializeField] private float speedUnitToInternalSpeed = 1f;

    private SettingsTab currentTab = SettingsTab.Basic;

    private void Start()
    {
        ShowBasicTab();
        LoadFromCurrentExercise();
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        ShowBasicTab();
        LoadFromCurrentExercise();
    }

    public void ClosePanel()
    {
        ApplyToCurrentExercise();
        gameObject.SetActive(false);
    }

    public void ShowBasicTab() => ShowTab(SettingsTab.Basic);
    public void ShowPacemakerTab() => ShowTab(SettingsTab.Pacemaker);
    public void ShowSensorTab() => ShowTab(SettingsTab.Sensor);
    public void ShowMetricsTab() => ShowTab(SettingsTab.Metrics);

    private void ShowTab(SettingsTab tab)
    {
        currentTab = tab;

        if (basicTab != null) basicTab.SetActive(tab == SettingsTab.Basic);
        if (pacemakerTab != null) pacemakerTab.SetActive(tab == SettingsTab.Pacemaker);
        if (sensorTab != null) sensorTab.SetActive(tab == SettingsTab.Sensor);
        if (metricsTab != null) metricsTab.SetActive(tab == SettingsTab.Metrics);

        UpdateTabButtons();
    }

    private void UpdateTabButtons()
    {
        if (basicTabButton != null) basicTabButton.interactable = currentTab != SettingsTab.Basic;
        if (pacemakerTabButton != null) pacemakerTabButton.interactable = currentTab != SettingsTab.Pacemaker;
        if (sensorTabButton != null) sensorTabButton.interactable = currentTab != SettingsTab.Sensor;
        if (metricsTabButton != null) metricsTabButton.interactable = currentTab != SettingsTab.Metrics;
    }

    public void LoadFromCurrentExercise()
    {
        if (trackEditor == null || trackEditor.CurrentExercise == null)
            return;

        if (trackEditor.CurrentExercise.settings == null)
            trackEditor.CurrentExercise.settings = new ExerciseSettingsData();

        ExerciseSettingsData settings = trackEditor.CurrentExercise.settings;

        SetToggle(showTrackToggle, settings.showTrack);
        SetToggle(usePacemakerToggle, settings.usePacemaker);
        SetToggle(allowWithoutPacemakerToggle, settings.allowWithoutPacemaker);
        SetToggle(useSensorToggle, settings.useSensor);
        SetToggle(trackDeviationToggle, settings.trackDeviation);
        SetToggle(saveSessionResultToggle, settings.saveSessionResult);

        SetInput(thicknessInput, settings.lineThickness);
        SetInput(corridorWidthInput, settings.corridorWidthCm);
        SetInput(hitRadiusInput, settings.hitRadiusCm);
        SetInput(sensitivityInput, settings.sensorSensitivity);
        SetInput(lagThresholdInput, settings.lagThresholdCm);
        SetInput(exitDebounceInput, settings.exitDebounceMs);
        SetInput(stopSpeedThresholdInput, settings.stopSpeedThresholdMmS);
        SetInput(stopConfirmInput, settings.stopConfirmMs);

        float displaySpeed = NormalizeSpeedPercentPerMinute(settings.defaultSpeed);
        SetInput(speedInput, displaySpeed);

        if (durationDropdown != null)
        {
            durationDropdown.value = DurationMsToDropdownIndex(settings.durationMs);
            durationDropdown.RefreshShownValue();
        }

        if (inputSourceDropdown != null)
        {
            inputSourceDropdown.value = settings.inputSource == PointerInputSource.Sensor ? 1 : 0;
            inputSourceDropdown.RefreshShownValue();
        }

        if (pacemakerLoopModeDropdown != null)
        {
            pacemakerLoopModeDropdown.value = settings.defaultPacemakerLoopMode == PacemakerLoopMode.PingPong ? 1 : 0;
            pacemakerLoopModeDropdown.RefreshShownValue();
        }
    }

    public void ApplyToCurrentExercise()
    {
        if (trackEditor == null || trackEditor.CurrentExercise == null)
            return;

        if (trackEditor.CurrentExercise.settings == null)
            trackEditor.CurrentExercise.settings = new ExerciseSettingsData();

        ExerciseSettingsData settings = trackEditor.CurrentExercise.settings;

        if (showTrackToggle != null) settings.showTrack = showTrackToggle.isOn;
        if (usePacemakerToggle != null) settings.usePacemaker = usePacemakerToggle.isOn;
        if (allowWithoutPacemakerToggle != null) settings.allowWithoutPacemaker = allowWithoutPacemakerToggle.isOn;
        if (trackDeviationToggle != null) settings.trackDeviation = trackDeviationToggle.isOn;
        if (saveSessionResultToggle != null) settings.saveSessionResult = saveSessionResultToggle.isOn;

        if (durationDropdown != null)
            settings.durationMs = DropdownIndexToDurationMs(durationDropdown.value);

        if (TryGetFloat(thicknessInput, out float thickness)) settings.lineThickness = thickness;
        if (TryGetFloat(corridorWidthInput, out float corridorWidth)) settings.corridorWidthCm = corridorWidth;
        if (TryGetFloat(hitRadiusInput, out float hitRadius)) settings.hitRadiusCm = hitRadius;
        if (TryGetFloat(sensitivityInput, out float sensitivity)) settings.sensorSensitivity = sensitivity;
        if (TryGetFloat(lagThresholdInput, out float lagThreshold)) settings.lagThresholdCm = lagThreshold;
        if (TryGetInt(exitDebounceInput, out int exitDebounce)) settings.exitDebounceMs = exitDebounce;
        if (TryGetFloat(stopSpeedThresholdInput, out float stopSpeed)) settings.stopSpeedThresholdMmS = stopSpeed;
        if (TryGetInt(stopConfirmInput, out int stopConfirm)) settings.stopConfirmMs = stopConfirm;

        if (TryGetFloat(speedInput, out float uiSpeed))
        {
            float speedPercentPerMinute = Mathf.Max(0f, uiSpeed);
            settings.defaultSpeed = speedPercentPerMinute;
            ApplySpeedToExistingPacemakers(settings, settings.defaultSpeed);
        }

        if (inputSourceDropdown != null)
        {
            settings.inputSource = inputSourceDropdown.value == 1
                ? PointerInputSource.Sensor
                : PointerInputSource.Mouse;
            settings.useSensor = settings.inputSource == PointerInputSource.Sensor;
            SetToggle(useSensorToggle, settings.useSensor);
        }
        else if (useSensorToggle != null)
        {
            settings.useSensor = useSensorToggle.isOn;
            settings.inputSource = settings.useSensor
                ? PointerInputSource.Sensor
                : PointerInputSource.Mouse;
        }

        if (pacemakerLoopModeDropdown != null)
        {
            settings.defaultPacemakerLoopMode = pacemakerLoopModeDropdown.value == 1
                ? PacemakerLoopMode.PingPong
                : PacemakerLoopMode.Loop;
            ApplyLoopModeToExistingPacemakers(settings, settings.defaultPacemakerLoopMode);
        }

        if (trackEditor != null)
            trackEditor.ApplyVisualSettingsFromCurrentExercise();

        Debug.Log($"Exercise settings applied: duration={settings.durationMs}ms, speed={settings.defaultSpeed}, input={settings.inputSource}, corridor={settings.corridorWidthCm}cm, hitRadius={settings.hitRadiusCm}cm, thickness={settings.lineThickness}, showTrack={settings.showTrack}");
    }

    private float NormalizeSpeedPercentPerMinute(float speed)
    {
        if (speed <= 0f)
            return 0f;

        // Compatibility with old saved projects: 0.003 -> 300 %/min.
        if (speed <= LegacyInternalSpeedMax)
            return speed * LegacyInternalToPercentPerMinute;

        return speed;
    }

    private void ApplySpeedToExistingPacemakers(ExerciseSettingsData settings, float speed)
    {
        if (settings.pacemakers == null)
            return;

        foreach (var pacemaker in settings.pacemakers)
        {
            if (pacemaker != null)
                pacemaker.speed = speed;
        }
    }

    private void ApplyLoopModeToExistingPacemakers(ExerciseSettingsData settings, PacemakerLoopMode loopMode)
    {
        if (settings.pacemakers == null)
            return;

        foreach (var pacemaker in settings.pacemakers)
        {
            if (pacemaker != null)
                pacemaker.loopMode = loopMode;
        }
    }

    private int DurationMsToDropdownIndex(int durationMs)
    {
        return durationMs switch
        {
            60000 => 0,
            120000 => 1,
            180000 => 2,
            240000 => 3,
            300000 => 4,
            _ => 0
        };
    }

    private int DropdownIndexToDurationMs(int index)
    {
        return index switch
        {
            0 => 60000,
            1 => 120000,
            2 => 180000,
            3 => 240000,
            4 => 300000,
            _ => 60000
        };
    }

    private void SetToggle(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.isOn = value;
    }

    private void SetInput(TMP_InputField input, float value)
    {
        if (input != null)
            input.text = value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void SetInput(TMP_InputField input, int value)
    {
        if (input != null)
            input.text = value.ToString(CultureInfo.InvariantCulture);
    }

    private bool TryGetFloat(TMP_InputField input, out float value)
    {
        value = 0f;
        if (input == null)
            return false;

        string text = input.text.Replace(',', '.');
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private bool TryGetInt(TMP_InputField input, out int value)
    {
        value = 0;
        if (input == null)
            return false;

        return int.TryParse(input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
