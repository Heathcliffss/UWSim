using UnityEngine;

[System.Serializable]
public class WanderSettings
{
    public float moveSpeed = 2f;
    public float rotationSpeed = 2f;
    public float minSwimDistance = 10f;
    public float reachTolerance = 2f;
}

[System.Serializable]
public class FlockSettings
{
    public bool isSchooling = true;
    public string speciesName; // YENÝ: Sadece bu isme sahip olanlarla sürü olur
    public int maxGroupSize = 10; // YENÝ: Sürü kaç kiþiyle sýnýrlý olsun?

    public float neighborRadius = 8f;
    public float separationWeight = 1.0f; // Çarpýþmama her zaman aktiftir
    public float alignmentWeight = 1.5f;
    public float cohesionWeight = 2.0f;

    [Header("Oyuncudan Kaçma")]
    public float avoidanceRadius = 5f;
    public float avoidanceWeight = 6f;
}

public class RandomWander : MonoBehaviour
{
    public BoxCollider roamArea;
    public WanderSettings movementSettings;
    public FlockSettings flockSettings;
    public WiggleSettings animSettings;

    private Vector3 targetPosition;
    private bool hasTarget = false;
    private Quaternion baseRotation;
    private Transform playerTransform;

    void Start()
    {
        baseRotation = transform.rotation;
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        GetNewTarget();
    }

    void Update()
    {
        if (hasTarget) MoveTowardsTarget();
    }

    void GetNewTarget()
    {
        if (roamArea == null) return;
        Bounds bounds = roamArea.bounds;
        targetPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        hasTarget = true;
    }

    void MoveTowardsTarget()
    {
        if (Vector3.Distance(transform.position, targetPosition) < movementSettings.reachTolerance) GetNewTarget();

        Vector3 wanderDirection = (targetPosition - transform.position).normalized;
        Vector3 finalDirection = wanderDirection;

        // --- OYUNCUDAN KAÇMA ---
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < flockSettings.avoidanceRadius)
            {
                finalDirection += (transform.position - playerTransform.position).normalized * flockSettings.avoidanceWeight;
            }
        }

        // --- TÜR BAZLI VE SINIRLI SÜRÜ MANTIÐI ---
        if (flockSettings.isSchooling)
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int sameSpeciesCount = 0;

            Collider[] nearby = Physics.OverlapSphere(transform.position, flockSettings.neighborRadius);
            foreach (Collider col in nearby)
            {
                if (col.gameObject == gameObject) continue;

                // 1. Çarpýþma önleme (Separation) HERKESE karþý yapýlýr (Ýç içe geçmemek için)
                Vector3 diff = transform.position - col.transform.position;
                separation += diff.normalized / diff.magnitude;

                // 2. Sadece KENDÝ TÜRÜ ile sürü olma ve SAYI SINIRI kontrolü
                if (col.CompareTag("Fish"))
                {
                    RandomWander otherFish = col.GetComponent<RandomWander>();
                    if (otherFish != null && otherFish.flockSettings.speciesName == flockSettings.speciesName)
                    {
                        if (sameSpeciesCount < flockSettings.maxGroupSize)
                        {
                            sameSpeciesCount++;
                            alignment += col.transform.forward;
                            cohesion += col.transform.position;
                        }
                    }
                }
            }

            if (sameSpeciesCount > 0)
            {
                alignment /= sameSpeciesCount;
                cohesion = (cohesion / sameSpeciesCount) - transform.position;
                finalDirection += (separation * flockSettings.separationWeight) +
                                 (alignment.normalized * flockSettings.alignmentWeight) +
                                 (cohesion.normalized * flockSettings.cohesionWeight);
            }
        }

        // Hareket Uygulama
        if (finalDirection != Vector3.zero)
        {
            Quaternion targetLook = Quaternion.LookRotation(finalDirection.normalized);
            baseRotation = Quaternion.Slerp(baseRotation, targetLook, Time.deltaTime * movementSettings.rotationSpeed);
        }

        float wiggle = Mathf.Sin(Time.time * animSettings.wiggleSpeed) * animSettings.wiggleAngle;
        transform.rotation = baseRotation * Quaternion.Euler(0, wiggle, 0);
        transform.position += baseRotation * Vector3.forward * movementSettings.moveSpeed * Time.deltaTime;
    }
}

[System.Serializable]
public class WiggleSettings
{
    public float wiggleAngle = 15f;
    public float wiggleSpeed = 5f;
}