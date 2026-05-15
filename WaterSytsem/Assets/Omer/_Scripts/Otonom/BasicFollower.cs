using UnityEngine;

public class BasicFollower : MonoBehaviour
{
    [Header("Takip Edilecek Hedef")]
    public Transform liderRobot;

    [Header("Takip Ayarlarý")]
    public float takipMesafesi = 3f;
    public float hareketHizi = 5f;
    public float donusHizi = 3f;

    void Update()
    {
        if (liderRobot == null) return;

        // 1. Hedefe Dönüþ (Mavi Oku Lidere Çevir)
        Vector3 hedefYon = liderRobot.position - transform.position;
        if (hedefYon.sqrMagnitude > 0.01f) // Hedefle tam üst üste deðilsek
        {
            Quaternion hedefRotasyon = Quaternion.LookRotation(hedefYon);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedefRotasyon, donusHizi * Time.deltaTime);
        }

        // 2. Ýlerleme (Vector3.MoveTowards fiziksel olarak daha güvenlidir)
        float aradakiMesafe = Vector3.Distance(transform.position, liderRobot.position);

        if (aradakiMesafe > takipMesafesi)
        {
            // Lidere doðru güvenli bir þekilde süzül
            transform.position = Vector3.MoveTowards(transform.position, liderRobot.position, hareketHizi * Time.deltaTime);
        }
    }
}