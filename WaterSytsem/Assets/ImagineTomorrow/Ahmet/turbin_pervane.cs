using UnityEngine;

public class turbin_pervane : MonoBehaviour
{
    [Header("Dönüş Ayarları")]
    public float donmeHizi = 100f; // Hızı buradan ayarlayabilirsin

    void Update()
    {
        // Görseldeki eksenlere bakıldığında pervane mavi (Z) ekseninde dönmeli
        transform.Rotate(Vector3.forward * donmeHizi * Time.deltaTime);
    }
}
