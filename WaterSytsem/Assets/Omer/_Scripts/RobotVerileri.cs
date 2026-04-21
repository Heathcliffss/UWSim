using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RobotVerileri : MonoBehaviour
{
    [Header("Bileşenler")]
    private Rigidbody rb;
    private GameObject[] cableSegments;

    [Header("UI Metinleri (TMP)")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI pitchText;
    public TextMeshProUGUI rollText;
    public TextMeshProUGUI liveLocationText;

    [Header("Manyetik Sensör UI")]
    public Slider signalSlider;
    public Image signalFillImage; // Slider'ın 'Fill' objesi

    [Header("Performans Ayarları")]
    [Range(0.01f, 0.5f)]
    public float updateInterval = 0.1f;
    private float nextUpdateTime;

    [Header("Sektörel Veri Ayarları")]
    private const double baseLat = 54.123400;
    private const double baseLong = 2.567800;
    private const double meterToDegree = 0.000009;
    public string cableTag = "Cable";

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Sahnedeki kabloları bir kez bulup listeye alıyoruz (Performans için çok kritik!)
        cableSegments = GameObject.FindGameObjectsWithTag(cableTag);
    }

    void Update()
    {
        // VR'da kasmayı önleyen ana kilit: Her karede değil, aralıklarla çalış
        if (Time.time >= nextUpdateTime)
        {
            UpdateAllData();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    void UpdateAllData()
    {
        if (rb == null) return;

        // --- 1. TELEMETRİ HESAPLAMALARI ---
        float speed = rb.linearVelocity.magnitude;
        float depth = Mathf.Abs(transform.position.y);
        float p = NormalizeAngle(transform.eulerAngles.x);
        float r = NormalizeAngle(transform.eulerAngles.z);

        // --- 2. MANYETİK SİNYAL HESAPLAMASI (En Yakın Nokta) ---
        float closestDist = float.MaxValue;
        if (cableSegments != null && cableSegments.Length > 0)
        {
            foreach (GameObject segment in cableSegments)
            {
                // sqrMagnitude kullanmak Vector3.Distance'tan daha hızlıdır (kök almaz)
                float sqrDist = (transform.position - segment.transform.position).sqrMagnitude;
                if (sqrDist < closestDist) closestDist = sqrDist;
            }
            closestDist = Mathf.Sqrt(closestDist); // Sadece en yakın olanın kökünü alıyoruz
        }

        float signalStrength = Mathf.Clamp(100f - (closestDist * 10f), 0f, 100f);

        // --- 3. KOORDİNAT HESAPLAMASI ---
        double currentLat = baseLat + (transform.position.z * meterToDegree);
        double currentLong = baseLong + (transform.position.x * meterToDegree);

        // --- 4. UI GÜNCELLEME (SetText ile sıfır bellek yükü) ---
        if (speedText) speedText.SetText("Hız: {0:F2} m/s", speed);
        if (depthText) depthText.SetText("Depth: {0:F2} m", depth);
        if (pitchText) pitchText.SetText("Pitch: {0:F1}°", p);
        if (rollText) rollText.SetText("Roll: {0:F1}°", r);
        if (liveLocationText) liveLocationText.SetText("LAT: {0:F6} N\nLONG: {1:F6} E", (float)currentLat, (float)currentLong);

        // Manyetik Slider ve Renk Güncelleme
        if (signalSlider) signalSlider.value = signalStrength;
        if (signalFillImage) signalFillImage.color = Color.Lerp(Color.red, Color.green, signalStrength / 100f);
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        else if (angle < -180f) angle += 360f;
        return angle;
    }
}
