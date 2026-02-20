using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _jumpPower = 8f;

    [Header("Network")]
    [SerializeField] private float _sendInterval = 0.05f; // 20Hz

    private CharacterController _controller;
    private FPSCameraLook _camera;

    private float _verticalVelocity;

    private Vector3 _lastSentPos;
    private float _lastSentYaw;
    private float _sendTimer;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _camera = GetComponentInChildren<FPSCameraLook>();

        _lastSentPos = transform.position;
        _lastSentYaw = _camera.GetYaw();
    }

    private void Update()
    {
        handleMovement();
        handleNetworkSend();
    }

    private void handleMovement()
    {
        Vector2 input = Vector2.zero;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.isPressed) input.x -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.wKey.isPressed) input.y += 1;

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0f) _verticalVelocity = -2f;
            if (kb.spaceKey.wasPressedThisFrame) _verticalVelocity = _jumpPower;
        }

        else _verticalVelocity += _gravity * Time.deltaTime;

        Vector3 velocity = move.normalized * _speed + Vector3.up * _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
    }

    private void handleNetworkSend()
    {
        _sendTimer += Time.deltaTime;

        if (_sendTimer < _sendInterval) return;

        _sendTimer = 0f;

        Vector3 pos = transform.position;
        float yaw = _camera.GetYaw();

        Vector2 curXZ = new Vector2(pos.x, pos.z);
        Vector2 lastXZ = new Vector2(_lastSentPos.x, _lastSentPos.z);

        bool movedXZ = (curXZ - lastXZ).sqrMagnitude > 0.0004f;
        bool movedY = Mathf.Abs(pos.y - _lastSentPos.y) > 0.01f;
        bool rotated = Mathf.Abs(yaw - _lastSentYaw) > 0.5f;

        if (!movedXZ && !movedY && !rotated) return;

        _lastSentPos = pos;
        _lastSentYaw = yaw;

        NetworkManager.Instance.SendMoveState(pos.x, pos.y, pos.z, yaw);
    }
}