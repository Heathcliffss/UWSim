using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CubeController : MonoBehaviour
{
    [Header("Simülasyon Ayarları")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 40f;

    [Header("Kritik UI Referansları")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI altitudeText;
    public RectTransform artificialHorizonLine;

    [Header("Görsel UI Referansları (İsteğe Bağlı)")]
    public TextMeshProUGUI pitchTextDisplay;
    public TextMeshProUGUI rollTextDisplay;

    private float currentPitch = 0f;
    private float currentRoll = 0f;

    void Update()
    {
        // 1. HAREKET (W,S,A,D)
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveX, moveY, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);

        // 2. DÖNÜŞ (Roll ve Pitch)
        // A ve D tuşları ile Roll değerini güncelliyoruz
        currentRoll += moveX * rotateSpeed * Time.deltaTime;

        // Ok tuşları ile Pitch değerini güncelliyoruz
        if (Input.GetKey(KeyCode.UpArrow))
        {
            currentPitch += rotateSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            currentPitch -= rotateSpeed * Time.deltaTime;
        }

        currentPitch = Mathf.Clamp(currentPitch, -80f, 80f);

        // Küpün kendi rotasyonunu uygula
        transform.rotation = Quaternion.Euler(currentPitch, 0, -currentRoll);

        // 3. UI GÜNCELLEME
        UpdateTelemetriUI(moveX, moveY);
    }

    void UpdateTelemetriUI(float x, float y)
    {
        // Metin verilerini güncelle
        float currentSpeedVal = (x != 0 || y != 0) ? moveSpeed : 0;
        speedText.text = "Hız (m/s): " + currentSpeedVal.ToString("F2");
        altitudeText.text = "Altitude-rel (m): " + transform.position.y.ToString("F2");

        if (pitchTextDisplay) pitchTextDisplay.text = "Pitch: " + currentPitch.ToString("F1") + "°";
        if (rollTextDisplay) rollTextDisplay.text = "Roll: " + currentRoll.ToString("F1") + "°";

        // İSTEDİĞİN DEĞİŞİKLİK: 
        // localPosition (yukarı-aşağı kayma) kodunu tamamen sildim.
        // Çizgi artık sadece merkezinde (Z ekseninde) dönerek Roll açısını gösterecek.
        artificialHorizonLine.localRotation = Quaternion.Euler(0, 0, currentRoll);
    }
}