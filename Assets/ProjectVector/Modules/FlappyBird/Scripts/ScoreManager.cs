using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private const string BestScoreKey = "FlappyShooter_BestScore";

    [Header("생존/거리 점수")]
    [SerializeField] private float scrollSpeed = 3f;      // 파이프 이동 속도와 동일하게
    [SerializeField] private float pointsPerUnit = 1f;

    [Header("처치 점수")]
    [SerializeField] private int enemyKillScore = 10;
    [SerializeField] private int obstacleDestroyScore = 5;
    [SerializeField] private int bossKillScore = 100;

    public float Distance { get; private set; }
    public int BonusScore { get; private set; }
    public int Score => Mathf.FloorToInt(Distance * pointsPerUnit) + BonusScore;
    public int BestScore { get; private set; }
    public bool IsPlaying { get; private set; }

    public event Action<int> OnScoreChanged;
    public event Action<int> OnBestScoreChanged;

    private int lastNotifiedScore = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void Start() => StartGame();

    private void Update()
    {
        if (!IsPlaying) return;

        Distance += scrollSpeed * Time.deltaTime;
        NotifyIfChanged();
    }

    public void StartGame()
    {
        Distance = 0f;
        BonusScore = 0;
        IsPlaying = true;
        lastNotifiedScore = -1;
        NotifyIfChanged();
    }

    public void AddEnemyKill() => AddBonus(enemyKillScore);
    public void AddObstacleDestroy() => AddBonus(obstacleDestroyScore);
    public void AddBossKill() => AddBonus(bossKillScore);

    // 난이도 올라가서 속도 바뀔 때 호출
    public void SetScrollSpeed(float speed) => scrollSpeed = speed;

    public void AddBonus(int amount)
    {
        if (!IsPlaying) return;
        BonusScore += amount;
        NotifyIfChanged();
    }

    // 게임오버 생기면 거기서 호출
    public void EndGame()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        SaveBestScore();
    }

    // 점수 바뀔 때만 UI 갱신
    private void NotifyIfChanged()
    {
        int s = Score;
        if (s == lastNotifiedScore) return;
        lastNotifiedScore = s;
        OnScoreChanged?.Invoke(s);

        if (s > BestScore)
        {
            BestScore = s;
            OnBestScoreChanged?.Invoke(BestScore);
        }
    }

    private void SaveBestScore()
    {
        if (BestScore <= PlayerPrefs.GetInt(BestScoreKey, 0)) return;
        PlayerPrefs.SetInt(BestScoreKey, BestScore);
        PlayerPrefs.Save();
    }

    // 플레이 모드 종료, 씬 전환 시 저장
    private void OnDisable() => SaveBestScore();

    [ContextMenu("최고기록 초기화")]
    private void ResetBestScore()
    {
        PlayerPrefs.DeleteKey(BestScoreKey);
        BestScore = 0;
    }
}