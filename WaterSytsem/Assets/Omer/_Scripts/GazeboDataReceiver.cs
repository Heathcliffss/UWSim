using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Globalization;
public class GazeboDataReceiver : MonoBehaviour
{
    [Header("Ağ Ayarları")]
    public int listenPort = 5008;

    [Header("Ölçek")]
    public float positionScale = 1.0f;

    [Header("Ascend (Yuzeye Cikis) Modu")]
    public bool ascendActive = false;          // TEST: elle isaretleyince yukari cikar
    public float surfaceY = -0.298f;           // su yuzeyi Unity Y (elle tune et)
    public float ascendSpeed = 150f;           // birim/saniye yukari hiz
    public int ascendUdpPort = 5014;           // OpenCV'den ASCEND sinyali
    public bool listenForAscend = true;        // sadece bu follower'da acik
    public string ascendMessage = "ASCEND";

    [Header("GPS Surface Log (yuzeye varinca X-Z yaz)")]
    public bool logSurfaceGps = true;          // sadece bu follower'da acik
    public float surfaceReachTolerance = 0.1f; // surfaceY'ye bu kadar yaklasinca 'vardi'
    public string gpsLogFileName = "gps_surface_log.csv";

    [Header("Camera-local body-frame position mapping")]
    public bool useInitialYawBodyFramePositionMapping = true;
    public bool invertCameraForwardDelta = false;
    public bool invertCameraRightDelta = false;
    public bool invertCameraUpDelta = false;

    [Header("Body-frame mapping debug")]
    public bool debugBodyFrameMapping = true;
    public int debugBodyFrameEveryNPackets = 30;

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

    [Header("Debug")]
    public bool debugPackets = true;
    public int debugEveryNPackets = 60;
    private int _packetCounter = 0;

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
    
    // --- Ascend (yuzeye cikis) durum degiskenleri ---
    private bool _ascending = false;
    private Vector3 _ascendLockedPos;
    private UdpClient _ascendUdp;
    private Thread _ascendThread;
    private volatile bool _ascendRunning = false;
    private volatile bool _ascendRequested = false;

    private bool _gpsLogged = false;   // yuzey konumu bir kez yazilsin

    [Header("Gazebo relative reference mode")]
    public bool useFirstPacketAsOrigin = true;

    private bool _hasFirstPacketReference = false;
    private Vector3 _firstPacketPosition;
    private Quaternion _firstPacketRotationUnity;
    private float _firstPacketYawDeg = 0f;

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
        
        if (listenForAscend)
            StartAscendListener();
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
        
        _packetCounter++;

        if (debugPackets && (_packetCounter <= 5 || _packetCounter % debugEveryNPackets == 0))
        {
            Debug.Log(
                "[UDP] packet port=" + listenPort +
                " seq=" + sample.seq +
                " pos=(" + sample.px.ToString("F3") + ", " +
                         sample.py.ToString("F3") + ", " +
                         sample.pz.ToString("F3") + ")" +
                " rpy=(" + sample.roll.ToString("F1") + ", " +
                         sample.pitch.ToString("F1") + ", " +
                         sample.yaw.ToString("F1") + ")" +
                " time=" + sample.time.ToString("F3")
            );
        }

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
        // UDP thread'inden ya da Inspector'dan ascend istegi geldi mi?
        if ((_ascendRequested || ascendActive) && !_ascending)
        {
            _ascending = true;
            // O anki konumu kilitle -> sicrama olmaz, Gazebo'nun son yerinden devam
            _ascendLockedPos = useLocalTransform ? transform.localPosition : transform.position;
            Debug.Log("[ASCEND] Yuzeye cikis basladi. Kilitli konum: " + _ascendLockedPos);
        }

