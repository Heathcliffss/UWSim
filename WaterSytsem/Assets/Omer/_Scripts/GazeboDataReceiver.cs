using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class GazeboDataReceiver : MonoBehaviour
{
    [Header("Ağ Ayarları")]
    public int listenPort = 5007;

    [Header("Hareket Ayarları")]
    [Tooltip("Python'dan gelen devasa veriyi Unity ölçeğine uydurmak için (örn: 0.01 veya 0.05)")]
    public float positionScale = 0.05f;

    [Header("Yumuşatma (VR İçin)")]
    public bool useSmoothing = true;
    public float lerpSpeed = 15f;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private readonly object lockObject = new object();

    void Start()
    {
        isRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData))
        {
            IsBackground = true
        };
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        try
        {
            udpClient = new UdpClient(listenPort);
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                // Bir paket oku
                byte[] data = udpClient.Receive(ref anyIP);

                // ÖNEMLİ: Eğer kuyrukta bekleyen BAŞKA paketler varsa, onları da oku ve çöpe at.
                // Sadece en son gelen (en güncel) paketi işleme alacağız. Bu titremeyi (jitter) önler.
                while (udpClient.Available > 0)
                {
                    data = udpClient.Receive(ref anyIP);
                }

                // Paket boyutu doğrulaması
                if (data.Length >= 80)
                {
                    float x = BitConverter.ToSingle(data, 0 * 4);
                    float y = BitConverter.ToSingle(data, 1 * 4);
                    float z = BitConverter.ToSingle(data, 2 * 4);

                    float roll = BitConverter.ToSingle(data, 3 * 4);
                    float pitch = BitConverter.ToSingle(data, 4 * 4);
                    float yaw = BitConverter.ToSingle(data, 5 * 4);

                    lock (lockObject)
                    {
                        // Ölçeklendirme uygula (Çok hızlı uçmasını engeller)
                        targetPosition = new Vector3(-y, z, x) * positionScale;

                        targetRotation = Quaternion.Euler(-pitch, -yaw, roll);
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (isRunning)
                Debug.LogError("UDP Dinleme Hatası: " + e.Message);
        }
    }

    void Update()
    {
        lock (lockObject)
        {
            if (useSmoothing)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * lerpSpeed);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * lerpSpeed);
            }
            else
            {
                transform.localPosition = targetPosition;
                transform.localRotation = targetRotation;
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (udpClient != null)
            udpClient.Close();

        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Join(500);
    }
}