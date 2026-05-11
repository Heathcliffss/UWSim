using UnityEngine;
using UnityEngine.InputSystem;

public class MultiROVCameraFollow : MonoBehaviour
{
    [System.Serializable]
    public struct CameraView
    {
        public string viewName;      // Düzenleme kolaylýðý için isim
        public Transform target;     // Takip edilecek merkez (3. Þahýs için)
        public Transform fpvPoint;  // FPV kamerasý konumu
        public bool isFPV;          // Bu mod FPV mi yoksa 3. Þahýs mý?
    }

    [Header("Kamera Bakýþ Açýlarý")]
    public CameraView[] views = new CameraView[4];

    [Header("3. Þahýs Ayarlarý")]
    public Vector3 thirdPersonOffset = new Vector3(0, 2, -5);

    [Header("Yumuþatma")]
    public float smoothSpeed = 10f;

    [Header("VR Kontrol (B Tuþu)")]
    public InputActionProperty toggleViewAction;

    private int currentViewIndex = 0;

    void OnEnable() => toggleViewAction.action.Enable();
    void OnDisable() => toggleViewAction.action.Disable();

    void Update()
    {
        // Tuþa her basýldýðýnda 0-1-2-3-0 þeklinde döner
        if (toggleViewAction.action.WasPressedThisFrame())
        {
            currentViewIndex = (currentViewIndex + 1) % views.Length;
            Debug.Log("Kamera Modu Deðiþti: " + views[currentViewIndex].viewName);
        }
    }

    void LateUpdate()
    {
        CameraView currentView = views[currentViewIndex];

        if (currentView.target == null || currentView.fpvPoint == null) return;

        if (currentView.isFPV)
        {
            // BÝRÝNCÝ ÞAHIS (FPV) MODU
            transform.position = Vector3.Lerp(transform.position, currentView.fpvPoint.position, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, currentView.fpvPoint.rotation, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // ÜÇÜNCÜ ÞAHIS MODU
            Vector3 desiredPosition = currentView.target.TransformPoint(thirdPersonOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Hedefe yumuþakça odaklan
            Quaternion targetRotation = Quaternion.LookRotation(currentView.target.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
    }
}