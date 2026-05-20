using UnityEngine;

public class PlayerButtons : MonoBehaviour
{
    [SerializeField] private PlayerStartPanel startPanel;
    [SerializeField] private PacemakerManager pacemakerManager;
    [SerializeField] private TrackingEvaluator trackingEvaluator;
    [SerializeField] private ExerciseLoader exerciseLoader;
    [SerializeField] private SessionTimerUI timerUI;
    [SerializeField] private SessionResultUI resultUI;

    public void OnLoadClick()
    {
        Debug.Log("Load нажата");

        if (resultUI != null)
            resultUI.Hide();

        if (startPanel != null)
            startPanel.OpenPanel();
        else
            Debug.LogError("PlayerButtons: startPanel == null");
    }

    public void OnStopClick()
    {
        Debug.Log("Stop нажата");

        if (pacemakerManager != null)
            pacemakerManager.StopPacemakers();
        else
            Debug.LogError("PlayerButtons: pacemakerManager == null");

        SessionResult result = null;

        if (trackingEvaluator != null)
        {
            result = trackingEvaluator.StopEvaluation();

            Debug.Log("=== SESSION RESULT ===");
            Debug.Log("Total time: " + result.totalTimeSec);
            Debug.Log("Mean deviation mm: " + result.meanDeviationMm);
            Debug.Log("Max deviation mm: " + result.maxDeviationMm);
            Debug.Log("RMSE deviation mm: " + result.rmseDeviationMm);
            Debug.Log("Time outside sec: " + result.timeOutsideSec);
            Debug.Log("Time outside pct: " + result.timeOutsidePct);
            Debug.Log("Outside episodes: " + result.outsideEpisodesCount);
            Debug.Log("Longest outside sec: " + result.longestOutsideEpisodeSec);
            Debug.Log("Lag pct: " + result.lagTimePct);
            Debug.Log("Lead pct: " + result.leadTimePct);
            Debug.Log("Pointer path mm: " + result.pointerPathLengthMm);
            Debug.Log("Mean pointer speed mm/s: " + result.meanPointerSpeedMmS);
            Debug.Log("Max pointer speed mm/s: " + result.maxPointerSpeedMmS);
            Debug.Log("Stop count: " + result.stopCount);
            Debug.Log("Longest stop sec: " + result.longestStopSec);
        }
        else
        {
            Debug.LogError("PlayerButtons: trackingEvaluator == null");
        }

        if (exerciseLoader != null)
            exerciseLoader.StopExercise();
        else
            Debug.LogError("PlayerButtons: exerciseLoader == null");

        if (timerUI != null)
            timerUI.ResetTimer();

        if (resultUI != null && result != null)
            resultUI.ShowResult(result);

        if (startPanel != null)
            startPanel.gameObject.SetActive(true);
    }

    public void StopAll()
    {
        OnStopClick();
    }

    public void OnBackClick()
    {
        Debug.Log("Back нажата");
    }
}