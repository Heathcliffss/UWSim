using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Rigidbody))]
public class ROSHydrodynamics : MonoBehaviour
{
    [Header("HDRP Su Sistemi")]
    [Tooltip("Sahnendeki Water Surface objesini buraya sürükle")]
    public WaterSurface targetWaterSurface;

    [Tooltip("Aracın gövde yüksekliği. Yumuşak çıkış için 0.6 yapıldı.")]
    public float vehicleHeight = 0.6f;

    [Header("Akışkan ve Araç Parametreleri")]
    [Tooltip("Haliç'in üst ve alt katman ortalaması olan hacim hesaplandı.")]
    public float vehicleVolume = 0.00979f;
    public float atmosphericPressure = 101325f;

    [Header("Haliç Katman Verileri (Canlı)")]
    [Tooltip("O anki derinliğe göre suyun hesaplanan gerçek yoğunluğu")]
    public float currentWaterDensity;

    [Header("Hidrodinamik Sönümleme (Damping)")]
    // Y ekseni (Yukarı/Aşağı) direnci artırıldı ki dibe inmek/çıkmak zorlaşsın.
    public Vector3 linearDamping = new Vector3(15f, 25f, 10f);
    public Vector3 angularDamping = new Vector3(5f, 5f, 5f);

    [Header("Sensörler (ROS Simülasyonu İçin)")]

    [Header("Referans Noktası")]
    [Tooltip("Aracın içine koyduğumuz 'MerkezPivot' objesini buraya sürükle.")]
    public Transform merkezPivot;
    public LayerMask seaFloorLayer;
    public float depth;
    public float altitude;
    public float pressureBar;

    private float currentWaterSurfaceY;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // Aracın ağırlık merkezini doğrudan senin koyduğun o Pivot noktasına eşitliyoruz!
        if (merkezPivot != null)
        {
            rb.centerOfMass = merkezPivot.localPosition;
        }
        else
        {
            rb.centerOfMass = new Vector3(0f, -0.15f, 0f);
        }
    }

    void FixedUpdate()
    {
        UpdateDynamicWaterHeight();
        CalculateSensors();

        float submersionRatio = CalculateSubmersion();

        if (submersionRatio > 0f)
        {
            ApplyBuoyancy(submersionRatio);
            ApplyHydrodynamicDrag(submersionRatio);
        }
    }

    private void UpdateDynamicWaterHeight()
    {
        if (targetWaterSurface != null)
        {
            WaterSearchParameters searchParams = new WaterSearchParameters();
            searchParams.startPositionWS = transform.position;
            searchParams.targetPositionWS = transform.position;
            searchParams.error = 0.01f;
            searchParams.maxIterations = 8;

            WaterSearchResult searchResult = new WaterSearchResult();

            if (targetWaterSurface.ProjectPointOnWaterSurface(searchParams, out searchResult))
            {
                currentWaterSurfaceY = searchResult.projectedPositionWS.y;
            }
        }
        else
        {
            currentWaterSurfaceY = 0f;
        }
    }

    // YENİ HALİÇ KATMAN SİSTEMİ: Derinliğe göre su yoğunluğunu hesaplar
    private float GetDynamicWaterDensity(float currentDepth)
    {
        if (currentDepth <= 15f)
        {
            return 1014f; // Üst Katman (Karadeniz suyu)
        }
        else if (currentDepth >= 25f)
        {
            return 1028f; // Alt Katman (Akdeniz suyu)
        }
        else
        {
            // Haloklin Bariyeri (Geçiş bölgesi)
            float t = (currentDepth - 15f) / 10f;
            return Mathf.Lerp(1014f, 1028f, t);
        }
    }

    private void CalculateSensors()
    {
        depth = currentWaterSurfaceY - transform.position.y;

        // O anki derinliğe göre suyun yoğunluğunu güncelliyoruz
        currentWaterDensity = GetDynamicWaterDensity(depth > 0 ? depth : 0);

        if (depth > 0)
        {
            // Basınç hesabı artık Haliç'in dinamik yoğunluğuna göre yapılıyor
            float pressurePascal = atmosphericPressure + (currentWaterDensity * Mathf.Abs(Physics.gravity.y) * depth);
            pressureBar = pressurePascal / 100000f;
        }
        else
        {
            depth = 0;
            pressureBar = atmosphericPressure / 100000f;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1000f, seaFloorLayer))
        {
            altitude = hit.distance;
        }
        else
        {
            altitude = -1f;
        }
    }

    private float CalculateSubmersion()
    {
        float topPoint = transform.position.y + (vehicleHeight / 2f);
        float bottomPoint = transform.position.y - (vehicleHeight / 2f);

        if (bottomPoint > currentWaterSurfaceY)
        {
            return 0f;
        }
        else if (topPoint < currentWaterSurfaceY)
        {
            return 1f;
        }
        else
        {
            return (currentWaterSurfaceY - bottomPoint) / vehicleHeight;
        }
    }

    private void ApplyBuoyancy(float submersionRatio)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        // Kaldırma kuvveti Haliç'in değişken yoğunluğuna (currentWaterDensity) göre çalışır
        float buoyancyForce = currentWaterDensity * vehicleVolume * gravity * submersionRatio;
        rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Force);
    }

    private void ApplyHydrodynamicDrag(float submersionRatio)
    {
        // "Yunus Atlayışı" Koruması: Araç sudan çıksa bile sürtünmenin %30'u kalır ki roket gibi uçmasın.
        float effectiveRatio = Mathf.Clamp(submersionRatio, 0.3f, 1.0f);

        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        // Direnç kuvvetleri submersionRatio yerine effectiveRatio ile çarpılıyor
        Vector3 dragForce = new Vector3(
            -linearDamping.x * localVelocity.x,
            -linearDamping.y * localVelocity.y,
            -linearDamping.z * localVelocity.z
        ) * effectiveRatio;

        Vector3 dragTorque = new Vector3(
            -angularDamping.x * localAngularVelocity.x,
            -angularDamping.y * localAngularVelocity.y,
            -angularDamping.z * localAngularVelocity.z
        ) * effectiveRatio;

        rb.AddRelativeForce(dragForce, ForceMode.Force);
        rb.AddRelativeTorque(dragTorque, ForceMode.Force);
    }
}