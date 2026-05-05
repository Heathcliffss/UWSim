using UnityEngine;

// 1. YENÝ: Inspector panelinde hem balýðý hem de sayýsýný yan yana 
// görebilmek için kendi özel sýnýfýmýzý (Class) yaratýyoruz.
[System.Serializable]
public class FishSpawnGroup
{
    [Tooltip("Üretilecek balýk modeli (Prefab)")]
    public GameObject fishPrefab;

    [Tooltip("Bu balýk türünden kaç tane üretilecek?")]
    public int spawnCount;
}

public class FishSpawner : MonoBehaviour
{
    [Header("Üretim Ayarlarý")]
    [Tooltip("Farklý balýk türlerini ve sayýlarýný buradan ayarlayabilirsin")]
    // 2. YENÝ: Artýk sadece GameObject listesi deðil, oluþturduðumuz bu özel sýnýfýn listesini tutuyoruz
    public FishSpawnGroup[] fishGroups;

    [Tooltip("Balýklarýn üretileceði ve gezeceði alan (BoxCollider)")]
    public BoxCollider spawnAndRoamArea;

    void Start()
    {
        SpawnFishes();
    }

    void SpawnFishes()
    {
        if (fishGroups == null || fishGroups.Length == 0 || spawnAndRoamArea == null)
        {
            Debug.LogError("FishSpawner: Lütfen balýk gruplarýný ve BoxCollider atamasýný yapýn!");
            return;
        }

        Bounds bounds = spawnAndRoamArea.bounds;

        // 3. YENÝ: Listedeki her bir balýk "grubu" için döngüye gir
        foreach (FishSpawnGroup group in fishGroups)
        {
            // Eðer prefab yuvasý boþ býrakýlmýþsa hata vermemesi için atla
            if (group.fishPrefab == null) continue;

            // 4. O anki grup için belirlenen sayý (spawnCount) kadar üretim yap
            for (int i = 0; i < group.spawnCount; i++)
            {
                // Kutu içinde rastgele bir konum belirle
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                float randomZ = Random.Range(bounds.min.z, bounds.max.z);
                Vector3 randomSpawnPosition = new Vector3(randomX, randomY, randomZ);

                // Belirtilen balýk modelini üret
                GameObject newFish = Instantiate(group.fishPrefab, randomSpawnPosition, Quaternion.identity);

                // Üretilen balýða gezinme alanýný ata
                RandomWander wanderScript = newFish.GetComponent<RandomWander>();
                if (wanderScript != null)
                {
                    wanderScript.roamArea = spawnAndRoamArea;
                }

                // Hiyerarþiyi düzenli tutmak için üretilenleri bu spawner'ýn içine al
                newFish.transform.SetParent(this.transform);
            }
        }
    }
}