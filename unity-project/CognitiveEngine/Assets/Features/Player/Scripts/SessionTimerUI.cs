using TMPro;
using UnityEngine;

public class SessionTimerUI : MonoBehaviour
{
    [SerializeField] private TrackingEvaluator trackingEvaluator;
    [SerializeField] private TMP_Text timerText;

    private float durationSec;

    public void Init(ExerciseData exercise)
    {
        if (exercise == null || exercise.settings == null)
            return;

        durationSec = exercise.settings.durationMs / 1000f;
    }

    private void Update()
    {
        if (trackingEvaluator == null || timerText == null)
            return;

        SessionResult result = trackingEvaluator.GetCurrentResult();

        if (result == null)
        {
            timerText.text = "00:00.0";
            return;
        }

        float remaining = durationSec - result.totalTimeSec;
        if (remaining < 0f) remaining = 0f;

        int minutes = Mathf.FloorToInt(remaining / 60f);
        float seconds = remaining % 60f;

        timerText.text = $"{minutes:00}:{seconds:00.0}";
    }

    public void ResetTimer()
    {
        if (timerText != null)
            timerText.text = "00:00.0";
    }
}