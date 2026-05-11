using UnityEngine;

public class BasicFollower : MonoBehaviour
{
    [Header("Takip Edilecek Hedef")]
    public Transform liderRobot;

    [Header("Takip Ayarlarý")]
    [Tooltip("Lidere ne kadar yaklaþýnca dursun?")]
    public float takipMesafesi = 3f;

    [Tooltip("Aracýn maksimum hýzý")]
    public float hareketHizi = 5f;

    [Tooltip("Aracýn hedefe dönme hýzý")]
    public float donusHizi = 2f;

    void Update()
    {
        if (liderRobot == null) return;

        // 1. Lidere Doðru Dönme (Rotasyon)
        Vector3 hedefYon = liderRobot.position - transform.position;
        if (hedefYon != Vector3.zero)
        {
            Quaternion hedefRotasyon = Quaternion.LookRotation(hedefYon);
            // Slerp ile yumuþak bir dönüþ (robot anýnda dönmez, gerçekçi döner)
            transform.rotation = Quaternion.Slerp(transform.rotation, hedefRotasyon, donusHizi * Time.deltaTime);
        }

        // 2. Lidere Doðru Ýlerleme
        float aradakiMesafe = Vector3.Distance(transform.position, liderRobot.position);

        // Eðer liderden uzaksak, burnumuzun baktýðý yöne (forward) doðru ilerle
        if (aradakiMesafe > takipMesafesi)
        {
            transform.position += transform.forward * hareketHizi * Time.deltaTime;
        }
    }
}