        if (_ascending)
        {
            Vector3 p = useLocalTransform ? transform.localPosition : transform.position;
            float newY = Mathf.MoveTowards(p.y, surfaceY, ascendSpeed * Time.deltaTime);
            Vector3 up = new Vector3(_ascendLockedPos.x, newY, _ascendLockedPos.z);

            if (useLocalTransform) transform.localPosition = up;
            else transform.position = up;

            // --- GPS: yuzeye varinca (surfaceY'ye yaklasinca) X-Z'yi BIR KEZ CSV'ye yaz ---
            if (logSurfaceGps && !_gpsLogged &&
                Mathf.Abs(newY - surfaceY) <= surfaceReachTolerance)
            {
                WriteSurfaceGpsLog(up);
                _gpsLogged = true;
            }

            return;   // Gazebo isleme mantigina HIC girme
        }


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
        Vector3 pyRaw = new Vector3(
            s.px,
            s.py,
            s.pz
        );

        Quaternion unityRotCurrent = PythonRPYToUnityRotation(
            s.roll,
            s.pitch,
            s.yaw
        );

        if (useFirstPacketAsOrigin && !_hasFirstPacketReference)
        {
            _firstPacketPosition = pyRaw;
            _firstPacketRotationUnity = unityRotCurrent;
            _firstPacketYawDeg = s.yaw;
            _hasFirstPacketReference = true;

            Debug.Log(
                "[UDP] First packet reference set. pos=(" +
                _firstPacketPosition.x.ToString("F3") + ", " +
                _firstPacketPosition.y.ToString("F3") + ", " +
                _firstPacketPosition.z.ToString("F3") + ")" +
                " yaw=" + _firstPacketYawDeg.ToString("F2")
            );
        }

        Vector3 pyRel;

        if (useFirstPacketAsOrigin)
        {
            pyRel = pyRaw - _firstPacketPosition;
        }
        else if (keyboardRelativeMode)
        {
            // Klavye kayıt modu:
            // Python doğrudan relatif x,y,z gönderir.
            pyRel = pyRaw;
        }
        else
        {
            // Eski Gazebo absolute pose modu:
            // Sabit pythonOrigin çıkarılır.
            pyRel = new Vector3(
                s.px - pythonOrigin.x,
                s.py - pythonOrigin.y,
                s.pz - pythonOrigin.z
            );
        }

        Vector3 unityRel = PositionDeltaToUnity(pyRel);

        UnityPose pose;
        pose.position = _startPosition + unityRel * positionScale;

        Quaternion deltaRot;

        if (useFirstPacketAsOrigin)
        {
            deltaRot = Quaternion.Inverse(_firstPacketRotationUnity) * unityRotCurrent;
        }
        else
        {
            deltaRot = unityRotCurrent;
        }

        pose.rotation = _startRotation * deltaRot;

