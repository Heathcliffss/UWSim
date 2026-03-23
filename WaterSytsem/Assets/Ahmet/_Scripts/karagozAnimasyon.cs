using UnityEngine;

public class karagozAnimasyon : MonoBehaviour
{
    public float swimSpeed = 2f;
    public float tailSpeed = 5f;
    public float tailAngle = 15f;

    void Update()
    {
        // İleri doğru hareket
        transform.Translate(Vector3.forward * swimSpeed * Time.deltaTime);

        // Sağa sola hafif kuyruk/gövde sallanma efekti
        float rotationY = Mathf.Sin(Time.time * tailSpeed) * tailAngle;
        transform.localRotation = Quaternion.Euler(0, rotationY, 0);
    }
}
