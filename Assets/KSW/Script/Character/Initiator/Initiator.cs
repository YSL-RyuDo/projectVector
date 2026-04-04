using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Initiator : MonoBehaviour
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
        if (Keyboard.current.cKey.wasPressedThisFrame) scanner(); // 스캐너 (C키)
        if (Keyboard.current.eKey.wasPressedThisFrame) electricMagneticWave(); // 전기자석파 (E키)
        if (Keyboard.current.xKey.wasPressedThisFrame) dischargeOfElectricity(); // 방전 (X키)
    }

    /// <summary>
    /// 테이저 드론
    /// </summary>
    public void SetDroneMode(bool active)
    {
        _isDroneMode = active;

        if (_isDroneMode)
        {
            // 드론 소환 로직
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
            // 드론 해제 로직
            if (_currentTaserDrone != null)
            {
                Destroy(_currentTaserDrone.gameObject);
                _currentTaserDrone = null; // 참조 초기화 필수
            }

            Player.Instance.localPlayerController.CanMove = true;
            FPSCameraLook.Instance.CanMove = true;

            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    /// <summary>
    /// 전기자석파
    /// </summary>
    private void electricMagneticWave()
    {

    }

    /// <summary>
    /// 스캐너
    /// </summary>
    private void scanner()
    {

    }

    /// <summary>
    /// 방전(궁극기)
    /// </summary>
    private void dischargeOfElectricity()
    {

    }
}