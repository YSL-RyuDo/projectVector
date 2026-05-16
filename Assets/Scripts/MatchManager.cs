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
                $"[SPAWN TRY] playerId={member.playerId}, " +
                $"team={member.team}, slot={member.slotIndex}, " +
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

            GameObject characterObj = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            characterObj.name =
                $"Player_{member.playerId}_T{member.team}_S{member.slotIndex}_C{member.characterId}";

            bool isMine = member.playerId == GetMyPlayerId();
            bool isMyTeam = member.team == myTeam;

            characterObj.tag = isMyTeam ? "Player" : "Enemy";

            PlayerState playerState = characterObj.GetComponent<PlayerState>();
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
                    $"[SPAWN MINE] playerId={member.playerId}, " +
                    $"nickname={member.nickname}, team={member.team}, slot={member.slotIndex}"
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

        CinemachineCamera childCamera = target.GetComponentInChildren<CinemachineCamera>();
        if (childCamera != null)
            childCamera.gameObject.SetActive(false);

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
}