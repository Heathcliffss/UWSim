using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Takip Edilecek Ara�")]
    public Transform target; // Buraya Arac objeni s�r�kle

    [Header("Mesafe Ayarlar�")]
    public Vector3 offset = new Vector3(0, 2, -5); // Arac�n ne kadar �st�nde/arkas�nda duracak?

    [Header("Yumu�atma (D���k de�er = Daha yumu�ak)")]
    public float smoothSpeed = 10f;

    void LateUpdate() // Render'dan hemen �nce �al���r, titremeyi �nler
    {
        if (target == null) return;

        // Arac�n pozisyonuna g�re hedef konumu hesapla
        Vector3 desiredPosition = target.TransformPoint(offset);

        // Kameray� o konuma yumu�ak bir �ekilde s�zerek g�t�r
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Kameray� her zaman araca bakt�r
        transform.LookAt(target);
    }
}