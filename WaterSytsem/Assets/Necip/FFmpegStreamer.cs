using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// LiderKamera görüntüsünü AsyncGPUReadback ile okuyup
/// FFmpeg üzerinden UDP/MPEG-TS stream eder.
///
/// Donma sorunu için:
///   - autoRTWidth/Height'ı 1280x720'ye düşür (mobil için yeterli)
///   - maxInFlight = 2 → pipeline'da her zaman 2 readback uçuyor, boşluk kalmıyor
/// </summary>
public class FFmpegStreamer : MonoBehaviour
{
    [Header("Kaynak — ikisinden birini doldur")]
    [Tooltip("LiderKamera objesini buraya sürükle. RT yoksa otomatik oluşturulur ve kameraya atanır.")]
    public Camera kaynakKamera;

    [Tooltip("Kamera yerine doğrudan RenderTexture asset'i kullanmak istersen buraya sürükle.")]
    public RenderTexture manuelRT;

    [Header("Otomatik RT Boyutu (kamera RT'si yoksa kullanılır)")]
    [Tooltip("Mobil stream için 1280x720 yeterli. 1080p ise GPU readback çok yavaşlar.")]
    public int autoRTWidth  = 1280;
    public int autoRTHeight = 720;

    [Header("FFmpeg")]
    public string ffmpegYolu  = @"C:\ffmpeg\bin\ffmpeg.exe";
    public string hedefIP     = "192.168.137.229";
    public int    hedefPort   = 1234;
    public int    fps         = 25;
    public int    bitrateMbps = 2;

    [Header("Gelişmiş")]
    [Tooltip("Aynı anda kaç GPU readback uçabilir. 2 ideal; 1 ise donmaya neden olur.")]
    [Range(1, 3)]
    public int maxInFlight = 2;

    // ── İç değişkenler ──────────────────────────────────────────────────────
    private RenderTexture _rt;
    private Process       _proc;
    private int           _inFlightCount = 0;
    private bool          _running       = false;
    private float         _nextFrameTime = 0f;

    // GC-free pipe yazımı için çift buffer
    // Frame callback ana thread'de gelir; yazma arka thread'de olur.
    // _writeReady=true olduğunda arka thread _writeBuf'ı pipe'a yazar.
    private byte[]        _buf0;          // readback'ten doldurulur
    private byte[]        _writeBuf;      // arka thread'in yazdığı
    private bool          _writeReady   = false;
    private readonly object _writeLock  = new object();
    private System.Threading.Thread _writeThread;

    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        // 1. Kameranın mevcut targetTexture'ı var mı?
        if (kaynakKamera != null && kaynakKamera.targetTexture != null)
        {
            _rt = kaynakKamera.targetTexture;
            UnityEngine.Debug.Log("[FFmpegStreamer] Kameranın mevcut RT'si kullanılıyor.");
        }
        // 2. Manuel RT atanmış mı?
        else if (manuelRT != null)
        {
            _rt = manuelRT;
            UnityEngine.Debug.Log("[FFmpegStreamer] Manuel RT kullanılıyor.");
        }
        // 3. Kamera var ama RT yok → bizim oluştur, kameraya ata
        else if (kaynakKamera != null)
        {
            _rt = new RenderTexture(autoRTWidth, autoRTHeight, 24, RenderTextureFormat.ARGB32);
            _rt.name = "FFmpegStreamer_AutoRT";
            _rt.Create();
            kaynakKamera.targetTexture = _rt;
            UnityEngine.Debug.Log($"[FFmpegStreamer] RT oluşturuldu ve kameraya atandı ({autoRTWidth}x{autoRTHeight}).");
        }

