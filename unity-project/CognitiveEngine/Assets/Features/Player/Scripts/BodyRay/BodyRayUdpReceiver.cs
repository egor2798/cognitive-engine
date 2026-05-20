using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class BodyRayUdpReceiver : MonoBehaviour
{
    [Header("UDP")]
    [SerializeField] private int listenPort = 50555;
    [SerializeField] private bool listenOnStart = true;

    [Header("Pointer Drive")]
    [Tooltip("If enabled, incoming BodyRay packets move the assigned pointer transform.")]
    [SerializeField] private bool drivePointerTransform = true;

    [Tooltip("Assign the same object that PointerTracker uses as cursor/target visual.")]
    [SerializeField] private Transform pointerTransform;

    [Tooltip("Keep original Z while applying incoming X/Y.")]
    [SerializeField] private bool keepPointerZ = true;

    [SerializeField] private float fixedPointerZ = 0f;

    [Header("Signal")]
    [SerializeField] private float packetTimeoutSec = 0.5f;
    [SerializeField] private bool logPackets = false;

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running;

    private readonly object packetLock = new object();
    private BodyRayUnityPacket latestPacket;
    private string latestRawJson = "";
    private float latestPacketUnityTime = -999f;
    private int packetsReceived;

    public int PacketsReceived => packetsReceived;
    public string LatestRawJson => latestRawJson;
    public bool HasFreshPacket => Time.time - latestPacketUnityTime <= packetTimeoutSec;

    private void Start()
    {
        if (listenOnStart)
            StartListening();
    }

    private void Update()
    {
        if (!drivePointerTransform || pointerTransform == null)
            return;

        if (!TryGetLatestPacket(out BodyRayUnityPacket packet))
            return;

        float z = keepPointerZ ? pointerTransform.position.z : fixedPointerZ;
        pointerTransform.position = new Vector3(packet.correctedWorldX, packet.correctedWorldY, z);
    }

    public void StartListening()
    {
        if (running)
            return;

        try
        {
            udpClient = new UdpClient(listenPort);
            running = true;

            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "BodyRayUdpReceiver"
            };

            receiveThread.Start();
            Debug.Log("BodyRayUdpReceiver listening on UDP port " + listenPort);
        }
        catch (Exception ex)
        {
            Debug.LogError("BodyRayUdpReceiver start failed: " + ex.Message);
            running = false;
        }
    }

    public void StopListening()
    {
        running = false;

        try
        {
            udpClient?.Close();
            udpClient = null;
        }
        catch
        {
            // ignored
        }

        try
        {
            if (receiveThread != null && receiveThread.IsAlive)
                receiveThread.Join(100);
        }
        catch
        {
            // ignored
        }

        receiveThread = null;
    }

    public bool TryGetLatestPacket(out BodyRayUnityPacket packet)
    {
        lock (packetLock)
        {
            packet = latestPacket;
        }

        return packet != null && HasFreshPacket;
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string json = Encoding.UTF8.GetString(data);

                BodyRayUnityPacket packet = JsonUtility.FromJson<BodyRayUnityPacket>(json);

                if (packet == null)
                    continue;

                lock (packetLock)
                {
                    latestPacket = packet;
                    latestRawJson = json;
                    packetsReceived++;
                }

                latestPacketUnityTime = Time.time;

                if (logPackets)
                    Debug.Log("BodyRay UDP packet: " + json);
            }
            catch (SocketException)
            {
                if (running)
                    Debug.LogWarning("BodyRayUdpReceiver socket interrupted.");
            }
            catch (Exception ex)
            {
                if (running)
                    Debug.LogWarning("BodyRayUdpReceiver parse error: " + ex.Message);
            }
        }
    }

    private void OnDisable()
    {
        StopListening();
    }

    private void OnApplicationQuit()
    {
        StopListening();
    }
}
