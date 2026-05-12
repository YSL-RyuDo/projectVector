using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 3.0f;      // 이동 속도
    public float distance = 2.0f;   // 중심점에서 좌우로 움직일 거리 (반경)

    private Vector3 startPosition;
    private bool isMoving = false;

    // 오브젝트가 활성화(SetActive(true))될 때마다 초기 위치 재설정
    void OnEnable()
    {
        startPosition = transform.position;
        isMoving = true;
    }

    void Update()
    {
        if (isMoving)
        {
            // Mathf.PingPong을 이용한 좌우 왕복 계산
            // 0 ~ (distance * 2) 사이를 왔다갔다 하는 값을 만듭니다.
            float move = Mathf.PingPong(Time.time * speed, distance * 2);

            // 시작 위치에서 (-distance)만큼 보정하여 중앙을 기준으로 왕복하게 함
            transform.position = startPosition + new Vector3(move - distance, 0, 0);
        }
    }

    // 비활성화될 때 이동 정지
    void OnDisable()
    {
        isMoving = false;
    }
}