        if (_rt != null)
        {
            // Buffer'ları bir kez oluştur — artık her frame allocation yok
            int bufSize = _rt.width * _rt.height * 4;
            _buf0     = new byte[bufSize];
            _writeBuf = new byte[bufSize];
            StartFFmpeg();
            StartWriteThread();
        }
        else
            UnityEngine.Debug.LogError("[FFmpegStreamer] RT alınamadı! Inspector'da Kaynak Kamera veya Manuel RT ata.");
    }

    // ────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_rt == null || !_running) return;

        if (_proc == null || _proc.HasExited)
        {
            UnityEngine.Debug.LogWarning("[FFmpegStreamer] FFmpeg process sonlandı.");
            _running = false;
            return;
        }

        // Hedef fps'e göre throttle — fazla readback isteme
        if (Time.unscaledTime < _nextFrameTime) return;

        // Pipeline'da maxInFlight'tan fazla readback olmasın
        if (_inFlightCount >= maxInFlight) return;

        _nextFrameTime = Time.unscaledTime + (1f / fps);
        _inFlightCount++;
        AsyncGPUReadback.Request(_rt, 0, TextureFormat.RGBA32, OnReadbackComplete);
    }

    // ────────────────────────────────────────────────────────────────────────
    void StartFFmpeg()
    {
        string args =
            // ── Giriş ────────────────────────────────────────────────────────
            $"-y " +
            $"-f rawvideo -pix_fmt rgba " +
            $"-s {_rt.width}x{_rt.height} " +
            $"-r {fps} -i - " +
            // ── Filtre ───────────────────────────────────────────────────────
            $"-vf vflip " +
            // ── Encoder ──────────────────────────────────────────────────────
            $"-c:v h264_nvenc -preset p1 -tune ll " +
            $"-zerolatency 1 -rc cbr " +
            $"-b:v {bitrateMbps}M -maxrate {bitrateMbps}M -bufsize 500k " +
            $"-g {fps} -bf 0 -rc-lookahead 0 -delay 0 " +
            // ── Çıkış ────────────────────────────────────────────────────────
            $"-an " +
            $"-muxdelay 0.001 -muxpreload 0 " +
            $"-flush_packets 1 " +
            $"-f mpegts udp://{hedefIP}:{hedefPort}?pkt_size=1316";

        var info = new ProcessStartInfo
        {
            FileName              = ffmpegYolu,
            Arguments             = args,
            UseShellExecute       = false,
            RedirectStandardInput = true,
            CreateNoWindow        = true
        };

        _proc = new Process { StartInfo = info };
        try
        {
            _proc.Start();
            _running = true;
            UnityEngine.Debug.Log(
                $"[FFmpegStreamer] Başladı → udp://{hedefIP}:{hedefPort}  " +
                $"({_rt.width}x{_rt.height} @ {fps}fps  maxInFlight={maxInFlight})");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[FFmpegStreamer] FFmpeg başlatılamadı: {e.Message}");
            enabled = false;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    void StartWriteThread()
    {
        _writeThread = new System.Threading.Thread(() =>
        {
            while (_running)
            {
                bool hasData = false;
                lock (_writeLock)
                {
                    if (_writeReady)
                    {
                        // Swap: writeBuf ↔ buf0 (sıfır kopya swap)
                        var tmp   = _writeBuf;
                        _writeBuf = _buf0;
                        _buf0     = tmp;
                        _writeReady = false;
                        hasData = true;
                    }
                }

                if (hasData)
                {
                    try
                    {
                        var stream = _proc.StandardInput.BaseStream;
                        stream.Write(_writeBuf, 0, _writeBuf.Length);
                       // stream.Flush();
                    }
                    catch
                    {
                        _running = false;
                        return;
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(1); // CPU yakmadan bekle
                }
            }
        })
        { IsBackground = true, Name = "FFmpegPipeWriter" };
        _writeThread.Start();
    }

    // ────────────────────────────────────────────────────────────────────────
    void OnReadbackComplete(AsyncGPUReadbackRequest req)
    {
        _inFlightCount--;

        if (!_running) return;

        if (req.hasError)
        {
            UnityEngine.Debug.LogWarning("[FFmpegStreamer] GPU readback hatası.");
            return;
        }

        // NativeArray → _buf0 (önceden ayrılmış buffer, GC yok)
        req.GetData<byte>().CopyTo(_buf0);

        lock (_writeLock)
            _writeReady = true;   // arka thread alır ve yazar
    }

    // ────────────────────────────────────────────────────────────────────────
    void OnDestroy()
    {
        _running = false;
        _writeThread?.Join(500); // arka thread'in temiz kapanmasını bekle

        if (_proc == null) return;

        try
        {
            if (!_proc.HasExited)
            {
                _proc.StandardInput.Close();
                _proc.WaitForExit(2000);
                if (!_proc.HasExited) _proc.Kill();
            }
        }
        catch { }
        finally
        {
            _proc.Dispose();
            _proc = null;
        }
    }
}
