using UnityEngine;
using TMPro;

public class RobotTelemetryManager : MonoBehaviour
{
    [Header("Robot Bileşenleri")]
    private RobotController robotCtrl;
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
        robotCtrl = GetComponent<RobotController>();
        hydro = GetComponent<ROSHydrodynamics>();
        rb = GetComponent<Rigidbody>();

        if (robotCtrl == null || hydro == null)
            Debug.LogError("RobotTelemetryManager: Gerekli robot scriptleri bulunamadı!");
    }

    void Update()
    {
        if (robotCtrl == null || hydro == null) return;

        UpdateUI();
    }

    void UpdateUI()
    {
        // 1. GERÇEK HIZ (Rigidbody'den canlı veri)
        float speed = rb.linearVelocity.magnitude;
        if (speedText) speedText.text = "Hız (m/s): " + speed.ToString("F2");

        // 2. GERÇEK DERİNLİK (Hydrodynamics scriptindeki 'depth' verisi)
        if (depthText) depthText.text = "Depth (m): " + hydro.depth.ToString("F2");

        // 3. PITCH VE ROLL (MerkezPivot'tan açısal veri)
        // Açıları -180 ile 180 arasına normalize ederek gerçek bir cihaz ekranı gibi yapıyoruz
        float p = NormalizeAngle(robotCtrl.merkezPivot.localEulerAngles.x);
        float r = NormalizeAngle(robotCtrl.merkezPivot.localEulerAngles.z);

        if (pitchText) pitchText.text = "Pitch: " + p.ToString("F1") + "°";
        if (rollText) rollText.text = "Roll: " + r.ToString("F1") + "°";

        // 4. YAPAY UFUK DÖNÜŞÜ (Roll açısına göre sabit eksende dönüş)
        if (horizonLine)
        {
            horizonLine.localRotation = Quaternion.Euler(0, 0, r);
        }
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360;
        if (angle > 180) angle -= 360;
        return angle;
    }
}