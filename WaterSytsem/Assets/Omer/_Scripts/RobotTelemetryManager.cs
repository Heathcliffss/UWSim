using UnityEngine;
using TMPro;

public class RobotTelemetryManager : MonoBehaviour
{
    [Header("Robot Bileşenleri")]
    private ROSHydrodynamics hydro;
    private Rigidbody rb;

    [Header("UI Metinleri (TMP)")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI pitchText;
    public TextMeshProUGUI rollText;

    [Header("Görsel Gösterge")]
    public RectTransform horizonLine;

    void Start()
    {
        // Scriptleri aynı obje üzerinde oldukları için otomatik yakalıyoruz
        hydro = GetComponent<ROSHydrodynamics>();
        rb = GetComponent<Rigidbody>();

        if (hydro == null || rb == null)
            Debug.LogError("RobotTelemetryManager: Gerekli Rigidbody veya ROSHydrodynamics bulunamadı!");
    }

    void Update()
    {
        if (hydro == null || rb == null) return;

        UpdateUI();
    }

    void UpdateUI()
    {
        // 1. GERÇEK HIZ (Rigidbody'den canlı veri)
        float speed = rb.linearVelocity.magnitude;
        if (speedText) speedText.text = "Hız (m/s): " + speed.ToString("F2");

        // 2. GERÇEK DERİNLİK (Hydrodynamics scriptindeki 'depth' verisi)
        if (depthText) depthText.text = "Depth (m): " + hydro.depth.ToString("F2");

        // 3. PITCH VE ROLL (Ana objenin Transform'undan açısal veri)
        // Hata Düzeltildi: localEulerAngles yerine doğrudan ana objenin rotasyonu alınıyor.
        float p = NormalizeAngle(transform.eulerAngles.x);
        float r = NormalizeAngle(transform.eulerAngles.z);

        if (pitchText) pitchText.text = "Pitch: " + p.ToString("F1") + "°";
        if (rollText) rollText.text = "Roll: " + r.ToString("F1") + "°";

        // 4. YAPAY UFUK DÖNÜŞÜ (Roll açısına göre sabit eksende dönüş)
        if (horizonLine)
        {
            horizonLine.localRotation = Quaternion.Euler(0, 0, r);
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