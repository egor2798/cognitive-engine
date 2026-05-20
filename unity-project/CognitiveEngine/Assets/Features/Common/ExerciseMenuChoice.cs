using UnityEngine;
using UnityEngine.SceneManagement;

public class ExerciseMenuChoice : MonoBehaviour
{
    [Header("Exercise")]
    [SerializeField] private string exerciseName = "Упражнение №1";
    [SerializeField] private string projectName = "";

    [Header("Scene")]
    [SerializeField] private string playerSceneName = "PlayerScene";

    private const string SelectedProjectPrefsKey = "selectedProjectName";
    private const string SelectedExercisePrefsKey = "selectedExerciseName";

    public void OpenExercise()
    {
        PlayerPrefs.SetString(SelectedExercisePrefsKey, exerciseName ?? "");
        PlayerPrefs.SetString(SelectedProjectPrefsKey, projectName ?? "");
        PlayerPrefs.Save();

        Debug.Log($"ExerciseMenuChoice: selected exercise='{exerciseName}', project='{projectName}'");
        SceneManager.LoadScene(playerSceneName);
    }
}
