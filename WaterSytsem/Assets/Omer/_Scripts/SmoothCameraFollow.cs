using UnityEngine;
using UnityEngine.InputSystem; // Input System paketi gerekli

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Takip Edilecek Objeler")]
    public Transform target;       // Robotun kendisi (3. Şahıs için)
    public Transform fpvPoint;    // Robotun burnuna koyacağın boş bir GameObject

    [Header("3. Şahıs Ayarları")]
    public Vector3 thirdPersonOffset = new Vector3(0, 2, -5);
    
    [Header("Yumuşatma")]
    public float smoothSpeed = 10f;

    [Header("VR Kontrol (B Tuşu)")]
    // Inspector'da "XRI RightHand Interaction/Secondary Button" seçilmeli
    public InputActionProperty toggleViewAction; 

    private bool isFirstPerson = false;

    void OnEnable() => toggleViewAction.action.Enable();
    void OnDisable() => toggleViewAction.action.Disable();

    void Update()
    {
        // B tuşuna basıldığında (basıldığı an bir kere çalışır)
        if (toggleViewAction.action.WasPressedThisFrame())
        {
            isFirstPerson = !isFirstPerson;
        }
    }

    void LateUpdate()
    {
        if (target == null || fpvPoint == null) return;

        if (isFirstPerson)
        {
            // BİRİNCİ ŞAHIS (FPV) MODU
            // Kamerayı tam robotun gözüne (fpvPoint) ışınla/süz
            transform.position = Vector3.Lerp(transform.position, fpvPoint.position, smoothSpeed * Time.deltaTime);
            
            // Robotun baktığı yöne bak (Daha gerçekçi)
            transform.rotation = Quaternion.Slerp(transform.rotation, fpvPoint.rotation, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // ÜÇÜNCÜ ŞAHIS MODU (Eski sistemin)
            Vector3 desiredPosition = target.TransformPoint(thirdPersonOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            
            // Robotun merkezine odaklan
            transform.LookAt(target);
        }
    }
}