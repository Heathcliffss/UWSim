using UnityEngine;
using UnityEngine.Rendering.HighDefinition; // HDRP SU SİSTEMİ İÇİN ŞART

[RequireComponent(typeof(Rigidbody))]
public class ROSHydrodynamics : MonoBehaviour
{
    [Header("HDRP Su Sistemi")]
    [Tooltip("Sahnendeki Water Surface objesini buraya sürükle")]
    public WaterSurface targetWaterSurface;

    [Tooltip("Aracın gövde yüksekliği (Metre). Yüzeyde dengeli durması için.")]
    public float vehicleHeight = 0.5f;

    [Header("Akışkan ve Araç Parametreleri")]
    public float waterDensity = 1023.6f;
    public float vehicleVolume = 0.05f;
    public float atmosphericPressure = 101325f;

    [Header("Hidrodinamik Sönümleme (Damping)")]
    public Vector3 linearDamping = new Vector3(15f, 30f, 10f);
    public Vector3 angularDamping = new Vector3(5f, 5f, 5f);

    [Header("Sensörler (ROS Simülasyonu İçin)")]
    public LayerMask seaFloorLayer;
    public float depth;
    public float altitude;
    public float pressureBar;

    // Anlık dalga yüksekliği (Artık sabit 0 değil, sürekli güncellenecek)
    private float currentWaterSurfaceY;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    void FixedUpdate()
    {
        // 1. Önce HDRP'den anlık dalga yüksekliğini bul
        UpdateDynamicWaterHeight();

        // 2. Sensörleri ve derinliği bu yeni dalga yüksekliğine göre hesapla
        CalculateSensors();

        // 3. Aracın ne kadarı suyun içinde (Kaldırma kuvvetini sıfırlayıp uçmasını engeller)
        float submersionRatio = CalculateSubmersion();

        // 4. Sadece araç suya temas ediyorsa fizik kurallarını uygula
        if (submersionRatio > 0f)
        {
            ApplyBuoyancy(submersionRatio);
            ApplyHydrodynamicDrag(submersionRatio);
        }
    }

    private void UpdateDynamicWaterHeight()
    {
        // Eğer Inspector'dan HDRP Water eklendiyse
        if (targetWaterSurface != null)
        {
            WaterSearchParameters searchParams = new WaterSearchParameters();
            searchParams.startPositionWS = transform.position; // Aracın mevcut konumu
            searchParams.targetPositionWS = transform.position;
            searchParams.error = 0.01f;
            searchParams.maxIterations = 8;

            WaterSearchResult searchResult = new WaterSearchResult();

            // HDRP API: Aracın bulunduğu noktadaki dalganın dünya koordinatını hesapla
            if (targetWaterSurface.ProjectPointOnWaterSurface(searchParams, out searchResult))
            {
                currentWaterSurfaceY = searchResult.projectedPositionWS.y;
            }
        }
        else
        {
            currentWaterSurfaceY = 0f; // Eğer su objesi atanmadıysa varsayılan 0 al
        }
    }

    private void CalculateSensors()
    {
        // Derinlik artık aracın o anki dalgaya olan mesafesi!
        depth = currentWaterSurfaceY - transform.position.y;

        if (depth > 0)
        {
            float pressurePascal = atmosphericPressure + (waterDensity * Mathf.Abs(Physics.gravity.y) * depth);
            pressureBar = pressurePascal / 100000f;
        }
        else
        {
            depth = 0;
            pressureBar = atmosphericPressure / 100000f;
        }

        // İrtifa (Altimetre) Raycast'i
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
            return 0f; // Tamamen havada (Uçamaz, çünkü oran 0 dönüyor)
        }
        else if (topPoint < currentWaterSurfaceY)
        {
            return 1f; // Tamamen sular altında
        }
        else
        {
            // Suyun yüzeyindeyken oran hesapla (Yüzmesini / Sekmesini sağlar)
            return (currentWaterSurfaceY - bottomPoint) / vehicleHeight;
        }
    }

    private void ApplyBuoyancy(float submersionRatio)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        float buoyancyForce = waterDensity * vehicleVolume * gravity * submersionRatio;
        rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Force);
    }

    private void ApplyHydrodynamicDrag(float submersionRatio)
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        Vector3 dragForce = new Vector3(
            -linearDamping.x * localVelocity.x,
            -linearDamping.y * localVelocity.y,
            -linearDamping.z * localVelocity.z
        ) * submersionRatio;

        Vector3 dragTorque = new Vector3(
            -angularDamping.x * localAngularVelocity.x,
            -angularDamping.y * localAngularVelocity.y,
            -angularDamping.z * localAngularVelocity.z
        ) * submersionRatio;

        rb.AddRelativeForce(dragForce, ForceMode.Force);
        rb.AddRelativeTorque(dragTorque, ForceMode.Force);
    }
}