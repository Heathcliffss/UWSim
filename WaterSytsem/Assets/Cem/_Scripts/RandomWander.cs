using UnityEngine;

public class RandomWander : MonoBehaviour
{
    public BoxCollider roamArea;

    [Header("Hareket Ayarlarý")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 2f;
    public float minSwimDistance = 10f;
    public float reachTolerance = 2f;

    [Header("Sürü (Boids) Ayarlarý")]
    public bool isSchooling = true; // Sürüye katýlsýn mý?
    public float neighborRadius = 4f; // Diðer balýklarý görme mesafesi
    public float separationWeight = 1.5f; // Birbirinden kaçma gücü
    public float alignmentWeight = 1.0f;  // Sürüyle hizalanma gücü
    public float cohesionWeight = 1.0f;   // Sürü merkezine çekilme gücü

    [Header("Görsel Dalgalanma")]
    public float wiggleAngle = 15f;
    public float wiggleSpeed = 5f;

    private Vector3 targetPosition;
    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
        GetNewTarget();
    }

    void Update()
    {
        MoveTowardsTarget();
    }

    void GetNewTarget()
    {
        Bounds b = roamArea.bounds;
        Vector3 pos;
        int safety = 0;
        do
        {
            pos = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), Random.Range(b.min.z, b.max.z));
            safety++;
        } while (Vector3.Distance(transform.position, pos) < minSwimDistance && safety < 10);
        targetPosition = pos;
    }

    void MoveTowardsTarget()
    {
        if (Vector3.Distance(transform.position, targetPosition) < reachTolerance) GetNewTarget();

        // 1. Temel Hedef Yönü
        Vector3 desiredDir = (targetPosition - transform.position).normalized;

        // 2. Sürü Hesaplamalarý
        if (isSchooling)
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int neighbors = 0;

            Collider[] cols = Physics.OverlapSphere(transform.position, neighborRadius);
            foreach (var col in cols)
            {
                if (col.gameObject != gameObject && col.CompareTag("Fish"))
                {
                    neighbors++;
                    Vector3 diff = transform.position - col.transform.position;
                    separation += diff.normalized / diff.magnitude; // Mesafe kýsaysa itme artar
                    alignment += col.transform.forward;
                    cohesion += col.transform.position;
                }
            }

            if (neighbors > 0)
            {
                alignment /= neighbors;
                cohesion = (cohesion / neighbors) - transform.position;

                // Tüm kuvvetleri birleþtir
                desiredDir += (separation * separationWeight);
                desiredDir += (alignment.normalized * alignmentWeight);
                desiredDir += (cohesion.normalized * cohesionWeight);
                desiredDir = desiredDir.normalized;
            }
        }

        // 3. Yumuþak Dönüþ ve Hareket
        Quaternion targetLook = Quaternion.LookRotation(desiredDir);
        baseRotation = Quaternion.Slerp(baseRotation, targetLook, Time.deltaTime * rotationSpeed);

        float wiggle = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAngle;
        transform.rotation = baseRotation * Quaternion.Euler(0, wiggle, 0);
        transform.position += baseRotation * Vector3.forward * moveSpeed * Time.deltaTime;
    }
}