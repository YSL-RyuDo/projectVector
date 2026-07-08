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

        _currentTaserDrone = drone;
        _currentDroneId = droneId;
        _exitRequested = false;

        SetLocalDroneMode(true);
    }

    public void OnOwnerDroneDestroyed(uint droneId)
    {
        if (droneId != 0 && _currentDroneId != droneId)
            return;

        _currentTaserDrone = null;
        _currentDroneId = 0;
        _exitRequested = false;

        SetLocalDroneMode(false);
    }

    private void SetLocalDroneMode(bool active)
    {
        _isDroneMode = active;
        IsLocalDroneMode = active;

        if (_playerMovement != null)
            _playerMovement.CanMove = !active;

        if (_cameraLook != null)
        {
            if (active)
                _cameraLook.ForceExitADS();

            _cameraLook.CanMove = !active;
        }

        if (_playerCamera != null)
            _playerCamera.enabled = !active;

        if (_playerAudioListener != null)
            _playerAudioListener.enabled = !active;

        if (_playerItem != null)
            _playerItem.SetWeaponVisibility(!active);

        if (DroneModeUIController.Instance != null)
            DroneModeUIController.Instance.SetDroneModeUI(active);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        LocalDroneBodyProxy bodyProxy = GetComponent<LocalDroneBodyProxy>();
        if (bodyProxy != null)
            bodyProxy.SetVisible(active);

        if (DroneHUDUI.Instance != null)
            DroneHUDUI.Instance.SetVisible(active);
    }

    private void ForceReturnToPlayerCamera()
    {
        _isDroneMode = false;
        IsLocalDroneMode = false;

        if (_playerMovement != null)
            _playerMovement.CanMove = true;

        if (_cameraLook != null)
            _cameraLook.CanMove = true;

        if (_playerCamera != null)
            _playerCamera.enabled = true;

        if (_playerAudioListener != null)
            _playerAudioListener.enabled = true;

        if (_playerItem != null)
            _playerItem.SetWeaponVisibility(true);

        if (_currentTaserDrone != null)
            _currentTaserDrone.SetLocalCameraActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        LocalDroneBodyProxy bodyProxy = GetComponent<LocalDroneBodyProxy>();
        if (bodyProxy != null)
            bodyProxy.SetVisible(false);
    }
}