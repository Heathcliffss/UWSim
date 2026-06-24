using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

public class LiderCameraController : MonoBehaviour
{
    [Header("Freelook")]
    public float mouseSensitivity = 3f;

    [Header("Zoom")]
    public float scrollSpeed = 5f;
    public float minDistance = 1f;
    public float maxDistance = 30f;

#if UNITY_EDITOR
    public static RenderTexture EditorRT;
    public static bool ShowWindow = true;
#endif

    private float  _yaw;
    private float  _pitch;
    private float  _distance;
    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void Start()
    {
        _distance = transform.localPosition.magnitude;
        if (_distance < 0.01f) _distance = 5f;
        _yaw   = transform.localEulerAngles.y;
        _pitch = transform.localEulerAngles.x;

        _cam.stereoTargetEye = StereoTargetEyeMask.None;
        _cam.depth           = 0;
        _cam.enabled         = true;

#if UNITY_EDITOR
        EditorRT = new RenderTexture(1920, 1080, 24);
        _cam.targetTexture = EditorRT;
        Debug.Log("[LiderCam] EDITOR — Window > LiderKamera penceresini aç (K ile aç/kapa)");
#else
        _cam.targetDisplay = 0;
        StartCoroutine(DisableXRMirror());
#endif
    }

    IEnumerator DisableXRMirror()
    {
        yield return new WaitForEndOfFrame();
        try
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            if (displays.Count == 0) { Debug.Log("[LiderCam] XR yok, mirror atlandı"); yield break; }
            foreach (var d in displays)
                d.SetPreferredMirrorBlitMode(XRMirrorViewBlitMode.None);
            Debug.Log("[LiderCam] XR mirror kapatıldı");
        }
        catch (Exception e) { Debug.LogWarning("[LiderCam] DisableXRMirror hata: " + e.Message); }
    }

    public void ToggleCam()
    {
#if UNITY_EDITOR
        ShowWindow = !ShowWindow;
        Debug.Log($"[LiderCam] K → pencere {(ShowWindow ? "AÇIK" : "KAPALI")}");
#else
        _cam.enabled = !_cam.enabled;
        try
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays)
                d.SetPreferredMirrorBlitMode(
                    _cam.enabled ? XRMirrorViewBlitMode.None : XRMirrorViewBlitMode.LeftEye);
        }
        catch (Exception e) { Debug.LogWarning("[LiderCam] ToggleCam XR hata: " + e.Message); }
        Debug.Log($"[LiderCam] K → {(_cam.enabled ? "ROV Kamera" : "VR Mirror")}");
#endif
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            ToggleCam();
    }

    void LateUpdate()
    {
        if (!_cam.enabled) return;

        if (Input.GetMouseButton(1))
        {
            _yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -80f, 80f);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _distance -= scroll * scrollSpeed;
        _distance  = Mathf.Clamp(_distance, minDistance, maxDistance);

        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.localPosition = rot * (Vector3.back * _distance);
        transform.localRotation = rot;
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        if (EditorRT != null) { EditorRT.Release(); EditorRT = null; }
#endif
    }
}
