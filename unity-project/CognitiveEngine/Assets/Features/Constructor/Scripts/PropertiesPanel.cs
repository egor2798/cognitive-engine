using UnityEngine;

public class PropertiesPanel : MonoBehaviour
{
    public void ShowTrackProperties(Track track)
    {
        if (track == null)
        {
            Debug.Log("No track selected");
            return;
        }

        Debug.Log($"Track selected. Points count: {track.Points.Count}");
    }
}