using UnityEngine;
using UnityEngine.InputSystem;

public class DroneCamera : MonoBehaviour
{
    [Header("Mouse")]
    [SerializeField] private float _baseSensitivity = 0.2f;
    [SerializeField] private float _adsSensitivityMultiplier = 0.5f;

    [Header("Recoil")]
    [SerializeField] private float _recoilReturnSpeed = 10f;

    private float _xRotation = 0f;
    private float _yaw = 0f;
    private float _recoilOffset = 0f;
    private bool _isADS = false;

    private Transform _target;

    private void Start()
    {
        _target = transform.parent;
        _yaw = _target.eulerAngles.y;
        _xRotation = transform.localEulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float sensitivity = _baseSensitivity * (_isADS ? _adsSensitivityMultiplier : 1f);
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        _yaw += mouseX;
        _target.rotation = Quaternion.Euler(0f, _yaw, 0f);

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -80f, 80f);

        _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, _recoilReturnSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(_xRotation + _recoilOffset, 0f, 0f);
    }

    public float GetYaw() => _yaw;
    public void AddRecoil(float amount) => _recoilOffset -= amount;
    public void SetADS(bool ads) => _isADS = ads;
}