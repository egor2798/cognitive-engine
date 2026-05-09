using UnityEngine;

public class ExternalLinkButton : MonoBehaviour
{
    [Header("External URL")]
    [SerializeField] private string url = "http://localhost:3000";

    public void OpenUrl()
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("ExternalLinkButton: URL is empty");
            return;
        }

        Application.OpenURL(url);
    }

    public void SetUrl(string newUrl)
    {
        url = newUrl;
    }
}
