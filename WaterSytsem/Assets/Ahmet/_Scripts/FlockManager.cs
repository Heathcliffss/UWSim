using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public GameObject fishPrefab; // Buraya hazırladığın Prefab'ı sürükle
    public int fishCount = 20;
    public Vector3 areaLimits = new Vector3(5, 3, 5); // Balıkların dağılacağı alan

    public GameObject[] allFish;
    public Vector3 goalPos; // Sürünün genel hedefi

    void Start()
    {
        allFish = new GameObject[fishCount];
        for (int i = 0; i < fishCount; i++)
        {
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-areaLimits.x, areaLimits.x),
                Random.Range(-areaLimits.y, areaLimits.y),
                Random.Range(-areaLimits.z, areaLimits.z)
            );

            allFish[i] = Instantiate(fishPrefab, randomPos, Quaternion.identity);
            // Balığa yöneticiyi tanıtıyoruz (Aşağıdaki script için gerekli)
            allFish[i].GetComponent<FlockUnit>().myManager = this;
        }
    }

    void Update()
    {
        // Ara ara sürünün hedef noktasını değiştir (Rastgele gezinme)
        if (Random.Range(0, 1000) < 10)
        {
            goalPos = transform.position + new Vector3(
                Random.Range(-areaLimits.x, areaLimits.x),
                Random.Range(-areaLimits.y, areaLimits.y),
                Random.Range(-areaLimits.z, areaLimits.z)
            );
        }
    }
}