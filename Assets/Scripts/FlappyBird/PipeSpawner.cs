
using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject pipePrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnX = 10f;
    [SerializeField] private float minY = -1.5f;
    [SerializeField] private float maxY = 1.5f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            float randomY = Random.Range(minY, maxY);

            Instantiate(
                pipePrefab,
                new Vector3(spawnX, randomY, 0f),
                Quaternion.identity
            );

            timer = 0f;
        }
    }
}
