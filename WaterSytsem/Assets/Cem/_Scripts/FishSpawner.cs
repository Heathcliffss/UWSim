using UnityEngine;

[System.Serializable]
public class FishSpawnGroup
{
    public GameObject fishPrefab; // Hangi balýk modeli?
    public int spawnCount;        // Bu türden kaç tane üretilecek?
}

public class FishSpawner : MonoBehaviour
{
    [Header("Üretim Ayarlarý")]
    public FishSpawnGroup[] fishGroups; // Balýk türü ve sayýsý listesi

    [Header("Alan Atamasý")]
    public BoxCollider spawnAndRoamArea; // Balýklarýn gezeceði kutu alaný

    void Start()
    {
        SpawnFishes();
    }

    void SpawnFishes()
    {
        if (fishGroups == null || fishGroups.Length == 0 || spawnAndRoamArea == null)
        {
            Debug.LogError("FishSpawner: Gerekli atamalar yapýlmadý!");
            return;
        }

        Bounds bounds = spawnAndRoamArea.bounds;

        foreach (FishSpawnGroup group in fishGroups)
        {
            if (group.fishPrefab == null) continue;

            for (int i = 0; i < group.spawnCount; i++)
            {
                // Rastgele spawn noktasý
                Vector3 spawnPos = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                GameObject newFish = Instantiate(group.fishPrefab, spawnPos, Quaternion.identity);

                // Balýða alaný tanýtýyoruz
                RandomWander wander = newFish.GetComponent<RandomWander>();
                if (wander != null) wander.roamArea = spawnAndRoamArea;

                // Sahne hiyerarþisi düzenli olsun
                newFish.transform.SetParent(this.transform);
            }
        }
    }
}