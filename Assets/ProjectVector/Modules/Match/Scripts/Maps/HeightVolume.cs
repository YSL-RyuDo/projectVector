using UnityEngine;

[ExecuteAlways]
public class HeightVolume : MonoBehaviour
{
    [Tooltip("이 영역에서 적용될 바닥 높이 (월드 Y)")]
    public float groundY;

    [Tooltip("레이캐스트 시작 높이")]
    public float rayHeight = 50f;

    [Tooltip("바닥 레이어")]
    public LayerMask groundMask;

    // 스크립트 붙였을 때
    private void Reset()
    {
        AutoSetGroundY();
    }

    // 씬 로드 / 활성화될 때
    private void OnEnable()
    {
        AutoSetGroundY();
    }

#if UNITY_EDITOR
    // 값 바뀔 때마다 (에디터 전용)
    private void OnValidate()
    {
        AutoSetGroundY();
    }
#endif

    private void AutoSetGroundY()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            rayHeight * 2f, groundMask))
        {
            groundY = hit.point.y;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
