using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;

    [Header("Top Info")]
    [SerializeField] private TopInfoUI topInfoUI;

    [Header("Panels")]
    [SerializeField] private GameObject playerPickPanel;
    [SerializeField] private MatchLoadingPanelUI matchLoadingPanelUI;
    [SerializeField] private GameObject inGameUIPanel;

    [Header("Maps")]
    [SerializeField] private GameObject[] maps;

    [Header("Characters")]
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Sprite[] characterSprites;

    [Header("Team List UI")]
    [SerializeField] private GameObject ourTeamListRoot;
    [SerializeField] private GameObject enemyTeamListRoot;
    [SerializeField] private Image[] ourTeamImages;
    [SerializeField] private Image[] enemyTeamImages;

    [Header("Invisiables")]
    [SerializeField] private GameObject[] targets;
    [SerializeField] private GameObject gameUIs;

    private GameObject spawnedMap;
    private MapInfo currentMapInfo;

    private byte myTeam = 0;
    private bool matchStarted = false;
    private bool charactersSpawned = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SetMatchManager(this);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SetMatchManager(null);

        if (Instance == this)
            Instance = null;

        SetTeamListVisible(false);
    }

    public void OnRecvGameAllReady(GameAllReadyData data)
    {
        if (data == null)
        {
            Debug.LogError("[MATCH MANAGER] GameAllReadyData is null");
            return;
        }

        Debug.Log($"[MATCH MANAGER] Game all ready received. memberCount={data.members.Count}");

        for (int i = 0; i < data.members.Count; i++)
        {
            GameReadyMemberData member = data.members[i];

            Debug.Log(
                $"[GAME MEMBER] index={i}, " +
                $"playerId={member.playerId}, " +
                $"userId={member.userId}, " +
                $"nickname={member.nickname}, " +
                $"team={member.team}, " +
                $"slot={member.slotIndex}, " +
                $"characterId={member.characterId}"
            );
        }

        matchStarted = true;

        FindMyTeam(data);

        if (playerPickPanel != null)
            playerPickPanel.SetActive(false);

        if (matchLoadingPanelUI != null)
            matchLoadingPanelUI.Hide();

        if (inGameUIPanel != null)
            inGameUIPanel.SetActive(true);

        StartCoroutine(ScreenFader.Instance.FadeOut(1f));
        StartCoroutine(ScreenFader.Instance.FadeIn(1f));

        SetGamePlayVisibility(true);

        SetTeamListVisible(true);

        if (topInfoUI != null)
        {
            topInfoUI.gameObject.SetActive(true);
            topInfoUI.ApplyGameStartInfo();
            topInfoUI.SetTimer(data.prepareSeconds);
        }

        SpawnMapIfNeeded();

        if (currentMapInfo == null)
        {
            Debug.LogError("[MATCH MANAGER] currentMapInfo is null. Cannot spawn characters.");
            return;
        }

        ApplyTeamCharacterImages(data);
        SpawnCharacters(data);

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnServerPrepareStarted();

        Debug.Log("[MATCH MANAGER] In-game setup complete");
    }

    public void OnRecvGamePrepareTimer(ushort remainingSeconds)
    {
        if (!matchStarted)
            return;

        if (topInfoUI != null)
            topInfoUI.SetTimer(remainingSeconds);

        if (remainingSeconds == 0)
        {
            if (RoundManager.Instance != null)
                RoundManager.Instance.OnServerPrepareFinished();
        }
    }

    public void OnRecvScoreUpdate(byte blueScore, byte redScore)
    {
        if (topInfoUI != null)
            topInfoUI.SetScore(blueScore, redScore);
    }

    private void SpawnMapIfNeeded()
    {
        if (spawnedMap != null)
            return;

        if (maps == null || maps.Length == 0 || maps[0] == null)
        {
            Debug.LogError("[MATCH MANAGER] 등록된 맵이 없습니다.");
            return;
        }

        spawnedMap = Instantiate(maps[0], Vector3.zero, Quaternion.identity);
        currentMapInfo = spawnedMap.GetComponent<MapInfo>();

        if (currentMapInfo == null)
        {
            Debug.LogError("[MATCH MANAGER] 맵 프리팹에 MapInfo가 없습니다.");
            return;
        }

        if (RoundManager.Instance != null)
            RoundManager.Instance.SetMapInfo(currentMapInfo);

        Debug.Log($"[MATCH MANAGER] Map spawned. map={spawnedMap.name}");
    }

    private void FindMyTeam(GameAllReadyData data)
    {
        uint myPlayerId = GetMyPlayerId();

        for (int i = 0; i < data.members.Count; i++)
        {
            GameReadyMemberData member = data.members[i];

            if (member.playerId == myPlayerId)
            {
                myTeam = member.team;
                Debug.Log($"[MATCH MANAGER] My playerId={myPlayerId}, myTeam={myTeam}");
                return;
            }
        }

        Debug.LogWarning($"[MATCH MANAGER] 내 playerId를 찾지 못했습니다. myPlayerId={myPlayerId}");
    }

    private void ApplyTeamCharacterImages(GameAllReadyData data)
    {
        ClearTeamImages(ourTeamImages);
        ClearTeamImages(enemyTeamImages);

        for (int i = 0; i < data.members.Count; i++)
        {
            GameReadyMemberData member = data.members[i];
            Sprite sprite = GetCharacterSprite(member.characterId);

            if (member.team == myTeam)
            {
                SetTeamImage(ourTeamImages, member.slotIndex, sprite);
            }
            else
            {
                SetTeamImage(enemyTeamImages, member.slotIndex, sprite);
            }
        }
    }

    private void SpawnCharacters(GameAllReadyData data)
    {
        if (currentMapInfo == null)
        {
            Debug.LogError("[MATCH MANAGER] currentMapInfo is null. Spawn failed.");
            return;
        }

        if (data == null || data.members == null)
        {
            Debug.LogError("[MATCH MANAGER] GameAllReadyData members is null.");
            return;
        }

        for (int i = 0; i < data.members.Count; i++)
        {
            GameReadyMemberData member = data.members[i];

            GameObject prefab = GetCharacterPrefab(member.characterId);
            Transform spawnPoint = GetSpawnPoint(member.team, member.slotIndex);

            Debug.Log(
                $"[SPAWN TRY] " +
                $"playerId={member.playerId}, " +
                $"userId={member.userId}, " +
                $"nickname={member.nickname}, " +
                $"team={member.team}, " +
                $"slot={member.slotIndex}, " +
                $"characterId={member.characterId}, " +
                $"prefab={(prefab != null ? prefab.name : "NULL")}, " +
                $"spawnPoint={(spawnPoint != null ? spawnPoint.name : "NULL")}"
            );

            if (prefab == null)
            {
                Debug.LogError($"[SPAWN FAIL] prefab is null. characterId={member.characterId}");
                continue;
            }

            if (spawnPoint == null)
            {
                Debug.LogError($"[SPAWN FAIL] spawnPoint is null. team={member.team}, slot={member.slotIndex}");
                continue;
            }

            if (string.IsNullOrEmpty(member.userId))
            {
                Debug.LogError(
                    $"[SPAWN FAIL] userId is empty. " +
                    $"playerId={member.playerId}, " +
                    $"nickname={member.nickname}, " +
                    $"team={member.team}, " +
                    $"slot={member.slotIndex}"
                );

                continue;
            }

            GameObject characterObj = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            characterObj.name = MakePlayerObjectName(member);

            PlayerIdentity identity = characterObj.GetComponent<PlayerIdentity>();

            if (identity == null)
                identity = characterObj.GetComponentInChildren<PlayerIdentity>(true);

            if (identity == null)
            {
                Debug.LogError($"[SPAWN FAIL] PlayerIdentity not found. object={characterObj.name}");
                Destroy(characterObj);
                continue;
            }

            identity.PlayerId = member.playerId;
            identity.UserId = member.userId;
            identity.Nickname = member.nickname;
            identity.Team = member.team;
            identity.SlotIndex = member.slotIndex;
            identity.CharacterId = member.characterId;

            Debug.Log(
                $"[IDENTITY SET] " +
                $"object={characterObj.name}, " +
                $"playerId={identity.PlayerId}, " +
                $"userId={identity.UserId}, " +
                $"nickname={identity.Nickname}, " +
                $"team={identity.Team}, " +
                $"slot={identity.SlotIndex}, " +
                $"characterId={identity.CharacterId}"
            );

            bool isMine = member.userId == NetworkManager.Instance.MyUserId;
            bool isMyTeam = member.team == myTeam;

            characterObj.tag = isMyTeam ? "Player" : "Enemy";

            if (isMine)
            {
                characterObj.layer = 6;

                CinemachineCamera cam = characterObj.GetComponentInChildren<CinemachineCamera>(true);

                if (cam == null)
                {
                    Debug.LogError($"[SPAWN FAIL] CinemachineCamera not found. object={characterObj.name}");
                }
                else
                {
                    cam.Priority = 10;
                }
            }

            PlayerState playerState = characterObj.GetComponent<PlayerState>();

            if (playerState == null)
                playerState = characterObj.GetComponentInChildren<PlayerState>(true);

            if (playerState != null)
            {
                if (member.team == 0)
                    playerState.state = PlayerState.BattleState.Attack;
                else
                    playerState.state = PlayerState.BattleState.Defence;
            }

            if (!isMine)
            {
                DisableControlComponents(characterObj);
            }
            else
            {
                Debug.Log(
                    $"[SPAWN MINE] " +
                    $"playerId={member.playerId}, " +
                    $"userId={member.userId}, " +
                    $"nickname={member.nickname}, " +
                    $"team={member.team}, " +
                    $"slot={member.slotIndex}"
                );
            }

            Debug.Log($"[SPAWN SUCCESS] {characterObj.name}");
        }
    }

    private Transform GetSpawnPoint(byte team, byte slotIndex)
    {
        if (currentMapInfo == null)
        {
            Debug.LogError("[MATCH MANAGER] currentMapInfo is null");
            return null;
        }

        return currentMapInfo.GetTeamSlotSpawnPoint(team, slotIndex);
    }

    private void DisableControlComponents(GameObject target)
    {
        if (target == null)
            return;

        /*
        PlayerMovement movement = target.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerItem item = target.GetComponent<PlayerItem>();
        if (item != null)
            item.enabled = false;

        PlayerSpike spike = target.GetComponent<PlayerSpike>();
        if (spike != null)
            spike.enabled = false;

        UnityEngine.InputSystem.PlayerInput input = target.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null)
            input.enabled = false;
        */

        CinemachineCamera childCamera = target.GetComponentInChildren<CinemachineCamera>();
        if (childCamera != null)
            childCamera.Priority = 1;

        AudioListener listener = target.GetComponentInChildren<AudioListener>();
        if (listener != null)
            listener.enabled = false;
    }

    private void ClearTeamImages(Image[] images)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;

            images[i].enabled = false;
            images[i].sprite = null;
        }
    }

    private void SetTeamImage(Image[] images, byte slotIndex, Sprite sprite)
    {
        if (images == null)
            return;

        if (slotIndex >= images.Length)
            return;

        if (images[slotIndex] == null)
            return;

        images[slotIndex].enabled = sprite != null;
        images[slotIndex].sprite = sprite;
    }

    private Sprite GetCharacterSprite(int characterId)
    {
        if (characterSprites == null)
            return null;

        if (characterId < 0 || characterId >= characterSprites.Length)
            return null;

        return characterSprites[characterId];
    }

    private GameObject GetCharacterPrefab(int characterId)
    {
        if (characterPrefabs == null)
            return null;

        if (characterId < 0 || characterId >= characterPrefabs.Length)
            return null;

        return characterPrefabs[characterId];
    }

    private uint GetMyPlayerId()
    {
        if (NetworkManager.Instance == null)
            return 0;

        return NetworkManager.Instance.MyPlayerId;
    }

    private void SetTeamListVisible(bool visible)
    {
        if (ourTeamListRoot != null)
            ourTeamListRoot.SetActive(visible);

        if (enemyTeamListRoot != null)
            enemyTeamListRoot.SetActive(visible);
    }

    private void SetGamePlayVisibility(bool isGameStarting)
    {
        // Invisiables(targets) 애들은 게임 시작 시 꺼줌 (isGameStarting이 true일 때 false로 세팅)
        if (targets != null)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].SetActive(!isGameStarting);
            }
            Debug.Log($"[MATCH MANAGER] Invisiables(targets) 세팅 완료. Active = {!isGameStarting}");
        }

        // 기본 인게임 메인 판넬 활성화
        if (inGameUIPanel != null)
        {
            inGameUIPanel.SetActive(isGameStarting);
        }

        // 3. gameUIs 배열에 등록된 개별 UI 요소들을 일괄적으로 켜줌
        if (gameUIs != null)
        {
            gameUIs.SetActive(isGameStarting);
            Debug.Log($"[MATCH MANAGER] gameUIs 켜기 완료. Active = {isGameStarting}");
        }
    }

    private string MakePlayerObjectName(GameReadyMemberData member)
    {
        return $"T{member.team}_{member.userId}_S{member.slotIndex}_C{member.characterId}";
    }
}