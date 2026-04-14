using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class script_button_settings : MonoBehaviour
{
    public void OnExitClick()
    {
        SceneManager.LoadScene("menu_start");
    } 
        
}
