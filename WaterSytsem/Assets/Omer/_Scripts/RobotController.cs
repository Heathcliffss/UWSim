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

    [Tooltip("Sol Orta Parmak (Grip) - Sola Yuvarlanma (Roll)")]
    public InputActionReference leftGrip;

    [Tooltip("Sað Orta Parmak (Grip) - Saða Yuvarlanma (Roll)")]
    public InputActionReference rightGrip;

    [Header("Motor Ýtiþ Gücü (Thrust) Ayarlarý")]
    [Tooltip("Haliç'in yoðun alt katmanýna inebilmek için dikey gücü yüksek tutmalýyýz.")]
    public float verticalThrust = 100f;   // Dibe inmek için artýrýldý
    public float forwardThrust = 50f;     // Gerçekçi, oturaklý ileri gidiþ

    [Header("Dönüþ Gücü (Torque) Ayarlarý")]
    public float turnTorque = 15f;        // Sað/Sol dönme gücü (Yaw)
    public float rollTorque = 20f;        // Kendi ekseninde yuvarlanma gücü (Roll)

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

        // --- 2. KENDÝ EKSENÝNDE SAÐA/SOLA DÖNÜÞ (YAW) ---
        float rotationInput = 0f;
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

        // --- 4. YUVARLANMA HAREKETÝ (ROLL - Grip Tuþlarý) ---
        float rollLeftInput = leftGrip.action.ReadValue<float>();
        float rollRightInput = rightGrip.action.ReadValue<float>();

        // Unity'de Z ekseninde pozitif dönüþ sola, negatif dönüþ saða yatýrýr.
        // Sol tuþa basarsak pozitif, sað tuþa basarsak negatif deðer elde ederiz.
        float zRollMovement = rollLeftInput - rollRightInput;

        // --- MOTOR KUVVETLERÝNÝ (THRUST) UYGULAMA ---
        Vector3 linearThrust = new Vector3(0f, upDownInput * verticalThrust, zMovement * forwardThrust);
        rb.AddRelativeForce(linearThrust, ForceMode.Force);

        // --- DÖNÜÞ KUVVETLERÝNÝ (TORQUE) BÝRLEÞTÝRME VE UYGULAMA ---
        float finalYawTorque = 0f;

        // Joystick ufak tefek oynamalarýný (deadzone) yoksaymak için eþik deðeri
        if (Mathf.Abs(rotationInput) > 0.05f)
        {
            finalYawTorque = rotationInput * turnTorque;
        }

        // Y ekseni (Yaw/Sað-Sol Dönüþ) ve Z ekseni (Roll/Yuvarlanma) torklarýný birleþtir
        Vector3 angularThrust = new Vector3(0f, finalYawTorque, zRollMovement * rollTorque);

        // Eðer herhangi bir dönüþ girdisi varsa kuvveti uygula
        if (angularThrust != Vector3.zero)
        {
            rb.AddRelativeTorque(angularThrust, ForceMode.Force);
        }
    }

    private void OnEnable()
    {
        leftJoystick.action.Enable();
        rightJoystick.action.Enable();
        leftTrigger.action.Enable();
        rightTrigger.action.Enable();
        if (leftGrip != null) leftGrip.action.Enable();
        if (rightGrip != null) rightGrip.action.Enable();
    }

    private void OnDisable()
    {
        leftJoystick.action.Disable();
        rightJoystick.action.Disable();
        leftTrigger.action.Disable();
        rightTrigger.action.Disable();
        if (leftGrip != null) leftGrip.action.Disable();
        if (rightGrip != null) rightGrip.action.Disable();
    }
}