using UnityEngine;

public class PipeArray : MonoBehaviour
{
    public GameObject pipePrefab; // Dizilecek boru modeli
    public int count = 10;        // Kaç tane dizilsin?
    public Vector3 offset = new Vector3(0, 0, 2f); // Aralarındaki mesafe

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = transform.position + (offset * i);
            Instantiate(pipePrefab, spawnPos, transform.rotation, transform);
        }
    }
}