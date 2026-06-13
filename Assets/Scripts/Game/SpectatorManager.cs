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

    private PlayerIdentity currentHiddenTarget;

    private bool isSpikeSpectating;

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (!isSpectating)
            return;

        BuildTargets();

        if (targets.Count == 0)
        {
            SpectateSpike();
            return;
        }

        isSpikeSpectating = false;

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
        // 이전 관전 상태가 남아 있다면 먼저 복구
        RestoreHiddenTargetModel();

        myPlayer = FindMyPlayer();

        if (myPlayer == null)
        {
            Debug.LogError("[SPECTATOR] my player not found");
            return;
        }

        PlayerCameraController myCameraController =
            myPlayer.GetComponent<PlayerCameraController>();

        if (myCameraController == null ||
            myCameraController.PlayerCamera == null)
        {
            Debug.LogError("[SPECTATOR] my camera not found");
            return;
        }

        // 관전에는 죽은 내 플레이어의 카메라를 계속 사용한다.
        myCamera = myCameraController.PlayerCamera;
        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        BuildTargets();

        isSpectating = true;
        isSpikeSpectating = false;
        currentIndex = 0;

        if (targets.Count == 0)
        {
            SpectateSpike();

            Debug.Log(
                "[SPECTATOR] start spike spectate"
            );

            return;
        }

        FollowTarget();

        Debug.Log(
            $"[SPECTATOR] start target={targets[currentIndex].PlayerId}"
        );
    }
    public void StopSpectating()
    {
        // 현재 관전 대상의 모델과 3인칭 무기 복구
        RestoreHiddenTargetModel();

        isSpectating = false;
        isSpikeSpectating = false;

        targets.Clear();
        currentIndex = 0;

        myPlayer = null;
        myCamera = null;
        currentHiddenTarget = null;

        Debug.Log("[SPECTATOR] stop");
    }

    private void BuildTargets()
    {
        uint previousTargetId = 0;

        if (targets.Count > 0 &&
            currentIndex >= 0 &&
            currentIndex < targets.Count &&
            targets[currentIndex] != null)
        {
            previousTargetId = targets[currentIndex].PlayerId;
        }

        targets.Clear();

        PlayerIdentity mine = FindMyPlayer();

        if (mine == null)
            return;

        PlayerIdentity[] players =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity player in players)
        {
            if (player == null)
                continue;

            if (player.PlayerId == NetworkManager.Instance.MyPlayerId)
                continue;

            if (player.Team != mine.Team)
                continue;

            PlayerDeathController death =
                player.GetComponent<PlayerDeathController>();

            if (death != null && death.IsDead)
                continue;

            PlayerHP hp = player.GetComponent<PlayerHP>();

            if (hp != null && hp.IsDead)
                continue;

            targets.Add(player);
        }

        if (targets.Count == 0)
        {
            currentIndex = 0;
            RestoreHiddenTargetModel();
            return;
        }

        if (targets.Count == 1)
        {
            currentIndex = 0;
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

        uint myId = NetworkManager.Instance.MyPlayerId;

        PlayerIdentity[] players =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity player in players)
        {
            if (player == null)
                continue;

            if (player.PlayerId == myId)
                return player;
        }

        Debug.LogError($"[SPECTATOR] cannot find my player. myId={myId}, count={players.Length}");
        return null;
    }

    private void FollowTarget()
    {
        if (myCamera == null)
            return;

        if (targets.Count == 0)
            return;

        if (currentIndex < 0 || currentIndex >= targets.Count)
            currentIndex = 0;

        PlayerIdentity target = targets[currentIndex];

        if (target == null)
            return;

        isSpikeSpectating = false;

        ApplySpectatorTargetModelVisibility(target);

        PlayerCameraController targetCameraController =
            target.GetComponent<PlayerCameraController>();

        if (targetCameraController == null || targetCameraController.PlayerCamera == null)
            return;

        Camera targetCamera = targetCameraController.PlayerCamera;

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        Vector3 targetCameraEuler = targetCamera.transform.rotation.eulerAngles;
        Vector3 targetBodyEuler = target.transform.rotation.eulerAngles;

        float pitch = NormalizeAngle(targetCameraEuler.x);
        float yaw = targetBodyEuler.y;

        myCamera.transform.position = targetCamera.transform.position;
        myCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        myCamera.fieldOfView = targetCamera.fieldOfView;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void SpectateSpike()
    {
        RestoreHiddenTargetModel();

        if (myCamera == null)
            return;

        Spike spike = Spike.PlantedSpike;

        if (spike == null)
        {
            Debug.Log("[SPECTATOR] no alive teammate and no planted spike");
            return;
        }

        Transform point = spike.SpectatePoint;

        if (point == null)
            point = spike.transform;

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        Vector3 euler = point.rotation.eulerAngles;

        myCamera.transform.position = point.position;
        myCamera.transform.rotation = Quaternion.Euler(
            NormalizeAngle(euler.x),
            euler.y,
            0f
        );

        isSpikeSpectating = true;
    }

    private void NextTarget()
    {
        BuildTargets();

        if (targets.Count <= 1)
        {
            currentIndex = 0;

            if (targets.Count == 0)
                SpectateSpike();
            else
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

        if (targets.Count <= 1)
        {
            currentIndex = 0;

            if (targets.Count == 0)
                SpectateSpike();
            else
                FollowTarget();

            return;
        }

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = targets.Count - 1;

        FollowTarget();

        Debug.Log($"[SPECTATOR] prev={targets[currentIndex].PlayerId}");
    }

    private void ApplySpectatorTargetModelVisibility(
    PlayerIdentity newTarget)
    {
        if (newTarget == null)
            return;

        // 이미 같은 플레이어를 관전 중이면 매 프레임 다시 설정하지 않는다.
        if (currentHiddenTarget == newTarget)
            return;

        // 이전 관전 대상을 정상적인 3인칭 표시로 복구
        RestoreHiddenTargetModel();

        PlayerDeathController death =
            newTarget.GetComponent<PlayerDeathController>();

        if (death == null)
        {
            death =
                newTarget.GetComponentInChildren<PlayerDeathController>(true);
        }

        // 이미 죽은 플레이어는 관전 대상으로 적용하지 않는다.
        if (death != null && death.IsDead)
        {
            Debug.LogWarning(
                $"[SPECTATOR VIEW] target is dead. playerId={newTarget.PlayerId}"
            );

            return;
        }

        currentHiddenTarget = newTarget;

        // 관전 대상의 몸 모델은 숨긴다.
        // 그래야 머리, 몸통, 3인칭 팔이 카메라 앞에 보이지 않는다.
        if (death != null)
        {
            death.SetModelVisible(false);
        }

        PlayerWeaponViewController weaponView =
            newTarget.GetComponent<PlayerWeaponViewController>();

        if (weaponView == null)
        {
            weaponView =
                newTarget.GetComponentInChildren<PlayerWeaponViewController>(true);
        }

        if (weaponView != null)
        {
            // 관전 대상:
            // CameraRoot/WeaponHolder ON
            // Model/WeaponHolder(Model) OFF
            weaponView.ShowFirstPerson();
        }
        else
        {
            Debug.LogWarning(
                $"[SPECTATOR VIEW] PlayerWeaponViewController missing. " +
                $"playerId={newTarget.PlayerId}"
            );
        }

        PlayerItem playerItem =
            newTarget.GetComponent<PlayerItem>();

        if (playerItem == null)
        {
            playerItem =
                newTarget.GetComponentInChildren<PlayerItem>(true);
        }

        if (playerItem != null)
        {
            // 1인칭 Holder 안의 현재 무기 프리팹을 활성화
            playerItem.SetWeaponVisibility(true);
        }

        Debug.Log(
            $"[SPECTATOR VIEW APPLY] playerId={newTarget.PlayerId}, " +
            $"firstPersonWeapon=true, model=false"
        );
    }

    private void RestoreHiddenTargetModel()
    {
        if (currentHiddenTarget == null)
            return;

        PlayerIdentity previousTarget = currentHiddenTarget;

        // 먼저 참조를 비운다.
        currentHiddenTarget = null;

        PlayerDeathController death =
            previousTarget.GetComponent<PlayerDeathController>();

        if (death == null)
        {
            death =
                previousTarget.GetComponentInChildren<PlayerDeathController>(true);
        }

        PlayerWeaponViewController weaponView =
            previousTarget.GetComponent<PlayerWeaponViewController>();

        if (weaponView == null)
        {
            weaponView =
                previousTarget.GetComponentInChildren<PlayerWeaponViewController>(true);
        }

        PlayerItem playerItem =
            previousTarget.GetComponent<PlayerItem>();

        if (playerItem == null)
        {
            playerItem =
                previousTarget.GetComponentInChildren<PlayerItem>(true);
        }

        if (death != null && death.IsDead)
        {
            // 관전 중 대상이 죽었다면 무기와 모델 모두 숨김
            if (weaponView != null)
                weaponView.HideAll();

            if (playerItem != null)
                playerItem.SetWeaponVisibility(false);

            death.SetModelVisible(false);
        }
        else
        {
            // 이전 관전 대상을 정상적인 원격 플레이어 모습으로 복구
            if (playerItem != null)
                playerItem.SetWeaponVisibility(false);

            if (weaponView != null)
            {
                // CameraRoot/WeaponHolder OFF
                // Model/WeaponHolder(Model) ON
                weaponView.ShowThirdPerson();
            }

            if (death != null)
                death.SetModelVisible(true);
        }

        Debug.Log(
            $"[SPECTATOR VIEW RESTORE] playerId={previousTarget.PlayerId}"
        );
    }
}