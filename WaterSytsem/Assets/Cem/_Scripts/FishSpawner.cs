using UnityEngine;
using System.Collections.Generic;

// HATAYA SEBEP OLAN EKSÝK KISIM BURASIYDI
// Inspector'da listeyi görebilmemiz için bu sýnýfýn tanýmlý olmasý þart
[System.Serializable]
public class FishSpawnGroup
{
    public GameObject fishPrefab;
    public int spawnCount;
}

public class FishSpawner : MonoBehaviour
{
    public FishSpawnGroup[] fishGroups;
    public BoxCollider spawnAndRoamArea;

    [Tooltip("Balýklarýn baþlangýçta birbirine en fazla ne kadar yaklaþabileceði")]
    public float minSpawnSpacing = 2f;

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
        List<Vector3> spawnedPositions = new List<Vector3>();

        foreach (FishSpawnGroup group in fishGroups)
        {
            if (group.fishPrefab == null) continue;

            for (int i = 0; i < group.spawnCount; i++)
            {
                Vector3 spawnPos = GetRandomPos(bounds, spawnedPositions);
                GameObject newFish = Instantiate(group.fishPrefab, spawnPos, Quaternion.identity);

                RandomWander wander = newFish.GetComponent<RandomWander>();
                if (wander != null)
                {
                    wander.roamArea = spawnAndRoamArea;
                    // OTOMATÝK TÜR ATAMA: Prefab ismini tür ismi yapýyoruz
                    wander.flockSettings.speciesName = group.fishPrefab.name;
                }
                newFish.transform.SetParent(this.transform);
                spawnedPositions.Add(spawnPos);
            }
        }
    }

    // Eþit daðýlým için rastgele ama diðerlerine uzak nokta bulma fonksiyonu
    Vector3 GetRandomPos(Bounds b, List<Vector3> existing)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 p = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );

            bool ok = true;
            foreach (Vector3 ex in existing)
            {
                if (Vector3.Distance(p, ex) < minSpawnSpacing)
                {
                    ok = false;
                    break;
                }
            }
            if (ok) return p;
        }

        // 30 denemede yer bulamazsa mecburen tamamen rastgele bir yer ver
        return new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), Random.Range(b.min.z, b.max.z));
    }
}