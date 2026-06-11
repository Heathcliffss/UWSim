using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class RovLeds : MonoBehaviour
{
    [Header("Isik Gruplari (renkli kureler)")]
    public GameObject[] frontLEDs;
    public GameObject[] backLEDs;
    public GameObject[] leftLEDs;
    public GameObject[] rightLEDs;

    [Header("Test Ayarlari")]
    public bool isEmergency = false;

    // Mevcut back-only pattern testi icin korunuyor.
    public bool backOnlyTest = true;

    // true yapilirsa sadece BACK LED'ler surekli acik kalir. Pattern/blink uygulanmaz.
    public bool forceBackConstantOn = false;

    [Header("Zamanlama Ayari")]
    public int targetFPS = 60;
    public float tickRate = 0.1f;   // SimulasyonVeriAlici.cs eristigi icin korunuyor

    [Header("Frame Tabanli Pattern Ayari")]
    public int framesPerBit = 6;

    [Header("Emergency (Kirmizi) Ayari")]
    // Emergency'de LED kurelerinin MATERYALI kirmiziya cevrilir.
    // Asagidaki gruplardaki (front/back/left/right) kurelerin emission'i
    // ve base color'i kirmizi yapilir. Ekstra atama gerekmez.
    public Color emergencyColor = Color.red;
    public float emergencyEmissionNits = 1200f;   // mevcut LED ile ayni parlaklik

    [Header("Emergency UDP Tetik")]
    public bool listenForEmergency = true;   // sadece LIDER'de true olsun (follower'da kapat)
    public int emergencyUdpPort = 5012;
    public string emergencyOnMessage = "EMERGENCY";
    public string emergencyOffMessage = "EMERGENCY_OFF";

    private string pFront = "11110000";
    private string pBack = "11001100";
    private string pLeft = "10101010";
    private string pRight = "10011001";

    // 12 acik / 4 kapali (%75 acik). FPS dususu / paket kaybina dayanikli alarm.
    private string pEmer = "1111111111110000";

    private int startFrame;

    // HDRP/Lit property ID'leri
    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ID_EmissiveColor = Shader.PropertyToID("_EmissiveColor");

    // --- Materyal instance'lari ve orijinal renkleri ---
    private struct LedMat
    {
        public Material mat;
        public Color baseColor;
        public Color emissive;
        public bool hasBase;
        public bool hasEmissive;
    }
    private List<LedMat> _leds = new List<LedMat>();
    private bool _emergencyApplied = false;
    private bool _captured = false;

    // --- UDP dinleyici ---
    private UdpClient _udp;
    private Thread _udpThread;
    private volatile bool _udpRunning = false;
    private volatile int _pendingEmergency = 0;   // 0=yok, 1=ac, 2=kapat

    void Start()
    {
        Application.targetFrameRate = targetFPS;
        startFrame = Time.frameCount;
        UpdateFramesPerBit();

        CaptureLedMaterials();

        if (listenForEmergency)
            StartUdpListener();
    }

    void OnDestroy() { StopUdpListener(); }
    void OnApplicationQuit() { StopUdpListener(); }

    void Update()
    {
        UpdateFramesPerBit();

        if (_pendingEmergency == 1) { isEmergency = true; _pendingEmergency = 0; }
        else if (_pendingEmergency == 2) { isEmergency = false; _pendingEmergency = 0; }

        int elapsedFrames = Time.frameCount - startFrame;
        int bitIndex = elapsedFrames / framesPerBit;

        // ============================================================
        // 1) EMERGENCY MODE  --  EN YUKSEK ONCELIK
        // Tum LED kureleri KIRMIZIYA cevrilir, 12/4 paterniyle yanip soner.
        // ============================================================
        if (isEmergency)
        {
            ApplyEmergencyColor();

            bool state = pEmer[bitIndex % pEmer.Length] == '1';
            SetGroup(frontLEDs, state);
            SetGroup(backLEDs, state);
            SetGroup(leftLEDs, state);
            SetGroup(rightLEDs, state);
            return;
        }
        else
        {
            RestoreOriginalColor();
        }

        // ============================================================
        // 2) CONSTANT ON BACK TEST MODE
        // ============================================================
        if (forceBackConstantOn)
        {
            SetGroup(frontLEDs, false);
            SetGroup(backLEDs, true);
            SetGroup(leftLEDs, false);
            SetGroup(rightLEDs, false);
            return;
        }

        // ============================================================
        // 3) NORMAL PATTERN MODE
        // ============================================================
        bool frontState = pFront[bitIndex % pFront.Length] == '1';
        bool backState = pBack[bitIndex % pBack.Length] == '1';
        bool leftState = pLeft[bitIndex % pLeft.Length] == '1';
        bool rightState = pRight[bitIndex % pRight.Length] == '1';

        if (backOnlyTest)
        {
            SetGroup(frontLEDs, false);
            SetGroup(backLEDs, backState);
            SetGroup(leftLEDs, false);
            SetGroup(rightLEDs, false);
        }
        else
        {
            SetGroup(frontLEDs, frontState);
            SetGroup(backLEDs, backState);
            SetGroup(leftLEDs, leftState);
            SetGroup(rightLEDs, rightState);
        }
    }

    // ============================================================
    // LED MATERYAL RENK KONTROLU  --  HDRP/Lit (Base + Emission)
    // ============================================================
    void CaptureLedMaterials()
    {
        _leds.Clear();
        AddGroup(frontLEDs);
        AddGroup(backLEDs);
        AddGroup(leftLEDs);
        AddGroup(rightLEDs);
        _captured = _leds.Count > 0;
    }

    void AddGroup(GameObject[] leds)
    {
        if (leds == null) return;
        foreach (var go in leds)
        {
            if (go == null) continue;
            var rend = go.GetComponent<Renderer>();
            if (rend == null) continue;

            // .material -> bu obje icin instance olusturur (sharedMaterial'i kalici bozmaz)
            Material m = rend.material;

            LedMat led = new LedMat();
            led.mat = m;
            led.hasBase = m.HasProperty(ID_BaseColor);
            led.hasEmissive = m.HasProperty(ID_EmissiveColor);
            if (led.hasBase) led.baseColor = m.GetColor(ID_BaseColor);
            if (led.hasEmissive) led.emissive = m.GetColor(ID_EmissiveColor);
            _leds.Add(led);
        }
    }

    void ApplyEmergencyColor()
    {
        if (_emergencyApplied || !_captured) return;

        // Emission rengi = renk * yogunluk (HDRP Nits). Parlak kirmizi LED.
        Color emis = emergencyColor * emergencyEmissionNits;

        foreach (var led in _leds)
        {
            if (led.mat == null) continue;
            if (led.hasBase) led.mat.SetColor(ID_BaseColor, emergencyColor);
            if (led.hasEmissive)
            {
                led.mat.SetColor(ID_EmissiveColor, emis);
                led.mat.EnableKeyword("_EMISSION");
            }
        }
        _emergencyApplied = true;
    }

    void RestoreOriginalColor()
    {
        if (!_emergencyApplied || !_captured) return;

        foreach (var led in _leds)
        {
            if (led.mat == null) continue;
            if (led.hasBase) led.mat.SetColor(ID_BaseColor, led.baseColor);
            if (led.hasEmissive) led.mat.SetColor(ID_EmissiveColor, led.emissive);
        }
        _emergencyApplied = false;
    }

    // ============================================================
    // UDP DINLEYICI (port 5012) -- ayri thread
    // ============================================================
    void StartUdpListener()
    {
        try
        {
            _udp = new UdpClient(emergencyUdpPort);
            _udpRunning = true;
            _udpThread = new Thread(UdpLoop) { IsBackground = true };
            _udpThread.Start();
            Debug.Log($"[RovLeds] Emergency UDP dinleniyor: port {emergencyUdpPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[RovLeds] UDP baslatilamadi (port {emergencyUdpPort}): {e.Message}");
        }
    }

    void UdpLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_udpRunning)
        {
            try
            {
                byte[] data = _udp.Receive(ref remote);
                string msg = Encoding.UTF8.GetString(data).Trim();

                if (msg == emergencyOnMessage) _pendingEmergency = 1;
                else if (msg == emergencyOffMessage) _pendingEmergency = 2;
            }
            catch (SocketException) { break; }
            catch (System.Exception) { }
        }
    }

    void StopUdpListener()
    {
        _udpRunning = false;
        try { _udp?.Close(); } catch { }
        _udp = null;
    }

    // ============================================================
    void UpdateFramesPerBit()
    {
        if (targetFPS <= 0) targetFPS = 60;
        if (tickRate <= 0f) tickRate = 0.1f;
        framesPerBit = Mathf.Max(1, Mathf.RoundToInt(tickRate * targetFPS));
    }

    void SetGroup(GameObject[] leds, bool state)
    {
        if (leds == null) return;
        foreach (var led in leds)
        {
            if (led != null)
                led.SetActive(state);
        }
    }
}