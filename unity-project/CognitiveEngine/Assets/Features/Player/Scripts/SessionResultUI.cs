using TMPro;
using UnityEngine;

public class SessionResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject resultPanel;

    private void Start()
    {
        Hide();
    }

    public void ShowResult(SessionResult result)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText == null || result == null)
            return;

        resultText.text =
            $"Результаты\n\n" +
            $"Время: {result.totalTimeSec:F1} с\n" +
            $"Среднее отклонение: {result.meanDeviationMm:F2} мм\n" +
            $"Максимальное отклонение: {result.maxDeviationMm:F2} мм\n" +
            $"RMSE отклонения: {result.rmseDeviationMm:F2} мм\n" +
            $"Время вне зоны: {result.timeOutsideSec:F2} с\n" +
            $"Процент вне зоны: {result.timeOutsidePct:F2}%\n" +
            $"Количество выходов: {result.outsideEpisodesCount}\n" +
            $"Самый длинный выход: {result.longestOutsideEpisodeSec:F2} с\n" +
            $"Средняя скорость: {result.meanPointerSpeedMmS:F2} мм/с\n" +
            $"Максимальная скорость: {result.maxPointerSpeedMmS:F2} мм/с";
    }

    public void Hide()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultText != null)
            resultText.text = "";
    }
}