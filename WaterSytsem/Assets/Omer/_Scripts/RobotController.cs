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
    [Tooltip("Suyun direncini kırmak için bu değerler YÜKSEK tutulmalı!")]
    public float turnTorque = 500f;        // Sağa/Sola dönme gücü (Yaw) - ARTTIRILDI
    public float rollTorque = 300f;        // Kendi ekseninde yuvarlanma gücü (Roll) - ARTTIRILDI

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ağırlık merkezini merkezPivot'a göre ayarla (Eğer atanmışsa)
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

        // --- 1. HAREKET KUVVETİ (Lineer İtki) ---
        // Aracın kendi ana transformunu (transform.up/forward) kullanıyoruz.
        Vector3 linearThrust = (transform.up * (upDownInput * verticalThrust)) +
                               (transform.forward * (zMovement * forwardThrust));

        rb.AddForce(linearThrust, ForceMode.Force);

        // --- 2. DÖNÜŞ KUVVETİ (Açısal İtki) ---
        float finalYawTorque = (Mathf.Abs(rotationInput) > 0.05f) ? rotationInput * turnTorque : 0f;

        // KRİTİK DEĞİŞİKLİK: Dönüşü merkezPivot'tan DEĞİL, robotun KENDİ ekseninden (transform.up) alıyoruz.
        Vector3 angularThrust = (transform.up * finalYawTorque) +
                                (transform.forward * zRollMovement * rollTorque);

        if (angularThrust != Vector3.zero)
        {
            // ForceMode.Acceleration kullanarak kütle/hacim direncini aşıyor, anında tepki vermesini sağlıyoruz.
            rb.AddTorque(angularThrust, ForceMode.Acceleration);
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