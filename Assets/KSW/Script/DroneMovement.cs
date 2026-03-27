using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneMovement : MonoBehaviour
{
    [SerializeField] private DroneCamera _droneCamera;

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
        handleInput();
    }

    private void FixedUpdate()
    {
        move();
    }

    private void move()
    {
        float accelRate = (_targetVelocity.magnitude > 0.01f) ? _acceleration : _deceleration;

        _currentVelocity = Vector3.MoveTowards(_currentVelocity, _targetVelocity, accelRate * Time.fixedDeltaTime);
        _rb.linearVelocity = _currentVelocity;
    }

    private void handleInput()
    {
        Vector3 inputDir = Vector3.zero;
        Transform cam = _droneCamera.transform;

        if (Keyboard.current.wKey.isPressed)
            inputDir += cam.forward;
        if (Keyboard.current.sKey.isPressed)
            inputDir -= cam.forward;
        if (Keyboard.current.dKey.isPressed)
            inputDir += cam.right;
        if (Keyboard.current.aKey.isPressed)
            inputDir -= cam.right;

        inputDir.Normalize();
        _targetVelocity = inputDir * _moveSpeed;
    }
}