using UnityEngine;

public class ItemSpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("스폰 지점 이름")]
    [SerializeField] private string spawnPointName;

    public string SpawnPointName => spawnPointName;

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

#if UNITY_EDITOR
    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.75f);

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.25f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        // 씬 뷰에서 전방 방향 확인용 표시선
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.8f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
#endif
}
