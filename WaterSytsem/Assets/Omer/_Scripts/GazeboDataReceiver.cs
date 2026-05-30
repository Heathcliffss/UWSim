using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class GazeboDataReceiver : MonoBehaviour
{
    [Header("Ağ Ayarları")]
    public int listenPort = 5007;

    [Header("Ölçek")]
    public float positionScale = 1.0f;

    [Header("Python başlangıç ofseti")]
    public Vector3 pythonOrigin = new Vector3(-104.002f, -118.9f, 873.614f);

    [Header("Unity kayıt başlangıç pozu")]
    public bool forceUnityStartPose = true;
    public bool useLocalTransform = true;
    public Vector3 forcedUnityStartPosition = new Vector3(-124.123f, -125.322f, 940.1f);
    public Vector3 forcedUnityStartEuler = new Vector3(0f, 0f, 0f);

    [Header("Klavye kayıt modu")]
    public bool keyboardRelativeMode = true;

    [Header("Interpolation Buffer")]
    [Tooltip("Görsel hareketi kaç saniye geriden interpolate edeceğiz. 0.08 - 0.15 arası genelde iyi.")]
    public float interpolationDelay = 0.06f;

    [Tooltip("Buffer içinde tutulacak maksimum paket sayısı.")]
    public int maxBufferSamples = 200;

    [Header("Jitter Azaltma")]
    public bool useSmoothing = true;
    public float positionSmoothTime = 0.025f;
    public float rotationSmoothSpeed = 18f;
    public float positionDeadband = 0.001f;
    public float rotationDeadbandDeg = 0.03f;

    private Vector3 _smoothVelocity = Vector3.zero;

    private UdpClient _udp;
    private Thread _thread;
    private bool _running = false;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private readonly object _bufferLock = new object();
    private readonly List<PacketSample> _buffer = new List<PacketSample>();

    private bool _hasData = false;

    private struct PacketSample
    {
        public double time;

        public float px;
        public float py;
        public float pz;

        public float roll;
        public float pitch;
        public float yaw;

        public float seq;
        public float senderDt;
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

        Debug.Log("[UDP] useLocalTransform: " + useLocalTransform);
        Debug.Log("[UDP] Unity start LOCAL position: " + transform.localPosition);
        Debug.Log("[UDP] Unity start WORLD position: " + transform.position);
        Debug.Log("[UDP] Python origin: " + pythonOrigin);

        _running = true;

        _thread = new Thread(ReceiveLoop);
        _thread.IsBackground = true;
        _thread.Start();

        Debug.Log("[UDP] Receiver started on port " + listenPort);
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

                // Artık eski paketleri çöpe atmıyoruz.
                // Kuyrukta bekleyen her paketi buffer'a ekliyoruz.
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
                Debug.LogError("[UDP] Socket exception.");
        }
        catch (Exception e)
        {
            if (_running)
                Debug.LogError("[UDP] Hata: " + e.Message);
        }
    }

    private void ProcessPacket(byte[] data)
    {
        // İlk 9 float lazım:
        // 0:x, 1:y, 2:z, 3:roll, 4:pitch, 5:yaw, 6:timestamp, 7:seq, 8:senderDt
        // 9 float * 4 byte = 36 byte
        if (data == null || data.Length < 36)
            return;

        PacketSample sample = new PacketSample();

        sample.px = BitConverter.ToSingle(data, 0);
        sample.py = BitConverter.ToSingle(data, 4);
        sample.pz = BitConverter.ToSingle(data, 8);

        sample.roll = BitConverter.ToSingle(data, 12);
        sample.pitch = BitConverter.ToSingle(data, 16);
        sample.yaw = BitConverter.ToSingle(data, 20);

        // Python vals[6], vals[7], vals[8]
        sample.time = BitConverter.ToSingle(data, 24);
        sample.seq = BitConverter.ToSingle(data, 28);
        sample.senderDt = BitConverter.ToSingle(data, 32);

        lock (_bufferLock)
        {
            // Python restart/reset sonrası timestamp geriye düşerse buffer'ı temizle
            if (_buffer.Count > 0 && sample.time < _buffer[_buffer.Count - 1].time)
            {
                _buffer.Clear();
            }

            _buffer.Add(sample);
            _hasData = true;

            // Buffer çok büyümesin
            while (_buffer.Count > maxBufferSamples)
            {
                _buffer.RemoveAt(0);
            }
        }
    }

    void Update()
    {
        if (!_hasData)
            return;

        PacketSample a;
        PacketSample b;
        float alpha;

        bool ok = TryGetInterpolatedSamples(out a, out b, out alpha);

        if (!ok)
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

        // Pozisyon deadband
        Vector3 currentPos = useLocalTransform ? transform.localPosition : transform.position;
        Quaternion currentRot = useLocalTransform ? transform.localRotation : transform.rotation;

        float posErrorSqr = (targetPos - currentPos).sqrMagnitude;

        float deadbandSqr = positionDeadband * positionDeadband;

        if (posErrorSqr > deadbandSqr)
        {
            Vector3 smoothedPos = Vector3.SmoothDamp(
                currentPos,
                targetPos,
                ref _smoothVelocity,
                positionSmoothTime,
                Mathf.Infinity,
                dt
            );

            if (useLocalTransform)
                transform.localPosition = smoothedPos;
            else
                transform.position = smoothedPos;
        }

        // Rotasyon deadband
        float angleError = Quaternion.Angle(currentRot, targetRot);

        if (angleError > rotationDeadbandDeg)
        {
            float t = 1.0f - Mathf.Exp(-rotationSmoothSpeed * dt);

            Quaternion smoothedRot = Quaternion.Slerp(
                currentRot,
                targetRot,
                t
            );

            if (useLocalTransform)
                transform.localRotation = smoothedRot;
            else
                transform.rotation = smoothedRot;
        }
    }

    private bool TryGetInterpolatedSamples(out PacketSample a, out PacketSample b, out float alpha)
    {
        a = default;
        b = default;
        alpha = 0f;

        lock (_bufferLock)
        {
            if (_buffer.Count == 0)
                return false;

            if (_buffer.Count == 1)
            {
                a = _buffer[0];
                b = _buffer[0];
                alpha = 0f;
                return true;
            }

            double newestTime = _buffer[_buffer.Count - 1].time;
            double renderTime = newestTime - interpolationDelay;

            // Çok eski paketleri temizle ama renderTime'dan hemen önceki paketi koru
            while (_buffer.Count >= 3 && _buffer[1].time < renderTime)
            {
                _buffer.RemoveAt(0);
            }

            // renderTime buffer'ın en başından da eskiyse ilk sample'ı kullan
            if (renderTime <= _buffer[0].time)
            {
                a = _buffer[0];
                b = _buffer[0];
                alpha = 0f;
                return true;
            }

            // renderTime iki sample arasında mı?
            for (int i = 0; i < _buffer.Count - 1; i++)
            {
                PacketSample s0 = _buffer[i];
                PacketSample s1 = _buffer[i + 1];

                if (s0.time <= renderTime && renderTime <= s1.time)
                {
                    a = s0;
                    b = s1;

                    double dt = s1.time - s0.time;

                    if (dt <= 1e-6)
                    {
                        alpha = 0f;
                    }
                    else
                    {
                        alpha = Mathf.Clamp01((float)((renderTime - s0.time) / dt));
                    }

                    return true;
                }
            }

            // renderTime en son sample'dan yeniyse son sample'ı kullan
            a = _buffer[_buffer.Count - 1];
            b = _buffer[_buffer.Count - 1];
            alpha = 0f;
            return true;
        }
    }

    private UnityPose PacketToUnityPose(PacketSample s)
    {
        Vector3 pyRel;

        if (keyboardRelativeMode)
        {
            // Klavye kayıt modu:
            // Python doğrudan relatif x,y,z gönderir.
            // Bu yüzden pythonOrigin çıkarılmaz.
            pyRel = new Vector3(
                s.px,
                s.py,
                s.pz
            );
        }
        else
        {
            // Gazebo / ArduSub absolute pose modu:
            // Python absolute pose gönderir, origin çıkarılır.
            pyRel = new Vector3(
                s.px - pythonOrigin.x,
                s.py - pythonOrigin.y,
                s.pz - pythonOrigin.z
            );
        }

        Vector3 unityRel = PythonVectorToUnity(pyRel);

        UnityPose pose;
        pose.position = _startPosition + unityRel * positionScale;

        Quaternion unityRotRelative = PythonRPYToUnityRotation(
            s.roll,
            s.pitch,
            s.yaw
        );

        pose.rotation = _startRotation * unityRotRelative;

        return pose;
    }

    void OnDestroy()
    {
        _running = false;

        try
        {
            _udp?.Close();
        }
        catch { }

        try
        {
            if (_thread != null && _thread.IsAlive)
                _thread.Join(500);
        }
        catch { }
    }

    // --------------------------------------------------------------------
    // Python:
    //   +X = forward
    //   +Y = right
    //   +Z = up
    //
    // Unity model:
    //   +X = forward
    //   +Y = up
    //   +Z = left
    //
    // Mapping:
    //   Python X -> Unity X
    //   Python Y -> Unity -Z
    //   Python Z -> Unity Y
    // --------------------------------------------------------------------
    private Vector3 PythonVectorToUnity(Vector3 pyVec)
    {
        return new Vector3(
            pyVec.x,
            pyVec.z,
            -pyVec.y
        );
    }

    private Quaternion PythonRPYToUnityRotation(float rollDeg, float pitchDeg, float yawDeg)
    {
        float roll = rollDeg * Mathf.Deg2Rad;
        float pitch = pitchDeg * Mathf.Deg2Rad;
        float yaw = yawDeg * Mathf.Deg2Rad;

        float cy = Mathf.Cos(yaw);
        float sy = Mathf.Sin(yaw);

        float cp = Mathf.Cos(pitch);
        float sp = Mathf.Sin(pitch);

        float cr = Mathf.Cos(roll);
        float sr = Mathf.Sin(roll);

        Vector3 pyForward = new Vector3(
            cp * cy,
            cp * sy,
            sp
        );

        Vector3 pyRight = new Vector3(
            sr * sp * cy - cr * sy,
            sr * sp * sy + cr * cy,
            -sr * cp
        );

        Vector3 pyUp = new Vector3(
            -(cr * sp * cy + sr * sy),
            cy * sr - cr * sp * sy,
            cr * cp
        );

        Vector3 unityRight = PythonVectorToUnity(pyRight).normalized;
        Vector3 unityUp = PythonVectorToUnity(pyUp).normalized;

        // Model local +Z = left, yani left = -right
        Vector3 unityLeft = -unityRight;

        unityUp = Vector3.ProjectOnPlane(unityUp, unityLeft).normalized;

        if (unityUp.sqrMagnitude < 1e-6f)
            unityUp = Vector3.up;

        Quaternion q = Quaternion.LookRotation(unityLeft, unityUp);

        return q;
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
}