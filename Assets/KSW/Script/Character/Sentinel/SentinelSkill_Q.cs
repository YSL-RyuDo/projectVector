using UnityEngine;

public class SentinelSkill_Q : CharacterSkill
{
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private float _throwRange = 10f;
    [SerializeField] private LayerMask _groundMask = ~0;

    private PlayerIdentity _playerIdentity;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (Input.GetKeyDown(KeyCode.Q))
            UseSkill();
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.Q))
            return;

        if (RoundManager.Instance != null &&
            !RoundManager.Instance.CanAttack)
        {
            return;
        }

        if (Camera.main == null ||
            NetworkManager.Instance == null)
        {
            return;
        }

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

        NetworkManager.Instance.SendSentinelQPlace(position);

        Debug.Log($"[SENTINEL Q PLACE REQUEST] position={position}");
    }

    private bool IsMine()
    {
        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        return
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }
}
