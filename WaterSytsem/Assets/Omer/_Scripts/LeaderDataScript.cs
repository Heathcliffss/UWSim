using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// Python otonom lider scriptinden (gem_leader_scan.py) gelen pozu alýr.
///
/// GazeboDataReceiver'dan FARKI:
///   - Pozisyon eþlemesi AYNI (fallback: px -> X, pz -> Y up, -py -> Z forward).
///     Python zaten (wx-START_X, -(wz-START_Z), height-START_Y) yolluyor,
///     yani burada ekstra dönüþüme gerek yok, 1:1 oturuyor.
///   - Rotasyon: RPY matrisi / first-packet-relative ZÝNCÝRÝ YOK.
///     Burun doðrudan hareket heading'ini (paketteki yaw) takip eder.
///     Bu, "yan gitme" (crab-walk) sorununu kökten çözer.
///
/// GazeboDataReceiver.cs'e HÝÇ dokunulmaz. Bu ayrý bir objeye eklenir.
/// Python tarafýnda UDP_PORT'u bu listenPort ile eþle (Gazebo 5007'de kalsýn,
/// lider örn. 5008 kullansýn), yoksa iki alýcý ayný portu dinleyemez.
/// </summary>
public class LeaderDataReceiver : MonoBehaviour
{
    [Header("Að Ayarlarý")]
    public int listenPort = 5008;   // Gazebo 5007'de; lider farklý port

    [Header("Ölçek")]
    public float positionScale = 1.0f;

    [Header("Unity kayýt baþlangýç pozu")]
    public bool forceUnityStartPose = false;
    public bool useLocalTransform = true;
    public Vector3 forcedUnityStartPosition = new Vector3(-152f, -145.35f, 923f);
    public Vector3 forcedUnityStartEuler = Vector3.zero;

    [Header("Model forward düzeltme")]
    [Tooltip("Mesh burnu +X ise -90, +Z ise 0, ters ise 180. Inspector'dan canlý dene.")]
    public float modelForwardOffsetDeg = -90f;

    [Header("Interpolation Buffer")]
    [Tooltip("Görsel hareketi kaç saniye geriden interpolate edeceðiz. 0.06 - 0.12 arasý iyi.")]
    public float interpolationDelay = 0.06f;
    public int maxBufferSamples = 200;

    [Header("Jitter Azaltma")]
    public bool useSmoothing = true;
    public float positionSmoothTime = 0.05f;   // GazeboReceiver'daki 0.225 çok yüksekti, düþük tut
    public float rotationSmoothSpeed = 18f;
    public float positionDeadband = 0.001f;
    public float rotationDeadbandDeg = 0.03f;

    [Header("Debug")]
    public bool debugPackets = true;
    public int debugEveryNPackets = 60;
    private int _packetCounter = 0;

    [Header("First packet origin")]
    public bool useFirstPacketAsOrigin = true;

    private Vector3 _smoothVelocity = Vector3.zero;

    private UdpClient _udp;
    private Thread _thread;
    private bool _running = false;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private bool _hasFirstPacketReference = false;
    private Vector3 _firstPacketPosition;

    private readonly object _bufferLock = new object();
    private readonly List<PacketSample> _buffer = new List<PacketSample>();
    private bool _hasData = false;

    private struct PacketSample
    {
        public double time;
        public float px, py, pz;
        public float roll, pitch, yaw;
        public float seq, senderDt;
    }

