using TMPro;
using UnityEngine;

public class BodyRayUdpDebugPanel : MonoBehaviour
{
    [SerializeField] private BodyRayUdpReceiver receiver;
    [SerializeField] private TMP_Text text;

    private void Update()
    {
        if (text == null)
            return;

        if (receiver == null)
        {
            text.text = "BodyRay UDP: receiver not assigned";
            return;
        }

        if (receiver.TryGetLatestPacket(out BodyRayUnityPacket packet))
        {
            text.text =
                "BodyRay UDP: OK\\n" +
                "Packets: " + receiver.PacketsReceived + "\\n" +
                "Mode: " + packet.trackingMode + "\\n" +
                "Tracker: " + packet.trackerId + "\\n" +
                "Cursor: " + packet.cursorId + "\\n" +
                "World: " + packet.correctedWorldX.ToString("0.###") + ", " + packet.correctedWorldY.ToString("0.###") + "\\n" +
                "Confidence: " + packet.confidence.ToString("0.00") + "\\n" +
                "Calibration: " + packet.calibrationStatus;
        }
        else
        {
            text.text =
                "BodyRay UDP: waiting\\n" +
                "Packets: " + receiver.PacketsReceived;
        }
    }
}
