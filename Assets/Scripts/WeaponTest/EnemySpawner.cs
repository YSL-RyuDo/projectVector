using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 필요
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct DifficultySettings
    {
        public string name;      // "Easy", "Hard" 등
        public float activeTime; // 적이 머무는 시간
        public float spawnDelay; // 다음 적 생성 간격
    }

    [Header("난이도 데이터")]
    public DifficultySettings[] difficulties;

    [Header("연결 설정")]
    public GameObject enemyObject;
    public BoxCollider spawnArea;
    public TMP_Text scoreText; // 점수 표시용 텍스트

    [Header("설정")]
    public int maxSpawnCount = 30;    // 총 소환 횟수
    private float currentActiveTime;
    private float currentSpawnDelay;

    private int currentSpawnCount = 0; // 현재까지 소환된 횟수
    private int score = 0;             // 맞춘 개수
    private Coroutine trainingCoroutine;

    public void StartTrainingByDifficulty(string difficultyName)
    {
        // 진행 중인 훈련이 있다면 중지
        if (trainingCoroutine != null) StopCoroutine(trainingCoroutine);

        // 해당 이름의 난이도 설정 찾기
        foreach (var setting in difficulties)
        {
            if (setting.name.ToLower() == difficultyName.ToLower())
            {
                currentActiveTime = setting.activeTime;
                currentSpawnDelay = setting.spawnDelay;

                Debug.Log($"{setting.name} 난이도로 시작합니다!");
                StartTraining();
                return;
            }
        }
        Debug.LogWarning($"{difficultyName} 난이도를 찾을 수 없습니다.");
    }

    private void StartTraining()
    {
        score = 0;
        currentSpawnCount = 0;
        UpdateUI();
        trainingCoroutine = StartCoroutine(TrainingRoutine());
    }

    IEnumerator TrainingRoutine()
    {
        while (currentSpawnCount < maxSpawnCount)
        {
            yield return new WaitForSeconds(currentSpawnDelay);

            enemyObject.transform.position = GetRandomPositionInBox();
            enemyObject.SetActive(true);
            currentSpawnCount++;

            float timer = 0;
            while (timer < currentActiveTime)
            {
                if (!enemyObject.activeSelf) break;
                timer += Time.deltaTime;
                yield return null;
            }
            enemyObject.SetActive(false);
        }
    }

    public void AddScore()
    {
        score++;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{score} / {maxSpawnCount}";
        }
    }

    Vector3 GetRandomPositionInBox()
    {
        Bounds bounds = spawnArea.bounds;
        return new Vector3(Random.Range(bounds.min.x, bounds.max.x), 1.0f, Random.Range(bounds.min.z, bounds.max.z));
    }
}