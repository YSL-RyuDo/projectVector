using UnityEngine;

public class FlappyGameManager : MonoBehaviour
{
    public GameObject enemyPrefab;

    public int pipeCount = 0;
    public int enemySpawnInterval = 5;

    public float minY = -3f;
    public float maxY = 3f;

    public Transform spawnPoint;

    public void AddPipeCount()
    {
        pipeCount++;

        Debug.Log("통과한 파이프: " + pipeCount);

        if (enemySpawnInterval > 0 && pipeCount % enemySpawnInterval == 0)
        {
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        float randomY = Random.Range(minY, maxY);

        Vector3 spawnPosition = new Vector3(
            spawnPoint.position.x,
            randomY,
            spawnPoint.position.z
        );

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}