    private struct UnityPose
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    void Start()
    {
        if (forceUnityStartPose)
        {
            if (useLocalTransform)
            {
                transform.localPosition = forcedUnityStartPosition;
                transform.localRotation = Quaternion.Euler(forcedUnityStartEuler);
            }
            else
            {
                transform.position = forcedUnityStartPosition;
                transform.rotation = Quaternion.Euler(forcedUnityStartEuler);
            }
        }

        _startPosition = useLocalTransform ? transform.localPosition : transform.position;
        _startRotation = useLocalTransform ? transform.localRotation : transform.rotation;

        Debug.Log("[LEADER UDP] useLocalTransform: " + useLocalTransform);
        Debug.Log("[LEADER UDP] start LOCAL pos: " + transform.localPosition);
        Debug.Log("[LEADER UDP] start WORLD pos: " + transform.position);

        _running = true;
        _thread = new Thread(ReceiveLoop) { IsBackground = true };
        _thread.Start();

        Debug.Log("[LEADER UDP] Receiver started on port " + listenPort);
    }

    void ReceiveLoop()
    {
        try
        {
            _udp = new UdpClient(listenPort);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                byte[] data = _udp.Receive(ref ep);
                ProcessPacket(data);

                while (_udp.Available > 0)
                {
                    data = _udp.Receive(ref ep);
                    ProcessPacket(data);
                }
            }
        }
        catch (SocketException)
        {
            if (_running)
                Debug.LogError("[LEADER UDP] Socket exception (port " + listenPort + " kullanýmda olabilir).");
        }
        catch (Exception e)
        {
            if (_running)
                Debug.LogError("[LEADER UDP] Hata: " + e.Message);
        }
    }

    private void ProcessPacket(byte[] data)
    {
        // 9 float * 4 byte = 36 byte
        // 0:x 1:y 2:z 3:roll 4:pitch 5:yaw 6:time 7:seq 8:senderDt
        if (data == null || data.Length < 36)
            return;

        PacketSample s = new PacketSample();
        s.px = BitConverter.ToSingle(data, 0);
        s.py = BitConverter.ToSingle(data, 4);
        s.pz = BitConverter.ToSingle(data, 8);
        s.roll = BitConverter.ToSingle(data, 12);
        s.pitch = BitConverter.ToSingle(data, 16);
        s.yaw = BitConverter.ToSingle(data, 20);
        s.time = BitConverter.ToSingle(data, 24);
        s.seq = BitConverter.ToSingle(data, 28);
        s.senderDt = BitConverter.ToSingle(data, 32);

        _packetCounter++;

        if (debugPackets && (_packetCounter <= 5 || _packetCounter % debugEveryNPackets == 0))
        {
            Debug.Log(
                "[LEADER UDP] seq=" + s.seq +
                " pos=(" + s.px.ToString("F3") + ", " + s.py.ToString("F3") + ", " + s.pz.ToString("F3") + ")" +
                " yaw=" + s.yaw.ToString("F1") +
                " t=" + s.time.ToString("F3"));
        }

        lock (_bufferLock)
        {
            // Python restart -> timestamp geriye düþerse buffer'ý temizle
            if (_buffer.Count > 0 && s.time < _buffer[_buffer.Count - 1].time)
                _buffer.Clear();

            _buffer.Add(s);
            _hasData = true;

            while (_buffer.Count > maxBufferSamples)
                _buffer.RemoveAt(0);
        }
    }

    void Update()
    {
        if (!_hasData)
            return;

        PacketSample a, b;
        float alpha;
        if (!TryGetInterpolatedSamples(out a, out b, out alpha))
            return;

        UnityPose poseA = PacketToUnityPose(a);
        UnityPose poseB = PacketToUnityPose(b);

        Vector3 targetPos = Vector3.Lerp(poseA.position, poseB.position, alpha);
        Quaternion targetRot = Quaternion.Slerp(poseA.rotation, poseB.rotation, alpha);

        if (!useSmoothing)
        {
            ApplyPose(targetPos, targetRot);
            return;
        }

        float dt = Time.deltaTime;
        Vector3 currentPos = useLocalTransform ? transform.localPosition : transform.position;
        Quaternion currentRot = useLocalTransform ? transform.localRotation : transform.rotation;

        if ((targetPos - currentPos).sqrMagnitude > positionDeadband * positionDeadband)
        {
            Vector3 smoothedPos = Vector3.SmoothDamp(
                currentPos, targetPos, ref _smoothVelocity,
                positionSmoothTime, Mathf.Infinity, dt);

            if (useLocalTransform) transform.localPosition = smoothedPos;
            else transform.position = smoothedPos;
        }

        if (Quaternion.Angle(currentRot, targetRot) > rotationDeadbandDeg)
        {
            float t = 1.0f - Mathf.Exp(-rotationSmoothSpeed * dt);
            Quaternion smoothedRot = Quaternion.Slerp(currentRot, targetRot, t);

            if (useLocalTransform) transform.localRotation = smoothedRot;
            else transform.rotation = smoothedRot;
        }
    }

    private bool TryGetInterpolatedSamples(out PacketSample a, out PacketSample b, out float alpha)
    {
        a = default; b = default; alpha = 0f;

        lock (_bufferLock)
        {
            if (_buffer.Count == 0)
                return false;

            if (_buffer.Count == 1)
            {
                a = _buffer[0]; b = _buffer[0]; alpha = 0f;
                return true;
            }

            double newestTime = _buffer[_buffer.Count - 1].time;
            double renderTime = newestTime - interpolationDelay;

            while (_buffer.Count >= 3 && _buffer[1].time < renderTime)
                _buffer.RemoveAt(0);

            if (renderTime <= _buffer[0].time)
            {
                a = _buffer[0]; b = _buffer[0]; alpha = 0f;
                return true;
            }

            for (int i = 0; i < _buffer.Count - 1; i++)
            {
                PacketSample s0 = _buffer[i];
                PacketSample s1 = _buffer[i + 1];

                if (s0.time <= renderTime && renderTime <= s1.time)
                {
                    a = s0; b = s1;
                    double dt = s1.time - s0.time;
                    alpha = (dt <= 1e-6) ? 0f : Mathf.Clamp01((float)((renderTime - s0.time) / dt));
                    return true;
                }
            }

            a = _buffer[_buffer.Count - 1]; b = _buffer[_buffer.Count - 1]; alpha = 0f;
            return true;
        }
    }

    private UnityPose PacketToUnityPose(PacketSample s)
    {
        // ---- POZÝSYON (GazeboReceiver fallback ile birebir ayný) ----
        Vector3 pyRaw = new Vector3(s.px, s.py, s.pz);

        if (useFirstPacketAsOrigin && !_hasFirstPacketReference)
        {
            _firstPacketPosition = pyRaw;
            _hasFirstPacketReference = true;
            Debug.Log("[LEADER UDP] First packet origin set: " + _firstPacketPosition.ToString("F3"));
        }

        Vector3 pyRel = useFirstPacketAsOrigin ? (pyRaw - _firstPacketPosition) : pyRaw;

        // Python X -> Unity X, Python Z -> Unity Y (up), Python Y -> Unity -Z (forward)
        Vector3 unityRel = new Vector3(pyRel.x, pyRel.z, -pyRel.y);

        UnityPose pose;
        pose.position = _startPosition + (unityRel * positionScale);


        // ---- ROTASYON (heading-takip, RPY zinciri YOK) ----
        // Python yaw konvansiyonu pozisyonla ayný: yaw=0 -> +Z, yaw=90 -> +X
        float yawRad = s.yaw * Mathf.Deg2Rad;
        Vector3 fwdLocal = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));

        Quaternion faceLocal = Quaternion.LookRotation(fwdLocal, Vector3.up)
                               * Quaternion.Euler(0f, modelForwardOffsetDeg, 0f);

        pose.rotation = faceLocal;

        return pose;
    }

    private void ApplyPose(Vector3 position, Quaternion rotation)
    {
        if (useLocalTransform)
        {
            transform.localPosition = position;
            transform.localRotation = rotation;
        }
        else
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }

    void OnDestroy()
    {
        _running = false;
        try { _udp?.Close(); } catch { }
        try { if (_thread != null && _thread.IsAlive) _thread.Join(500); } catch { }
    }
}