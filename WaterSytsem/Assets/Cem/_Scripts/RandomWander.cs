using UnityEngine;

// Verilerin Inspector panelinde düzenli ve görünür olmasý için grupluyoruz
[System.Serializable]
public class WanderSettings
{
    public float moveSpeed = 2f;
    public float rotationSpeed = 2f;
    public float minSwimDistance = 5f;

    [Tooltip("Balýðýn hedefe ulaþtýðýný kabul edeceði mesafe. Kendi etrafýnda dönerse bu deðeri artýrýn.")]
    public float reachTolerance = 1.5f;
}

[System.Serializable]
public class WiggleSettings
{
    public float wiggleAngle = 20f;
    public float wiggleSpeed = 5f;
}

public class RandomWander : MonoBehaviour
{
    public BoxCollider roamArea;

    [Space(10)]
    public WanderSettings movementSettings;
    public WiggleSettings animSettings;

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
        // 1. Hedefe ulaþtýysak beklemeden anýnda yeni hedef seç
        if (Vector3.Distance(transform.position, targetPosition) < movementSettings.reachTolerance)
        {
            GetNewTarget(); 
        }

        // 2. Yönelme ve Dönüþ Hesaplamasý
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion targetLook = Quaternion.LookRotation(direction);
        
        baseRotation = Quaternion.Slerp(baseRotation, targetLook, Time.deltaTime * movementSettings.rotationSpeed);

        // 3. Kuyruk Sallama Efekti
        float wiggleOffset = Mathf.Sin(Time.time * animSettings.wiggleSpeed) * animSettings.wiggleAngle;
        transform.rotation = baseRotation * Quaternion.Euler(0, wiggleOffset, 0);

        // 4. Sürekli Ýleri Hareket (Hiç duraksamaz)
        transform.position += baseRotation * Vector3.forward * movementSettings.moveSpeed * Time.deltaTime;
    }
    
}