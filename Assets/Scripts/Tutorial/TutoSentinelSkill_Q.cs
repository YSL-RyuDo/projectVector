using UnityEngine;

public class TutoSentinelSkill_Q : MonoBehaviour
{
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private float _throwRange = 10f;
    [SerializeField] private LayerMask _groundMask = ~0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            UseSkill();
    }

    private void UseSkill()
    {
        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width * 0.5f, Screen.height * 0.5f)
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                _throwRange,
                _groundMask,
                QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[SENTINEL Q PLACE BLOCK] 설치 가능한 지면이 없습니다.");
            return;
        }

        if (Vector3.Angle(hit.normal, Vector3.up) >= 30f)
        {
            Debug.Log("[SENTINEL Q PLACE BLOCK] 경사가 너무 큽니다.");
            return;
        }

        Vector3 position = hit.point + hit.normal * 0.03f;

        Instantiate(_trapPrefab, position, Quaternion.identity);

        Debug.Log($"[SENTINEL Q PLACE] position={position}");
    }
}