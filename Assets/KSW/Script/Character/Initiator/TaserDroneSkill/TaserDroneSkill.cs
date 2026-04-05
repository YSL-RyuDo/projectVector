using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaserDroneSkill : MonoBehaviour
{
    [SerializeField] private Drone _taserDronePrefab;
    private Drone _currentTaserDrone;
    private bool _isDroneMode = false;

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame) SetDroneMode(!_isDroneMode); // 테이저 드론 (Q키)
        if (Keyboard.current.eKey.wasPressedThisFrame) electricMagneticWave(); // 전기자석파 (E키)
        if (Keyboard.current.xKey.wasPressedThisFrame) dischargeOfElectricity(); // 방전 (X키)
    }

    public void SetDroneMode(bool active)
    {
        _isDroneMode = active;

        if (_isDroneMode)
        {
            if (_currentTaserDrone == null)
            {
                Vector3 spwanPos = Player.Instance.transform.position + Player.Instance.transform.forward * 1.5f + Vector3.up * 1.5f;
                Quaternion spawnRot = Quaternion.LookRotation(Player.Instance.transform.forward);
                _currentTaserDrone = Instantiate(_taserDronePrefab, spwanPos, spawnRot);
            }

            Player.Instance.localPlayerController.CanMove = false;
            FPSCameraLook.Instance.CanMove = false;
        }
        else
        {
            if (_currentTaserDrone != null)
            {
                Destroy(_currentTaserDrone.gameObject);
                _currentTaserDrone = null;
            }

            Player.Instance.localPlayerController.CanMove = true;
            FPSCameraLook.Instance.CanMove = true;

            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void electricMagneticWave()
    {

    }

    private void dischargeOfElectricity()
    {

    }
}