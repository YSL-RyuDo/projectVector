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
            SpectateSpike();
            return;
        }

        if (targets.Count == 1)
            currentIndex = 0;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextTarget();
        }

        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            PrevTarget();
        }

        FollowTarget();
    }

    public void StartSpectating()
    {
        // 이전 관전 대상이 남아 있으면 먼저 3인칭으로 복구한다.
        RestoreSpectatorTarget();

        myPlayer = FindMyPlayer();

        if (myPlayer == null)
        {
            Debug.LogError(
                "[SPECTATOR] 내 플레이어를 찾지 못했습니다."
            );

            return;
        }

        PlayerCameraController myCameraController =
            GetCameraController(myPlayer);

        if (myCameraController == null ||
            myCameraController.PlayerCamera == null)
        {
            Debug.LogError(
                "[SPECTATOR] 내 플레이어 카메라를 찾지 못했습니다."
            );

            return;
        }

        // 죽은 내 캐릭터의 무기와 모델을 숨긴다.
        myCameraController.RefreshViewVisuals();

        /*
         * 관전 중에는 죽은 내 플레이어 카메라를 계속 사용한다.
         * 관전 대상의 Camera 컴포넌트는 활성화하지 않는다.
         */
        myCamera =
            myCameraController.PlayerCamera;

        if (!myCamera.gameObject.activeSelf)
            myCamera.gameObject.SetActive(true);

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        AudioListener listener =
            myCamera.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = true;

        BuildTargets();

        isSpectating = true;
        currentIndex = 0;

        if (targets.Count == 0)
        {
            SpectateSpike();

            Debug.Log(
                "[SPECTATOR] 살아 있는 팀원이 없어 스파이크 관전을 시작합니다."
            );

            return;
        }

        FollowTarget();

        Debug.Log(
            $"[SPECTATOR] 관전 시작. target={targets[currentIndex].PlayerId}"
        );
    }

    public void StopSpectating()
    {
        // 관전 대상은 다시 일반 원격 플레이어 상태로 복구한다.
        RestoreSpectatorTarget();

        isSpectating = false;

        targets.Clear();
        currentIndex = 0;

        myPlayer = null;
        myCamera = null;

        Debug.Log("[SPECTATOR] 관전 종료");
    }

    private void BuildTargets()
    {
        uint previousTargetId = 0;

        if (currentSpectatorTarget != null)
        {
            previousTargetId =
                currentSpectatorTarget.PlayerId;
        }

        targets.Clear();

        PlayerIdentity mine =
            FindMyPlayer();

        if (mine == null)
            return;

        if (NetworkManager.Instance == null)
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

            // 자기 자신은 관전 대상에서 제외
            if (player.PlayerId ==
                NetworkManager.Instance.MyPlayerId)
            {
                continue;
            }

            // 같은 팀만 관전
            if (player.Team != mine.Team)
                continue;

            PlayerDeathController death =
                player.GetComponent<PlayerDeathController>();

            if (death == null)
            {
                death =
                    player.GetComponentInChildren<PlayerDeathController>(true);
            }

            if (death != null &&
                death.IsDead)
            {
                continue;
            }

            PlayerHP hp =
                player.GetComponent<PlayerHP>();

            if (hp == null)
            {
                hp =
                    player.GetComponentInChildren<PlayerHP>(true);
            }

            if (hp != null &&
                hp.IsDead)
            {
                continue;
            }

            targets.Add(player);
        }

        if (targets.Count == 0)
        {
            currentIndex = 0;
            RestoreSpectatorTarget();
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].PlayerId ==
                previousTargetId)
            {
                currentIndex = i;
                return;
            }
        }

        currentIndex =
            Mathf.Clamp(
                currentIndex,
                0,
                targets.Count - 1
            );
    }

    private PlayerIdentity FindMyPlayer()
    {
        if (NetworkManager.Instance == null)
            return null;

        uint myPlayerId =
            NetworkManager.Instance.MyPlayerId;

        PlayerIdentity[] players =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity player in players)
        {
            if (player == null)
                continue;

            if (player.PlayerId == myPlayerId)
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
        if (myCamera == null)
            return;

        if (targets.Count == 0)
            return;

        if (currentIndex < 0 ||
            currentIndex >= targets.Count)
        {
            currentIndex = 0;
        }

        PlayerIdentity target =
            targets[currentIndex];

        if (target == null)
            return;

        ApplySpectatorTarget(target);

        PlayerCameraController targetController =
            GetCameraController(target);

        if (targetController == null ||
            targetController.PlayerCamera == null)
        {
            Debug.LogWarning(
                $"[SPECTATOR] 관전 대상 카메라가 없습니다. " +
                $"playerId={target.PlayerId}"
            );

            return;
        }

        /*
         * 관전 대상의 Camera 컴포넌트는 꺼진 상태다.
         * Camera Transform의 위치와 회전만 사용한다.
         */
        Camera targetCamera =
            targetController.PlayerCamera;

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        Vector3 targetCameraEuler =
            targetCamera.transform.rotation.eulerAngles;

        Vector3 targetBodyEuler =
            target.transform.rotation.eulerAngles;

        float pitch =
            NormalizeAngle(targetCameraEuler.x);

        float yaw =
            targetBodyEuler.y;

        myCamera.transform.position =
            targetCamera.transform.position;

        myCamera.transform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        myCamera.fieldOfView =
            targetCamera.fieldOfView;
    }

    private void ApplySpectatorTarget(
        PlayerIdentity newTarget)
    {
        if (newTarget == null)
            return;

        if (currentSpectatorTarget == newTarget)
            return;

        // 기존 관전 대상은 3인칭 상태로 복구한다.
        RestoreSpectatorTarget();

        PlayerDeathController death =
            newTarget.GetComponent<PlayerDeathController>();

        if (death == null)
        {
            death =
                newTarget.GetComponentInChildren<PlayerDeathController>(true);
        }

        if (death != null &&
            death.IsDead)
        {
            Debug.LogWarning(
                $"[SPECTATOR] 죽은 플레이어는 관전할 수 없습니다. " +
                $"playerId={newTarget.PlayerId}"
            );

            return;
        }

        PlayerCameraController targetController =
            GetCameraController(newTarget);

        if (targetController == null)
        {
            Debug.LogWarning(
                $"[SPECTATOR] PlayerCameraController가 없습니다. " +
                $"playerId={newTarget.PlayerId}"
            );

            return;
        }

        currentSpectatorTarget =
            newTarget;

        /*
         * 관전 대상의 카메라는 켜지 않는다.
         * 모델과 무기 표시만 1인칭으로 변경한다.
         */
        targetController.SetSpectatorView(true);

        Debug.Log(
            $"[SPECTATOR TARGET APPLY] " +
            $"playerId={newTarget.PlayerId}, " +
            $"firstPersonWeapon=true, " +
            $"model=false"
        );
    }

    private void RestoreSpectatorTarget()
    {
        if (currentSpectatorTarget == null)
            return;

        PlayerIdentity previousTarget =
            currentSpectatorTarget;

        currentSpectatorTarget = null;

        PlayerCameraController previousController =
            GetCameraController(previousTarget);

        if (previousController != null)
        {
            /*
             * 살아 있으면 3인칭 무기와 모델로 복구되고,
             * 이미 죽었으면 무기와 모델이 모두 숨겨진다.
             */
            previousController.SetSpectatorView(false);
        }

        Debug.Log(
            $"[SPECTATOR TARGET RESTORE] " +
            $"playerId={previousTarget.PlayerId}"
        );
    }

    private PlayerCameraController GetCameraController(
        PlayerIdentity identity)
    {
        if (identity == null)
            return null;

        PlayerCameraController controller =
            identity.GetComponent<PlayerCameraController>();

        if (controller == null)
        {
            controller =
                identity.GetComponentInChildren<PlayerCameraController>(true);
        }

        return controller;
    }

    private void SpectateSpike()
    {
        RestoreSpectatorTarget();

        if (myCamera == null)
            return;

        Spike spike =
            Spike.PlantedSpike;

        if (spike == null)
        {
            Debug.Log(
                "[SPECTATOR] 살아 있는 팀원과 설치된 스파이크가 없습니다."
            );

            return;
        }

        Transform spectatePoint =
            spike.SpectatePoint;

        if (spectatePoint == null)
            spectatePoint = spike.transform;

        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        Vector3 euler =
            spectatePoint.rotation.eulerAngles;

        myCamera.transform.position =
            spectatePoint.position;

        myCamera.transform.rotation =
            Quaternion.Euler(
                NormalizeAngle(euler.x),
                euler.y,
                0f
            );
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

        Debug.Log(
            $"[SPECTATOR] next={targets[currentIndex].PlayerId}"
        );
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

        Debug.Log(
            $"[SPECTATOR] prev={targets[currentIndex].PlayerId}"
        );
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}