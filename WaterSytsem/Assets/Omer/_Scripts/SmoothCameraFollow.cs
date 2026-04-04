using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Takip Edilecek Araç")]
    public Transform target; 
    
    [Header("Mesafe Ayarları")]
    [Tooltip("Tam içinden bakmak için 0,0,0 yapın.")]
    public Vector3 offset = Vector3.zero; 
    
    [Header("Yumuşatma")]
    public float smoothSpeed = 20f;

    void LateUpdate() 
    {
        if (target == null) return;

        // Hedef pozisyonu hesapla
        Vector3 desiredPosition = target.TransformPoint(offset);
        
        // Pozisyonu yumuşakça takip et
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // ARTIK LOOKAT YERİNE: Aracın kendi dönüşünü (rotasyonunu) kopyala
        // Bu sayede tam olarak robotun baktığı yere bakarsın.
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, smoothSpeed * Time.deltaTime);
    }
}