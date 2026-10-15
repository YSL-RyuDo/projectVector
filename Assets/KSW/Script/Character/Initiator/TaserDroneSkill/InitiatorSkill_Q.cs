using UnityEngine;
using UnityEngine.InputSystem;

public class InitiatorSkill_Q : CharacterSkill
{
    private PlayerIdentity _playerIdentity;
    private PlayerMovement _playerMovement;
    private FPSCameraLook _cameraLook;
    private Camera _playerCamera;
    private AudioListener _playerAudioListener;

    [SerializeField] private Transform _droneSpawnPoint;

    private Drone _currentTaserDrone;
    private bool _isDroneMode;
    private uint _currentDroneId;
    private bool _exitRequested;

    public static bool IsLocalDroneMode { get; private set; }
    private PlayerItem _playerItem;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
        _playerMovement = GetComponent<PlayerMovement>();
        _cameraLook = GetComponentInChildren<FPSCameraLook>(true);
        _playerCamera = GetComponentInChildren<Camera>(true);
        _playerAudioListener = GetComponentInChildren<AudioListener>(true);
        _playerItem = GetComponent<PlayerItem>();
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (Keyboard.current == null)
            return;

        if (GameController.Instance != null && GameController.Instance.IsPaused)
            return;

        if (SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    private bool IsMine()
    {
        if (_playerIdentity == null)
            return false;

        if (NetworkManager.Instance == null)
            return false;

        return _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }

    protected override void UseSkill()
    {
        if (!IsMine())
            return;

        if (_isDroneMode)
        {
            RequestDroneExit();
        }
        else
        {
            if (!CanUsePurchasedSkill(SkillSlot.Q))
                return;

            RequestDroneDeploy();
        }
    }

    private void RequestDroneDeploy()
    {
        if (!IsMine())
            return;

        if (_isDroneMode)
            return;

        if (_currentDroneId != 0)
            return;

        Vector3 spawnPos;

        if (_droneSpawnPoint != null)
        {
            spawnPos = _droneSpawnPoint.position;
        }
        else
        {
            spawnPos =
                transform.position +
                transform.forward * 2.5f +
                Vector3.up * 2.5f;
        }

        float yaw = transform.eulerAngles.y;
        float pitch = 0f;

        NetworkManager.Instance.SendDroneDeploy(
            spawnPos.x,
            spawnPos.y,
            spawnPos.z,
            yaw,
            pitch
        );

        Debug.Log(
            $"[DRONE DEPLOY REQUEST] " +
            $"playerId={_playerIdentity.PlayerId}, " +
            $"pos={spawnPos}, yaw={yaw}"
        );
    }

    private void RequestDroneExit()
    {
        if (!IsMine())
            return;

        if (_currentDroneId == 0)
        {
            ForceReturnToPlayerCamera();
            return;
        }

        if (_exitRequested)
            return;

        _exitRequested = true;

        NetworkManager.Instance.SendDroneExit(_currentDroneId);

        ForceReturnToPlayerCamera();
    }

    public void OnOwnerDroneSpawned(Drone drone, uint droneId)
    {
        if (!IsMine())
            return;
        if (!IsAlive())
        {
            drone?.SetLocalCameraActive(false);
            NetworkManager.Instance?.SendDroneExit(droneId);
            return;
        }

        _currentTaserDrone = drone;
        _currentDroneId = droneId;
        _exitRequested = false;

        SetLocalDroneMode(true);
    }

    public void OnOwnerDroneDestroyed(uint droneId)
    {
        if (!IsMine()) return;
        if (droneId != 0 && _currentDroneId != droneId)
            return;

        _currentTaserDrone = null;
        _currentDroneId = 0;
        _exitRequested = false;

        SetLocalDroneMode(false);
    }

    private void SetLocalDroneMode(bool active)
    {
        if (active && !IsAlive()) return;
        _isDroneMode = active;
        IsLocalDroneMode = active;
        bool restore = !active && IsAlive();

        if (_playerMovement != null)
            _playerMovement.CanMove = restore;

        if (_cameraLook != null)
        {
            if (active)
                _cameraLook.ForceExitADS();

            _cameraLook.CanMove = restore;
        }

        if (_playerCamera != null)
            _playerCamera.enabled = restore;

        if (_playerAudioListener != null)
            _playerAudioListener.enabled = restore;

        if (_playerItem != null)
        {
            if (active) _playerItem.CancelReload(false);
            _playerItem.SetWeaponVisibility(restore);
        }

        if (DroneModeUIController.Instance != null)
            DroneModeUIController.Instance.SetDroneModeUI(active);

        if (restore && !VectorVoicePanel.IsAnyOpen && !(SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen))
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

        LocalDroneBodyProxy bodyProxy = GetComponent<LocalDroneBodyProxy>();
        if (bodyProxy != null)
            bodyProxy.SetVisible(active);

        if (DroneHUDUI.Instance != null)
            DroneHUDUI.Instance.SetVisible(active);
    }

    private void ForceReturnToPlayerCamera()
    {
        if (_currentTaserDrone != null)
            _currentTaserDrone.SetLocalCameraActive(false);
        SetLocalDroneMode(false);
    }

    private bool IsAlive() => GetComponent<PlayerHP>()?.IsDead != true && GetComponent<PlayerDeathController>()?.IsDead != true;
    private void OnDisable()
    {
        if (!Application.isPlaying || !_isDroneMode) return;
        ForceReturnToPlayerCamera();
        _currentTaserDrone = null; _currentDroneId = 0; _exitRequested = false;
    }
}
