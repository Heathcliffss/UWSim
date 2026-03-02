using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RobotController : MonoBehaviour
{
    [Header("Input Referanslarý")]
    [Tooltip("Sol Joystick (Vector2)")]
    public InputActionReference leftJoystick;

    [Tooltip("Sað Joystick (Vector2)")]
    public InputActionReference rightJoystick;

    [Tooltip("Sol Ýþaret Parmaðý Tetiði (Float) - Geri Gitme")]
    public InputActionReference leftTrigger;

    [Tooltip("Sað Ýþaret Parmaðý Tetiði (Float) - Ýleri Gitme")]
    public InputActionReference rightTrigger;

    [Header("Hýz Ayarlarý")]
    public float verticalSpeed = 3f;   // Yukarý/Aþaðý hýzý
    public float forwardSpeed = 5f;    // Ýleri/Geri hýzý
    public float rotationSpeed = 90f;  // Kendi etrafýnda dönme hýzý

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // --- 1. YUKARI / AÞAÐI HAREKET (Sol Joystick Y Ekseni) ---
        Vector2 leftJoyValue = leftJoystick.action.ReadValue<Vector2>();
        float upDownInput = leftJoyValue.y;

        // --- 2. KENDÝ EKSENÝNDE DÖNÜÞ (Sað Joystick X Ekseni) ---
        Vector2 rightJoyValue = rightJoystick.action.ReadValue<Vector2>();
        float rotationInput = rightJoyValue.x;

        // --- 3. ÝLERÝ / GERÝ HAREKET (Tetikler) ---
        // Tetikler 0 ile 1 arasýnda deðer döndürür.
        float forwardInput = rightTrigger.action.ReadValue<float>();
        float backwardInput = leftTrigger.action.ReadValue<float>();

        // Sað tetik basýlýysa pozitif, sol tetik basýlýysa negatif deðer elde ederiz.
        // Ýkisine birden basýlýrsa birbirini nötrler (0 olur).
        float zMovement = forwardInput - backwardInput;

        // --- HAREKETÝ FÝZÝKSEL OLARAK UYGULAMA ---

        // Robotun yerel (kendi baktýðý) yönüne göre hareket vektörünü oluþtur.
        // X ekseni (sað/sol) = 0, Y ekseni = yukarý/aþaðý, Z ekseni = ileri/geri
        Vector3 movement = new Vector3(0f, upDownInput * verticalSpeed, zMovement * forwardSpeed);
        Vector3 localMovement = transform.TransformDirection(movement) * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + localMovement);

        // --- DÖNÜÞÜ FÝZÝKSEL OLARAK UYGULAMA ---
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0f, rotationInput * rotationSpeed * Time.fixedDeltaTime, 0f));
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    // Inputlarý aktif etme ve kapatma iþlemleri
    private void OnEnable()
    {
        leftJoystick.action.Enable();
        rightJoystick.action.Enable();
        leftTrigger.action.Enable();
        rightTrigger.action.Enable();
    }

    private void OnDisable()
    {
        leftJoystick.action.Disable();
        rightJoystick.action.Disable();
        leftTrigger.action.Disable();
        rightTrigger.action.Disable();
    }
}