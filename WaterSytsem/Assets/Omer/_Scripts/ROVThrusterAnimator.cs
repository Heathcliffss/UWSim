using UnityEngine;
using UnityEngine.InputSystem;

// Bu satır, bu scriptin olduğu yerde kesinlikle bir 'RobotController' olmasını zorunlu kılar.
[RequireComponent(typeof(RobotController))] 
public class ROVThrusterAnimator : MonoBehaviour
{
    [Header("Üst Katman Pervaneler (Sadece Mavi Kısımlar)")]
    public Transform verticalFrontLeft;
    public Transform verticalFrontRight;
    public Transform verticalRearLeft;
    public Transform verticalRearRight;

    [Header("Alt Katman Pervaneler (Sadece Mavi Kısımlar)")]
    public Transform horizontalFrontLeft;
    public Transform horizontalFrontRight;
    public Transform horizontalRearLeft;
    public Transform horizontalRearRight;

    [Header("Görsel Dönüş Ayarları")]
    public float maxSpinSpeed = 1500f; 
    public Vector3 spinAxis = new Vector3(0, 0, 1); 

    // Ana kontrolcüye referans
    private RobotController rc;

    void Start()
    {
        // Script başladığında aynı obje üzerindeki RobotController'ı otomatik olarak bulup içine kaydeder.
        rc = GetComponent<RobotController>();
    }

    void Update()
    {
        if (rc == null) return; // Güvenlik önlemi

        // 1. Girdileri Doğrudan RobotController Üzerinden Çekiyoruz!
        float vertical = rc.leftJoystick.action.ReadValue<Vector2>().y;
        
        float yaw = 0f;
        float pitch = 0f;

        if (rc.rightJoystick.action.expectedControlType == "Vector2")
        {
            Vector2 rightJoy = rc.rightJoystick.action.ReadValue<Vector2>();
            yaw = rightJoy.x;
            pitch = rightJoy.y;
        }
        else
        {
            yaw = rc.rightJoystick.action.ReadValue<float>();
        }

        float forward = rc.rightTrigger.action.ReadValue<float>() - rc.leftTrigger.action.ReadValue<float>();
        float roll = rc.leftGrip.action.ReadValue<float>() - rc.rightGrip.action.ReadValue<float>();

        // 2. DİKEY MOTOR KARIŞIMI (Yukarı/Aşağı, Eğilme ve Yatma)
        float vFL = vertical + pitch + roll;
        float vFR = vertical + pitch - roll;
        float vRL = vertical - pitch + roll;
        float vRR = vertical - pitch - roll;

        // 3. YATAY MOTOR KARIŞIMI (İleri/Geri ve Sağa/Sola Dönüş)
        float hFL = forward + yaw;
        float hFR = forward - yaw;
        float hRL = forward - yaw;
        float hRR = forward + yaw;

        // 4. Mavi Pervaneleri Döndür
        RotatePropeller(verticalFrontLeft, vFL);
        RotatePropeller(verticalFrontRight, vFR);
        RotatePropeller(verticalRearLeft, vRL);
        RotatePropeller(verticalRearRight, vRR);

        RotatePropeller(horizontalFrontLeft, hFL);
        RotatePropeller(horizontalFrontRight, hFR);
        RotatePropeller(horizontalRearLeft, hRL);
        RotatePropeller(horizontalRearRight, hRR);
    }

    void RotatePropeller(Transform prop, float inputPower)
    {
        if (prop != null && Mathf.Abs(inputPower) > 0.05f)
        {
            prop.Rotate(spinAxis * inputPower * maxSpinSpeed * Time.deltaTime, Space.Self);
        }
    }
}