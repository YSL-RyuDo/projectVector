using UnityEngine;
using UnityEngine.InputSystem;

public class DroneMovement : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _deceleration = 10f;
    [SerializeField, Min(.1f)] private float _verticalSpeed = 2f;
    [SerializeField, Min(.1f)] private float _verticalAcceleration = 4f;
    [SerializeField, Min(.1f)] private float _verticalDeceleration = 8f;

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
    private float _ceiling;
    private float _launchHeight;
    public float HeightAboveLaunch => Mathf.Max(0, transform.position.y - _launchHeight);
    public float GroundClearance { get; private set; } = float.NaN;
    public bool CeilingLimited { get; private set; }
    public Vector3 Velocity => _currentVelocity;
    public static bool InputBlocked => GameplayActionPolicy.MenuBlocksInput || Game.Pings.PingSystem.WheelOpen;

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
        _launchHeight = transform.position.y;
        _ceiling = _launchHeight + DroneFlightSafety.MaxClimb;

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
        if (InputBlocked || !InitiatorSkill_Q.IsLocalDroneMode) return;
        if (Mouse.current == null)
            return;

        if (_cameraTransform == null)
            return;

        // Ping wheel consumes look input; keep recoil, movement and networking updates.
        Vector2 mouseDelta = Game.Pings.PingSystem.WheelOpen
            ? Vector2.zero
            : Mouse.current.delta.ReadValue();

        _yaw += mouseDelta.x * _mouseSensitivity;
        _pitch -= mouseDelta.y * _mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleInput()
    {
        if (Keyboard.current == null || InputBlocked || !InitiatorSkill_Q.IsLocalDroneMode)
        { _targetVelocity = Vector3.zero; return; }
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
            ? Vector3.Scale(moveDir.normalized,new Vector3(_moveSpeed,_verticalSpeed,_moveSpeed))
            : Vector3.zero;
    }

    private void Move()
    {
        float accelRate =
            _targetVelocity.magnitude > 0.01f
                ? _acceleration
                : _deceleration;

        float vertical=Mathf.MoveTowards(_currentVelocity.y,_targetVelocity.y,
            (Mathf.Abs(_targetVelocity.y)>.01f ? _verticalAcceleration : _verticalDeceleration)*Time.fixedDeltaTime);
        _currentVelocity=Vector3.MoveTowards(new Vector3(_currentVelocity.x,0,_currentVelocity.z),
            new Vector3(_targetVelocity.x,0,_targetVelocity.z),accelRate*Time.fixedDeltaTime);
        _currentVelocity.y=vertical;

        Vector3 requested = _currentVelocity * Time.fixedDeltaTime;
        Vector3 allowed = DroneFlightSafety.Constrain(transform.position, requested, _ceiling, 0.48f, transform);
        CeilingLimited = requested.y > 0.001f && allowed.y < requested.y - 0.0001f;
        _currentVelocity = allowed / Time.fixedDeltaTime;

        if (_rb != null)
        {
            _rb.linearVelocity = _currentVelocity;
        }
        DroneFlightSafety.TryGetGroundClearance(transform.position,transform,out float clearance);
        GroundClearance=clearance;
        if (DroneHUDUI.Instance != null)
            DroneHUDUI.Instance.SetTelemetry(GroundClearance, _currentVelocity.magnitude, CeilingLimited);
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
