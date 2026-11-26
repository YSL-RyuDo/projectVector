using UnityEngine;

public class FlappyGameManager : MonoBehaviour
{
    [Header("Pipe")]
    public int pipeCount = 0;
    public int pipesPerPhase = 5;
    public int currentPhase = 0;

    [Header("Enemy")]
    public GameObject enemyPrefab;
    public int enemySpawnInterval = 5;

    public float minY = -3f;
    public float maxY = 3f;
    public Transform spawnPoint;

    [Header("Boss")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public int bossPhaseInterval = 3;

    private bool bossActive = false;

    public void AddPipeCount()
    {
        pipeCount++;

        Debug.Log("통과한 파이프: " + pipeCount);

        if (pipesPerPhase > 0 && pipeCount % pipesPerPhase == 0)
        {
            currentPhase++;

            Debug.Log("현재 페이즈: " + currentPhase);

            if (bossPhaseInterval > 0 &&
                currentPhase % bossPhaseInterval == 0)
            {
                SpawnBoss();
            }
        }

        if (!bossActive &&
            enemySpawnInterval > 0 &&
            pipeCount % enemySpawnInterval == 0)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoint == null)
            return;

        float randomY = Random.Range(minY, maxY);

        Vector3 spawnPosition = new Vector3(
            spawnPoint.position.x,
            randomY,
            spawnPoint.position.z
        );

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }

    private void SpawnBoss()
    {
        if (bossActive || bossPrefab == null || bossSpawnPoint == null)
            return;

        Instantiate(
            bossPrefab,
            bossSpawnPoint.position,
            Quaternion.identity
        );

        bossActive = true;
    }

    public void BossDefeated()
    {
        bossActive = false;

        Debug.Log("중간 보스 처치!");
    }
}