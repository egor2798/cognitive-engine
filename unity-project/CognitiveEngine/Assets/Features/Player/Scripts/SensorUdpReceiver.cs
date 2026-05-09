using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;

public class SensorUdpReceiver : MonoBehaviour
{
    [SerializeField] private PointerTracker pointerTracker;
    [SerializeField] private TMP_Text sensorValueText;
    [SerializeField] private int port = 5005;
    [SerializeField] private float noDataTimeoutSec = 1.0f;
    [SerializeField] private float uiRefreshIntervalSec = 0.25f;
    [SerializeField] private bool logRawMessages = false;

    private UdpClient udpClient;
    private IPEndPoint endPoint;

    private readonly object syncLock = new object();
    private SensorImuData latestData;
    private bool hasNewData;
    private bool hasAnyData;
    private int pendingPacketCount;

    private float lastDataTime;
    private float lastUiRefreshTime;
    private float lastHzTime;
    private int packetsSinceLastHz;
    private float currentHz;

    private bool isClosing;

    private void Start()
    {
        SetStatusNoData();
        lastHzTime = Time.time;

        try
        {
            endPoint = new IPEndPoint(IPAddress.Any, port);
            udpClient = new UdpClient(port);
            udpClient.BeginReceive(OnReceive, null);
            Debug.Log($"[UDP] Listening on port {port}");
        }
        catch (Exception e)
        {
            Debug.LogError("[UDP] Start error: " + e);
        }
    }

    private void Update()
    {
        bool shouldApply = false;
        SensorImuData dataToApply = default;
        int packetCountToAdd = 0;

        lock (syncLock)
        {
            if (hasNewData)
            {
                dataToApply = latestData;
                hasNewData = false;
                shouldApply = true;
            }

            packetCountToAdd = pendingPacketCount;
            pendingPacketCount = 0;
        }

        if (packetCountToAdd > 0)
        {
            packetsSinceLastHz += packetCountToAdd;
            float dt = Time.time - lastHzTime;
            if (dt >= 1.0f)
            {
                currentHz = packetsSinceLastHz / dt;
                packetsSinceLastHz = 0;
                lastHzTime = Time.time;
            }
        }

        if (shouldApply)
        {
            if (pointerTracker != null)
                pointerTracker.SetSensorData(dataToApply);
            else
                Debug.LogError("[UDP] pointerTracker == null");

            hasAnyData = true;
            lastDataTime = Time.time;
        }

        bool noData = !hasAnyData || Time.time - lastDataTime > noDataTimeoutSec;
        if (noData)
        {
            currentHz = 0f;
            SetStatusNoData();
            return;
        }

        if (Time.time - lastUiRefreshTime >= uiRefreshIntervalSec)
        {
            lastUiRefreshTime = Time.time;
            SetStatusData();
        }
    }

    private void OnDestroy()
    {
        isClosing = true;

        try
        {
            udpClient?.Close();
        }
        catch
        {
        }

        udpClient = null;
    }

    private void OnReceive(IAsyncResult ar)
    {
        if (isClosing || udpClient == null)
            return;

        try
        {
            byte[] data = udpClient.EndReceive(ar, ref endPoint);
            string msg = Encoding.UTF8.GetString(data).Trim();

            if (logRawMessages)
                Debug.Log("[UDP] Raw message: " + msg);

            if (TryParseSensorMessage(msg, out SensorImuData parsed))
            {
                lock (syncLock)
                {
                    latestData = parsed;
                    hasNewData = true;
                    pendingPacketCount++;
                }
            }
            else
            {
                Debug.LogWarning("[UDP] Parse failed: " + msg);
            }
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception e)
        {
            if (!isClosing)
                Debug.LogError("[UDP] Receive error: " + e);
        }

        if (isClosing || udpClient == null)
            return;

        try
        {
            udpClient.BeginReceive(OnReceive, null);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception e)
        {
            if (!isClosing)
                Debug.LogError("[UDP] BeginReceive restart error: " + e);
        }
    }

    private bool TryParseSensorMessage(string msg, out SensorImuData data)
    {
        data = default;

        if (string.IsNullOrWhiteSpace(msg))
            return false;

        string[] parts = msg.Split(',');

        // Старый формат: roll,pitch
        if (parts.Length >= 2 &&
            TryParseFloat(parts[0], out float roll) &&
            TryParseFloat(parts[1], out float pitch))
        {
            data.roll = roll;
            data.pitch = pitch;

            // Новый формат:
            // roll,pitch,yaw,accX,accY,accZ,gyroX,gyroY,gyroZ[,sequenceId]
            if (parts.Length >= 9 &&
                TryParseFloat(parts[2], out float yaw) &&
                TryParseFloat(parts[3], out float accX) &&
                TryParseFloat(parts[4], out float accY) &&
                TryParseFloat(parts[5], out float accZ) &&
                TryParseFloat(parts[6], out float gyroX) &&
                TryParseFloat(parts[7], out float gyroY) &&
                TryParseFloat(parts[8], out float gyroZ))
            {
                data.yaw = yaw;
                data.accelerationG = new Vector3(accX, accY, accZ);
                data.gyroDegS = new Vector3(gyroX, gyroY, gyroZ);
                data.hasAcceleration = true;
                data.hasGyro = true;

                if (parts.Length >= 10 && int.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sequenceId))
                {
                    data.sequenceId = sequenceId;
                    data.hasSequence = true;
                }
            }
            else
            {
                data.yaw = 0f;
                data.accelerationG = Vector3.zero;
                data.gyroDegS = Vector3.zero;
                data.hasAcceleration = false;
                data.hasGyro = false;
            }

            return true;
        }

        return false;
    }

    private bool TryParseFloat(string text, out float value)
    {
        return float.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value
        );
    }

    private void SetStatusData()
    {
        if (sensorValueText == null)
            return;

        if (pointerTracker != null)
            sensorValueText.text = pointerTracker.GetSensorDebugText(currentHz);
        else
            sensorValueText.text = "Датчик: OK, но PointerTracker не назначен";
    }

    private void SetStatusNoData()
    {
        if (sensorValueText != null)
            sensorValueText.text = "Датчик: нет данных";
    }
}
