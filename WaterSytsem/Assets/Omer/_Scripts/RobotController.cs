using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RobotController : MonoBehaviour
{
    [Header("Input Referanslarý")]
    [Tooltip("Sol Joystick (Yukarý/Aþaðý)")]
    public InputActionReference leftJoystick;

    [Tooltip("Sað Joystick (Saða/Sola Dönüþ)")]
    public InputActionReference rightJoystick;

    [Tooltip("Sol Ýþaret Parmaðý Tetiði (Geri Gitme)")]
    public InputActionReference leftTrigger;

    [Tooltip("Sað Ýþaret Parmaðý Tetiði (Ýleri Gitme)")]
    public InputActionReference rightTrigger;

    [Header("Hýz Ayarlarý")]
    public float verticalSpeed = 3f;
    public float forwardSpeed = 5f;
    public float rotationSpeed = 90f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // VR'da hareket ederken oluþabilecek kamera titremesini (jitter) önler.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        // --- 1. YUKARI / AÞAÐI HAREKET ---
        Vector2 leftJoyValue = leftJoystick.action.ReadValue<Vector2>();
        float upDownInput = leftJoyValue.y;

        // --- 2. KENDÝ EKSENÝNDE DÖNÜÞ (KESÝN ÇÖZÜM UYGULANDI) ---
        float rotationInput = 0f;

        // Input aksiyonunun Vector2 mi yoksa Float(Axis) mi olduðunu kontrol edip ona göre okuyoruz.
        if (rightJoystick.action.expectedControlType == "Vector2")
        {
            rotationInput = rightJoystick.action.ReadValue<Vector2>().x;
        }
        else
        {
            rotationInput = rightJoystick.action.ReadValue<float>();
        }

        // --- 3. ÝLERÝ / GERÝ HAREKET ---
        float forwardInput = rightTrigger.action.ReadValue<float>();
        float backwardInput = leftTrigger.action.ReadValue<float>();
        float zMovement = forwardInput - backwardInput;

        // --- HAREKETÝ FÝZÝKSEL OLARAK UYGULAMA ---
        Vector3 movement = new Vector3(0f, upDownInput * verticalSpeed, zMovement * forwardSpeed);
        Vector3 localMovement = transform.TransformDirection(movement) * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + localMovement);

        // --- DÖNÜÞÜ FÝZÝKSEL OLARAK UYGULAMA ---
        // Joystick ufak tefek oynamalarýný (deadzone) yoksaymak için ufak bir eþik deðeri ekledik.
        if (Mathf.Abs(rotationInput) > 0.05f)
        {
            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0f, rotationInput * rotationSpeed * Time.fixedDeltaTime, 0f));
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

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