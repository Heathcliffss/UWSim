using UnityEngine;
using UnityEngine.UI;

public class ManyetikAlan : MonoBehaviour
{
    [Header("UI Elemanlarý")]
    public Slider signalSlider; // Unity'deki Slider'ý buraya sürükle
    public Image fillImage;     // Slider'ýn 'Fill' kýsmýný buraya sürükle (Renk deðiþimi için)

    [Header("Ayarlar")]
    public string targetTag = "Cable"; // Kablonun Tag'i
    public float detectionRadius = 10f; // 10 metreden itibaren algýlamaya baþlar

    private GameObject cableObject;

    void Start()
    {
        // Sahnedeki kabloyu bul (Eðer birden fazlaysa en yakýný bulacak bir mantýk eklenebilir)
        cableObject = GameObject.FindGameObjectWithTag(targetTag);
    }

    void Update()
    {
        if (cableObject == null) return;

        // Robot ile kablo arasýndaki mesafeyi ölçüyoruz
        float distance = Vector3.Distance(transform.position, cableObject.transform.position);

        // Senin formülün: Signal = clamp(100 - (distance * 10), 0, 100)
        float signalStrength = Mathf.Clamp(100f - (distance * 10f), 0f, 100f);

        // Slider deðerini güncelle (0-100 arasý)
        signalSlider.value = signalStrength;

        // Görsel Geri Bildirim: Sinyal arttýkça rengi Kýrmýzýdan Yeþile çevir
        if (fillImage != null)
        {
            // Lerp (Renk geçiþi): 0 sinyalde Kýrmýzý, 100 sinyalde Yeþil
            fillImage.color = Color.Lerp(Color.red, Color.green, signalStrength / 100f);
        }
    }
}
