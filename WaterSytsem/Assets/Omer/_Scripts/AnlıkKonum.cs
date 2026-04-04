using UnityEngine;
using TMPro;

public class AnlıkKonum : MonoBehaviour
{
    [Header("UI Ayarları")]
    public TextMeshProUGUI coordText; // UI'daki metin kutusunu buraya sürükle

    [Header("Kuzey Denizi Başlangıç Noktası")]
    // Sahnenin (0,0,0) noktasının gerçek dünyadaki karşılığı
    private double baseLat = 54.123400;
    private double baseLong = 2.567800;

    // Metreyi dereceye çeviren katsayı (Dünya üzerinde yaklaşık 1 metre)
    private double meterToDegree = 0.000009;

    void Update()
    {
        // Robotun Unity içindeki pozisyonunu alıyoruz
        float posX = transform.position.x;
        float posZ = transform.position.z;

        // Canlı koordinat hesaplama
        // Z ekseni Kuzey(+)/Güney(-), X ekseni Doğu(+)/Batı(-) yönüdür.
        double currentLat = baseLat + (posZ * meterToDegree);
        double currentLong = baseLong + (posX * meterToDegree);

        // UI'ya profesyonel formatta yazdırma (6 haneli küsurat gerçekçilik sağlar)
        coordText.text = $"LAT: {currentLat:F6} N\nLONG: {currentLong:F6} E";
    }
}
