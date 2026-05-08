using UnityEngine;

public class Otonom : MonoBehaviour
{
    // Robotun rolünü belirleyeceğimiz liste
    public enum RobotRolu { Lider, Takipci }

    [Header("Temel Ayarlar")]
    [Tooltip("Bu robot Lider mi yoksa Takipçi mi?")]
    public RobotRolu rol = RobotRolu.Lider;

    public float yuzmeHizi = 5f;
    public float donusHizi = 2f;

    [Header("--- LİDER AYARLARI ---")]
    [Tooltip("Sadece Lider rolündeyse kullanılır.")]
    public string hedefTag = "Target";
    private Transform anaHedef; // Hedefin kendisi

    [Header("--- TAKİPÇİ AYARLARI ---")]
    [Tooltip("Sadece Takipçi rolündeyse kullanılır.")]
    public Transform liderRobot;
    public float arkaMesafe = 4f;
    public float sagMesafe = 3f;
    public float dikeyMesafe = 0f;

    void Start()
    {
        // Eğer bu robot Lider ise, sahnedeki tag'e sahip hedefi bul
        if (rol == RobotRolu.Lider)
        {
            GameObject hedefObje = GameObject.FindGameObjectWithTag(hedefTag);
            if (hedefObje != null)
            {
                anaHedef = hedefObje.transform;
            }
            else
            {
                Debug.LogWarning("Lider robot için hedef obje bulunamadı! Tag'i kontrol edin.");
            }
        }
    }

    void Update()
    {
        // Seçilen role göre uygun hareket fonksiyonunu çalıştır
        if (rol == RobotRolu.Lider)
        {
            LiderHareketi();
        }
        else if (rol == RobotRolu.Takipci)
        {
            TakipciHareketi();
        }
    }

    private void LiderHareketi()
    {
        if (anaHedef == null) return;

        // Hedefe doğru yönelme
        Vector3 yon = (anaHedef.position - transform.position).normalized;

        if (yon != Vector3.zero)
        {
            Quaternion bakisAcisi = Quaternion.LookRotation(yon);
            transform.rotation = Quaternion.Slerp(transform.rotation, bakisAcisi, Time.deltaTime * donusHizi);
        }

        // İleri doğru hareket
        transform.position += transform.forward * yuzmeHizi * Time.deltaTime;
    }

    private void TakipciHareketi()
    {
        if (liderRobot == null) return;

        // Liderin X birim sağı, Z birim arkasındaki dünya koordinatını hesapla
        Vector3 formasyonHedefi = liderRobot.TransformPoint(new Vector3(sagMesafe, dikeyMesafe, -arkaMesafe));

        // Belirlenen noktaya yumuşakça ilerle (Sualtı hissiyatı)
        transform.position = Vector3.Lerp(transform.position, formasyonHedefi, Time.deltaTime * yuzmeHizi);

        // Liderle aynı yöne bak (Paralel yüzme)
        transform.rotation = Quaternion.Slerp(transform.rotation, liderRobot.rotation, Time.deltaTime * donusHizi);
    }
}