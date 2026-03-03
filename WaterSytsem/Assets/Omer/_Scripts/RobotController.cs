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

    [Header("Motor Ýtiþ Gücü (Thrust) Ayarlarý")]
    [Tooltip("Motorlarýn Newton cinsinden itme kuvveti. Suyun direncini yenmek için yüksek deðerler (Örn: 100-500) gerekebilir.")]
    public float verticalThrust = 150f;
    public float forwardThrust = 200f;
    public float turnTorque = 100f;

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

        // --- MOTOR KUVVETLERÝNÝ (THRUST) UYGULAMA ---

        // Ýleri/Geri ve Yukarý/Aþaðý itiþ gücünü tek bir vektörde birleþtiriyoruz.
        // AddRelativeForce, bu kuvveti doðrudan aracýn o an baktýðý yöne uygular (Gerçek motor gibi).
        Vector3 linearThrust = new Vector3(0f, upDownInput * verticalThrust, zMovement * forwardThrust);
        rb.AddRelativeForce(linearThrust, ForceMode.Force);

        // --- DÖNÜÞ KUVVETÝNÝ (TORQUE) UYGULAMA ---
        // Joystick ufak tefek oynamalarýný (deadzone) yoksaymak için ufak bir eþik deðeri ekledik.
        if (Mathf.Abs(rotationInput) > 0.05f)
        {
            // AddRelativeTorque, aracý kendi merkez ekseninden çevirmek için fiziksel tork (dönüþ kuvveti) uygular.
            Vector3 angularThrust = new Vector3(0f, rotationInput * turnTorque, 0f);
            rb.AddRelativeTorque(angularThrust, ForceMode.Force);
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