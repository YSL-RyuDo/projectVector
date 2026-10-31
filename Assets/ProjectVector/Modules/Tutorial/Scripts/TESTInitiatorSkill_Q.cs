using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TESTInitiatorSkill_Q : CharacterSkill
{
    private TESTPlayerMovement _playerMovement;
    private FPSCameraLook _cameraLook;
    private Camera _playerCamera;
    private AudioListener _playerAudioListener;

    [SerializeField] private Transform _droneSpawnPoint;
    [SerializeField] private Drone _dronePrefab;

    [Header("드론 스캔 (Quick Outline 연동)")]
    [SerializeField] private float _scanRadius = 1.0f; 

    private Drone _currentTaserDrone;
    private bool _isDroneMode;
    private bool _exitRequested;

    public static bool IsLocalDroneMode { get; private set; }

    private TESTPlayerItem _playerItem;

   
    private HashSet<Outline> _activeOutlines = new HashSet<Outline>();

    private void Awake()
    {
        _playerMovement = GetComponent<TESTPlayerMovement>();
        _cameraLook = GetComponentInChildren<FPSCameraLook>(true);
        _playerCamera = GetComponentInChildren<Camera>(true);
        _playerAudioListener = GetComponentInChildren<AudioListener>(true);
        _playerItem = GetComponent<TESTPlayerItem>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
            return;

        if (SettingsMenu.Instance != null &&
            SettingsMenu.Instance.IsOpen)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            UseSkill();
        }

        if (_isDroneMode && _currentTaserDrone != null)
        {
            ScanEnemiesAroundDrone();
        }
    }

    protected override void UseSkill()
    {
        if (_isDroneMode)
            RequestDroneExit();
        else
            RequestDroneDeploy();
    }

    private void RequestDroneDeploy()
    {
        if (_isDroneMode)
            return;

        if (_currentTaserDrone != null)
            return;

        if (_dronePrefab == null)
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

        _currentTaserDrone = Instantiate(
            _dronePrefab,
            spawnPos,
            Quaternion.Euler(pitch, yaw, 0f)
        );

        DroneMovement movement =
            _currentTaserDrone.GetComponent<DroneMovement>();

        if (movement != null)
            movement.Init(_currentTaserDrone, true);

        Rigidbody rb =
            _currentTaserDrone.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _exitRequested = false;

        OnOwnerDroneSpawned(_currentTaserDrone);
    }

    private void RequestDroneExit()
    {
        if (_currentTaserDrone == null)
        {
            ForceReturnToPlayerCamera();
            return;
        }

        if (_exitRequested)
            return;

        _exitRequested = true;

        Drone drone = _currentTaserDrone;

        OnOwnerDroneDestroyed();

        if (drone != null)
            Destroy(drone.gameObject);
    }

    public void OnOwnerDroneSpawned(Drone drone)
    {
        _currentTaserDrone = drone;
        _exitRequested = false;

        SetLocalDroneMode(true);
    }

    public void OnOwnerDroneDestroyed()
    {
        _currentTaserDrone = null;
        _exitRequested = false;

        SetLocalDroneMode(false);
    }

    private void SetLocalDroneMode(bool active)
    {
        _isDroneMode = active;
        IsLocalDroneMode = active;

       
        if (!active)
        {
            ResetScannedOutlines();
        }

        if (_playerMovement != null)
            _playerMovement.enabled = !active;

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

        LocalDroneBodyProxy bodyProxy =
            GetComponent<LocalDroneBodyProxy>();

        if (bodyProxy != null)
            bodyProxy.SetVisible(active);

        if (_currentTaserDrone != null)
        {
            Camera droneCamera =
                _currentTaserDrone.GetComponentInChildren<Camera>(true);

            if (droneCamera != null)
            {
                droneCamera.gameObject.SetActive(active);
                droneCamera.enabled = active;
            }

            AudioListener droneListener =
                _currentTaserDrone.GetComponentInChildren<AudioListener>(true);

            if (droneListener != null)
            {
                droneListener.gameObject.SetActive(active);
                droneListener.enabled = active;
            }
        }

        if (DroneHUDUI.Instance != null)
            DroneHUDUI.Instance.SetVisible(active);
    }

   
    private void ScanEnemiesAroundDrone()
    {
        if (_currentTaserDrone == null) return;

        Collider[] hitColliders = Physics.OverlapSphere(_currentTaserDrone.transform.position, _scanRadius);
        HashSet<Outline> currentInRangeOutlines = new HashSet<Outline>();

        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Enemy"))
            {
               
                Outline outline = col.GetComponentInChildren<Outline>();
                if (outline != null)
                {
                    outline.enabled = true; 
                    currentInRangeOutlines.Add(outline);
                    _activeOutlines.Add(outline);
                }
            }
        }

        List<Outline> toRemove = new List<Outline>();
        foreach (var outline in _activeOutlines)
        {
            if (!currentInRangeOutlines.Contains(outline))
            {
                if (outline != null)
                {
                    outline.enabled = false;
                }
                toRemove.Add(outline);
            }
        }

        foreach (var outline in toRemove)
        {
            _activeOutlines.Remove(outline);
        }
    }

    private void ResetScannedOutlines()
    {
        foreach (var outline in _activeOutlines)
        {
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
        _activeOutlines.Clear();
    }

    private void ForceReturnToPlayerCamera()
    {
        ResetScannedOutlines();

        _isDroneMode = false;
        IsLocalDroneMode = false;

        if (_playerMovement != null)
            _playerMovement.enabled = true;

        if (_cameraLook != null)
            _cameraLook.CanMove = true;

        if (_playerCamera != null)
            _playerCamera.enabled = true;

        if (_playerAudioListener != null)
            _playerAudioListener.enabled = true;

        if (_playerItem != null)
            _playerItem.SetWeaponVisibility(true);

        if (_currentTaserDrone != null)
        {
            Camera droneCamera =
                _currentTaserDrone.GetComponentInChildren<Camera>(true);

            if (droneCamera != null)
            {
                droneCamera.enabled = false;
                droneCamera.gameObject.SetActive(false);
            }

            AudioListener droneListener =
                _currentTaserDrone.GetComponentInChildren<AudioListener>(true);

            if (droneListener != null)
            {
                droneListener.enabled = false;
                droneListener.gameObject.SetActive(false);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        LocalDroneBodyProxy bodyProxy =
            GetComponent<LocalDroneBodyProxy>();

        if (bodyProxy != null)
            bodyProxy.SetVisible(false);

        if (DroneHUDUI.Instance != null)
            DroneHUDUI.Instance.SetVisible(false);
    }
}