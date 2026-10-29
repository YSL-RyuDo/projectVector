using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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
    private bool _isPreparing;
    private bool _deployPending;
    private bool _cancelPendingSpawn;
    private float _requestTime;
    [SerializeField] private DroneLaunchRig _launchRig;
    public static bool IsLocalPreparing { get; private set; }
    public static bool IsLocalReturning { get; private set; }
    private bool _returnPending;
    private int _queuedWeaponSlot = -1;
    private float _weaponReadyAt;
    public bool BlocksWeaponPresentation => _isPreparing || _deployPending || _isDroneMode || _returnPending;
    public bool ShowTransitionWeapon => IsAlive() && !_isDroneMode && (_launchRig != null && _launchRig.WeaponVisible);
    private Coroutine _launchSequence;
    private bool _ownsLocalState;
    public bool CanRestoreQueuedWeapon => _returnPending && !_deployPending && _currentDroneId == 0 && IsAlive();

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

        if ((_isPreparing || _deployPending) && (!IsAlive() || NetworkManager.Instance.ReconnectBusy ||
            (RoundManager.Instance != null && (RoundManager.Instance.currentState == RoundManager.RoundState.RoundEnd ||
             RoundManager.Instance.currentState == RoundManager.RoundState.GameOver))))
            CancelPreparation();
        if (_deployPending && Time.unscaledTime - _requestTime > 8f) CancelPreparation();

        if (CanRestoreQueuedWeapon && _launchRig != null && _launchRig.ReadyForWeaponDraw)
        {
            _playerItem?.RestoreQueuedDroneWeapon(_queuedWeaponSlot);
            _launchRig.DrawAllowed = true;
        }

        if (_returnPending && !_deployPending && _currentDroneId == 0 && Time.unscaledTime >= _weaponReadyAt &&
            (_launchRig == null || !_launchRig.IsReturning))
        {
            int slot = _queuedWeaponSlot;
            _returnPending = IsLocalReturning = false;
            _queuedWeaponSlot = -1;
            IsLocalPreparing = false;
            if (IsAlive()) { _playerItem?.TrySelectWeapon(slot); _playerItem?.SetWeaponVisibility(true); }
            if (!_isDroneMode) DroneHUDUI.Instance?.SetVisible(false);
        }

        if (Keyboard.current == null)
            return;

        if (DroneMovement.InputBlocked) return;

        if (BlocksWeaponPresentation)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) { ReturnToWeapon(0); return; }
            if (Keyboard.current.digit2Key.wasPressedThisFrame) { ReturnToWeapon(1); return; }
            if (Keyboard.current.digit3Key.wasPressedThisFrame) { ReturnToWeapon(2); return; }
        }
        if (_returnPending) return;

        if (_isPreparing && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (CanUsePurchasedSkill(SkillSlot.Q)) RequestDroneDeploy();
            else CancelPreparation();
            return;
        }
        if ((_isPreparing || _deployPending) && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        { ReturnToWeapon(_playerItem != null ? _playerItem.CurrentIndex : -1); return; }

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
        if (GetComponent<InitiatorSkill_E>()?.BlocksActions == true) return;
        if (GetComponent<InitiatorSkill_X>()?.IsCasting == true) return;
        if (!IsMine())
            return;

        if (_isDroneMode)
        {
            ReturnToWeapon(_playerItem != null ? _playerItem.CurrentIndex : -1);
        }
        else
        {
            if (_returnPending) return;
            if (_isPreparing || _deployPending) { ReturnToWeapon(_playerItem != null ? _playerItem.CurrentIndex : -1); return; }
            if (!CanUsePurchasedSkill(SkillSlot.Q))
                return;
            _isPreparing = IsLocalPreparing = true;
            _ownsLocalState = true;
            _cancelPendingSpawn = false;
            _cameraLook?.ForceExitADS();
            if (_playerItem != null) _playerItem.CancelReload(false);
            _launchRig?.SetPrepared(true);
            DroneHUDUI.Instance?.ShowPreparation(false);
        }
    }

    private void CancelPreparation()
    {
        if (IsAlive() && !_returnPending && (_isPreparing || _deployPending))
            BeginWeaponReturn(_playerItem != null ? _playerItem.CurrentIndex : -1);
        if (_deployPending) _cancelPendingSpawn = true;
        if (_launchSequence != null) { StopCoroutine(_launchSequence); _launchSequence = null; }
        _isPreparing = _deployPending = IsLocalPreparing = false;
        _launchRig?.SetPrepared(false);
        if (IsAlive() && !_returnPending) _playerItem?.SetWeaponVisibility(true);
        if (!_isDroneMode) DroneHUDUI.Instance?.SetVisible(false);
    }

    // Queue the selected gun. Do not unlock fire until the authoritative drone
    // exit (including a late spawn response) and the arm return have completed.
    public bool ReturnToWeapon(int slot)
    {
        if (!IsMine() || !IsAlive() || DroneMovement.InputBlocked || _playerItem == null) return false;
        if (slot < 0 || slot > 2 || _playerItem.Items[slot] == null) return false;
        if (!BlocksWeaponPresentation) return false;
        if (_returnPending) { _queuedWeaponSlot = slot; return true; }
        BeginWeaponReturn(slot);
        if (_launchSequence != null)
        {
            StopCoroutine(_launchSequence); _launchSequence = null;
            _deployPending = false;
            CancelPreparation();
        }
        else if (_deployPending)
        {
            _cancelPendingSpawn = true;
            _isPreparing = false;
            IsLocalPreparing = true;
        }
        else if (_isDroneMode) RequestDroneExit();
        else CancelPreparation();
        return true;
    }

    private void BeginWeaponReturn(int slot)
    {
        _ownsLocalState = true;
        _queuedWeaponSlot = slot;
        _returnPending = IsLocalReturning = true;
        _weaponReadyAt = Time.unscaledTime + (_launchRig != null ? _launchRig.ReturnDuration : .18f);
        _launchRig?.SetPrepared(false);
        if (_launchRig != null) _launchRig.DrawAllowed = false;
    }

    private void RequestDroneDeploy()
    {
        if (!IsMine())
            return;

        if (_isDroneMode)
            return;
        if (_deployPending || _returnPending || !_isPreparing)
            return;
        if (_launchRig != null && !_launchRig.ReadyToLaunch) return;

        if (_currentDroneId != 0)
            return;

        Vector3 spawnPos;

        if (_launchRig != null && _launchRig.TryGetReleasePosition(out spawnPos))
        {
            // Use the actual left-arm release endpoint, not a second elevated spawn.
        }
        else if (_droneSpawnPoint != null)
        {
            spawnPos = _droneSpawnPoint.position;
        }
        else
        {
            spawnPos =
                transform.position +
                transform.forward * 1.1f +
                Vector3.up * 0.3f;
        }

        float yaw = transform.eulerAngles.y;
        float pitch = 0f;

        // Do not deploy a camera through cover. The player capsule is ignored here.
        Vector3 origin = transform.position + Vector3.up * 1.3f;
        Vector3 offset = spawnPos - origin;
        foreach (var hit in Physics.SphereCastAll(origin, 0.48f, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            DroneHUDUI.Instance?.ShowNotice("사출 공간이 부족합니다");
            return;
        }
        _isPreparing = false;
        _deployPending = IsLocalPreparing = true;
        _requestTime = Time.unscaledTime;
        DroneHUDUI.Instance?.ShowPreparation(true);
        _launchRig?.BeginLaunch();
        _launchSequence = StartCoroutine(CompleteLaunch(spawnPos, yaw, pitch));
    }

    private IEnumerator CompleteLaunch(Vector3 spawnPos, float yaw, float pitch)
    {
        // Keep the body view for the visible release; no network drone exists yet.
        yield return new WaitForSeconds(_launchRig != null ? _launchRig.LaunchDuration : .28f);
        _launchSequence = null;
        if (!_deployPending || _cancelPendingSpawn || !IsAlive()) { CancelPreparation(); yield break; }
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
        if (!IsAlive() || _cancelPendingSpawn)
        {
            _currentDroneId = droneId;
            _deployPending = false;
            drone?.SetLocalCameraActive(false);
            NetworkManager.Instance?.SendDroneExit(droneId);
            CancelPreparation();
            return;
        }

        _currentTaserDrone = drone;
        _currentDroneId = droneId;
        _exitRequested = false;
        _isPreparing = _deployPending = IsLocalPreparing = false;
        _launchRig?.SetDeployed();

        SetLocalDroneMode(true);
    }

    public void OnOwnerDroneDestroyed(uint droneId)
    {
        OnOwnerDroneDestroyed(droneId, 0);
    }

    public void OnOwnerDroneDestroyed(uint droneId, byte reason)
    {
        if (!IsMine()) return;
        if (droneId != 0 && _currentDroneId != droneId)
            return;

        _currentTaserDrone = null;
        _currentDroneId = 0;
        _exitRequested = false;

        SetLocalDroneMode(false);
        if (reason == 3) DroneHUDUI.Instance?.ShowExitNotice("본체 피격 · 드론 연결 종료");
        else if (reason == 1) DroneHUDUI.Instance?.ShowExitNotice("드론 파괴 · 연결 종료");
        else if (reason == 4) DroneHUDUI.Instance?.ShowExitNotice("비행 제한 · 연결 종료");
    }

    private void SetLocalDroneMode(bool active)
    {
        if (active && !IsAlive()) return;
        _ownsLocalState = true;
        if (!active && _isDroneMode && IsAlive() && !_returnPending)
            BeginWeaponReturn(_playerItem != null ? _playerItem.CurrentIndex : -1);
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
            _playerItem.SetWeaponVisibility(restore && !_returnPending);
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
        if (!_ownsLocalState && !IsMine()) return;
        if (_launchSequence != null) { StopCoroutine(_launchSequence); _launchSequence = null; }
        if (_isDroneMode) ForceReturnToPlayerCamera();
        _launchRig?.ResetPresentation();
        _isPreparing = _deployPending = IsLocalPreparing = false;
        _returnPending = IsLocalReturning = false;
        _queuedWeaponSlot = -1;
        _isDroneMode = IsLocalDroneMode = false;
        _currentTaserDrone = null; _currentDroneId = 0; _exitRequested = false;
        _ownsLocalState = false;
    }
}
