using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;

    [Header("Maps")]
    public GameObject[] maps;

    [Header("Characters")]
    public GameObject[] characters;

    [Header("Invisiables")]
    public GameObject[] targets;

    private void Awake()
    {
        Instance = this;
    }

    public void SetupMatch(MatchLoadingInfoData loadingData)
    {
        if (maps.Length == 0)
        {
            Debug.LogError("등록된 맵이 없습니다!");
            return;
        }

        // 맵 생성 (0번 고정) 및 스폰 포인트 정보 획득
        GameObject spawnedMap = Instantiate(maps[0], Vector3.zero, Quaternion.identity);
        MapInfo mapInfo = spawnedMap.GetComponent<MapInfo>();

        if (mapInfo == null)
        {
            Debug.LogError("맵 프리팹에 MapInfo 스크립트가 없습니다!");
            return;
        }

        // RoundManager에 스폰 포인트 포워딩
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.attackSpawns = mapInfo.attackSpawnPoints;
            RoundManager.Instance.defendSpawns = mapInfo.defendSpawnPoints;
        }

        // 내가 어떤 팀(공격/수비)에 속해있는지 확인
        bool isMyTeamAttack = CheckIfIAmAttack(loadingData);

        // 공격팀 생성
        SpawnTeam(loadingData.attackMembers, PlayerState.BattleState.Attack, isMyTeamAttack, mapInfo.attackSpawnPoints);

        // 수비팀 생성
        SpawnTeam(loadingData.defenseMembers, PlayerState.BattleState.Defence, !isMyTeamAttack, mapInfo.defendSpawnPoints);

        Debug.Log("모든 캐릭터 생성 및 팀 배치 완료!");
    }
    private bool CheckIfIAmAttack(MatchLoadingInfoData loadingData)
    {
        foreach (var member in loadingData.attackMembers)
        {
            if (member.playerId == myPlayerId) return true;
        }
        return false;
    }

    // 팀 생성 핵심 로직
    private void SpawnTeam(List<MatchLoadingMemberData> members, PlayerState.BattleState role, bool isMyTeam, Transform[] spawnPoints)
    {
        for (int i = 0; i < members.Count; i++)
        {
            MatchLoadingMemberData member = members[i];

            if (member.characterId < 0 || member.characterId >= characters.Length)
            {
                Debug.LogError($"[MatchManager] 잘못된 캐릭터 ID: {member.characterId}");
                continue;
            }

            // 순서대로 스폰 포인트 지정
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];

            // 캐릭터 프리팹 생성
            GameObject charObj = Instantiate(characters[member.characterId], spawnPoint.position, spawnPoint.rotation);
            charObj.name = $"[{role}] {member.nickname}";

            // 태그 지정: 나와 같은 팀이면 "Player", 적 팀이면 "Enemy"
            charObj.tag = isMyTeam ? "Player" : "Enemy";

            // PlayerState 기본 정보 설정
            PlayerState pState = charObj.GetComponent<PlayerState>();
            if (pState != null)
            {
                pState.state = role;
            }

            // 내가 조종할 캐릭터가 아니라면 조작 컴포넌트를 모두 차단
            if (member.playerId != myPlayerId)
            {
                DisableControlComponents(charObj);
            }
            else
            {
                Debug.Log($"내 캐릭터 생성 완료! (ID: {member.playerId}, 이름: {member.nickname})");
            }
        }
    }

    // 본인 제외 다른 캐릭터들의 입력을 차단하는 함수
    private void DisableControlComponents(GameObject target)
    {
        // 조작 스크립트들을 비활성화
        if (target.TryGetComponent(out PlayerMovement move)) move.enabled = false;
        if (target.TryGetComponent(out PlayerItem item)) item.enabled = false;
        if (target.TryGetComponent(out PlayerSpike spike)) spike.enabled = false;

        // 유니티 자체 Input System 컴포넌트가 있다면 비활성화 (추후 변경 예정)
        if (target.TryGetComponent(out UnityEngine.InputSystem.PlayerInput input)) input.enabled = false;

        // 다른 유닛의 카메라는 꺼야 내 화면이 정상적으로 나옵니다.
        CinemachineCamera childCamera = target.GetComponentInChildren<CinemachineCamera>();
        if (childCamera != null) childCamera.gameObject.SetActive(false);

        AudioListener listener = target.GetComponentInChildren<AudioListener>();
        if (listener != null) listener.enabled = false;
    }
}