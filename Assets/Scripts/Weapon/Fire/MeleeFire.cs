using UnityEngine;

public class MeleeFire : IFireStrategy
{
    private Transform ownerRoot;
    private PlayerIdentity ownerIdentity;
    private float range;

    public MeleeFire(Transform owner, float range, int dmg)
    {
        ownerRoot = owner.root;
        ownerIdentity = owner.GetComponentInParent<PlayerIdentity>();
        this.range = range;
    }

    public void Fire()
    {
        if (ownerIdentity == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        // 내 클라이언트에서만 서버에 타격 요청
        if (ownerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        Vector3 center = ownerIdentity.transform.position + Vector3.up * 1.0f;
        Vector3 forward = ownerIdentity.transform.forward;

        Collider[] hits = Physics.OverlapSphere(
            center + forward * 1.0f,
            range
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null)
                continue;

            if (hit.transform.root == ownerRoot)
                continue;

            PlayerIdentity targetIdentity =
                hit.GetComponentInParent<PlayerIdentity>();

            if (targetIdentity == null)
                continue;

            if (targetIdentity.PlayerId == ownerIdentity.PlayerId)
                continue;

            if (targetIdentity.Team == ownerIdentity.Team)
            {
                Debug.Log($"[MELEE] friendly ignored target={targetIdentity.PlayerId}");
                continue;
            }

            PlayerHP targetHP = targetIdentity.GetComponent<PlayerHP>();
            if (targetHP != null && targetHP.IsDead)
                continue;

            Debug.Log($"[MELEE HIT SEND] targetPlayerId={targetIdentity.PlayerId}");

            NetworkManager.Instance.SendHit(targetIdentity.PlayerId);
            return;
        }

        Debug.Log("[MELEE MISS]");
    }
}