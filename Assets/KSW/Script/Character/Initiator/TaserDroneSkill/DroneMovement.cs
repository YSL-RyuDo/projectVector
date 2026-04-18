using UnityEngine;
using UnityEngine.InputSystem;

public class DroneMovement : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _deceleration = 10f;

    private Vector3 _currentVelocity;
    private Vector3 _targetVelocity;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        float accelRate = (_targetVelocity.magnitude > 0.01f) ? _acceleration : _deceleration;
        _currentVelocity = Vector3.MoveTowards(_currentVelocity, _targetVelocity, accelRate * Time.fixedDeltaTime);
        _rb.linearVelocity = _currentVelocity;
    }

    private void HandleInput()
    {
        if (_cameraTransform == null) return;

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

        if (moveDir.sqrMagnitude > 0.001f)
        {
            _targetVelocity = moveDir.normalized * _moveSpeed;
        }
        else
        {
            _targetVelocity = Vector3.zero;
        }
    }
}