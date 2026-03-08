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
        if (Keyboard.current.tKey.wasPressedThisFrame) taserDrone();
        //electricMagneticWave();
        //scanner();
        //dischargeOfElectricity();
    }

    /// <summary>
    /// 테이저 드론
    /// </summary>
    private void taserDrone()
    {
        if (!_isDroneMode)
        {
            Vector3 spwanPos = Player.Instance.transform.position + Player.Instance.transform.forward * 1.5f + Vector3.up * 1.5f;
            Quaternion spawnRot = Quaternion.LookRotation(Player.Instance.transform.forward);
            _currentTaserDrone = Instantiate(_taserDronePrefab, spwanPos, spawnRot);
            _isDroneMode = true;

            Player.Instance.Movement.CanMove = false;
            FPSCameraLook.Instance.CanMove = false;
        }

        else
        {
            Destroy(_currentTaserDrone.gameObject);
            _isDroneMode = false;

            Player.Instance.Movement.CanMove = true;
            FPSCameraLook.Instance.CanMove = true;
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