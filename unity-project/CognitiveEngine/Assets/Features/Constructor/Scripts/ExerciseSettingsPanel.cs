using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExerciseSettingsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrackEditor trackEditor;

    [SerializeField] private Toggle usePacemakerToggle;
    [SerializeField] private Toggle allowWithoutPacemakerToggle;
    [SerializeField] private Toggle showTrackToggle;
    [SerializeField] private Toggle useSensorToggle;

    [SerializeField] private TMP_InputField speedInput;
    [SerializeField] private TMP_Dropdown durationDropdown;
    [SerializeField] private TMP_InputField thicknessInput;
    [SerializeField] private TMP_InputField sensitivityInput;
    private void Start()
    {
        LoadFromCurrentExercise();
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        LoadFromCurrentExercise();
    }

    public void ClosePanel()
    {
        ApplyToCurrentExercise();
        gameObject.SetActive(false);
    }
    public void LoadFromCurrentExercise()
    {
        if (trackEditor == null || trackEditor.CurrentExercise == null)
            return;

        ExerciseSettingsData settings = trackEditor.CurrentExercise.settings;

        usePacemakerToggle.isOn = settings.usePacemaker;
        allowWithoutPacemakerToggle.isOn = settings.allowWithoutPacemaker;
        showTrackToggle.isOn = settings.showTrack;
        useSensorToggle.isOn = settings.useSensor;

        speedInput.text = settings.defaultSpeed.ToString();
        thicknessInput.text = settings.lineThickness.ToString();
        sensitivityInput.text = settings.sensorSensitivity.ToString();

        switch (settings.durationMs)
        {
            case 60000: durationDropdown.value = 0; break;
            case 120000: durationDropdown.value = 1; break;
            case 180000: durationDropdown.value = 2; break;
            case 240000: durationDropdown.value = 3; break;
            case 300000: durationDropdown.value = 4; break;
            default: durationDropdown.value = 0; break;
        }
    }

    public void ApplyToCurrentExercise()
    {
        if (trackEditor == null || trackEditor.CurrentExercise == null)
            return;

        ExerciseSettingsData settings = trackEditor.CurrentExercise.settings;

        settings.usePacemaker = usePacemakerToggle.isOn;
        settings.allowWithoutPacemaker = allowWithoutPacemakerToggle.isOn;
        settings.showTrack = showTrackToggle.isOn;
        settings.useSensor = useSensorToggle.isOn;

        if (float.TryParse(speedInput.text, out float speed))
            settings.defaultSpeed = speed;

        if (float.TryParse(thicknessInput.text, out float thickness))
            settings.lineThickness = thickness;

        if (float.TryParse(sensitivityInput.text, out float sensitivity))
            settings.sensorSensitivity = sensitivity;

        switch (durationDropdown.value)
        {
            case 0: settings.durationMs = 60000; break;
            case 1: settings.durationMs = 120000; break;
            case 2: settings.durationMs = 180000; break;
            case 3: settings.durationMs = 240000; break;
            case 4: settings.durationMs = 300000; break;
            default: settings.durationMs = 60000; break;
        }

        Debug.Log("Exercise settings applied");
    }
}