using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Dead Player Image")]
    [SerializeField] private Material deadGrayscaleMaterial;

    private readonly Dictionary<Image, Material>
        originalTeamImageMaterials = new();

    [Header("Invisiables")]
    [SerializeField] private GameObject[] targets;
    [SerializeField] private GameObject gameUIs;

    private GameObject spawnedMap;
    private MapInfo currentMapInfo;

    [Header("Camera")]
    [SerializeField] private Camera sceneMainCamera;

    private PlayerCameraController localPlayerCameraController;
    private Transform localPlayerTransform;

    private byte myTeam = 0;
    private bool matchStarted = false;

    private void Awake()
    {
        Instance = this;

        if (sceneMainCamera == null)
        {
            GameObject mainCameraObject =
                GameObject.Find("Main Camera");

            if (mainCameraObject != null)
            {
                sceneMainCamera =
                    mainCameraObject.GetComponent<Camera>();
            }
        }

        if (sceneMainCamera == null)
        {
            Debug.LogError(
                "[MATCH MANAGER] Scene Main Camera가 연결되지 않았습니다."
            );
        }
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

        //StartCoroutine(ScreenFader.Instance.FadeOut(1f));
        StartCoroutine(ScreenFader.Instance.FadeIn(5f));

        SetGamePlayVisibility(true);

        SetTeamListVisible(true);

        if (topInfoUI != null)
        {
            topInfoUI.gameObject.SetActive(true);

            topInfoUI.SetMyTeam(myTeam);

            topInfoUI.ApplyGameStartInfo();
            topInfoUI.ResetMatchScore();
            topInfoUI.HideRoleTextsForGame();

            topInfoUI.SetTimer(data.prepareSeconds);
        }

        SpawnMapIfNeeded(data.mapName);

        if (currentMapInfo == null)
        {
            Debug.LogError("[MATCH MANAGER] currentMapInfo is null. Cannot spawn characters.");
            return;
        }

        // 1라운드 시작 시 바닥 스파이크 생성
        if (SpikeSpawner.Instance != null && currentMapInfo.spikeSpawnPoint != null)
        {
            SpikeSpawner.Instance.ResetSpike(currentMapInfo.spikeSpawnPoint);
            Debug.Log("[MATCH MANAGER] First round dropped spike spawned.");
        }

        ApplyTeamCharacterImages(data);

        localPlayerCameraController = null;
        localPlayerTransform = null;

        // 먼저 모든 캐릭터를 생성한다.
        SpawnCharacters(data);

        // 로컬 플레이어 카메라가 정상적으로 설정됐는지 확인한다.
        if (localPlayerCameraController == null ||
            localPlayerCameraController.PlayerCamera == null ||
            localPlayerTransform == null)
        {
            Debug.LogError(
                "[CAMERA SWITCH] 로컬 플레이어 카메라를 찾지 못했습니다. " +
                $"MyPlayerId={NetworkManager.Instance?.MyPlayerId}. " +
                "안전을 위해 기존 Main Camera를 유지합니다."
            );
        }
        else
        {
            // 로컬 카메라를 다시 한번 확실하게 활성화
            localPlayerCameraController.SetLocalPlayer(true);

            Camera localCamera =
                localPlayerCameraController.PlayerCamera;

            Debug.Log(
                $"[CAMERA SWITCH] 로컬 카메라 준비 완료. " +
                $"player={localPlayerTransform.name}, " +
                $"camera={localCamera.name}, " +
                $"enabled={localCamera.enabled}, " +
                $"active={localCamera.gameObject.activeInHierarchy}"
            );

            // 미니맵 연결
            SetupLocalPlayerMiniMap();

            // 로컬 카메라가 켜진 다음 기존 씬 카메라를 끈다.
            DisableSceneMainCamera();
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnServerPrepareStarted();
        }

        Debug.Log("[MATCH MANAGER] In-game setup complete");
    }

    public void OnRecvGamePrepareTimer(ushort remainingSeconds)
    {
        if (!matchStarted)
            return;

        if (topInfoUI != null)
        {
            topInfoUI.StopLocalCountdown();
            topInfoUI.SetTimer(remainingSeconds);
            topInfoUI.HideRoleTextsForGame();

            if (remainingSeconds > 0)
            {
                topInfoUI.SetGameDescription("구매 단계");
            }
            else
            {
                topInfoUI.SetGameDescription("전투 단계");
            }
        }

        if (RoundManager.Instance == null)
            return;

        if (remainingSeconds > 0)
        {
            RoundManager.Instance.OnServerPrepareStarted();
        }
        else
        {
            RoundManager.Instance.OnServerPrepareFinished();
        }
    }

    public void OnRecvScoreUpdate(byte scoreA, byte scoreB)
    {
        if (topInfoUI != null)
            topInfoUI.SetScoreByTeam(scoreA, scoreB);
    }

    public void OnRecvRoundResult(
    byte winnerTeam,
    byte scoreA,
    byte scoreB,
    byte currentRound,
    bool matchEnd,
    ushort delaySeconds)
    {
        if (topInfoUI != null)
        {
            topInfoUI.SetMyTeam(myTeam);
            topInfoUI.SetScoreByTeam(scoreA, scoreB);

            if (matchEnd)
                topInfoUI.SetMatchWinnerText(winnerTeam);
            else
                topInfoUI.SetRoundWinnerText(winnerTeam);

            topInfoUI.HideRoleTextsForGame();
            topInfoUI.StartLocalCountdown(delaySeconds);
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundResult(
                winnerTeam,
                scoreA,
                scoreB,
                currentRound,
                matchEnd,
                delaySeconds
            );
        }
    }

    public void OnRecvRoundReset(
    byte currentRound,
    byte scoreA,
    byte scoreB)
    {
        if (topInfoUI != null)
        {
            topInfoUI.SetMyTeam(myTeam);
            topInfoUI.SetScoreByTeam(scoreA, scoreB);

            if (currentRound >= 5)
                topInfoUI.SetGameDescription("듀스 라운드");
            else
                topInfoUI.SetGameDescription("구매 단계");

            topInfoUI.SetCurrentRoundRole(currentRound);
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnServerRoundReset(
                currentRound,
                scoreA,
                scoreB
            );
        }

        Debug.Log(
            $"[MATCH MANAGER] Round reset. round={currentRound}, " +
            $"score={scoreA}:{scoreB}"
        );
    }

    private void SpawnMapIfNeeded(string mapName)
    {
        if (spawnedMap != null)
            return;

        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("[MATCH MANAGER] No map prefabs are assigned.");
            return;
        }

        GameObject selectedMap = null;

        for (int i = 0; i < maps.Length; i++)
        {
            GameObject candidate = maps[i];
            if (candidate == null)
                continue;

            if (string.Equals(candidate.name, mapName, System.StringComparison.OrdinalIgnoreCase))
            {
                selectedMap = candidate;
                break;
            }
        }

        if (selectedMap == null)
        {
            selectedMap = maps[0];
            Debug.LogWarning($"[MATCH MANAGER] Unknown map '{mapName}'. Falling back to '{selectedMap?.name}'.");
        }

        if (selectedMap == null)
        {
            Debug.LogError("[MATCH MANAGER] Selected map prefab is null.");
            return;
        }

        spawnedMap = Instantiate(selectedMap, Vector3.zero, Quaternion.identity);
        currentMapInfo = spawnedMap.GetComponent<MapInfo>();

        if (currentMapInfo == null)
        {
            Debug.LogError("[MATCH MANAGER] MapInfo is missing from the selected map prefab.");
            return;
        }

        if (RoundManager.Instance != null)
            RoundManager.Instance.SetMapInfo(currentMapInfo);

        Debug.Log($"[MATCH MANAGER] Map spawned. requested={mapName}, prefab={selectedMap.name}");
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

            PlayerSkinLoadout skinLoadout = characterObj.GetComponent<PlayerSkinLoadout>();
            if (skinLoadout == null)
                skinLoadout = characterObj.GetComponentInChildren<PlayerSkinLoadout>(true);

            if (skinLoadout != null)
                skinLoadout.ApplyServerConfirmedLoadout(member.weaponSkinLoadout);
            else
                Debug.LogWarning($"[WEAPON SKIN LOADOUT] PlayerSkinLoadout not found. playerId={member.playerId}");

            PlayerItem spawnedPlayerItem =
                characterObj.GetComponent<PlayerItem>();

            if (spawnedPlayerItem == null)
            {
                spawnedPlayerItem =
                    characterObj.GetComponentInChildren<PlayerItem>(true);
            }

            if (spawnedPlayerItem != null)
                spawnedPlayerItem.InitializeForMatch();
            else
                Debug.LogWarning($"[PLAYER ITEM] PlayerItem not found. playerId={member.playerId}");

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

            bool isMine =
    member.playerId ==
    NetworkManager.Instance.MyPlayerId;

            bool isMyTeam =
                member.team == myTeam;

            characterObj.tag =
                isMyTeam ? "Player" : "Enemy";

            SetCharacterModelVisible(
                characterObj,
                !isMine
            );

            // 외곽선 설정
            PlayerOutline playerOutline =
                characterObj.GetComponent<PlayerOutline>();

            if (playerOutline == null)
            {
                playerOutline =
                    characterObj.GetComponentInChildren<PlayerOutline>(true);
            }

            if (playerOutline != null)
            {
                playerOutline.Configure(
                    isMine,
                    isMyTeam
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[PLAYER OUTLINE] PlayerOutline을 찾지 못했습니다. " +
                    $"object={characterObj.name}, " +
                    $"playerId={member.playerId}, " +
                    $"isMine={isMine}, " +
                    $"isMyTeam={isMyTeam}"
                );
            }

            PlayerCameraController cameraController =
                characterObj.GetComponent<PlayerCameraController>();

            if (cameraController == null)
                cameraController = characterObj.GetComponentInChildren<PlayerCameraController>(true);

            PlayerMovement movement =
                characterObj.GetComponent<PlayerMovement>();

            if (movement == null)
                movement = characterObj.GetComponentInChildren<PlayerMovement>(true);

            if (cameraController != null)
            {
                cameraController.SetLocalPlayer(isMine);

                if (isMine)
                {
                    localPlayerCameraController =
                        cameraController;

                    localPlayerTransform =
                        characterObj.transform;

                    Debug.Log(
                        $"[LOCAL PLAYER FOUND] " +
                        $"playerId={member.playerId}, " +
                        $"myPlayerId={NetworkManager.Instance.MyPlayerId}, " +
                        $"object={characterObj.name}, " +
                        $"camera={cameraController.PlayerCamera?.name}"
                    );
                }
            }
            else
            {
                Debug.LogError(
                    $"[SPAWN FAIL] PlayerCameraController not found. " +
                    $"object={characterObj.name}"
                );
            }

            if (movement != null)
            {
                movement.SetLocalPlayer(isMine);
            }
            else
            {
                Debug.LogError($"[SPAWN FAIL] PlayerMovement not found. object={characterObj.name}");
            }

            if (isMine)
            {
                Sprite localCharacterSprite = GetCharacterSprite(member.characterId);

                PickCharacterData pickData = null;

                if (PickManager.Instance != null)
                    pickData = PickManager.Instance.GetCharacterData(member.characterId);

                if (InGameUIManager.Instance != null)
                {
                    InGameUIManager.Instance.InitializeLocalPlayerUI(
                        member.characterId,
                        localCharacterSprite,
                        pickData
                    );
                }
                else
                {
                    Debug.LogError("[MATCH MANAGER] InGameUIManager.Instance is null");
                }

                StartCoroutine(RefreshLocalPlayerAmmoAfterSpawn(characterObj)
);

                PlayerSpike playerSpike = characterObj.GetComponent<PlayerSpike>();

                if (playerSpike != null && InGameUIManager.Instance != null)
                {
                    playerSpike.SetSpikeSlider(InGameUIManager.Instance.SpikeSliderObject);
                }

            }
            else
            {
                DisableControlComponents(characterObj);
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

        PlayerCameraController cameraController =
            target.GetComponent<PlayerCameraController>();

        if (cameraController == null)
            cameraController = target.GetComponentInChildren<PlayerCameraController>(true);

        if (cameraController != null)
        {
            cameraController.SetLocalPlayer(false);
        }
        else
        {
            Camera camera = target.GetComponentInChildren<Camera>(true);
            if (camera != null)
                camera.enabled = false;

            AudioListener listener = target.GetComponentInChildren<AudioListener>(true);
            if (listener != null)
                listener.enabled = false;

            FPSCameraLook look = target.GetComponentInChildren<FPSCameraLook>(true);
            if (look != null)
                look.enabled = false;
        }

        ControllerSkill_E skillE = target.GetComponent<ControllerSkill_E>();
        if (skillE != null) skillE.enabled = false;

        ControllerSkill_Q skillQ = target.GetComponent<ControllerSkill_Q>();
        if (skillQ != null) skillQ.enabled = false;

        ControllerSkill_C skillC = target.GetComponent<ControllerSkill_C>();
        if (skillC != null) skillC.enabled = false;

        ControllerSkill_X skillX = target.GetComponent<ControllerSkill_X>();
        if (skillX != null) skillX.enabled = false;
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

    private void SetTeamImage(
    Image[] images,
    byte slotIndex,
    Sprite sprite)
    {
        if (images == null)
            return;

        if (slotIndex >= images.Length)
            return;

        Image image =
            images[slotIndex];

        if (image == null)
            return;

        if (!originalTeamImageMaterials.ContainsKey(
            image))
        {
            originalTeamImageMaterials[image] =
                image.material;
        }

        image.enabled =
            sprite != null;

        image.sprite =
            sprite;

        image.color =
            Color.white;

        image.material =
            originalTeamImageMaterials[image];
    }

    private Sprite GetCharacterSprite(int characterId)
    {
        if (characterSprites == null)
            return null;

        if (characterId < 0 || characterId >= characterSprites.Length)
            return null;

        return characterSprites[characterId];
    }

    public Sprite GetCharacterSpriteForUI(
    int characterId)
    {
        return GetCharacterSprite(
            characterId
        );
    }

    public void SetPlayerDeadVisual(
    uint playerId,
    bool isDead)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        PlayerIdentity target = null;

        for (int i = 0; i < identities.Length; i++)
        {
            PlayerIdentity identity =
                identities[i];

            if (identity == null)
                continue;

            if (identity.PlayerId != playerId)
                continue;

            target = identity;
            break;
        }

        if (target == null)
            return;

        Image[] images =
            target.Team == myTeam
                ? ourTeamImages
                : enemyTeamImages;

        int slotIndex =
            target.SlotIndex;

        if (images == null)
            return;

        if (slotIndex < 0 ||
            slotIndex >= images.Length)
        {
            return;
        }

        Image image =
            images[slotIndex];

        if (image == null)
            return;

        if (!originalTeamImageMaterials.ContainsKey(
            image))
        {
            originalTeamImageMaterials[image] =
                image.material;
        }

        if (isDead)
        {
            if (deadGrayscaleMaterial != null)
            {
                image.material =
                    deadGrayscaleMaterial;

                image.color =
                    Color.white;
            }
            else
            {
                /*
                 * Grayscale Material이 연결되지 않은 경우
                 * 최소한 회색으로 표시한다.
                 */
                image.material =
                    originalTeamImageMaterials[image];

                image.color =
                    Color.gray;
            }
        }
        else
        {
            image.material =
                originalTeamImageMaterials[image];

            image.color =
                Color.white;
        }
    }

    public void ResetAllPlayerDeadVisuals()
    {
        ResetTeamImageVisuals(
            ourTeamImages
        );

        ResetTeamImageVisuals(
            enemyTeamImages
        );
    }

    private void ResetTeamImageVisuals(
        Image[] images)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
        {
            Image image =
                images[i];

            if (image == null)
                continue;

            if (originalTeamImageMaterials.TryGetValue(
                image,
                out Material originalMaterial))
            {
                image.material =
                    originalMaterial;
            }

            image.color =
                Color.white;
        }
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

    private void SetCharacterModelVisible(GameObject characterObj, bool visible)
    {
        if (characterObj == null)
            return;

        Transform modelRoot = characterObj.transform.Find("Model");

        if (modelRoot == null)
        {
            Debug.LogWarning($"[MODEL VISIBILITY] Model child not found. object={characterObj.name}");
            return;
        }

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }

        Debug.Log(
            $"[MODEL VISIBILITY] object={characterObj.name}, " +
            $"visible={visible}, " +
            $"rendererCount={renderers.Length}"
        );
    }

    private void SetupLocalPlayerMiniMap()
    {
        if (localPlayerTransform == null ||
            localPlayerCameraController == null ||
            localPlayerCameraController.PlayerCamera == null)
        {
            Debug.LogError(
                "[MINIMAP SETUP] 로컬 플레이어 또는 카메라가 없습니다."
            );

            return;
        }

        if (MiniMapCamera.Instance != null)
        {
            MiniMapCamera.Instance.SetTarget(
                localPlayerTransform
            );
        }
        else
        {
            Debug.LogError(
                "[MINIMAP SETUP] MiniMapCamera.Instance가 없습니다."
            );
        }

        if (MiniMapManager.Instance != null)
        {
            MiniMapManager.Instance.RefreshZoneTargets();

            MiniMapManager.Instance.RegisterLocalPlayer(
                localPlayerTransform,
                localPlayerCameraController.PlayerCamera
            );

            MiniMapManager.Instance.ShowHUDMiniMap();
        }
        else
        {
            Debug.LogError(
                "[MINIMAP SETUP] MiniMapManager.Instance가 없습니다."
            );
        }
    }

    private void DisableSceneMainCamera()
    {
        if (sceneMainCamera == null)
        {
            Debug.LogError(
                "[CAMERA SWITCH] Scene Main Camera가 없습니다."
            );

            return;
        }

        Camera localCamera =
            localPlayerCameraController != null
                ? localPlayerCameraController.PlayerCamera
                : null;

        if (sceneMainCamera == localCamera)
        {
            Debug.LogError(
                "[CAMERA SWITCH] Scene Camera와 Player Camera가 동일합니다."
            );

            return;
        }

        if (sceneMainCamera.CompareTag("MainCamera"))
            sceneMainCamera.tag = "Untagged";

        AudioListener sceneListener =
            sceneMainCamera.GetComponent<AudioListener>();

        if (sceneListener != null)
            sceneListener.enabled = false;

        // 기존 구조대로 씬 Main Camera 오브젝트를 종료
        sceneMainCamera.gameObject.SetActive(false);

        Debug.Log(
            $"[CAMERA SWITCH] Scene Main Camera 종료. " +
            $"localCamera={localCamera?.name}"
        );
    }

    private IEnumerator RefreshLocalPlayerAmmoAfterSpawn(
    GameObject playerObject)
    {
        yield return null;

        if (playerObject == null)
            yield break;

        PlayerItem playerItem =
            playerObject.GetComponent<PlayerItem>();

        if (playerItem == null)
        {
            playerItem =
                playerObject.GetComponentInChildren<PlayerItem>(true);
        }

        if (playerItem == null)
        {
            Debug.LogError(
                "[INITIAL AMMO UI] PlayerItem not found"
            );

            yield break;
        }

        playerItem.RefreshCurrentWeaponAmmoFromServer();
    }
}