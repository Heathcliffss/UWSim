using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RobotController : MonoBehaviour
{
    [Header("Referans Noktasý")]
    [Tooltip("Aracýn içine koyduðumuz 'MerkezPivot' objesini buraya sürükle.")]
    public Transform merkezPivot;

    [Header("Input Referanslarý")]
    public InputActionReference leftJoystick;
    public InputActionReference rightJoystick;
    public InputActionReference leftTrigger;
    public InputActionReference rightTrigger;
    public InputActionReference leftGrip;
    public InputActionReference rightGrip;

    [Header("Motor Ýtiþ Gücü (Thrust) Ayarlarý")]
    public float verticalThrust = 100f;
    public float forwardThrust = 50f;

    [Header("Dönüþ Gücü (Torque) Ayarlarý")]
    [Tooltip("Dönüþ hýzlarý daha kontrollü olmasý için yarý yarýya düþürüldü.")]
    public float turnTorque = 7.5f;        // Sað/Sol dönme gücü (Yaw)
    public float rollTorque = 10f;         // Kendi ekseninde yuvarlanma gücü (Roll)

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (merkezPivot == null) return; // Güvenlik önlemi

        // --- GÝRDÝLERÝ OKUMA ---
        float upDownInput = leftJoystick.action.ReadValue<Vector2>().y;

        float rotationInput = 0f;
        if (rightJoystick.action.expectedControlType == "Vector2")
            rotationInput = rightJoystick.action.ReadValue<Vector2>().x;
        else
            rotationInput = rightJoystick.action.ReadValue<float>();

        float forwardInput = rightTrigger.action.ReadValue<float>();
        float backwardInput = leftTrigger.action.ReadValue<float>();
        float zMovement = forwardInput - backwardInput;

        float rollLeftInput = leftGrip.action.ReadValue<float>();
        float rollRightInput = rightGrip.action.ReadValue<float>();
        float zRollMovement = rollLeftInput - rollRightInput;

        // --- 1. HAREKET KUVVETÝ (Doðrudan MerkezPivot'un eksenlerine göre) ---
        // Artýk AddRelativeForce yerine AddForce kullanýyoruz, çünkü yönü biz belirliyoruz.
        Vector3 linearThrust = (merkezPivot.up * (upDownInput * verticalThrust)) +
                               (merkezPivot.forward * (zMovement * forwardThrust));

        rb.AddForce(linearThrust, ForceMode.Force);

        // --- 2. DÖNÜÞ KUVVETÝ (Doðrudan MerkezPivot'un eksenlerine göre) ---
        float finalYawTorque = (Mathf.Abs(rotationInput) > 0.05f) ? rotationInput * turnTorque : 0f;

        // Y ekseni etrafýnda saða/sola dönüþ (Yaw) ve Z ekseni etrafýnda yuvarlanma (Roll)
        Vector3 angularThrust = (merkezPivot.up * finalYawTorque) +
                                (merkezPivot.forward * zRollMovement * rollTorque);

        if (angularThrust != Vector3.zero)
        {
            rb.AddTorque(angularThrust, ForceMode.Force);
        }
    }

    private void OnEnable()
    {
        if (leftJoystick != null) leftJoystick.action.Enable();
        if (rightJoystick != null) rightJoystick.action.Enable();
        if (leftTrigger != null) leftTrigger.action.Enable();
        if (rightTrigger != null) rightTrigger.action.Enable();
        if (leftGrip != null) leftGrip.action.Enable();
        if (rightGrip != null) rightGrip.action.Enable();
    }

    private void OnDisable()
    {
        if (leftJoystick != null) leftJoystick.action.Disable();
        if (rightJoystick != null) rightJoystick.action.Disable();
        if (leftTrigger != null) leftTrigger.action.Disable();
        if (rightTrigger != null) rightTrigger.action.Disable();
        if (leftGrip != null) leftGrip.action.Disable();
        if (rightGrip != null) rightGrip.action.Disable();
    }
}