using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region 변수, 프로퍼티
    // Movement
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _jumpPower = 8f;

    // 외부 변경용
    public bool CanMove = true;

    // Network (Receive - 원격 플레이어 동기화용 변수)
    private Vector3 _targetPosition;
    private float _targetYaw;
    private float _targetPitch;
    private float lastSentPitch;

    [SerializeField] private float pitchSendThreshold = 1.0f;
    [SerializeField] private float _lerpSpeed = 15f; // 원격 캐릭터 이동 부드러움 강도

    private CharacterController _cc;
    private FPSCameraLook _cam;
    private Animator _animator;
    private PlayerIdentity _playerIdentity;

    private float _verticalVelocity;
    [SerializeField] private bool _isMine; // 내 캐릭터인지 여부 저장

    public event Action OnJumpEvent;

    [SerializeField] private float sendInterval = 0.05f; // 20Hz
    private float lastSendTime;
    private Vector3 lastSentPos;
    private float lastSentYaw;
    [SerializeField] private float positionSendThreshold = 0.02f;
    [SerializeField] private float yawSendThreshold = 1.0f;

    private bool _isCrouching = false;
    private Vector3 _standingPos;
    private Vector3 _crouchingPos;

    [Header("카메라 높이 설정")]
    [SerializeField] private float _crouchHeightOffset = 0.5f; // 얼마나 내릴지
    [SerializeField] private float _crouchSpeed = 8f;         // 카메라 움직임 속도

    private int _currentAnimIndex = -1;

    private byte _targetAnimIndex = 0;
    private byte lastSentAnimIndex = 255;

    private PlayerItem _playerItem;

    public float Speed
    {
        get => _speed;
        set => _speed = value;
    }
    #endregion

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = GetComponentInChildren<FPSCameraLook>(true);
        _animator = GetComponentInChildren<Animator>();
        _playerIdentity = GetComponent<PlayerIdentity>();
        _playerItem = GetComponent<PlayerItem>();

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

            lastSentPitch = _cam.GetPitch();
            _targetPitch = _cam.GetPitch();
        }
        else
        {
            lastSentYaw = transform.eulerAngles.y;
            _targetYaw = transform.eulerAngles.y;

            lastSentPitch = 0f;
            _targetPitch = 0f;
        }

        _standingPos = _cam.transform.localPosition;
        _crouchingPos = _standingPos - new Vector3(0, _crouchHeightOffset, 0);
    }

    void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;

        if (_isMine)
        {
            // "내 캐릭터"라면 직접 조종하고 서버로 패킷 전송
            movement();
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
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_playerItem != null)
            {
                Weapon currentWeapon = _playerItem.GetCurrentWeapon();
                if (currentWeapon != null && currentWeapon.GetWeaponType() == WeaponData.WeaponType.Knife)
                {
                    if (_animator != null)
                    {
                        _animator.SetTrigger("Knife_Attack");
                    }
                }
            }
        }

        bool isMoving = input.sqrMagnitude > 0;

        Vector3 targetPos = _isCrouching ? _crouchingPos : _standingPos;
        _cam.transform.localPosition = Vector3.Lerp(_cam.transform.localPosition, targetPos, Time.deltaTime * _crouchSpeed);

        if (_cc.isGrounded)
        {
            if (_verticalVelocity < -2f) _verticalVelocity = -2f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _verticalVelocity = _jumpPower;
                _isCrouching = false; // 점프 시 앉기 상태 해제

                if (_animator != null)
                {
                    _animator.SetTrigger("Jump");
                }

                OnJumpEvent?.Invoke();
            }
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }
        Vector3 velocity = (move.normalized * _speed) + (Vector3.up * _verticalVelocity);
        _cc.Move(velocity * Time.deltaTime);

        if (_animator != null)
        {
            int myAnimIndex = GetCurrentAnimIndex();
            ApplyAnimationIndexFromServer((byte)myAnimIndex);
        }

        // 로컬 플레이어 애니메이션 상태 판단 및 로그 출력
        UpdateAnimationStateLog(isMoving, _isCrouching);
    }

    // 외부(NetworkManager 등 패킷 수신부)에서 호출해 줄 함수
    public void OnReceiveRemoteMove(Vector3 serverPos, float serverYaw, float serverPitch, byte serverAnimIndex)
    {
        if (_isMine)
            return;

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
        _targetPitch = serverPitch;

        if (_targetAnimIndex != serverAnimIndex && (serverAnimIndex == 5 || serverAnimIndex == 11 || serverAnimIndex == 17))
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Jump");
            }
        }

        _targetAnimIndex = serverAnimIndex;
        ApplyAnimationIndexFromServer(_targetAnimIndex);
    }

    private void ApplyAnimationIndexFromServer(byte animIndex)
    {
        if (_animator == null) return;

        bool isIndexChanged = (_currentAnimIndex != animIndex);
        _currentAnimIndex = animIndex;

        Weapon currentWeapon = null;
        if (_playerItem != null)
        {
            currentWeapon = _playerItem.GetCurrentWeapon();
        }

        // 무기 종류에 따라 WeaponType 판단
        int weaponTypeForAnimator = 0;
        if (currentWeapon != null)
        {
            WeaponData.WeaponType wType = currentWeapon.GetWeaponType();

            if (wType == WeaponData.WeaponType.AR ||
                wType == WeaponData.WeaponType.SMG ||
                wType == WeaponData.WeaponType.Shotgun ||
                wType == WeaponData.WeaponType.LMG ||
                wType == WeaponData.WeaponType.DMR ||
                wType == WeaponData.WeaponType.SR)
            {
                weaponTypeForAnimator = 1; // 긴총
            }
            else if (wType == WeaponData.WeaponType.Knife)
            {
                weaponTypeForAnimator = 2; // 칼
            }
        }

        int previousWeaponType = _animator.GetInteger("WeaponType");
        bool isWeaponChanged = (previousWeaponType != weaponTypeForAnimator);

        _animator.SetInteger("WeaponType", weaponTypeForAnimator);

        int rawMoveState = animIndex % 6; // 0~5 값

        if (rawMoveState != 5)
        {
            float targetMoveStateValue = (float)rawMoveState;
            if (weaponTypeForAnimator == 1)
            {
                targetMoveStateValue += 6f; // 긴총 구역이면 6.0 ~ 9.0 범위로 변환
            }
            else if (weaponTypeForAnimator == 2)
            {
                targetMoveStateValue += 12f; // 칼 구역이면 12.0 ~ 15.0 범위로 변환
            }

            if (isWeaponChanged)
            {
                _animator.SetFloat("MoveState", targetMoveStateValue);
            }
            else
            {
                _animator.SetFloat("MoveState", targetMoveStateValue, 0.15f, Time.deltaTime);
            }
        }

        if (rawMoveState == 5 && isIndexChanged)
        {
            // 0: 권총/맨손, 1: 긴총, 2: 칼
            if (weaponTypeForAnimator == 1)
            {
                _animator.SetTrigger("LongGun_Jump");
            }
            else if (weaponTypeForAnimator == 2)
            {
                _animator.SetTrigger("Knife_Jump");
            }
            else
            {
                _animator.SetTrigger("Pistol_Jump");
            }
        }
    }

    private void SyncRemotePlayerMovement()
    {
        Vector3 prevPosition = transform.position;

        _cc.enabled = false;
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, _targetYaw, 0), Time.deltaTime * _lerpSpeed);
        _cc.enabled = true;

        if (_cam != null)
        {
            Transform camTransform = _cam.transform;
            Quaternion targetCamRot = Quaternion.Euler(_targetPitch, 0f, 0f);

            camTransform.localRotation = Quaternion.Lerp(
                camTransform.localRotation,
                targetCamRot,
                Time.deltaTime * _lerpSpeed
            );
        }

        ApplyAnimationIndexFromServer(_targetAnimIndex);
    }

    private void UpdateAnimationStateLog(bool isMoving, bool isCrouching)
    {
        int newIndex = 0;

        if (isCrouching && isMoving)
        {
            newIndex = 3;
        }
        else if (isCrouching)
        {
            newIndex = 2;
        }
        else if (isMoving)
        {
            newIndex = 1;
        }

        // 디버깅 로그 출력용 인덱스 매칭 수정 (0~3 범위가 바뀔 때만 로그가 출력되도록 둠)
        if (_currentAnimIndex % 6 != newIndex)
        {
            string stateName = "정지";
            if (newIndex == 1) stateName = "걷기";
            if (newIndex == 2) stateName = "앉기";
            if (newIndex == 3) stateName = "앉아서 걷기";

            Debug.Log($"[{gameObject.name}] 애니메이션 상태 변경 -> {stateName} | IsMine: {_isMine}");
        }
    }

    public int GetCurrentAnimIndex()
    {
        if (_isMine)
        {
            Vector2 input = getInput();
            bool isMoving = input.sqrMagnitude > 0f;
            bool isJumping = !_cc.isGrounded;

            int weaponOffset = 0;

            Weapon currentWeapon = null;
            if (_playerItem != null)
            {
                currentWeapon = _playerItem.GetCurrentWeapon();
            }

            if (currentWeapon != null)
            {
                WeaponData.WeaponType wType = currentWeapon.GetWeaponType();

                if (wType == WeaponData.WeaponType.AR ||
                    wType == WeaponData.WeaponType.SMG ||
                    wType == WeaponData.WeaponType.Shotgun ||
                    wType == WeaponData.WeaponType.LMG ||
                    wType == WeaponData.WeaponType.DMR ||
                    wType == WeaponData.WeaponType.SR)
                {
                    weaponOffset = 6; // 긴총 구역 (인덱스 6~11)
                }
                else if (wType == WeaponData.WeaponType.Knife)
                {
                    weaponOffset = 12; // 칼 구역 (인덱스 12~17)
                }
            }

            if (isJumping) return weaponOffset + 5;
            if (_isCrouching && isMoving) return weaponOffset + 3;
            if (_isCrouching) return weaponOffset + 2;
            if (isMoving) return weaponOffset + 1;

            return weaponOffset + 0;
        }

        if (_currentAnimIndex != -1)
            return _currentAnimIndex;

        return 0;
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
                _animator.SetTrigger("Jump");
                _isCrouching = false;
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

            lastSentPitch = _cam.GetPitch();
            _targetPitch = _cam.GetPitch();
        }
        else
        {
            lastSentYaw = transform.eulerAngles.y;
            _targetYaw = transform.eulerAngles.y;

            lastSentPitch = 0f;
            _targetPitch = 0f;
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
        if (!_isMine)
            return;

        if (NetworkManager.Instance == null)
            return;

        if (Time.time - lastSendTime < sendInterval)
            return;

        Vector3 currentPos = transform.position;
        float currentYaw = transform.eulerAngles.y;
        float currentPitch = 0f;

        if (_cam != null)
        {
            currentYaw = _cam.GetYaw();
            currentPitch = _cam.GetPitch();
        }

        byte currentAnimIndex = (byte)GetCurrentAnimIndex();

        bool positionChanged =
            Vector3.Distance(currentPos, lastSentPos) >= positionSendThreshold;

        bool yawChanged =
            Mathf.Abs(Mathf.DeltaAngle(currentYaw, lastSentYaw)) >= yawSendThreshold;

        bool pitchChanged =
            Mathf.Abs(Mathf.DeltaAngle(currentPitch, lastSentPitch)) >= pitchSendThreshold;

        bool animChanged =
            currentAnimIndex != lastSentAnimIndex;

        if (!positionChanged && !yawChanged && !pitchChanged && !animChanged)
            return;

        lastSendTime = Time.time;
        lastSentPos = currentPos;
        lastSentYaw = currentYaw;
        lastSentPitch = currentPitch;
        lastSentAnimIndex = currentAnimIndex;

        NetworkManager.Instance.SendMoveState(
            currentPos.x,
            currentPos.y,
            currentPos.z,
            currentYaw,
            currentPitch,
            currentAnimIndex
        );
    }
}