using UnityEngine;

public class GeneralCameraShake : MonoBehaviour
{
    [Header("Bouncing Settings")]
    [SerializeField] private float returnSpeed = 10f; // 원래 위치로 돌아오는 속도
    [SerializeField] private float snapiness = 20f;   // 튀어 나갈 때의 속도/탄력 (회전보다 조금 더 높게 잡는 것이 좋습니다)

    private Vector3 currentPosition; // 현재 위치 반동 상태
    private Vector3 targetPosition;  // 반동이 도달해야 할 목표 위치

    void Update()
    {
        // 1. 목표치를 원점(Vector3.zero)으로 부드럽게 복귀시킴
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, returnSpeed * Time.deltaTime);

        // 2. 현재 위치를 목표 위치를 향해 빠르게 보간 (Time.deltaTime으로 통일)
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, snapiness * Time.deltaTime);

        // 3. 카메라의 '위치'에 반동 적용 (원래 위치를 기준으로 상대적 이동)
        transform.localPosition = currentPosition;
    }

    /// <summary>
    /// 카메라를 원하는 방향으로 튕겨나가게 합니다.
    /// 예: Fire(new Vector3(좌우, 위아래, 앞뒤))
    /// </summary>
    public void Fire(Vector3 bounceVector)
    {
        targetPosition += bounceVector;
    }
}