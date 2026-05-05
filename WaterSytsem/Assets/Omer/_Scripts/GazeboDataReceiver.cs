using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class GazeboDataReceiver : MonoBehaviour
{
    [Header("Ağ Ayarları")]
    public int listenPort = 5007;

    [Header("Yumuşatma (VR İçin)")]
    public float lerpSpeed = 15f; // Gelen veriye ne kadar hızlı adapte olunacağı

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    // Arka plandan ana thread'e veri taşımak için
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
                // Python'dan gelen bayt dizisini yakala
                byte[] data = udpClient.Receive(ref anyIP);

                // Paket boyutu doğrulaması (20 float = 80 bayt, kalanlar int)
                if (data.Length >= 80)
                {
                    // C# BitConverter ile byte array'den float çıkarma
                    float x = BitConverter.ToSingle(data, 0 * 4);
                    float y = BitConverter.ToSingle(data, 1 * 4);
                    float z = BitConverter.ToSingle(data, 2 * 4);

                    float roll = BitConverter.ToSingle(data, 3 * 4);
                    float pitch = BitConverter.ToSingle(data, 4 * 4);
                    float yaw = BitConverter.ToSingle(data, 5 * 4);

                    lock (lockObject)
                    {
                        // KOORDİNAT DÖNÜŞÜMÜ (ROS / Gazebo -> Unity)
                        // Simülasyonun Z-up veya Y-up olmasına göre bu eksenleri test sırasında 
                        // değiştirmeniz gerekebilir. Genel standart ROS -> Unity dönüşümü:
                        targetPosition = new Vector3(-y, z, x);

                        // Rotasyon için Euler açılarını quaternion'a çeviriyoruz
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
        // Unity Transform güncellemeleri ana thread'de yapılmak zorundadır
        lock (lockObject)
        {
            // VR ortamında titremeyi önlemek için konum ve rotasyonu yumuşakça (Lerp/Slerp) uyguluyoruz
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * lerpSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * lerpSpeed);
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (udpClient != null)
        {
            udpClient.Close();
        }
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(500);
        }
    }
}