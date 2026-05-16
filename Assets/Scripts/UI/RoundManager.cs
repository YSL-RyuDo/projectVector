using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    public enum RoundState { Preparation, MainRound, SpikePlanted, RoundEnd, GameOver }
    public RoundState currentState;

    private Coroutine mainRoundCoroutine;

    public bool CanAttack => currentState != RoundState.Preparation && currentState != RoundState.RoundEnd;

    [Header("Timer Settings")]
    public float prepTime = 20f;
    public float mainRoundTime = 60f;
    public float postPlantTime = 40f;
    public float roundEndTime = 5f;
    private float currentTimer;

    [Header("Score Settings")]
    public int attackScore = 0;
    public int defendScore = 0;
    public int winScore = 9;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statusText; // "준비 단계", "공격팀 승리" 등 표시

    [Header("Spawn Points")]
    public Transform[] attackSpawns;
    public Transform[] defendSpawns;
    public Transform spikeSpawns;

    [Header("Barriers")]
    public GameObject barriers;

    void Awake() => Instance = this;

    void Start()
    {
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while (attackScore < winScore && defendScore < winScore)
        {
            yield return StartCoroutine(PreparationPhase());
            mainRoundCoroutine = StartCoroutine(MainRoundPhase());
            yield return new WaitUntil(() => currentState == RoundState.RoundEnd);
            yield return StartCoroutine(RoundEndPhase());
        }

        // 게임 최종 종료
        currentState = RoundState.GameOver;
        statusText.text = (attackScore >= winScore) ? "공격팀 최종 승리!" : "수비팀 최종 승리!";
    }

    // 준비 단계 (20초)
    IEnumerator PreparationPhase()
    {
        currentState = RoundState.Preparation;
        currentTimer = prepTime;
        statusText.text = "준비 단계 (상점 이용)";
        barriers.SetActive(true);

        while (currentTimer > 0)
        {
            UpdateTimerUI();
            yield return null;
            currentTimer -= Time.deltaTime;
        }
    }

    // 게임 시작 (1분)
    IEnumerator MainRoundPhase()
    {
        currentState = RoundState.MainRound;
        currentTimer = mainRoundTime;
        statusText.text = "라운드 시작!";
        barriers.SetActive(false);   

        while (currentTimer > 0)
        {
            UpdateTimerUI();
            yield return null;
            currentTimer -= Time.deltaTime;
        }

        if (currentState == RoundState.MainRound)
        {
            EndRound(false);
        }
    }

    // 스파이크 설치 시 호출될 함수
    public void OnSpikePlanted()
    {
        if (currentState != RoundState.MainRound) return;
        if (mainRoundCoroutine != null) StopCoroutine(mainRoundCoroutine);

        currentState = RoundState.SpikePlanted;
        currentTimer = postPlantTime; // 타이머를 40초로 리셋
        statusText.text = "스파이크 설치됨!";

        mainRoundCoroutine = StartCoroutine(SpikeTimerRoutine());
    }

    IEnumerator SpikeTimerRoutine()
    {
        while (currentTimer > 0)
        {
            UpdateTimerUI();
            yield return null;
            currentTimer -= Time.deltaTime;
        }

        if (currentTimer <= 0)
        {
            EndRound(true); // 공격팀 승리 (폭파)
        }
    }

    // 라운드 종료 처리
    public void EndRound(bool attackWin)
    {
        if (currentState == RoundState.RoundEnd) return;

        if (attackWin) attackScore++;
        else defendScore++;

        UpdateScoreUI();
        currentState = RoundState.RoundEnd;
        statusText.text = attackWin ? "공격팀 라운드 승리!" : "수비팀 라운드 승리!";

        if (mainRoundCoroutine != null) StopCoroutine(mainRoundCoroutine);
    }

    // 라운드 종료 대기 (5초)
    IEnumerator RoundEndPhase()
    {
        currentTimer = roundEndTime;
        while (currentTimer > 0)
        {
            UpdateTimerUI();
            yield return null;
            currentTimer -= Time.deltaTime;
        }

        // 다음 라운드를 위한 리셋 로직 (플레이어 위치 이동 등)
        ResetRound();
    }

    void UpdateTimerUI()
    {
        int totalSeconds = Mathf.CeilToInt(currentTimer);

        // 분과 초 계산
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        // 0:00 형식으로 변환 (D2는 두 자리 숫자로 맞추고 비어있으면 0을 채운다는 뜻)
        timerText.text = $"{minutes}:{seconds:D2}";
    }

    void UpdateScoreUI()
    {
        scoreText.text = $"{attackScore} : {defendScore}";
    }

    void ResetRound()
    {
        Debug.Log("라운드 리셋 시작!");

        // 필드에 떨어진 스파이크 및 무기 청소 (태그나 레이어로 찾아서 파괴)
        CleanUpDroppedItems();

        // 플레이어 팀 리셋
        ResetTeam("Player");

        // 적 팀 리셋
        ResetTeam("Enemy");

        SpikeSpawner.Instance.ResetSpike(spikeSpawns);
    }

    void ResetTeam(string targetTag)
    {
        // 씬에 있는 해당 태그의 모든 오브젝트 찾기
        // (주의: 죽어서 SetActive(false) 된 오브젝트는 못 찾을 수 있으므로, 
        // 실제 게임에서는 GameManager 리스트에 플레이어들을 미리 담아두고 순회하는 것이 좋습니다.)
        GameObject[] units = GameObject.FindGameObjectsWithTag(targetTag);

        int attackIndex = 0;
        int defendIndex = 0;

        foreach (GameObject unit in units)
        {
            PlayerState state = unit.GetComponent<PlayerState>();
            if (state == null) continue;

            // 역할에 따라 스폰 위치 결정
            Transform spawnPoint = null;
            if (state.state == PlayerState.BattleState.Attack)
            {
                // 인원수만큼 스폰 포인트를 차례대로 배정
                spawnPoint = attackSpawns[attackIndex % attackSpawns.Length];
                attackIndex++;
            }
            else
            {
                spawnPoint = defendSpawns[defendIndex % defendSpawns.Length];
                defendIndex++;
            }

            // 생존 여부에 따라 다른 처리 적용
            if (state.IsDead)
            {
                state.ReviveAndResetInfo(spawnPoint.position);
            }
            else
            {
                state.Relocate(spawnPoint.position);
            }
        }
    }

    void CleanUpDroppedItems()
    {
        // 떨어진 무기 청소
        DroppedItem[] droppedWeapons = FindObjectsByType<DroppedItem>(FindObjectsSortMode.InstanceID);
        foreach (var weapon in droppedWeapons) Destroy(weapon.gameObject);

        // 바닥에 떨어진 스파이크 청소
        Spike[] spikes = FindObjectsByType<Spike>(FindObjectsSortMode.InstanceID);
        foreach (var spike in spikes) Destroy(spike.gameObject);
    }
}

// 우클릭 사용 중 아이템 사용 금지 시키면 다시 원래대로 안돌아감