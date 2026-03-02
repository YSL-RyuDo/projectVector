using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraLook : MonoBehaviour
{
    [Header("Mouse")]
    // 기본 마우스 감도
    [SerializeField] private float _baseSensitivity = 0.02f;
    // ADS(조준) 상태일 때 감도 배율 (0.5면 절반으로 감소)
    [SerializeField] private float _adsSensitivityMultiplier = 0.5f;

    [Header("Recoil")]
    // 반동이 원래 위치로 돌아오는 속도
    [SerializeField] private float _recoilReturnSpeed = 10f;

    // 카메라 상하 회전값
    private float _xRotation = 0f;
    // 플레이어 좌우 회전값
    private float _yaw = 0f;
    // 반동으로 인해 추가되는 상하 회전값
    private float _recoilOffset = 0f;
    // 현재 ADS(조준) 상태인지 여부
    private bool _isADS = false;

    private Transform _target;

    private void Start()
    {
        _target = transform.parent;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // ADS(조준) 상태면 감도 낮춤
        float sensitivity = _baseSensitivity * (_isADS ? _adsSensitivityMultiplier : 1f);
        
        // 마우스 이동값 읽기
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        // 좌우 회전
        _yaw += mouseX;
        _target.rotation = Quaternion.Euler(0f, _yaw, 0f);

        // 상하 회전
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -80f, 80f);

        // 위아래 각도 제한
        _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, _recoilReturnSpeed * Time.deltaTime);

        // 반동 복구 처리
        transform.localRotation = Quaternion.Euler(_xRotation + _recoilOffset, 0f, 0f);
    }

    /// <summary>
    /// 좌우 회전값 반환
    /// </summary>
    /// <returns></returns>
    public float GetYaw()
    {
        return _yaw;
    }

    /// <summary>
    /// 총 발사 시 호출
    /// </summary>
    /// <param name="amount"></param>
    public void AddRecoil(float amount)
    {
        _recoilOffset -= amount;
    }

    /// <summary>
    /// ADS(조준) 상태 설정
    /// </summary>
    /// <param name="ads"></param>
    public void SetADS(bool ads)
    {
        _isADS = ads;
    }
}