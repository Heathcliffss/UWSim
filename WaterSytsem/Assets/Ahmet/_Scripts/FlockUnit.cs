using UnityEngine;

public class FlockUnit : MonoBehaviour
{
    public FlockManager myManager;
    private float speed;

    void Start()
    {
        speed = Random.Range(1.5f, 3.0f);
    }

    void Update()
    {
        ApplyBehaviors();
        // Artık Parent kullandığın için Vector3.forward her zaman doğru yöndür!
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void ApplyBehaviors()
    {
        GameObject[] neighbors = myManager.allFish;
        Vector3 averageCenter = Vector3.zero;
        Vector3 avoidanceVector = Vector3.zero;
        int groupSize = 0;

        foreach (GameObject go in neighbors)
        {
            if (go != this.gameObject)
            {
                float dist = Vector3.Distance(go.transform.position, transform.position);
                if (dist <= 3.0f) // Komşu algılama mesafesi
                {
                    averageCenter += go.transform.position;
                    groupSize++;

                    if (dist < 1.0f) // Çok yakınsa kaç (Çarpışma önleme)
                    {
                        avoidanceVector += (transform.position - go.transform.position);
                    }
                }
            }
        }

        if (groupSize > 0)
        {
            averageCenter = (averageCenter / groupSize) + (myManager.goalPos - transform.position);
            Vector3 direction = (averageCenter + avoidanceVector) - transform.position;

            if (direction != Vector3.zero)
            {
                // Yumuşak dönüş
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                     Quaternion.LookRotation(direction),
                                     2.0f * Time.deltaTime);
            }
        }
    }
}