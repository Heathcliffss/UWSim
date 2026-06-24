using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

/// <summary>
/// Meta Quest Gözlemci — İki Mod:
///
///  MOD 0 — SERBEST GEZME
///    Sol stick Y/X   → dikey / strafe
///    Sağ stick Y     → ileri/geri
///    Sağ stick X     → yaw dönme
///    Sağ grip        → mainCamera'ya ışınlan
///
///  MOD 1 — ROBOT TAKİBİ (TPS)
///    XR Origin robotu tpsOffset mesafesinden yumuşakça takip eder.
///    Baş rotasyonu (Tracked Pose Driver) serbesttir — etrafına bakabilirsin.
///    Stickler devre dışı; grip teleport hâlâ çalışır.
///
///  Sağ el A tuşu   → mod geçişi (Serbest ↔ Robot Takibi)
///
///  VR bağlı değilse script sessizce devre dışı kalır.
/// </summary>
public class VRObserver_Necip : MonoBehaviour
{
    public enum Mod { Serbest, RobotTakibi }

    [Header("Teleport Hedefi")]
    public Transform mainCamera;

    [Header("Robot Takip (TPS)")]
    public Transform takipHedefi;               // Inspector'dan lider veya takipçi robotu sürükle
    public Vector3   tpsOffset = new Vector3(0f, 2f, -4f); // robotun lokal uzayında kamera ofseti
    public float     tpsSmoothSpeed = 8f;

    [Header("Serbest Mod Hızları")]
    public float moveSpeed = 5f;
    public float turnSpeed = 60f;
    public float vertSpeed = 3f;

    [Header("Mevcut Mod (Runtime'da değişir)")]
    public Mod mevcutMod = Mod.Serbest;

    // ── InputAction'lar ──────────────────────────────────────────────────────
    private InputAction _leftStick;
    private InputAction _rightStick;
    private InputAction _grip;
    private InputAction _aButton;   // sağ el primaryButton = A

    private bool _gripPrev;
    private bool _aPrev;
    private bool _xrAvailable = false;

    // ── Dahili referanslar ───────────────────────────────────────────────────
    private XROrigin _xrOrigin;

    // ════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        _xrOrigin = GetComponent<XROrigin>();

        try
        {
            _leftStick = new InputAction("LeftStick",  binding: "<XRController>{LeftHand}/thumbstick");
            _rightStick= new InputAction("RightStick", binding: "<XRController>{RightHand}/thumbstick");
            _grip      = new InputAction("Grip",       binding: "<XRController>{RightHand}/grip");
            _aButton   = new InputAction("AButton",    binding: "<XRController>{RightHand}/primaryButton");

            _leftStick.Enable();
            _rightStick.Enable();
            _grip.Enable();
            _aButton.Enable();
            _xrAvailable = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[VRObserver] XR InputAction başlatılamadı: " + e.Message);
        }
    }

    void Start()
    {
        if (!_xrAvailable) { enabled = false; return; }

        if (mainCamera != null) Teleport();
        Debug.Log("[VRObserver] Başladı | A: mod geçişi | Sağ grip: teleport");
    }

    // ════════════════════════════════════════════════════════════════════════
    void Update()
    {
        // ── A tuşu: mod geçişi (rising edge) ────────────────────────────────
        bool aDown = _aButton.ReadValue<float>() > 0.5f;
        if (aDown && !_aPrev)
        {
            mevcutMod = mevcutMod == Mod.Serbest ? Mod.RobotTakibi : Mod.Serbest;
            Debug.Log($"[VRObserver] Mod → {mevcutMod}");
        }
        _aPrev = aDown;

        // ── Grip: teleport (her iki modda da çalışır) ────────────────────────
        bool gripDown = _grip.ReadValue<float>() > 0.85f;
        if (gripDown && !_gripPrev) Teleport();
        _gripPrev = gripDown;

        // ── Mod mantığı ──────────────────────────────────────────────────────
        if (mevcutMod == Mod.Serbest)
            SerbstGezme();
        else
            RobotTakip();
    }

    // ════════════════════════════════════════════════════════════════════════
    void SerbstGezme()
    {
        Vector2 left  = _leftStick.ReadValue<Vector2>();
        Vector2 right = _rightStick.ReadValue<Vector2>();
        float   dt    = Time.deltaTime;

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        transform.position += fwd            * right.y * moveSpeed * dt;
        transform.Rotate(Vector3.up,            right.x * turnSpeed * dt, Space.World);
        transform.position += Vector3.up      * left.y  * vertSpeed * dt;
        transform.position += transform.right * left.x  * moveSpeed * dt;
    }

    // ════════════════════════════════════════════════════════════════════════
    void RobotTakip()
    {
        if (takipHedefi == null) return;

        // Robotun lokal uzayındaki tpsOffset → dünya uzayına çevir
        // XR Origin'i doğrudan bu noktaya taşı.
        // Baş rotasyonu Tracked Pose Driver tarafından bağımsız yönetilir;
        // kafanı serbestçe çevirebilirsin.
        Vector3 hedef = takipHedefi.TransformPoint(tpsOffset);
        transform.position = Vector3.Lerp(transform.position, hedef, tpsSmoothSpeed * Time.deltaTime);

        Debug.Log($"[VRObserver] TPS takip → origin={transform.position:F1}  hedef={hedef:F1}");
    }

    // ════════════════════════════════════════════════════════════════════════
    void Teleport()
    {
        if (mainCamera == null) return;
        try
        {
            // vrCam'ı burada bul; null olsa bile Camera.main ile devam et
            Camera vrCam = (_xrOrigin != null && _xrOrigin.Camera != null)
                           ? _xrOrigin.Camera
                           : Camera.main;
            if (vrCam == null) { transform.position = mainCamera.position; return; }

            Vector3 delta = mainCamera.position - vrCam.transform.position;
            transform.position += delta;

            float targetYaw  = mainCamera.eulerAngles.y;
            float currentYaw = vrCam.transform.eulerAngles.y;
            transform.Rotate(Vector3.up, targetYaw - currentYaw, Space.World);

            Debug.Log($"[VRObserver] Teleport → {mainCamera.position:F1}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[VRObserver] Teleport hata: " + e.Message);
        }
    }

    void OnDestroy()
    {
        _leftStick?.Dispose();
        _rightStick?.Dispose();
        _grip?.Dispose();
        _aButton?.Dispose();
    }
}
