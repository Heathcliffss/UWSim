using UnityEngine;
using UnityEngine.UI;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Buraya UI'daki pusula resmini sürükle
    public RectTransform compassImage;

    void Update()
    {
        // 1. Robotun dünya üzerindeki gerçek Y dönüşünü alıyoruz
        float currentYRotation = transform.eulerAngles.y;

        // 2. Konsola yazdıralım ki dönüp dönmediğini 'Console' sekmesinden görelim
        // Debug.Log("Robot Dönüşü: " + currentYRotation);

        if (compassImage != null)
        {
            // 3. Pusula resmini robotun tam tersi yönünde döndürüyoruz
            // Eğer resim ters yöne dönüyorsa başındaki '-' işaretini kaldırabilirsin
            compassImage.localRotation = Quaternion.Euler(0, 0, -currentYRotation);
        }
    }
}
