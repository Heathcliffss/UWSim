using UnityEngine;

[System.Serializable]
public class WanderSettings
{
    public float moveSpeed = 2f;
    public float rotationSpeed = 2f;
    public float minSwimDistance = 5f;
    public float reachTolerance = 1.5f;
}

[System.Serializable]
public class WiggleSettings
{
    public float wiggleAngle = 20f;
    public float wiggleSpeed = 5f;
}

[System.Serializable]
public class FlockSettings
{
    [Tooltip("Bu balýk türü sürü halinde mi gezecek? (Kapalýysa yalnýz takýlýr)")]
    public bool isSchooling = true;

    [Tooltip("Diðer balýklarý algýlama yarýçapý")]
    public float neighborRadius = 3f;

    [Tooltip("Çarpýþmayý önleme gücü (Ayrýþma)")]
    public float separationWeight = 2f;

    [Tooltip("Sürüyle ayný yöne gitme gücü (Hizalanma)")]
    public float alignmentWeight = 1f;

    [Tooltip("Sürünün merkezinde kalma gücü (Bütünlük)")]
    public float cohesionWeight = 1.5f;
}

public class RandomWander : MonoBehaviour
{
    public BoxCollider roamArea;

    [Space(10)]
    public WanderSettings movementSettings;
    public WiggleSettings animSettings;
    public FlockSettings flockSettings;

    private Vector3 targetPosition;
    private bool hasTarget = false;
    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
        GetNewTarget();
    }

    void Update()
    {
        if (hasTarget)
        {
            MoveTowardsTarget();
        }
    }

    void GetNewTarget()
    {
        if (roamArea == null) return;

        Bounds bounds = roamArea.bounds;
        Vector3 potentialTarget;

        int maxAttempts = 15;
        int attempts = 0;

        do
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            potentialTarget = new Vector3(randomX, randomY, randomZ);
            attempts++;

        } while (Vector3.Distance(transform.position, potentialTarget) < movementSettings.minSwimDistance && attempts < maxAttempts);

        targetPosition = potentialTarget;
        hasTarget = true;
    }

    void MoveTowardsTarget()
    {
        if (Vector3.Distance(transform.position, targetPosition) < movementSettings.reachTolerance)
        {
            GetNewTarget();
        }

        // 1. Temel Hedef Yönü (Kutu içinde seçilen rastgele noktaya gidiþ)
        Vector3 wanderDirection = (targetPosition - transform.position).normalized;
        Vector3 finalDirection = wanderDirection;

        // 2. SÜRÜ VE ÇARPIÞMA ÖNLEME MANTIÐI (BOIDS)
        if (flockSettings.isSchooling)
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int groupSize = 0;

            // Etraftaki balýklarý (Collider'larý) bul
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, flockSettings.neighborRadius);

            foreach (Collider col in nearbyObjects)
            {
                // Eðer bulduðu þey kendisi deðilse ve bir balýksa
                if (col.gameObject != gameObject && col.CompareTag("Fish"))
                {
                    groupSize++;
                    Vector3 diff = transform.position - col.transform.position;

                    // Ayrýþma: Yakýnlýða göre ters yöne itme kuvveti
                    separation += diff.normalized / diff.magnitude;

                    // Hizalanma: Komþunun baktýðý yön
                    alignment += col.transform.forward;

                    // Bütünlük: Komþunun konumu
                    cohesion += col.transform.position;
                }
            }

            if (groupSize > 0)
            {
                // Ortalamalarý al
                alignment /= groupSize;
                cohesion = (cohesion / groupSize) - transform.position;

                // Aðýrlýklara göre sürü kuvvetlerini hesapla
                Vector3 flockDirection = (separation * flockSettings.separationWeight) +
                                         (alignment.normalized * flockSettings.alignmentWeight) +
                                         (cohesion.normalized * flockSettings.cohesionWeight);

                // Normal hedef yönü ile sürü hissini birleþtir
                finalDirection = (wanderDirection + flockDirection).normalized;
            }
        }

        // 3. Dönüþü Uygula (Artýk finalDirection'a dönüyor)
        if (finalDirection != Vector3.zero)
        {
            Quaternion targetLook = Quaternion.LookRotation(finalDirection);
            baseRotation = Quaternion.Slerp(baseRotation, targetLook, Time.deltaTime * movementSettings.rotationSpeed);
        }

        // 4. Kuyruk Sallama Efekti
        float wiggleOffset = Mathf.Sin(Time.time * animSettings.wiggleSpeed) * animSettings.wiggleAngle;
        transform.rotation = baseRotation * Quaternion.Euler(0, wiggleOffset, 0);

        // 5. Ýleri Hareket
        transform.position += baseRotation * Vector3.forward * movementSettings.moveSpeed * Time.deltaTime;
    }
}