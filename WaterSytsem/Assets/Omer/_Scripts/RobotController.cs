using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RobotController : MonoBehaviour
{
    [Header("Referans Noktası")]
    [Tooltip("Sadece ağırlık merkezi için kullanılır. Dönüşler robotun kendi ekseninden yapılır.")]
    public Transform merkezPivot;

    [Header("Input Referansları")]
    public InputActionReference leftJoystick;
    public InputActionReference rightJoystick;
    public InputActionReference leftTrigger;
    public InputActionReference rightTrigger;
    public InputActionReference leftGrip;
    public InputActionReference rightGrip;

    [Header("Motor İtki Gücü (Thrust) Ayarları")]
    public float verticalThrust = 100f;
    public float forwardThrust = 50f;

    [Header("Dönüş Gücü (Torque) Ayarları")]
    public float turnTorque = 500f;        // Sağa/Sola dönme gücü (Yaw)
    public float rollTorque = 300f;        // Kendi ekseninde yuvarlanma gücü (Roll)
    public float pitchTorque = 400f;       // YENİ: Aşağı/Yukarı eğilme gücü (Pitch)

    [Header("Otomatik Dengeleme (Stabilization)")]
    [Tooltip("Robotun joystick bırakıldığında kendini yere paralel hale getirme gücü.")]
    public float stabilizeForce = 80f;     // YENİ: Hacıyatmaz etkisi gücü

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ağırlık merkezini merkezPivot'a göre ayarla
        if (merkezPivot != null)
        {
            rb.centerOfMass = merkezPivot.localPosition;
        }
    }

    void FixedUpdate()
    {
        // --- GİRDİLERİ OKUMA ---
        float upDownInput = leftJoystick.action.ReadValue<Vector2>().y;

        float rotationInput = 0f;
        float pitchInput = 0f; // YENİ: Eğilme girdisi

        if (rightJoystick.action.expectedControlType == "Vector2")
        {
            Vector2 rightJoyValue = rightJoystick.action.ReadValue<Vector2>();
            rotationInput = rightJoyValue.x; // Sağa/Sola dön (Yaw)
            pitchInput = rightJoyValue.y;    // YENİ: Aşağı/Yukarı eğil (Pitch)
        }
        else
        {
            rotationInput = rightJoystick.action.ReadValue<float>();
        }

        float forwardInput = rightTrigger.action.ReadValue<float>();
        float backwardInput = leftTrigger.action.ReadValue<float>();
        float zMovement = forwardInput - backwardInput;

        float rollLeftInput = leftGrip.action.ReadValue<float>();
        float rollRightInput = rightGrip.action.ReadValue<float>();
        float zRollMovement = rollLeftInput - rollRightInput;

        // --- 1. HAREKET KUVVETİ (Lineer İtki) ---
        Vector3 linearThrust = (transform.up * (upDownInput * verticalThrust)) +
                               (transform.forward * (zMovement * forwardThrust));

        rb.AddForce(linearThrust, ForceMode.Force);

        // --- 2. DÖNÜŞ KUVVETİ (Açısal İtki) ---
        float finalYawTorque = (Mathf.Abs(rotationInput) > 0.05f) ? rotationInput * turnTorque : 0f;
        
        // Pitch girdisi (Yukarı itince burun aşağı insin diye doğrudan kullanıyoruz. 
        // Eğer ters gelirse 'pitchInput' önüne eksi (-) koyabilirsin: -pitchInput * pitchTorque)
        float finalPitchTorque = (Mathf.Abs(pitchInput) > 0.05f) ? pitchInput * pitchTorque : 0f;

        // Y (Yaw), Z (Roll) ve X (Pitch) eksenlerindeki torkları birleştiriyoruz
        Vector3 angularThrust = (transform.up * finalYawTorque) +
                                (transform.forward * zRollMovement * rollTorque) +
                                (transform.right * finalPitchTorque); // YENİ: X ekseni etrafında eğilme

        if (angularThrust != Vector3.zero)
        {
            rb.AddTorque(angularThrust, ForceMode.Acceleration);
        }

        // --- 3. OTOMATİK DENGELEME (Hacıyatmaz Etkisi) ---
        // Dünya'nın 'Yukarı' yönü ile robotun kendi 'Yukarı' yönü arasındaki farkı hesaplar
        // Bu farkı kapatacak bir karşı tork (righting moment) uygular.
        Vector3 rightingTorque = Vector3.Cross(transform.up, Vector3.up) * stabilizeForce;
        
        // Dengeleme kuvvetini uygula
        rb.AddTorque(rightingTorque, ForceMode.Acceleration);
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