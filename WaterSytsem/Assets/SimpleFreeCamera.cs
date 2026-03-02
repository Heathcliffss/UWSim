using UnityEngine;

public class SimpleFreeCamera : MonoBehaviour
{
    [Header("Ayarlar")]
    public float hareketHizi = 10f;
    public float mouseHassasiyeti = 2f;

    private float dikeySikisma = 0f;
    private float yatayDonus = 0f;

    void Update()
    {
        // 1. Mouse ile Etrafa Bakma (Rotation)
        yatayDonus += Input.GetAxis("Mouse X") * mouseHassasiyeti;
        dikeySikisma -= Input.GetAxis("Mouse Y") * mouseHassasiyeti;

        // Kameranýn tam yukarý veya tam aþaðý bakarken takla atmasýný engelliyoruz
        dikeySikisma = Mathf.Clamp(dikeySikisma, -90f, 90f);

        // Kameranýn açýsýný güncelliyoruz
        transform.localRotation = Quaternion.Euler(dikeySikisma, yatayDonus, 0f);

        // 2. WASD ile Hareket Etme (Translation)
        float ileriGeri = Input.GetAxis("Vertical") * hareketHizi * Time.deltaTime;
        float sagaSola = Input.GetAxis("Horizontal") * hareketHizi * Time.deltaTime;

        // Kameranýn baktýðý yöne doðru hareket etmesini saðlýyoruz
        transform.Translate(sagaSola, 0, ileriGeri);
    }
}