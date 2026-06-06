using UnityEngine;
using UnityEngine.InputSystem;

public class InitiatorSkill_C : CharacterSkill
{
    [SerializeField] private float _baseForce = 12f;
    [SerializeField] private int _lineResolution = 30;
    [SerializeField] private float _lineSpacing = 0.12f;

    [SerializeField] private GameObject _scannerPrefab;
    
    private LineRenderer _lineRenderer;

    private bool _isAiming = false;
    private Camera _mainCam;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        _mainCam = Camera.main;
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
            _lineRenderer.useWorldSpace = true;
        }
    }

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            _isAiming = !_isAiming;
            if (_lineRenderer != null) _lineRenderer.enabled = _isAiming;
        }

        if (_isAiming)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                UseSkill();
                cancelAiming();
            }
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                cancelAiming();
            }
        }
    }

    void LateUpdate()
    {
        if (_isAiming)
        {
            drawTrajectory();
        }
    }

    /* * [포물선 운동 공식을 이용한 실시간 궤적 계산]
 * 공식: P(t) = P0 + V0*t + 0.5 * g * t^2
 * P(t): 시간 t에서의 위치, P0: 시작 위치, V0: 초기 속도, g: 중력 가속도
 */
    private void drawTrajectory()
    {
        if (_lineRenderer == null) return;

        Vector3 startPos = GetSpawnPosition(); // 생성 시작점
        Vector3 velocity = GetThrowVelocity(); // 계산된 발사 속도
        Vector3 gravity = Physics.gravity; // 유니티 월드 중력값

        _lineRenderer.positionCount = _lineResolution;

        for (int i = 0; i < _lineResolution; i++)
        {
            // 시간을 간격(Spacing)에 따라 순차적으로 계산
            float t = i * _lineSpacing;

            // 포물선 공식 적용 (위치 = 시작점 + 속도*시간 + 0.5*중력*시간제곱)
            Vector3 point = startPos + (velocity * t) + (0.5f * gravity * t * t);

            _lineRenderer.SetPosition(i, point);
        }
    }

    protected override void UseSkill()
    {
        if (_scannerPrefab == null) return;

        GameObject scanner = Instantiate(_scannerPrefab, GetSpawnPosition(), transform.rotation, null);
        Rigidbody rb = scanner.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Physics.IgnoreCollision(scanner.GetComponent<Collider>(), GetComponent<Collider>());
            rb.linearVelocity = GetThrowVelocity();
        }
    }

    /*
 * [카메라 각도 기반 발사 벡터 연산]
 * 1. Pitch(상하 각도)에 따른 상향각(upwardWeight) 보정
 * 2. 조준 방향에 따른 발사 강도(finalForce) 가중치 적용
 */
    Vector3 GetThrowVelocity()
    {
        // 캐릭터의 수평 정면 방향 계산
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        // 카메라의 상하 각도(-1 ~ 1) 추출
        float pitch = _mainCam.transform.forward.y;

        // 상향각 클램프
        float upwardWeight = Mathf.Clamp(pitch * 0.5f + 0.1f, -0.1f, 0.35f);

        // 위를 볼 땐 더 멀리, 아래를 볼 땐 감속
        float forceReduction = (pitch < 0) ? (1.0f + pitch * 0.5f) : 1.0f;
        float finalForce = (_baseForce + (Mathf.Max(0, pitch) * _baseForce * 0.5f)) * forceReduction;

        // 수평, 수직 정면 벡터를 upwardWeight값에 따라 보간하여 최종 방향 결정
        Vector3 launchDir = Vector3.Lerp(forward, Vector3.up, upwardWeight);

        return launchDir.normalized * finalForce;
    }

    /*
 * [투척물 스폰 위치 설정]
 * 캐릭터의 최상위 부모(Scale 1,1,1) 기준 월드 좌표 오프셋 설정
 */
    Vector3 GetSpawnPosition()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        float targetHeight = 2.0f;
        float targetForward = 1.8f;

        return transform.position + (Vector3.up * targetHeight) + (forward * targetForward);
    }

    private void cancelAiming()
    {
        _isAiming = false;
        if (_lineRenderer != null) _lineRenderer.enabled = false;
    }
}