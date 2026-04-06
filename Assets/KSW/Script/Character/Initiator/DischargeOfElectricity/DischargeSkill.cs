using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class DischargeSkill : MonoBehaviour
{
    [SerializeField] private float _range = 13f;
    [SerializeField] private float _width = 8f;
    [SerializeField] private LayerMask _enemyLayer;

    [SerializeField] private GameObject _decalPrefab;

    private bool _isCasting = false;

    private void Update()
    {
        if (Keyboard.current.xKey.wasPressedThisFrame && !_isCasting)
        {
            StartCoroutine(DischargeRoutine());
        }
    }

    private IEnumerator DischargeRoutine()
    {
        _isCasting = true;

        // 데칼 생성 위치 계산
        // x, z는 플레이어 앞쪽 중앙으로 계산하고, y는 1로 고정
        Vector3 spawnPos = transform.position + transform.forward * (_range / 2f);
        spawnPos.y = 1f; // 여기서 y값을 1로 고정!

        // 데칼 생성
        GameObject decalObj = Instantiate(_decalPrefab, spawnPos, transform.rotation);

        if (decalObj.TryGetComponent<DecalProjector>(out var projector))
        {
            // size.z를 10 정도로 주면, y=1 지점에서 아래로 5m, 위로 5m 투사
            projector.size = new Vector3(_width, _range, 10f);

            // 데칼이 바닥을 향하도록 회전
            decalObj.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        }

        // 예고 시간 (0.6초)
        yield return new WaitForSeconds(0.6f);

        // 낙뢰 시작 시 데칼 삭제
        Destroy(decalObj);

        // 낙뢰 생성 및 판정
        HashSet<GameObject> hitTargets = new HashSet<GameObject>();
        for (int i = 0; i < 7; i++)
        {
            Vector3 boltPos = transform.position + transform.forward * Random.Range(1f, _range) + transform.right * Random.Range(-_width / 2, _width / 2);
            boltPos.y = 0f; // 판정 기준점은 바닥(0)으로 고정

            StartCoroutine(DrawLightning(boltPos));
            ExecuteBolt(boltPos, hitTargets);
            yield return new WaitForSeconds(0.05f);
        }

        _isCasting = false;
    }

    private IEnumerator DrawLightning(Vector3 targetPos)
    {
        GameObject lineObj = new GameObject("LightningLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.white;
        lr.endColor = Color.cyan;

        int segments = 5;
        lr.positionCount = segments;
        Vector3 startPos = targetPos + Vector3.up * 10f;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

            if (i > 0 && i < segments - 1)
            {
                pos += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            }
            lr.SetPosition(i, pos);
        }

        yield return new WaitForSeconds(0.15f);
        Destroy(lineObj);
    }

    private void ExecuteBolt(Vector3 center, HashSet<GameObject> hitHistory)
    {
        Collider[] targets = Physics.OverlapSphere(center, 2.0f, _enemyLayer);
        foreach (var target in targets)
        {
            // 이미 이번 궁극기에 맞은 적은 통과
            if (hitHistory.Contains(target.gameObject)) continue;

            if (target.TryGetComponent<ElectricShock>(out var shock))
            {


                // 감전 2스택 부여 및 3.5초 유지
                for (int i = 0; i < 2; i++) shock.Shock(3.5f);

                // 맞은 목록에 추가
                hitHistory.Add(target.gameObject);
            }
        }
    }
}