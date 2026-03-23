using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Takip Edilecek Araç")]
    public Transform target; // Buraya Arac objeni sürükle

    [Header("Mesafe Ayarlarý")]
    public Vector3 offset = new Vector3(0, 2, -5); // Aracýn ne kadar üstünde/arkasýnda duracak?

    [Header("Yumuþatma (Düþük deðer = Daha yumuþak)")]
    public float smoothSpeed = 10f;

    void LateUpdate() // Render'dan hemen önce çalýþýr, titremeyi önler
    {
        if (target == null) return;

        // Aracýn pozisyonuna göre hedef konumu hesapla
        Vector3 desiredPosition = target.TransformPoint(offset);

        // Kamerayý o konuma yumuþak bir þekilde süzerek götür
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Kamerayý her zaman araca baktýr
        transform.LookAt(target);
    }
}