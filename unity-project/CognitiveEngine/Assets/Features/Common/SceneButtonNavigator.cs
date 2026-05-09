using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonNavigator : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "menu_start";
    [SerializeField] private string chooseSceneName = "menu_choose_scene";
    [SerializeField] private string settingsSceneName = "menu_settings";
    [SerializeField] private string constructorSceneName = "ConstructorScene";
    [SerializeField] private string playerSceneName = "PlayerScene";

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneButtonNavigator: scene name is empty");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    public void LoadChooseSceneMenu()
    {
        LoadScene(chooseSceneName);
    }

    public void LoadSettings()
    {
        LoadScene(settingsSceneName);
    }

    public void LoadConstructor()
    {
        LoadScene(constructorSceneName);
    }

    public void LoadPlayer()
    {
        LoadScene(playerSceneName);
    }

    public void QuitApp()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
