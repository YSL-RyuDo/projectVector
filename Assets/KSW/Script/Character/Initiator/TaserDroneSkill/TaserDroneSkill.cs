using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaserDroneSkill : MonoBehaviour
{
    [SerializeField] private Drone _taserDronePrefab;
    private Drone _currentTaserDrone;
    private bool _isDroneMode = false;

    private void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame) SetDroneMode(!_isDroneMode);
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
                        Vector3 spwanPos = Player.Instance.transform.position + Player.Instance.transform.forward * 2.5f + Vector3.up * 2.5f;
                        Quaternion spawnRot = Quaternion.LookRotation(Player.Instance.transform.forward);
                        _currentTaserDrone = Instantiate(_taserDronePrefab, spwanPos, spawnRot);

                        TaserGun gun = _currentTaserDrone.GetComponentInChildren<TaserGun>();
                        if (gun != null)
                        {
                            gun.Setup(this);
                        }
                    }

                    Player.Instance.Movement.CanMove = false;
                    FPSCameraLook.Instance.CanMove = false;
                }
                break;

             case false:
                if (_currentTaserDrone != null)
                {
                    Destroy(_currentTaserDrone.gameObject);
                    _currentTaserDrone = null;
                }

                Player.Instance.Movement.CanMove = true;
                FPSCameraLook.Instance.CanMove = true;

                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }
}