        return pose;
    }

    private Vector3 PythonDirectionToUnity(Vector3 pyVec)
    {
        // Raw Python/Gazebo direction vector to Unity direction vector.
        // Bu fonksiyon sadece rotasyon basis vektörleri için kullanılacak.
        // First-packet origin, camera-local delta, invert flag vb. burada uygulanmaz.

        return new Vector3(
            pyVec.x,
            pyVec.z,
            -pyVec.y
        );
    }

    private void WriteSurfaceGpsLog(Vector3 surfacePos)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, gpsLogFileName);
            bool newFile = !File.Exists(path);

            using (StreamWriter sw = new StreamWriter(path, true))  // append
            {
                if (newFile)
                    sw.WriteLine("unix_time,follower_surface_x,follower_surface_z,surfaceY");

                double unixTime = (System.DateTime.UtcNow -
                    new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
                
                var ci = CultureInfo.InvariantCulture;
                sw.WriteLine(
                    unixTime.ToString("F2", ci) + "," +
                    surfacePos.x.ToString("F4", ci) + "," +
                    surfacePos.z.ToString("F4", ci) + "," +
                    surfaceY.ToString("F4", ci)
                );
            }

            Debug.Log("[GPS] Yuzey konumu yazildi -> " + path +
                      "  X=" + surfacePos.x.ToString("F3") +
                      " Z=" + surfacePos.z.ToString("F3"));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GPS] Log yazilamadi: " + e.Message);
        }
    }


    void StartAscendListener()
    {
        try
        {
            _ascendUdp = new UdpClient(ascendUdpPort);
            _ascendRunning = true;
            _ascendThread = new Thread(AscendLoop) { IsBackground = true };
            _ascendThread.Start();
            Debug.Log("[ASCEND] UDP dinleniyor: port " + ascendUdpPort);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ASCEND] UDP baslatilamadi (port " + ascendUdpPort + "): " + e.Message);
        }
    }

    void AscendLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        while (_ascendRunning)
        {
            try
            {
                byte[] data = _ascendUdp.Receive(ref ep);
                string msg = System.Text.Encoding.UTF8.GetString(data).Trim();
                if (msg == ascendMessage)
                    _ascendRequested = true;   // ana thread Update'te isler
            }
            catch (SocketException) { break; }
            catch (Exception) { }
        }
    }

    void OnDestroy()
    {
        _running = false;

        _ascendRunning = false;
        try { _ascendUdp?.Close(); } catch { }

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
    private Vector3 PositionDeltaToUnity(Vector3 pyVec)
    {
        if (useInitialYawBodyFramePositionMapping)
        {
            // pyVec burada world/global pozisyon farkı:
            // current_position - first_position
            //
            // İlk yaw açısına göre bu world delta'yı robotun
            // başlangıç body frame'ine çeviriyoruz.
            //
            // Python/Gazebo varsayımı:
            //   +X = forward world reference at yaw=0
            //   +Y = right/lateral horizontal axis
            //   +Z = up
            //
            // Body-frame:
            //   bodyForward = robotun ilk baktığı yöne doğru hareket
            //   bodyRight   = robotun ilk sağ yönüne doğru hareket
            //   bodyUp      = yukarı/aşağı hareket

            float yaw0 = _firstPacketYawDeg * Mathf.Deg2Rad;

            float cosYaw = Mathf.Cos(yaw0);
            float sinYaw = Mathf.Sin(yaw0);

            float dx = pyVec.x;
            float dy = pyVec.y;
            float dz = pyVec.z;

            float bodyForward = cosYaw * dx + sinYaw * dy;
            float bodyRight = -sinYaw * dx + cosYaw * dy;
            float bodyUp = dz;

            if (invertCameraForwardDelta)
                bodyForward = -bodyForward;

            if (invertCameraRightDelta)
                bodyRight = -bodyRight;

            if (invertCameraUpDelta)
                bodyUp = -bodyUp;

            // Unity Camera local:
            //   local X = right
            //   local Y = up
            //   local Z = forward
            Vector3 cameraLocalDelta = new Vector3(
                bodyRight,
                bodyUp,
                bodyForward
            );

            if (debugBodyFrameMapping && (_packetCounter <= 10 || _packetCounter % debugBodyFrameEveryNPackets == 0))
            {
                Debug.Log(
                    "[UDP POS MAP] pyRel=(" +
                    pyVec.x.ToString("F3") + ", " +
                    pyVec.y.ToString("F3") + ", " +
                    pyVec.z.ToString("F3") + ") " +
                    "body=(" +
                    bodyForward.ToString("F3") + " forward, " +
                    bodyRight.ToString("F3") + " right, " +
                    bodyUp.ToString("F3") + " up) " +
                    "camLocal=(" +
                    cameraLocalDelta.x.ToString("F3") + ", " +
                    cameraLocalDelta.y.ToString("F3") + ", " +
                    cameraLocalDelta.z.ToString("F3") + ")"
                );
            }

            // Elle hizalanmış kameranın başlangıç rotasyonuna göre
            // local delta'yı Unity parent/world frame'e taşı.
            return _startRotation * cameraLocalDelta;
        }

        // Eski model-style mapping fallback:
        // Python X -> Unity X
        // Python Y -> Unity -Z
        // Python Z -> Unity Y
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

        Vector3 unityRight = PythonDirectionToUnity(pyRight).normalized;
        Vector3 unityUp = PythonDirectionToUnity(pyUp).normalized;

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