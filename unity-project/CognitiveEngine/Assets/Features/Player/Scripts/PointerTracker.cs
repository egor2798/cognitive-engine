using UnityEngine;
using UnityEngine.InputSystem;

public class PointerTracker : MonoBehaviour
{
    [SerializeField] private PointerInputSource inputSource = PointerInputSource.Mouse;

    private Vector2 sensorPosition;
    private bool hasSensorData;

    public void Configure(PointerInputSource source)
    {
        inputSource = source;
    }

    public void SetSensorPosition(Vector2 position)
    {
        sensorPosition = position;
        hasSensorData = true;
    }

    public Vector2 GetPointerPosition()
    {
        if (inputSource == PointerInputSource.Sensor && hasSensorData)
            return sensorPosition;

        if (Mouse.current == null || Camera.main == null)
            return Vector2.zero;

        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f));
        world.z = 0f;
        return world;
    }
}