using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance;

    private readonly List<PlayerIdentity> targets = new();

    private int currentIndex;
    private bool isSpectating;

    private PlayerIdentity myPlayer;
    private Camera myCamera;
    private PlayerIdentity currentSpectatorTarget;

    public bool IsSpectating => isSpectating;
    public uint CurrentTargetPlayerId => currentSpectatorTarget != null ? currentSpectatorTarget.PlayerId : 0;

    private Transform savedCameraParent;
    private Vector3 savedCameraLocalPosition;
    private Quaternion savedCameraLocalRotation;
    private float savedCameraFieldOfView;
    private bool hasSavedCameraTransform;

    // 모든 아군이 죽었을 때 유지할 마지막 관전 카메라 월드 상태
    private bool hasLastSpectatorView;
    private bool isHoldingLastSpectatorView;
    private Vector3 lastSpectatorPosition;
    private Quaternion lastSpectatorRotation;
    private float lastSpectatorFieldOfView;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        if (!isSpectating)
            return;

        BuildTargets();

        if (targets.Count == 0)
        {
            HoldLastSpectatorView();
            return;
        }

        isHoldingLastSpectatorView = false;

        if (targets.Count == 1)
            currentIndex = 0;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            NextTarget();

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            PrevTarget();

        FollowTarget();
    }

    public void StartSpectating()
    {
        RestoreSpectatorTarget();

        myPlayer = FindMyPlayer();

        if (myPlayer == null)
        {
            Debug.LogError("[SPECTATOR] 내 플레이어를 찾지 못했습니다.");
            return;
        }

        PlayerCameraController myCameraController = GetCameraController(myPlayer);

        if (myCameraController == null || myCameraController.PlayerCamera == null)
        {
            Debug.LogError("[SPECTATOR] 내 플레이어 카메라를 찾지 못했습니다.");
            return;
        }

        myCameraController.RefreshViewVisuals();

        myCamera = myCameraController.PlayerCamera;

        SaveMyCameraTransform();

        hasLastSpectatorView = false;
        isHoldingLastSpectatorView = false;

        // 부모에서 분리하되 현재 월드 위치는 유지
        myCamera.transform.SetParent(null, true);

        // 관전 대상이 한 명도 없는 경우를 대비해 현재 사망 위치를 저장
        SaveLastSpectatorView();

        if (!myCamera.gameObject.activeSelf)
            myCamera.gameObject.SetActive(true);

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        AudioListener listener = myCamera.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = true;

        isSpectating = true;
        currentIndex = 0;

        BuildTargets();

        if (targets.Count == 0)
        {
            HoldLastSpectatorView();
            Debug.Log("[SPECTATOR] 살아 있는 팀원이 없어 현재 사망 위치의 카메라를 유지합니다.");
            return;
        }

        FollowTarget();

        Debug.Log($"[SPECTATOR] 관전 시작. target={targets[currentIndex].PlayerId}");
    }

    public void StopSpectating()
    {
        // LateUpdate에서 카메라를 다시 옮기지 못하도록 먼저 종료
        isSpectating = false;
        hasLastSpectatorView = false;
        isHoldingLastSpectatorView = false;

        RestoreSpectatorTarget();
        RestoreLocalPlayerUI();
        RestoreMyCameraTransform();

        targets.Clear();
        currentIndex = 0;

        myPlayer = null;
        myCamera = null;

        Debug.Log("[SPECTATOR] 관전 종료 및 카메라 복구");
    }

    private void BuildTargets()
    {
        uint previousTargetId = currentSpectatorTarget != null
            ? currentSpectatorTarget.PlayerId
            : 0;

        targets.Clear();

        PlayerIdentity mine = FindMyPlayer();

        if (mine == null || NetworkManager.Instance == null)
            return;

        PlayerIdentity[] players = FindObjectsByType<PlayerIdentity>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (PlayerIdentity player in players)
        {
            if (player == null)
                continue;

            // 자기 자신 제외
            if (player.PlayerId == NetworkManager.Instance.MyPlayerId)
                continue;

            // 같은 팀만 관전
            if (player.Team != mine.Team)
                continue;

            PlayerDeathController death = player.GetComponent<PlayerDeathController>();

            if (death == null)
                death = player.GetComponentInChildren<PlayerDeathController>(true);

            if (death != null && death.IsDead)
                continue;

            PlayerHP hp = player.GetComponent<PlayerHP>();

            if (hp == null)
                hp = player.GetComponentInChildren<PlayerHP>(true);

            if (hp != null && hp.IsDead)
                continue;

            targets.Add(player);
        }

        if (targets.Count == 0)
        {
            currentIndex = 0;

            // 여기서 RestoreSpectatorTarget을 호출하지 않는다.
            // HoldLastSpectatorView에서 카메라 월드 위치를 고정한 뒤 처리한다.
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].PlayerId == previousTargetId)
            {
                currentIndex = i;
                return;
            }
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, targets.Count - 1);
    }

    private PlayerIdentity FindMyPlayer()
    {
        if (NetworkManager.Instance == null)
            return null;

        uint myPlayerId = NetworkManager.Instance.MyPlayerId;

        PlayerIdentity[] players = FindObjectsByType<PlayerIdentity>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (PlayerIdentity player in players)
        {
            if (player != null && player.PlayerId == myPlayerId)
                return player;
        }

        Debug.LogError(
            $"[SPECTATOR] 내 플레이어를 찾지 못했습니다. " +
            $"myPlayerId={myPlayerId}, count={players.Length}"
        );

        return null;
    }

    private void FollowTarget()
    {
        if (myCamera == null || targets.Count == 0)
            return;

        if (currentIndex < 0 || currentIndex >= targets.Count)
            currentIndex = 0;

        PlayerIdentity target = targets[currentIndex];

        if (target == null)
            return;

        isHoldingLastSpectatorView = false;

        ApplySpectatorTarget(target);

        PlayerCameraController targetController = GetCameraController(target);

        if (targetController == null || targetController.PlayerCamera == null)
        {
            Debug.LogWarning($"[SPECTATOR] target camera missing. playerId={target.PlayerId}");
            return;
        }

        Camera targetCamera = targetController.PlayerCamera;

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        myCamera.transform.SetPositionAndRotation(
            targetCamera.transform.position,
            targetCamera.transform.rotation
        );

        myCamera.fieldOfView = targetCamera.fieldOfView;

        // 관전 중인 대상의 카메라 월드 상태를 계속 저장
        SaveLastSpectatorView();
    }

    private void ApplySpectatorTarget(PlayerIdentity newTarget)
    {
        if (newTarget == null)
            return;

        if (currentSpectatorTarget == newTarget)
            return;

        RestoreSpectatorTarget();

        PlayerDeathController death = newTarget.GetComponent<PlayerDeathController>();

        if (death == null)
            death = newTarget.GetComponentInChildren<PlayerDeathController>(true);

        if (death != null && death.IsDead)
        {
            Debug.LogWarning(
                $"[SPECTATOR] 죽은 플레이어는 관전할 수 없습니다. " +
                $"playerId={newTarget.PlayerId}"
            );

            return;
        }

        PlayerCameraController targetController = GetCameraController(newTarget);

        if (targetController == null)
        {
            Debug.LogWarning(
                $"[SPECTATOR] PlayerCameraController가 없습니다. " +
                $"playerId={newTarget.PlayerId}"
            );

            return;
        }

        currentSpectatorTarget = newTarget;

        targetController.SetSpectatorView(true);

        RefreshCurrentTargetState();

        PlayerItem targetItem = newTarget.GetComponent<PlayerItem>();

        if (targetItem == null)
            targetItem = newTarget.GetComponentInChildren<PlayerItem>(true);

        if (targetItem != null)
            targetItem.RefreshRemoteReloadVisual();

        Debug.Log(
            $"[SPECTATOR TARGET APPLY] " +
            $"playerId={newTarget.PlayerId}, " +
            $"firstPersonWeapon=true, model=false"
        );
    }

    public void NotifyPlayerStateChanged(uint playerId)
    {
        if (!isSpectating || currentSpectatorTarget == null)
            return;

        if (currentSpectatorTarget.PlayerId != playerId)
            return;

        RefreshCurrentTargetState();
    }

    private void RefreshCurrentTargetState()
    {
        if (!isSpectating || currentSpectatorTarget == null || NetworkManager.Instance == null)
            return;

        PlayerIdentity target = currentSpectatorTarget;

        if (!NetworkManager.Instance.TryGetCombatSnapshot(
            target.PlayerId,
            out PlayerCombatSnapshot snapshot))
        {
            Debug.LogWarning($"[SPECTATOR STATE] snapshot missing. playerId={target.PlayerId}");
            return;
        }

        PlayerItem playerItem = target.GetComponent<PlayerItem>();

        if (playerItem == null)
            playerItem = target.GetComponentInChildren<PlayerItem>(true);

        if (playerItem != null)
        {
            Weapon currentWeapon = playerItem.GetCurrentWeapon();

            bool wrongWeapon =
                snapshot.weaponId >= 0 &&
                (currentWeapon == null || currentWeapon.GetWeaponId() != snapshot.weaponId);

            if (wrongWeapon)
                playerItem.ApplyWeaponSwitchFromServer(snapshot.weaponId);

            if (snapshot.weaponId >= 0)
            {
                playerItem.SetWeaponAmmoFromServer(
                    snapshot.weaponId,
                    snapshot.magAmmo,
                    snapshot.reserveAmmo
                );
            }

            playerItem.SetADSVisualFromServer(snapshot.isADS);
        }

        FPSCameraLook targetLook = target.GetComponentInChildren<FPSCameraLook>(true);

        if (targetLook != null)
            targetLook.SetADSFromServer(snapshot.isADS);

        PlayerCameraController targetController = GetCameraController(target);

        if (targetController != null)
        {
            targetController.SetSpectatorView(true);
            targetController.RefreshViewVisuals();
        }

        if (playerItem != null)
            playerItem.RefreshRemoteReloadVisual();

        RefreshSpectatorUI(target, snapshot);

        Debug.Log(
            $"[SPECTATOR STATE REFRESH] playerId={target.PlayerId}, " +
            $"weaponId={snapshot.weaponId}, ammo={snapshot.magAmmo}/{snapshot.reserveAmmo}, " +
            $"ads={snapshot.isADS}"
        );
    }

    private void RefreshSpectatorUI(PlayerIdentity target, PlayerCombatSnapshot snapshot)
    {
        if (target == null || snapshot == null || InGameUIManager.Instance == null)
            return;

        Sprite characterSprite = null;

        if (MatchManager.Instance != null)
        {
            characterSprite = MatchManager.Instance.GetCharacterSpriteForUI(
                target.CharacterId
            );
        }

        InGameUIManager.Instance.SetSpectatorPlayerUI(
            target.CharacterId,
            characterSprite,
            snapshot.hp,
            snapshot.barrier,
            snapshot.maxHp,
            snapshot.maxBarrier,
            snapshot.weaponId,
            snapshot.magAmmo,
            snapshot.reserveAmmo
        );
    }

    private void RestoreSpectatorTarget()
    {
        if (currentSpectatorTarget == null)
            return;

        PlayerIdentity previousTarget = currentSpectatorTarget;
        currentSpectatorTarget = null;

        PlayerCameraController previousController = GetCameraController(previousTarget);

        if (previousController != null)
            previousController.SetSpectatorView(false);

        Debug.Log($"[SPECTATOR TARGET RESTORE] playerId={previousTarget.PlayerId}");
    }

    private PlayerCameraController GetCameraController(PlayerIdentity identity)
    {
        if (identity == null)
            return null;

        PlayerCameraController controller = identity.GetComponent<PlayerCameraController>();

        if (controller == null)
            controller = identity.GetComponentInChildren<PlayerCameraController>(true);

        return controller;
    }

    private void SaveLastSpectatorView()
    {
        if (myCamera == null)
            return;

        lastSpectatorPosition = myCamera.transform.position;
        lastSpectatorRotation = myCamera.transform.rotation;
        lastSpectatorFieldOfView = myCamera.fieldOfView;
        hasLastSpectatorView = true;
    }

    private void HoldLastSpectatorView()
    {
        if (myCamera == null)
            return;

        // 저장된 관전 위치가 없다면 현재 카메라 월드 위치를 사용
        if (!hasLastSpectatorView)
            SaveLastSpectatorView();

        if (!isHoldingLastSpectatorView)
        {
            isHoldingLastSpectatorView = true;

            // 관전 대상의 1인칭 표시만 원래 상태로 복구
            // myCamera는 부모에서 분리되어 있으므로 위치는 바뀌지 않는다.
            RestoreSpectatorTarget();

            Debug.Log(
                $"[SPECTATOR HOLD] 마지막 카메라 위치 유지. " +
                $"position={lastSpectatorPosition}, " +
                $"rotation={lastSpectatorRotation.eulerAngles}"
            );
        }

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        myCamera.transform.SetPositionAndRotation(
            lastSpectatorPosition,
            lastSpectatorRotation
        );

        myCamera.fieldOfView = lastSpectatorFieldOfView;
    }

    private void NextTarget()
    {
        BuildTargets();

        if (targets.Count == 0)
        {
            currentIndex = 0;
            HoldLastSpectatorView();
            return;
        }

        if (targets.Count == 1)
        {
            currentIndex = 0;
            FollowTarget();
            return;
        }

        currentIndex++;

        if (currentIndex >= targets.Count)
            currentIndex = 0;

        FollowTarget();

        Debug.Log($"[SPECTATOR] next={targets[currentIndex].PlayerId}");
    }

    private void PrevTarget()
    {
        BuildTargets();

        if (targets.Count == 0)
        {
            currentIndex = 0;
            HoldLastSpectatorView();
            return;
        }

        if (targets.Count == 1)
        {
            currentIndex = 0;
            FollowTarget();
            return;
        }

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = targets.Count - 1;

        FollowTarget();

        Debug.Log($"[SPECTATOR] prev={targets[currentIndex].PlayerId}");
    }

    private void SaveMyCameraTransform()
    {
        if (myCamera == null || hasSavedCameraTransform)
            return;

        savedCameraParent = myCamera.transform.parent;
        savedCameraLocalPosition = myCamera.transform.localPosition;
        savedCameraLocalRotation = myCamera.transform.localRotation;
        savedCameraFieldOfView = myCamera.fieldOfView;
        hasSavedCameraTransform = true;

        Debug.Log(
            $"[SPECTATOR CAMERA SAVE] parent={savedCameraParent?.name}, " +
            $"localPos={savedCameraLocalPosition}, " +
            $"localRot={savedCameraLocalRotation.eulerAngles}"
        );
    }

    private void RestoreMyCameraTransform()
    {
        if (myCamera == null || !hasSavedCameraTransform)
            return;

        myCamera.transform.SetParent(savedCameraParent, false);
        myCamera.transform.localPosition = savedCameraLocalPosition;
        myCamera.transform.localRotation = savedCameraLocalRotation;
        myCamera.fieldOfView = savedCameraFieldOfView;

        hasSavedCameraTransform = false;

        Debug.Log(
            $"[SPECTATOR CAMERA RESTORE] parent={savedCameraParent?.name}, " +
            $"localPos={myCamera.transform.localPosition}, " +
            $"localRot={myCamera.transform.localEulerAngles}"
        );
    }

    private void RestoreLocalPlayerUI()
    {
        if (myPlayer == null ||
            NetworkManager.Instance == null ||
            InGameUIManager.Instance == null)
        {
            return;
        }

        if (!NetworkManager.Instance.TryGetCombatSnapshot(
            myPlayer.PlayerId,
            out PlayerCombatSnapshot snapshot))
        {
            return;
        }

        Sprite characterSprite = null;

        if (MatchManager.Instance != null)
            characterSprite = MatchManager.Instance.GetCharacterSpriteForUI(myPlayer.CharacterId);

        InGameUIManager.Instance.SetSpectatorPlayerUI(
            myPlayer.CharacterId,
            characterSprite,
            snapshot.hp,
            snapshot.barrier,
            snapshot.maxHp,
            snapshot.maxBarrier,
            snapshot.weaponId,
            snapshot.magAmmo,
            snapshot.reserveAmmo
        );
    }
}