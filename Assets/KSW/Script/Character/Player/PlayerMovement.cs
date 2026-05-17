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

    // Network (Receive - 원격 플레이어 동기화용 변수)
    private Vector3 _targetPosition;
    private float _targetYaw;
    [SerializeField] private float _lerpSpeed = 15f; // 원격 캐릭터 이동 부드러움 강도

    private CharacterController _cc;
    private FPSCameraLook _cam;
    private Animator _animator;
    private PlayerIdentity _playerIdentity;

    private float _verticalVelocity;
    private bool _isMine; // 내 캐릭터인지 여부 저장

    public event Action OnJumpEvent;

    public float Speed
    {
        get => _speed;
        set => _speed = value;
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = GetComponentInChildren<FPSCameraLook>();
        _animator = GetComponentInChildren<Animator>();
        _playerIdentity = GetComponent<PlayerIdentity>();

        _lastSentPos = transform.position;
        _lastSentYaw = _cam.GetYaw();

        _targetPosition = transform.position;
    }

    void Start()
    {
        // 내 캐릭터인지 외부 확인용 변수 캐싱
        _isMine = (_playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId);

        if (_cam != null)
        {
            _lastSentYaw = _cam.GetYaw();
            _targetYaw = _cam.GetYaw();
        }
        else
        {
            _targetYaw = transform.eulerAngles.y;
        }
    }

    void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;

        if (_isMine)
        {
            // "내 캐릭터"라면 직접 조종하고 서버로 패킷 전송
            movement();
            networkSend();
        }
        else
        {
            // "원격 캐릭터"라면 서버에서 받은 정보로 부드럽게 이동
            SyncRemotePlayerMovement();
        }
    }

    // 로컬 플레이어(나)의 직접적인 조종 로직
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
        {
            bool isMoving = input.sqrMagnitude > 0;
            _animator.SetBool("Forward", isMoving);
        }
    }

    // 외부(NetworkManager 등 패킷 수신부)에서 호출해 줄 함수
    // 서버로부터 이 캐릭터의 최신 좌표와 방향을 받으면 목적지를 갱신
    public void OnReceiveRemoteMove(Vector3 serverPos, float serverYaw)
    {
        // 보안용: 실수로 내 캐릭터 패킷을 내가 받아서 강제 이동시키는 것 방지
        if (_isMine) return;

        // 너무 멀리 떨어져 있으면 렉으로 간주하고 순간이동
        if (Vector3.Distance(transform.position, serverPos) > 5f)
        {
            _cc.enabled = false; // 순간이동 시 CC 충돌 꼬임 방지
            transform.position = serverPos;
            _cc.enabled = true;
            _targetPosition = serverPos;
        }
        else
        {
            _targetPosition = serverPos;
        }

        _targetYaw = serverYaw;
    }

    // 원격 플레이어(타인)를 서버 목적지 좌표로 부드럽게 이동시키고 애니메이션을 제어하는 로직
    private void SyncRemotePlayerMovement()
    {
        // 위치 및 회전 보간(Lerp) 처리
        Vector3 prevPosition = transform.position;

        _cc.enabled = false;
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, _targetYaw, 0), Time.deltaTime * _lerpSpeed);
        _cc.enabled = true;

        // 이동량 기반 원격 캐릭터 애니메이션 처리
        if (_animator != null)
        {
            Vector3 movementDelta = transform.position - prevPosition;
            float horizontalSpeed = new Vector3(movementDelta.x, 0, movementDelta.z).magnitude / Time.deltaTime;

            // 정지 상태인지 달리는 상태인지 판단 (임계값 0.1f)
            bool isMoving = horizontalSpeed > 0.1f;
            _animator.SetBool("Forward", isMoving);

            // 공중 상태 애니메이션 처리 (Y축 변화율 확인)
            float verticalSpeed = movementDelta.y / Time.deltaTime;
            if (verticalSpeed < -1f)
            {
                _animator.SetBool("Down", true);
            }
            else if (verticalSpeed >= 0)
            {
                _animator.SetBool("Down", false);
            }
        }
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