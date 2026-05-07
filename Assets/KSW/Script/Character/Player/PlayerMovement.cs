using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Movement
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _jumpPower = 8f;

    // 외부 변경용
    public bool CanMove = true;

    // Network
    private float _sendInterval = 0.05f; // 20hz
    private float _lastSentYaw;
    private float _sendTimer;
    private Vector3 _lastSentPos;

    private CharacterController _cc;
    private FPSCameraLook _cam;
    private Animator _animator;

    private float _verticalVelocity;

    public event Action OnJumpEvent;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = GetComponentInChildren<FPSCameraLook>();
        _animator = GetComponentInChildren<Animator>();

        _lastSentPos = transform.position;
        _lastSentYaw = _cam.GetYaw();
    }

    void Update()
    {
        movement();
        networkSend();
    }

    private void movement()
    {
        if (!CanMove) return;

        Vector2 input = getInput();
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        if (_cc.isGrounded)
        {
            if (_verticalVelocity < -2f) _verticalVelocity = -2f;

            if (_animator != null) _animator.SetBool("Down", false);

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _verticalVelocity = _jumpPower;

                OnJumpEvent?.Invoke();

                if (_animator != null) _animator.SetTrigger("Jump");
            }
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
            if (_animator != null && _verticalVelocity < 0 && !_animator.GetBool("Down"))
            {
                _animator.SetBool("Down", true);
            }
        }

        Vector3 velocity = (move.normalized * _speed) + (Vector3.up * _verticalVelocity);
        _cc.Move(velocity * Time.deltaTime);

        if (_animator != null)
            _animator.SetBool("Walk", input.sqrMagnitude > 0 && _cc.isGrounded);
    }

    public void SetVerticalVelocity(float force)
    {
        _verticalVelocity = force;
    }

    private Vector2 getInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;
        Vector2 input = Vector2.zero;
        if (kb.aKey.isPressed) input.x -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.wKey.isPressed) input.y += 1;
        return input;
    }

    public void ApplyJumpForce(float force)
    {
        if (_cc.isGrounded)
        {
            _verticalVelocity = force;
            if (_animator != null)
            {
                _animator.SetBool("Crouch", false);
                _animator.SetTrigger("Jump");
            }
        }
    }

    private void networkSend()
    {
        _sendTimer += Time.deltaTime;
        if (_sendTimer < _sendInterval)
            return;

        _sendTimer = 0f;

        Vector3 pos = transform.position;
        float yaw = _cam.GetYaw();

        Vector2 curXZ = new Vector2(pos.x, pos.z);
        Vector2 lastXZ = new Vector2(_lastSentPos.x, _lastSentPos.z);

        bool movedXZ = (curXZ - lastXZ).sqrMagnitude > 0.0004f;
        bool movedY = Mathf.Abs(pos.y - _lastSentPos.y) > 0.01f;
        bool rotated = Mathf.Abs(yaw - _lastSentYaw) > 0.5f;

        if (!movedXZ && !movedY && !rotated) return;

        _lastSentPos = pos;
        _lastSentYaw = yaw;

        //NetworkManager.Instance.SendMoveState(pos.x, pos.y, pos.z, yaw);
    }
}