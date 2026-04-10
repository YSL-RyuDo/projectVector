using UnityEngine;
using UnityEngine.InputSystem;

public class ScannerSkill : MonoBehaviour
{
    [SerializeField] private GameObject _scannerPrefab; // 스캐너 프리팹

    [SerializeField] private float _throwForce = 10f; // 전방으로 던지는 힘
    [SerializeField] private float _upwardForce = 3f; // 위로 띄워주는 힘

    private void Update()
    {
        // C키를 누르면 던지기 실행
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            throwScanner();
        }
    }

    private void throwScanner()
    {
        if (_scannerPrefab == null) return;

        // 방향 결정 (수평 정면)
        Vector3 horizontalForward = transform.forward;
        horizontalForward.y = 0;
        horizontalForward.Normalize();

        // 생성 시점에 _firePoint의 실제 월드 좌표를 변수에 미리 담음
        Vector3 currentSpawnPos = transform.position + (Vector3.up * 1.4f) + (horizontalForward * 1.5f);

        // 월드 공간에 생성
        GameObject scanner = Instantiate(_scannerPrefab, currentSpawnPos, Quaternion.LookRotation(horizontalForward));

        // 프리팹 자체에 부모가 설정되어 있다면 해제
        scanner.transform.SetParent(null);

        Rigidbody rb = scanner.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 캐릭터와 충돌 무시
            Collider scannerCollider = scanner.GetComponent<Collider>();
            Collider playerCollider = GetComponent<Collider>();
            if (scannerCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(scannerCollider, playerCollider);
            }

            Vector3 force = (horizontalForward * _throwForce) + (Vector3.up * _upwardForce);

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}