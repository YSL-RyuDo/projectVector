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
    private bool _remoteIsMoving;

    private float weaponWeight = 1f;
    private float currentMoveSpeed;
    private bool isADS = false;

    private PlayerItem _playerItem;
    private bool lastSentCrouching;

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
        _animator = GetComponentInChildren<Animator>(true);

        _playerIdentity = GetComponent<PlayerIdentity>();
        _playerItem = GetComponent<PlayerItem>();

        lastSentPos = transform.position;
        lastSentYaw = transform.eulerAngles.y;

        _targetPosition = transform.position;
        _targetYaw = transform.eulerAngles.y;

        weaponWeight = 1f;
        isADS = false;
        CalculateMoveSpeed();
    }

    void Start()
    {
        if (_cam != null)
        {
            lastSentYaw = _cam.GetYaw();
            _targetYaw = _cam.GetYaw();

            lastSentPitch = _cam.GetPitch();
            _targetPitch = _cam.GetPitch();

            _standingPos =
                _cam.transform.localPosition;

            _crouchingPos =
                _standingPos -
                new Vector3(
                    0f,
                    _crouchHeightOffset,
                    0f
                );
        }
        else
        {
            lastSentYaw =
                transform.eulerAngles.y;

            _targetYaw =
                transform.eulerAngles.y;

            lastSentPitch = 0f;
            _targetPitch = 0f;

            Debug.LogError(
                $"[PLAYER MOVEMENT] FPSCameraLook가 없습니다. object={name}"
            );
        }
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
        if (!CanMove)
            return;

        Vector2 input = getInput();

        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        bool isGrounded = _cc.isGrounded;

        if (!isGrounded)
        {
            _isCrouching = false;
        }
        else if (Keyboard.current != null &&
                 Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            _isCrouching = !_isCrouching;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_playerItem != null)
            {
                Weapon currentWeapon =
                    _playerItem.GetCurrentWeapon();

                if (currentWeapon != null &&
                    currentWeapon.GetWeaponType() ==
                    WeaponData.WeaponType.Knife)
                {
                    PlayKnifeAttackAnimation();

                    if (NetworkManager.Instance != null)
                    {
                        NetworkManager.Instance.SendKnifeAttack();
                    }
                }
            }
        }

        bool isMoving =
            input.sqrMagnitude > 0f;

        if (isGrounded)
        {
            if (_verticalVelocity < -2f)
            {
                _verticalVelocity = -2f;
            }

            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _verticalVelocity = _jumpPower;

                // 점프하면 무조건 일어서기
                _isCrouching = false;

                OnJumpEvent?.Invoke();
            }
        }
        else
        {
            _verticalVelocity +=
                _gravity * Time.deltaTime;
        }

        if (_cam != null)
        {
            Vector3 targetPos =
                _isCrouching
                    ? _crouchingPos
                    : _standingPos;

            _cam.transform.localPosition =
                Vector3.Lerp(
                    _cam.transform.localPosition,
                    targetPos,
                    Time.deltaTime * _crouchSpeed
                );
        }

        Vector3 velocity =
            move.normalized * currentMoveSpeed +
            Vector3.up * _verticalVelocity;

        _cc.Move(
            velocity * Time.deltaTime
        );

        if (_animator != null)
        {
            int myAnimIndex =
                GetCurrentAnimIndex();

            ApplyAnimationIndexFromServer(
                (byte)myAnimIndex
            );
        }

        UpdateAnimationStateLog(
            isMoving,
            _isCrouching
        );
    }

    public void PlayKnifeAttackAnimation()
    {
        if (_animator == null)
        {
            _animator =
                GetComponentInChildren<Animator>(true);
        }

        if (_animator == null)
        {
            Debug.LogWarning(
                $"[KNIFE ATTACK] Animator not found. " +
                $"object={gameObject.name}"
            );

            return;
        }

        // 칼 애니메이션 구역으로 맞춤
        _animator.SetInteger(
            "WeaponType",
            2
        );

        // 이전 Trigger가 남아 있을 경우 정리
        _animator.ResetTrigger(
            "Knife_Attack"
        );

        _animator.SetTrigger(
            "Knife_Attack"
        );

        Debug.Log(
            $"[KNIFE ATTACK PLAY] " +
            $"object={gameObject.name}, " +
            $"isMine={_isMine}, " +
            $"playerId={_playerIdentity?.PlayerId}"
        );
    }

    // 외부(NetworkManager 등 패킷 수신부)에서 호출해 줄 함수
    public void OnReceiveRemoteMove(
    Vector3 serverPos,
    float serverYaw,
    float serverPitch,
    byte serverAnimIndex,
    bool serverIsCrouching)
    {
        if (_isMine)
            return;

        if (serverAnimIndex > 17)
        {
            Debug.LogWarning(
                $"[REMOTE ANIM INVALID] " +
                $"playerId={_playerIdentity?.PlayerId}, " +
                $"animIndex={serverAnimIndex}"
            );

            serverAnimIndex = 0;
        }

        float distanceFromServerPosition =
            Vector3.Distance(transform.position, serverPos);

        if (distanceFromServerPosition > 5f)
        {
            _cc.enabled = false;
            transform.position = serverPos;
            _cc.enabled = true;

            _targetPosition = serverPos;
            _remoteIsMoving = false;
        }
        else
        {
            _remoteIsMoving =
                Vector3.Distance(
                    _targetPosition,
                    serverPos
                ) >= positionSendThreshold;

            _targetPosition = serverPos;
        }

        _targetYaw = serverYaw;
        _targetPitch = serverPitch;
        _targetAnimIndex = serverAnimIndex;
        _isCrouching = serverIsCrouching;
        ApplyAnimationIndexFromServer(
            _targetAnimIndex
        );
    }

    private void ApplyAnimationIndexFromServer(byte animIndex)
    {
        if (_animator == null)
            return;

        if (animIndex > 17)
        {
            animIndex = 0;
        }

        bool isIndexChanged =
            _currentAnimIndex != animIndex;

        _currentAnimIndex = animIndex;

        int weaponTypeForAnimator;
        int rawMoveState;

        // 애니메이션 인덱스 자체에서
        // 무기 종류와 이동 상태를 분리한다.
        if (animIndex <= 5)
        {
            // 0 ~ 5: 권총
            weaponTypeForAnimator = 0;
            rawMoveState = animIndex;
        }
        else if (animIndex <= 11)
        {
            // 6 ~ 11: 긴 총
            weaponTypeForAnimator = 1;
            rawMoveState = animIndex - 6;
        }
        else
        {
            // 12 ~ 17: 칼
            weaponTypeForAnimator = 2;
            rawMoveState = animIndex - 12;
        }

        int previousWeaponType =
            _animator.GetInteger("WeaponType");

        bool isWeaponChanged =
            previousWeaponType != weaponTypeForAnimator;

        _animator.SetInteger(
            "WeaponType",
            weaponTypeForAnimator
        );

        // 현재 구조에서 10번은 긴 총 조준 상태
        bool isAiming = animIndex == 10;

        _animator.SetBool(
            "IsAiming",
            isAiming
        );

        if (isAiming)
        {
            if (_isMine)
            {
                Vector2 input = getInput();
                bool isMoving = input.sqrMagnitude > 0f;

                if (_isCrouching && isMoving)
                {
                    rawMoveState = 3;
                }
                else if (_isCrouching)
                {
                    rawMoveState = 2;
                }
                else if (isMoving)
                {
                    rawMoveState = 1;
                }
                else
                {
                    rawMoveState = 0;
                }
            }
            else
            {
                // 원격 플레이어에게 내 키보드 입력을 사용하면 안 된다.
                rawMoveState = _remoteIsMoving ? 1 : 0;
            }
        }

        // 5는 각 무기 구역의 점프 상태다.
        if (rawMoveState == 5)
        {
            if (isIndexChanged)
            {
                if (weaponTypeForAnimator == 1)
                {
                    _animator.ResetTrigger("Pistol_Jump");
                    _animator.ResetTrigger("Knife_Jump");
                    _animator.SetTrigger("LongGun_Jump");
                }
                else if (weaponTypeForAnimator == 2)
                {
                    _animator.ResetTrigger("Pistol_Jump");
                    _animator.ResetTrigger("LongGun_Jump");
                    _animator.SetTrigger("Knife_Jump");
                }
                else
                {
                    _animator.ResetTrigger("LongGun_Jump");
                    _animator.ResetTrigger("Knife_Jump");
                    _animator.SetTrigger("Pistol_Jump");
                }
            }

            return;
        }

        float targetMoveStateValue = rawMoveState;

        if (weaponTypeForAnimator == 1)
        {
            targetMoveStateValue += 6f;
        }
        else if (weaponTypeForAnimator == 2)
        {
            targetMoveStateValue += 12f;
        }

        if (isWeaponChanged)
        {
            // 무기가 변경된 순간에는 즉시 변경
            _animator.SetFloat(
                "MoveState",
                targetMoveStateValue
            );
        }
        else
        {
            // 같은 무기 내 이동 변경은 부드럽게 처리
            _animator.SetFloat(
                "MoveState",
                targetMoveStateValue,
                0.15f,
                Time.deltaTime
            );
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
            Transform camTransform =
                _cam.transform;

            Quaternion targetCamRot =
                Quaternion.Euler(
                    _targetPitch,
                    0f,
                    0f
                );

            camTransform.localRotation =
                Quaternion.Lerp(
                    camTransform.localRotation,
                    targetCamRot,
                    Time.deltaTime * _lerpSpeed
                );

            Vector3 targetCameraPosition =
                _isCrouching
                    ? _crouchingPos
                    : _standingPos;

            camTransform.localPosition =
                Vector3.Lerp(
                    camTransform.localPosition,
                    targetCameraPosition,
                    Time.deltaTime * _crouchSpeed
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

            // --- [추가/수정] 긴총을 들고 있고 마우스 우클릭 유지 중일 때 처리 ---
            if (weaponOffset == 6 && Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                // 점프 중이 아닐 때만 사격 자세(10번) 고정 (원한다면 점프 중에도 적용 가능)
                if (!isJumping)
                {
                    return 10; // 긴총 사격 자세 고정 인덱스
                }
            }
            // -----------------------------------------------------------------

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
        if (!_cc.isGrounded)
            return;

        _verticalVelocity = force;
        _isCrouching = false;
    }

    public void SetLocalPlayer(bool isMine)
    {
        _isMine = isMine;

        _cam =
            GetComponentInChildren<FPSCameraLook>(true);

        Debug.Log(
            $"[외부 호출] SetLocalPlayer 호출. " +
            $"object={gameObject.name}, " +
            $"isMine={isMine}"
        );

        if (_cam != null)
        {
            lastSentYaw = _cam.GetYaw();
            _targetYaw = _cam.GetYaw();

            lastSentPitch = _cam.GetPitch();
            _targetPitch = _cam.GetPitch();
        }
        else
        {
            lastSentYaw =
                transform.eulerAngles.y;

            _targetYaw =
                transform.eulerAngles.y;

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

        bool crouchingChanged =_isCrouching !=lastSentCrouching;

        if (!positionChanged &&!yawChanged &&!pitchChanged &&!animChanged &&!crouchingChanged)
        {
            return;
        }

        lastSendTime = Time.time;
        lastSentPos = currentPos;
        lastSentYaw = currentYaw;
        lastSentPitch = currentPitch;
        lastSentAnimIndex = currentAnimIndex;
        lastSentCrouching = _isCrouching;

        NetworkManager.Instance.SendMoveState(
            currentPos.x,
            currentPos.y,
            currentPos.z,
            currentYaw,
            currentPitch,
            currentAnimIndex,
            _isCrouching
        );
    }

    public void SetWeaponWeight(float weight)
    {
        weaponWeight = Mathf.Max(1f, weight);
        CalculateMoveSpeed();
    }

    public void SetADS(bool value)
    {
        isADS = value;
        CalculateMoveSpeed();
    }

    private void CalculateMoveSpeed()
    {
        currentMoveSpeed =
            _speed - (1f - (1f / weaponWeight));

        if (isADS)
        {
            currentMoveSpeed /= 1.5f;
        }
    }

    public void ResetAfterRespawn(float spawnYaw)
    {
        _verticalVelocity = -2f;
        _isCrouching = false;

        _targetPosition =
            transform.position;

        _targetYaw =
            spawnYaw;

        _targetPitch =
            0f;

        lastSentPos =
            transform.position;

        lastSentYaw =
            spawnYaw;

        lastSentPitch =
            0f;

        lastSendTime = 0f;
        lastSentAnimIndex = 255;

        _remoteIsMoving = false;
        _currentAnimIndex = -1;

        if (_cam == null)
        {
            _cam =
                GetComponentInChildren<FPSCameraLook>(true);
        }

        if (_cam != null)
        {
            // 관전 중 변경된 카메라 위치 제거
            _cam.transform.localPosition =
                _standingPos;

            _cam.ResetLookRotation(
                spawnYaw
            );
        }

        Debug.Log(
            $"[PLAYER RESPAWN RESET] " +
            $"object={gameObject.name}, " +
            $"position={transform.position}, " +
            $"yaw={spawnYaw}"
        );
    }

    public void ResetMoveSpeedState()
    {
        // 사망 전에 ADS 중이었더라도 무조건 해제
        isADS = false;

        // 현재 장착 무기의 무게는 유지하고
        // ADS 감속만 제거해서 다시 계산
        if (weaponWeight < 1f)
            weaponWeight = 1f;

        CalculateMoveSpeed();

        Debug.Log(
            $"[PLAYER MOVE SPEED RESET] " +
            $"baseSpeed={_speed}, " +
            $"weaponWeight={weaponWeight}, " +
            $"currentMoveSpeed={currentMoveSpeed}, " +
            $"isADS={isADS}"
        );
    }
}