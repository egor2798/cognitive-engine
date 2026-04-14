using UnityEngine;
using UnityEngine.SceneManagement;

public class script_button_quit : MonoBehaviour
{
    public void OnStartClick()
    {
        SceneManager.LoadScene("menu_settings");
    }
    public void OnSettingsClick()
    {
        
        SceneManager.LoadScene("menu_choose_scene");
    }
    public void OnExitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
