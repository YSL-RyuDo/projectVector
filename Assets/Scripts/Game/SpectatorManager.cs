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
        myPlayer = FindMyPlayer();

        if (myPlayer == null)
        {
            Debug.LogError("[SPECTATOR] my player not found");
            return;
        }

        PlayerCameraController myCameraController =
            myPlayer.GetComponent<PlayerCameraController>();

        if (myCameraController == null || myCameraController.PlayerCamera == null)
        {
            Debug.LogError("[SPECTATOR] my camera not found");
            return;
        }

        myCamera = myCameraController.PlayerCamera;
        myCamera.enabled = true;
        myCamera.tag = "MainCamera";

        BuildTargets();

        isSpectating = true;
        currentIndex = 0;

        if (targets.Count == 0)
        {
            SpectateSpike();
            Debug.Log("[SPECTATOR] start spike spectate");
            return;
        }

        FollowTarget();

        Debug.Log($"[SPECTATOR] start target={targets[currentIndex].PlayerId}");
    }

    public void StopSpectating()
    {
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

        myCamera.transform.SetPositionAndRotation(
            targetCamera.transform.position,
            targetCamera.transform.rotation
        );
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

        myCamera.transform.SetPositionAndRotation(
            point.position,
            point.rotation
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

    private void ApplySpectatorTargetModelVisibility(PlayerIdentity newTarget)
    {
        if (newTarget == null)
            return;

        if (currentHiddenTarget != null && currentHiddenTarget != newTarget)
        {
            PlayerDeathController prevDeath =
                currentHiddenTarget.GetComponent<PlayerDeathController>();

            if (prevDeath != null && !prevDeath.IsDead)
                prevDeath.SetModelVisible(true);
        }

        currentHiddenTarget = newTarget;

        PlayerDeathController currentDeath =
            currentHiddenTarget.GetComponent<PlayerDeathController>();

        if (currentDeath != null && !currentDeath.IsDead)
            currentDeath.SetModelVisible(false);
    }

    private void RestoreHiddenTargetModel()
    {
        if (currentHiddenTarget == null)
            return;

        PlayerDeathController death =
            currentHiddenTarget.GetComponent<PlayerDeathController>();

        if (death != null && !death.IsDead)
            death.SetModelVisible(true);

        currentHiddenTarget = null;
    }
}