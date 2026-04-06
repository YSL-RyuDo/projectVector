using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public class EMPWaveSkill : MonoBehaviour
{
    [SerializeField] private float _radius = 7.0f;
    [SerializeField] private LayerMask _enemyLayer;

    private LineRenderer _lineRenderer;
    private bool _isCasting = false;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame && !_isCasting)
        {
            StartCoroutine(EMPRoutine());
        }
    }

    private IEnumerator EMPRoutine()
    {
        _isCasting = true;

        yield return new WaitForSeconds(0.2f);

        // 적 탐색 및 기절 적용
        Collider[] targets = Physics.OverlapSphere(transform.position, _radius, _enemyLayer);

        if (targets.Length > 0)
        {
            // 모든 적과 연결하기 위해 포지션 개수 설정 (중심점 1개 + 적들 수)
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = targets.Length * 2;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].TryGetComponent<ElectricShock>(out var shock))
                {
                    // 스택이 0이면 기절시키지 않고 다음 적으로 넘어감
                    if (shock.ShockCount <= 0) continue;

                    // 스택에 따른 기절 시간 계산
                    float stunTime = 0f;
                    if (shock.ShockCount == 1) stunTime = 0.8f;
                    else if (shock.ShockCount == 2) stunTime = 1.3f;
                    else if (shock.ShockCount >= 3) stunTime = 1.8f;

                    shock.ApplyStun(stunTime);

                    // 라인 그리기 (내 위치 -> 적 위치)
                    _lineRenderer.SetPosition(i * 2, transform.position);
                    _lineRenderer.SetPosition(i * 2 + 1, targets[i].transform.position);
                }
            }
        }

        yield return new WaitForSeconds(0.15f);
        _lineRenderer.enabled = false;

        // 후딜레이 (나머지 0.1 정도 더 대기해서 총 0.25 맞춤)
        yield return new WaitForSeconds(0.1f);
        _isCasting = false;
    }
}