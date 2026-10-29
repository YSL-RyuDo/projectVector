using UnityEngine;

public class DroneCamera : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera _camera;

    [Header("FOV")]
    [SerializeField] private float _normalFOV = 60f;
    [SerializeField] private float _adsFOV = 40f;

    private bool _isADS;

    private void Awake()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
    }

    public Camera Camera => _camera;

    public float GetYaw()
    {
        Transform root = transform.parent;

        if (root != null)
            return root.eulerAngles.y;

        return transform.eulerAngles.y;
    }

    public float GetPitch()
    {
        float pitch = transform.localEulerAngles.x;

        if (pitch > 180f)
            pitch -= 360f;

        return pitch;
    }

    public void AddRecoil(float amount)
    {
        // 드론 회전은 DroneMovement가 담당한다.
        // 여기서 카메라 회전을 건드리면 네트워크 회전 동기화가 꼬인다.
    }

    public void SetADS(bool ads)
    {
        _isADS = ads;

        if (_camera == null)
            return;

        _camera.fieldOfView = _isADS
            ? _adsFOV
            : _normalFOV;
    }
}