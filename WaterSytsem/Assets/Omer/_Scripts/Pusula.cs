using UnityEngine;
using UnityEngine.UI;

public class NewMonoBehaviourScript : MonoBehaviour
{ 

    [Header("UI Objects")]
    public RectTransform compassImage; // UI'daki Pusula Resminin RectTransform'unu buraya sürükle

    void Update()
    {
        // Robotun Y eksenindeki dönüşünü alıyoruz (0-360 arası)
        float headingAngle = transform.eulerAngles.y;

        // Pusula resmi, robotun dönüşünün TERSİNE dönmelidir.
        // Robot sağa dönerse (N -> E -> S), resim sola dönmeli ki 'N' (Kuzey) hep aynı yönü göstersin (teoride).
        // Sen "direk bu dönsede olur" dediğin için tam olarak bunu yapacağız.
        // North'un (Kuzey) resmin üstünde (0 derece) olduğunu varsayıyoruz.

        compassImage.localRotation = Quaternion.Euler(0, 0, -headingAngle);
    }
}
