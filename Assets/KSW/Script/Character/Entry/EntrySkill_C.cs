using UnityEngine;
using UnityEngine.UI;

public class EntrySkill_C : MonoBehaviour
{
    private PlayerMovement _movement;
    private Animator _animator;
    private Slider _jumpGaugeSlider;

    // 초당 충전량
    [SerializeField] private float _chargeSpeed = 50f;
    // 미사용 시 초당 감소량
    [SerializeField] private float _dischargeSpeed = 30f;
    // 풀차징 시 추가될 점프력
    [SerializeField] private float _maxBonusForce = 6f;

    // C키 떼고 감소 시작까지 대기 시간
    [SerializeField] private float _waitBeforeDischarge = 2.0f;

    [SerializeField] private Transform _camera;
    // 웅크릴 때 Y 위치
    [SerializeField] private float _crouchY = 0.75f;
    // 웅크리기 false일 때 돌아갈 Y 위치
    [SerializeField] private float _standY = 1.2f;
    // 카메라 이동 부드러움 정도
    [SerializeField] private float _smoothSpeed = 10f;

    private float _currentGauge = 0f;
    private float _maxGauge = 100f;
    private bool _isCharging = false;
    private float _lastChargeTime;

    private void Start()
    {
        _movement = GetComponent<PlayerMovement>();
        _animator = GetComponentInChildren<Animator>();

        if (_movement != null)
        {
            _movement.OnJumpEvent += handleEnhancedJump;
        }

        GameObject jumpUI = GameObject.Find("JumpSlider");
        if (jumpUI != null)
        {
            _jumpGaugeSlider = jumpUI.GetComponent<Slider>();
            _jumpGaugeSlider.maxValue = _maxGauge;
            _jumpGaugeSlider.value = 0;
            _jumpGaugeSlider.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_movement != null)
        {
            _movement.OnJumpEvent -= handleEnhancedJump;
        }
    }

    private void Update()
    {
        handleGaugeLogic();
        handleCameraHeight();
    }

    private void handleGaugeLogic()
    {
        if (Input.GetKey(KeyCode.C))
        {
            _isCharging = true;
            _animator.SetBool("Crouch", true);

            if (_jumpGaugeSlider != null) _jumpGaugeSlider.gameObject.SetActive(true);

            _currentGauge += _chargeSpeed * Time.deltaTime;
            _lastChargeTime = Time.time;
        }
        else
        {
            _isCharging = false;

            if (_currentGauge > 0 && Time.time > _lastChargeTime + _waitBeforeDischarge)
            {
                _currentGauge -= _dischargeSpeed * Time.deltaTime;
            }
        }

        _currentGauge = Mathf.Clamp(_currentGauge, 0, _maxGauge);

        if (_jumpGaugeSlider != null)
        {
            _jumpGaugeSlider.value = _currentGauge;
            if (_currentGauge <= 0 && !_isCharging)
            {
                _animator.SetBool("Crouch", false);
                _jumpGaugeSlider.gameObject.SetActive(false);
            }
        }
    }

    private void handleCameraHeight()
    {
        if (_camera == null) return;

        bool isCrouching = _animator.GetBool("Crouch");
        float targetY = isCrouching ? _crouchY : _standY;

        Vector3 currentPos = _camera.localPosition;
        float nextY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * _smoothSpeed);

        _camera.localPosition = new Vector3(currentPos.x, nextY, currentPos.z);
    }

    private void handleEnhancedJump()
    {
        if (_currentGauge > 0)
        {
            float gaugeRatio = _currentGauge / _maxGauge;
            float finalJumpForce = 8f + (gaugeRatio * _maxBonusForce);

            _movement.SetVerticalVelocity(finalJumpForce);

            ResetGauge();
        }
    }

    public void ResetGauge()
    {
        _animator.SetBool("Crouch", false);
        _currentGauge = 0f;
    }
}