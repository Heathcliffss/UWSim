using UnityEngine;
using TMPro; // TextMeshPro kullanmak için gerekli

public class CubeController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;

    [Header("UI Referansları")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI depthText;

    void Update()
    {
        // 1. Hareket Girdilerini Al (W,A,S,D veya Ok Tuşları)
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        // 2. Hareketi Uygula (X ve Y ekseninde)
        Vector3 movement = new Vector3(moveX, moveY, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);

        // 3. UI Verilerini Güncelle
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        // Anlık hızı hesapla (Tuşlara basılıyorsa moveSpeed, değilse 0)
        float currentSpeed = (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0) ? moveSpeed : 0;

        speedText.text = "Hız: " + currentSpeed.ToString("F2");

        // Derinlik genellikle Z eksenidir
        depthText.text = "Derinlik (Z): " + transform.position.z.ToString("F2");
    }
}