using UnityEngine;

public class DecoyTrap : MonoBehaviour
{
    [SerializeField] private GameObject _decoyAreaPrefab; // 바닥에 생길 실제 '영역' 프리팹
    private bool _hasLanded = false;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 이미 어디 닿아서 전개 중이면 무시
        if (_hasLanded) return;

        // 1. 상태 체크
        _hasLanded = true;

        // 2. 충돌 지점 계산 (함정이 닿은 정확한 위치)
        ContactPoint contact = collision.contacts[0];
        Vector3 spawnPos = contact.point;

        // 3. 실제 '교란 영역(DecoyArea)' 소환
        // 영역은 벽면의 각도에 맞춰 생성되도록 rotation 설정 (Quaternion.LookRotation 사용 가능)
        Instantiate(_decoyAreaPrefab, spawnPos, Quaternion.LookRotation(contact.normal));

        // 4. 배달 끝났으니 투척체는 삭제
        Destroy(gameObject);
    }
}