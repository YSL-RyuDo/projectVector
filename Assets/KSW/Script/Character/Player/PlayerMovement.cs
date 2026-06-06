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

    [SerializeField] private float sendInterval = 0.05f; // 20Hz
    private float lastSendTime;
    private Vector3 lastSentPos;
    private float lastSentYaw;
    [SerializeField] private float positionSendThreshold = 0.02f;
    [SerializeField] private float yawSendThreshold = 1.0f;

    private int _currentAnimIndex = -1;
    private bool _isCrouching = false;

    public float Speed
    {
        get => _speed;
        set => _speed = value;
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = GetComponentInChildren<FPSCameraLook>(true);
        _animator = GetComponentInChildren<Animator>();
        _playerIdentity = GetComponent<PlayerIdentity>();

        lastSentPos = transform.position;
        lastSentYaw = transform.eulerAngles.y;

        _targetPosition = transform.position;
        _targetYaw = transform.eulerAngles.y;
    }

    void Start()
    {
        if (_cam != null)
        {
            lastSentYaw = _cam.GetYaw();
            _targetYaw = _cam.GetYaw();
        }
        else
        {
            lastSentYaw = transform.eulerAngles.y;
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
            //networkSend();
            TrySendMoveState();
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

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            _isCrouching = !_isCrouching;
            if (_animator != null) _animator.SetBool("Crouch", _isCrouching);
        }

        if (_cc.isGrounded)
        {
            if (_verticalVelocity < -2f) _verticalVelocity = -2f;

            if (_animator != null) _animator.SetBool("Down", false);

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _verticalVelocity = _jumpPower;
                _isCrouching = false; // 점프 시 앉기 해제

                OnJumpEvent?.Invoke();

                if (_animator != null)
                {
                    _animator.SetBool("Crouch", false);
                    _animator.SetTrigger("Jump");
                }
            }
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        Vector3 velocity = (move.normalized * _speed) + (Vector3.up * _verticalVelocity);
        _cc.Move(velocity * Time.deltaTime);

        bool isMoving = input.sqrMagnitude > 0;

        if (_animator != null)
        {
            _animator.SetBool("Forward", isMoving);
        }

        UpdateAnimationStateLog(isMoving, _isCrouching);
    }

    // 외부(NetworkManager 등 패킷 수신부)에서 호출해 줄 함수
    public void OnReceiveRemoteMove(Vector3 serverPos, float serverYaw)
    {
        if (_isMine) return;

        if (Vector3.Distance(transform.position, serverPos) > 5f)
        {
            _cc.enabled = false;
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

    private void SyncRemotePlayerMovement()
    {
        Vector3 prevPosition = transform.position;

        _cc.enabled = false;
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, _targetYaw, 0), Time.deltaTime * _lerpSpeed);
        _cc.enabled = true;

        // 이동량 기반 원격 캐릭터 애니메이션 처리
        Vector3 movementDelta = transform.position - prevPosition;
        float horizontalSpeed = new Vector3(movementDelta.x, 0, movementDelta.z).magnitude / Time.deltaTime;

        // 정지 상태인지 달리는 상태인지 판단 (임계값 0.1f)
        bool isMoving = horizontalSpeed > 0.1f;

        bool isRemoteCrouching = false;
        if (_animator != null)
        {
            // _animator.SetBool("Forward", isMoving);
            isRemoteCrouching = _animator.GetBool("Crouch");
        }

        UpdateAnimationStateLog(isMoving, isRemoteCrouching);
    }

    private void UpdateAnimationStateLog(bool isMoving, bool isCrouching)
    {
        int newIndex = 0; // 기본값 정지 (0)

        if (isCrouching)
        {
            newIndex = 2; // 앉기 (2)
        }
        else if (isMoving)
        {
            newIndex = 1; // 걷기 (1)
        }

        // 상태가 변경되었을 때만 Debug.Log 출력
        if (_currentAnimIndex != newIndex)
        {
            _currentAnimIndex = newIndex;
            string stateName = _currentAnimIndex == 0 ? "정지" : (_currentAnimIndex == 1 ? "걷기" : "앉기");

            Debug.Log($"[{gameObject.name}] 애니메이션 상태 변경 -> {stateName} ({_currentAnimIndex}) | IsMine: {_isMine}");
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
                _isCrouching = false; // 점프 시 앉기 상태 해제
            }
        }
    }

    public void SetLocalPlayer(bool isMine)
    {
        _isMine = isMine;
        _cam = GetComponentInChildren<FPSCameraLook>(true);

        if (_cam != null)
        {
            lastSentYaw = _cam.GetYaw();
            _targetYaw = _cam.GetYaw();
        }
        else
        {
            lastSentYaw = transform.eulerAngles.y;
            _targetYaw = transform.eulerAngles.y;
        }

        Debug.Log(
            $"[PLAYER MOVEMENT SET LOCAL] " +
            $"object={gameObject.name}, " +
            $"isMine={_isMine}, " +
            $"playerId={_playerIdentity?.PlayerId}"
        );
    }

    private void TrySendMoveState()
    {
        if (!_isMine) return;
        if (NetworkManager.Instance == null) return;
        if (Time.time - lastSendTime < sendInterval) return;

        Vector3 currentPos = transform.position;
        float currentYaw = transform.eulerAngles.y;

        bool positionChanged = Vector3.Distance(currentPos, lastSentPos) >= positionSendThreshold;
        bool yawChanged = Mathf.Abs(Mathf.DeltaAngle(currentYaw, lastSentYaw)) >= yawSendThreshold;

        if (!positionChanged && !yawChanged) return;

        lastSendTime = Time.time;
        lastSentPos = currentPos;
        lastSentYaw = currentYaw;

        NetworkManager.Instance.SendMoveState(
            currentPos.x,
            currentPos.y,
            currentPos.z,
            currentYaw
        );
    }
}