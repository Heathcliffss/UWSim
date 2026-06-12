using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;

/// <summary>
/// POZISYON LOGGER (Lider / Takipci yol kaydi)
/// ------------------------------------------------------------
/// Play'e basinca baslar, Play durana kadar transform.position (world)
/// + yaw degerini sabit Hz ile CSV'ye yazar. Test sonrasi gidilen
/// yollari analiz/gorsellestirme icin kullanilir.
///
/// AYNI script hem Lider hem Takipci objesine eklenir; her biri
/// kendi 'logFileName'ine yazar (Inspector'dan ayri ayri ayarla):
///     Lider   -> leader_path_log.csv
///     Takipci -> follower_path_log.csv
///
/// CSV (InvariantCulture, nokta ondalik, virgul SADECE ayrac):
///     time_s,x,y,z,yaw
///
/// NOT: GazeboDataReceiver/ascend/takip mantigina DOKUNMAZ; sadece
/// her karede objenin guncel konumunu OKUR. FPS'e etkisi ihmal edilebilir
/// (sabit Hz, StreamWriter acik tutulur, cikista kapatilir).
/// </summary>
public class PositionLogger : MonoBehaviour
{
    [Header("Dosya")]
    [Tooltip("CSV dosya adi. Lider ve takipci icin FARKLI ver (ornek: leader_path_log.csv / follower_path_log.csv).")]
    public string logFileName = "path_log.csv";

    [Tooltip("Bos birakilirsa Application.persistentDataPath kullanilir. Istersen tam klasor yolu yaz (ornek: C:/Dev/logs).")]
    public string customDirectory = "";

    [Header("Kayit Ayarlari")]
    [Tooltip("Saniyede kac kayit. 20 -> yol pururuzsuz, dosya makul. 0 veya negatif -> HER frame.")]
    public float logHz = 20f;

    [Tooltip("true: transform.position (world).  false: transform.localPosition.")]
    public bool useWorldPosition = true;

    [Tooltip("Zaman damgasi olarak Play basindan gecen saniye (Time.time). Kapatirsan sistem unix zamani yazilir.")]
    public bool usePlayRelativeTime = true;

    [Header("Debug")]
    public bool verboseStart = true;

    private StreamWriter _writer;
    private string _fullPath;
    private float _logPeriod;
    private float _nextLogTime = 0f;
    private bool _ready = false;
    private int _rowsWritten = 0;

    void Start()
    {
        try
        {
            string dir = string.IsNullOrEmpty(customDirectory)
                ? Application.persistentDataPath
                : customDirectory;

            Directory.CreateDirectory(dir);  // yoksa olustur
            _fullPath = Path.Combine(dir, logFileName);

            // Her Play'de TEMIZ dosya (uzerine yaz). Birden fazla test
            // birikmesini istersen 'false' yapip append'e cevirebilirsin.
            _writer = new StreamWriter(_fullPath, false, Encoding.UTF8);
            _writer.WriteLine("time_s,x,y,z,yaw");
            _writer.Flush();

            _logPeriod = (logHz > 0f) ? (1f / logHz) : 0f;
            _nextLogTime = 0f;
            _ready = true;

            if (verboseStart)
                Debug.Log("[PATH LOG] Basladi -> " + _fullPath +
                          "  (Hz=" + logHz + ", world=" + useWorldPosition + ")");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PATH LOG] Dosya acilamadi (" + logFileName + "): " + e.Message);
            _ready = false;
        }
    }

    void Update()
    {
        if (!_ready)
            return;

        // Sabit Hz kontrolu (logPeriod==0 ise her frame yazar)
        if (_logPeriod > 0f && Time.time < _nextLogTime)
            return;

        _nextLogTime = Time.time + _logPeriod;

        WriteRow();
    }

    private void WriteRow()
    {
        Vector3 pos = useWorldPosition ? transform.position : transform.localPosition;

        // Yaw: dunya rotasyonunun Y ekseni (Unity'de yatay yon)
        float yaw = useWorldPosition
            ? transform.eulerAngles.y
            : transform.localEulerAngles.y;

        float t = usePlayRelativeTime
            ? Time.time
            : (float)(System.DateTime.UtcNow -
                new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;

        var ci = CultureInfo.InvariantCulture;

        try
        {
            _writer.WriteLine(
                t.ToString("F4", ci) + "," +
                pos.x.ToString("F4", ci) + "," +
                pos.y.ToString("F4", ci) + "," +
                pos.z.ToString("F4", ci) + "," +
                yaw.ToString("F2", ci)
            );
            _rowsWritten++;

            // Cok seyrek flush degil; her ~1 saniyede bir diske bas (veri kaybina karsi).
            if (_rowsWritten % Mathf.Max(1, Mathf.RoundToInt(logHz)) == 0)
                _writer.Flush();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PATH LOG] Satir yazilamadi: " + e.Message);
        }
    }

    private void CloseWriter(string reason)
    {
        if (_writer != null)
        {
            try
            {
                _writer.Flush();
                _writer.Close();
            }
            catch { }
            _writer = null;

            Debug.Log("[PATH LOG] Kapandi (" + reason + ") -> " + _fullPath +
                      "  toplam satir=" + _rowsWritten);
        }
        _ready = false;
    }

    // Play durunca / obje yok edilince / uygulama kapaninca dosyayi duzgun kapat
    void OnDisable()
    {
        CloseWriter("OnDisable");
    }

    void OnApplicationQuit()
    {
        CloseWriter("OnApplicationQuit");
    }
}