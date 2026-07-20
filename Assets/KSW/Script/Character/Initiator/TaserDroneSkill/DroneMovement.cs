using UnityEngine;
using UnityEngine.InputSystem;

public class DroneMovement : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _deceleration = 10f;

    [Header("Look Settings")]
    [SerializeField] private float _mouseSensitivity = 0.12f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;

    [Header("Remote Interpolation")]
    [SerializeField] private float _remoteLerpSpeed = 15f;

    private Drone _drone;
    private Rigidbody _rb;

    private bool _isMine;

    private Vector3 _currentVelocity;
    private Vector3 _targetVelocity;

    private float _yaw;
    private float _pitch;

    private Vector3 _remoteTargetPosition;
    private Quaternion _remoteTargetRotation;
    private Quaternion _remoteTargetCameraRotation;

    private float _sendTimer;
    private const float SEND_INTERVAL = 0.05f; // 20Hz

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (_cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null)
                _cameraTransform = cam.transform;
        }
    }

    public void Init(Drone drone, bool isMine)
    {
        _drone = drone;
        _isMine = isMine;

        if (_cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null)
                _cameraTransform = cam.transform;
        }

        DisableConflictingCameraLookScripts();

        _yaw = transform.eulerAngles.y;
        _pitch = 0f;

        if (_cameraTransform != null)
        {
            float currentPitch = _cameraTransform.localEulerAngles.x;
            if (currentPitch > 180f)
                currentPitch -= 360f;

            _pitch = currentPitch;
        }

        _remoteTargetPosition = transform.position;
        _remoteTargetRotation = transform.rotation;
        _remoteTargetCameraRotation = _cameraTransform != null
            ? _cameraTransform.localRotation
            : Quaternion.identity;

        if (_rb != null)
        {
            _rb.isKinematic = !isMine;
        }

        Camera cam2 = GetComponentInChildren<Camera>(true);
        if (cam2 != null)
        {
            cam2.enabled = isMine;
        }

        AudioListener listener = GetComponentInChildren<AudioListener>(true);
        if (listener != null)
        {
            listener.enabled = isMine;
        }
    }
    private void Update()
    {
        if (_isMine)
        {
            HandleLook();
            HandleInput();
            SendStateTick();
        }
        else
        {
            InterpolateRemote();
        }
    }

    private void FixedUpdate()
    {
        if (!_isMine)
            return;

        Move();
    }

    private void HandleLook()
    {
        if (Mouse.current == null)
            return;

        if (_cameraTransform == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        _yaw += mouseDelta.x * _mouseSensitivity;
        _pitch -= mouseDelta.y * _mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleInput()
    {
        if (_cameraTransform == null)
            return;

        Vector3 moveDir = Vector3.zero;

        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        if (Keyboard.current.wKey.isPressed) moveDir += camForward;
        if (Keyboard.current.sKey.isPressed) moveDir -= camForward;
        if (Keyboard.current.dKey.isPressed) moveDir += camRight;
        if (Keyboard.current.aKey.isPressed) moveDir -= camRight;

        if (Keyboard.current.spaceKey.isPressed) moveDir += Vector3.up;
        if (Keyboard.current.leftCtrlKey.isPressed) moveDir += Vector3.down;

        _targetVelocity = moveDir.sqrMagnitude > 0.001f
            ? moveDir.normalized * _moveSpeed
            : Vector3.zero;
    }

    private void Move()
    {
        float accelRate =
            _targetVelocity.magnitude > 0.01f
                ? _acceleration
                : _deceleration;

        _currentVelocity = Vector3.MoveTowards(
            _currentVelocity,
            _targetVelocity,
            accelRate * Time.fixedDeltaTime
        );

        if (_rb != null)
        {
            _rb.linearVelocity = _currentVelocity;
        }
    }

    private void SendStateTick()
    {
        if (_drone == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        _sendTimer += Time.deltaTime;

        if (_sendTimer < SEND_INTERVAL)
            return;

        _sendTimer = 0f;

        Vector3 pos = transform.position;

        NetworkManager.Instance.SendDroneState(
            _drone.DroneId,
            pos.x,
            pos.y,
            pos.z,
            _yaw,
            _pitch
        );
    }

    public void ApplyRemoteState(
        Vector3 position,
        float yaw,
        float pitch)
    {
        if (_isMine)
            return;

        _remoteTargetPosition = position;
        _remoteTargetRotation = Quaternion.Euler(0f, yaw, 0f);
        _remoteTargetCameraRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void InterpolateRemote()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            _remoteTargetPosition,
            _remoteLerpSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _remoteTargetRotation,
            _remoteLerpSpeed * Time.deltaTime
        );

        if (_cameraTransform != null)
        {
            _cameraTransform.localRotation = Quaternion.Slerp(
                _cameraTransform.localRotation,
                _remoteTargetCameraRotation,
                _remoteLerpSpeed * Time.deltaTime
            );
        }
    }

    private void DisableConflictingCameraLookScripts()
    {
        if (_cameraTransform == null)
            return;

        DroneCamera droneCameraLook =
            _cameraTransform.GetComponent<DroneCamera>();

        if (droneCameraLook != null)
        {
            droneCameraLook.enabled = false;
            Debug.Log("[DRONE MOVEMENT] DroneCamera look script disabled");
        }

        FPSCameraLook fpsCameraLook =
            _cameraTransform.GetComponent<FPSCameraLook>();

        if (fpsCameraLook != null)
        {
            fpsCameraLook.enabled = false;
            Debug.Log("[DRONE MOVEMENT] FPSCameraLook disabled on drone camera");
        }
    }
}