using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScannerProjectile : MonoBehaviour
{
    [Header("Scanner Specs")]
    [SerializeField] private float _scanRadius = 6.5f;
    [SerializeField] private float _zapDuration = 0.15f; // 전기 줄기가 머무는 시간
    [SerializeField] private float _delayBetweenZaps = 0.1f; // 적과 적 사이 공격 간격
    [SerializeField] private LayerMask _enemyLayer;

    private LineRenderer _lineRenderer;
    private bool _isAnchored = false;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null) _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isAnchored || collision.gameObject.CompareTag("Player")) return;
        _isAnchored = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; }

        // 안착 즉시 순차 공격 시작
        StartCoroutine(sequentialZapRoutine());
        Destroy(gameObject, 3.5f);
    }

    private IEnumerator sequentialZapRoutine()
    {
        // 범위 내 적들 다 긁어모으기
        Collider[] targets = Physics.OverlapSphere(transform.position, _scanRadius, _enemyLayer);

        // 거리순으로 정렬 (제일 가까운 놈이 0번)
        var sortedTargets = targets
            .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
            .ToList();

        // 가까운 놈부터 한 명씩 차례대로 공격
        foreach (var target in sortedTargets)
        {
            if (target == null) continue;

            // 전기 줄기 쏘기
            yield return StartCoroutine(zapEffect(target.transform.position));

            // 즉시 판정
            if (target.TryGetComponent<ElectricShock>(out var shock))
            {
                shock.Shock(1.0f);
            }

            // 다음 적 쏘기 전 아주 잠깐의 간격
            yield return new WaitForSeconds(_delayBetweenZaps);
        }

        // 모든 타겟 순회 끝나면 라인 끄기
        _lineRenderer.enabled = false;
    }

    private IEnumerator zapEffect(Vector3 targetPos)
    {
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, targetPos);

        // 지정된 시간동안 전기를 보여줌
        yield return new WaitForSeconds(_zapDuration);

        _lineRenderer.enabled = false;
    }
}