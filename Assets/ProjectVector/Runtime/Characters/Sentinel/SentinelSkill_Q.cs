using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Input = SentinelCaptureInput;
#endif

public class SentinelSkill_Q : CharacterSkill
{
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private float _throwRange = 10f;
    [SerializeField] private LayerMask _groundMask = ~0;

    private PlayerIdentity _playerIdentity;
    private bool placing;

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
        if(placing || !GameplayActionPolicy.CanAct(this,GameplayAction.Skill))return;
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

        if (!TryGetPlacementHit(ray, out RaycastHit hit))
        {
            Debug.Log("[SENTINEL Q PLACE BLOCK] 설치 가능한 지면이 없습니다.");
            return;
        }

        if (Vector3.Angle(hit.normal, Vector3.up) >= 30f)
        {
            Debug.Log($"[SENTINEL Q PLACE BLOCK] 경사가 너무 큽니다. hit={hit.collider.name} normal={hit.normal}");
            return;
        }

        Vector3 position = hit.point + hit.normal * 0.03f;

        StartCoroutine(PlaceAfterLowering(position));

        Debug.Log($"[SENTINEL Q PLACE REQUEST] position={position}");
    }

    private System.Collections.IEnumerator PlaceAfterLowering(Vector3 position)
    {
        placing=true;GetComponent<SentinelCastMotion>()?.Release();
        yield return new WaitForSeconds(.2f);
        if(GameplayActionPolicy.CanAct(this,GameplayAction.Skill)&&CanUsePurchasedSkill(SkillSlot.Q))
            NetworkManager.Instance?.SendSentinelQPlace(position);
        placing=false;
    }
    private void OnDisable(){StopAllCoroutines();placing=false;}

    private bool TryGetPlacementHit(Ray ray, out RaycastHit hit)
    {
        // First-person arms/body retain gameplay colliders. They must not obstruct
        // their own placement ray; other actors and world geometry still do.
        hit = default;
        float nearest = float.PositiveInfinity;
        foreach (var candidate in Physics.RaycastAll(ray, _throwRange, _groundMask, QueryTriggerInteraction.Ignore))
        {
            if (candidate.collider.transform.IsChildOf(transform) || candidate.distance >= nearest) continue;
            nearest = candidate.distance;
            hit = candidate;
        }
        return hit.collider != null;
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
