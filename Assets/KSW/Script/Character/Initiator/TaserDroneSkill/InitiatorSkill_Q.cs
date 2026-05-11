using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InitiatorSkill_Q : CharacterSkill
{
    private PlayerMovement _playerMovement;
    private FPSCameraLook _camera;

    [SerializeField] private Drone _taserDronePrefab;
    private Drone _currentTaserDrone;
    private bool _isDroneMode = false;

    void Start()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _camera = GetComponentInChildren<FPSCameraLook>();
    }

    void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame) UseSkill();
    }

    protected override void UseSkill()
    {
        SetDroneMode(!_isDroneMode);
    }
    
    public void SetDroneMode(bool active)
    {
        _isDroneMode = active;

        switch(active)
        {
            case true:
                if (_isDroneMode)
                {
                    if (_currentTaserDrone == null)
                    {
                        Vector3 spwanPos = _playerMovement.transform.position + _playerMovement.transform.forward * 2.5f + Vector3.up * 2.5f;
                        Quaternion spawnRot = Quaternion.LookRotation(_playerMovement.transform.forward);
                        _currentTaserDrone = Instantiate(_taserDronePrefab, spwanPos, spawnRot);

                        TaserGun gun = _currentTaserDrone.GetComponentInChildren<TaserGun>();
                        if (gun != null)
                        {
                            gun.Setup(this);
                        }
                    }

                    _playerMovement.CanMove = false;
                    _camera.CanMove = false;
                }
                break;

             case false:
                if (_currentTaserDrone != null)
                {
                    Destroy(_currentTaserDrone.gameObject);
                    _currentTaserDrone = null;
                }

                _playerMovement.CanMove = true;
                _camera.CanMove = true;

                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }
}