using UnityEngine;
using TMPro;

public class RobotVerileri : MonoBehaviour
{
    [Header("Bileşenler")]
    private Rigidbody rb;

    [Header("UI Metinleri (TMP)")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI pitchText;
    public TextMeshProUGUI rollText;
    public TextMeshProUGUI liveLocationText; // Anlık Konum Metni

    [Header("Performans Ayarları")]
    [Tooltip("UI kaç saniyede bir güncellensin? 0.1f = Saniyede 10 kez. VR için idealdir.")]
    public float updateInterval = 0.1f;
    private float nextUpdateTime;

    [Header("Kuzey Denizi Koordinat Ayarları")]
    private const double baseLat = 54.123400;
    private const double baseLong = 2.567800;
    private const double meterToDegree = 0.000009;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // PERFORMANS KİLİDİ: Saniyede sadece 10 kez çalışır, işlemciyi yormaz.
        if (Time.time >= nextUpdateTime)
        {
            UpdateTelemetry();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    void UpdateTelemetry()
    {
        if (rb == null) return;

        // 1. HIZ HESAPLAMA
        float speed = rb.linearVelocity.magnitude;
        if (speedText != null)
            speedText.SetText("Hız (m/s): {0:F2}", speed);

        // 2. DERİNLİK HESAPLAMA (Deniz seviyesi 0 kabul edilir)
        float depth = Mathf.Abs(transform.position.y);
        if (depthText != null)
            depthText.SetText("Depth (m): {0:F2}", depth);

        // 3. PITCH & ROLL
        float p = NormalizeAngle(transform.eulerAngles.x);
        float r = NormalizeAngle(transform.eulerAngles.z);

        if (pitchText != null) pitchText.SetText("Pitch: {0:F1}°", p);
        if (rollText != null) rollText.SetText("Roll: {0:F1}°", r);

        // 4. ANLIK KONUM (Hata Düzeltildi: (float) casting eklendi)
        double currentLat = baseLat + (transform.position.z * meterToDegree);
        double currentLong = baseLong + (transform.position.x * meterToDegree);

        if (liveLocationText != null)
        {
            // SetText double kabul etmediği için (float) ile dönüştürüyoruz
            liveLocationText.SetText("LAT: {0:F6} N\nLONG: {1:F6} E", (float)currentLat, (float)currentLong);
        }
    }

    // Açıları -180 ile 180 arasına güvenli bir şekilde normalize eder
    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        else if (angle < -180f) angle += 360f;
        return angle;
    }
}
