using UnityEngine;

public class MiniMapCamera : MonoBehaviour
{
    public static MiniMapCamera Instance;

    private Transform _targetPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetTarget(Transform target)
    {
        _targetPlayer = target;
        Debug.Log($"[MiniMapCamera] 타겟이 정상적으로 설정됨: {target.name}");
    }

    private void LateUpdate()
    {
        if (_targetPlayer == null)
            return;

        transform.position = new Vector3(
            _targetPlayer.position.x,
            50f,
            _targetPlayer.position.z
        );
    }
}