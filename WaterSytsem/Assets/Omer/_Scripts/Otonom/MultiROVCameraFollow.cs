using UnityEngine;
using UnityEngine.InputSystem;

public class MultiROVCameraFollow : MonoBehaviour
{
    [Header("1. Robot (Lider)")]
    public Transform liderHedef;      // 3. Þahýs için robotun ana merkezi
    public Transform liderFpvNoktasi; // 1. Þahýs için robotun burnundaki boþ obje

    [Header("2. Robot (Takipçi)")]
    public Transform takipciHedef;      // 3. Þahýs için robotun ana merkezi
    public Transform takipciFpvNoktasi; // 1. Þahýs için robotun burnundaki boþ obje

    [Header("Kamera Ayarlarý")]
    public Vector3 ucuncuSahisOffset = new Vector3(0, 2, -5);
    public float smoothSpeed = 10f;

    [Header("VR Kontrol (Geçiþ Tuþu)")]
    public InputActionProperty toggleViewAction;

    // 0 = Lider 3. Þahýs
    // 1 = Lider FPV
    // 2 = Takipçi 3. Þahýs
    // 3 = Takipçi FPV
    private int mod = 0; 

    void OnEnable() => toggleViewAction.action.Enable();
    void OnDisable() => toggleViewAction.action.Disable();

    void Update()
    {
        // Tuþa basýldýðýnda modu 1 artýr. 3'ü geçerse tekrar 0'a dön.
        if (toggleViewAction.action.WasPressedThisFrame())
        {
            mod++;
            if (mod > 3) mod = 0;
            
            Debug.Log("Þu anki Kamera Modu: " + mod);
        }
    }

    void LateUpdate()
    {
        if (mod == 0) // Lider 3. Þahýs
        {
            if (liderHedef == null) return;
            Vector3 hedefPoz = liderHedef.TransformPoint(ucuncuSahisOffset);
            transform.position = Vector3.Lerp(transform.position, hedefPoz, smoothSpeed * Time.deltaTime);
            
            Quaternion hedefRot = Quaternion.LookRotation(liderHedef.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedefRot, smoothSpeed * Time.deltaTime);
        }
        else if (mod == 1) // Lider FPV
        {
            if (liderFpvNoktasi == null) return;
            transform.position = Vector3.Lerp(transform.position, liderFpvNoktasi.position, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, liderFpvNoktasi.rotation, smoothSpeed * Time.deltaTime);
        }
        else if (mod == 2) // Takipçi 3. Þahýs
        {
            if (takipciHedef == null) return;
            Vector3 hedefPoz = takipciHedef.TransformPoint(ucuncuSahisOffset);
            transform.position = Vector3.Lerp(transform.position, hedefPoz, smoothSpeed * Time.deltaTime);
            
            Quaternion hedefRot = Quaternion.LookRotation(takipciHedef.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedefRot, smoothSpeed * Time.deltaTime);
        }
        else if (mod == 3) // Takipçi FPV
        {
            if (takipciFpvNoktasi == null) return;
            transform.position = Vector3.Lerp(transform.position, takipciFpvNoktasi.position, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, takipciFpvNoktasi.rotation, smoothSpeed * Time.deltaTime);
        }
    }
}