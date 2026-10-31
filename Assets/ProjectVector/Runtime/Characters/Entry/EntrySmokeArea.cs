using System.Collections.Generic;
using UnityEngine;

public class EntrySmokeArea : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private SphereCollider _triggerCollider;

    [Header("Entry Smoke Buff")]
    [SerializeField] private float _buffDuration = 1.5f;
    [SerializeField] private float _speedMultiplier = 1.25f;

    private uint _smokeId;
    private uint _ownerPlayerId;
    private bool _ownerBuffUsed;

    public void Init(
        uint smokeId,
        uint ownerPlayerId,
        float radius,
        float duration)
    {
        _smokeId = smokeId;
        _ownerPlayerId = ownerPlayerId;
        _ownerBuffUsed = false;

        if (_triggerCollider == null)
            _triggerCollider =
                GetComponent<SphereCollider>();

        if (_triggerCollider != null)
        {
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = 1.0f;
        }

        Destroy(gameObject, duration);

        Debug.Log(
            $"[ENTRY SMOKE AREA INIT] smokeId={_smokeId}, " +
            $"owner={_ownerPlayerId}, radius={radius}, duration={duration}"
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyOwnerBuff(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyOwnerBuff(other);
    }

    private void TryApplyOwnerBuff(Collider other)
    {
        if (_ownerBuffUsed)
            return;

        PlayerIdentity identity =
            other.GetComponentInParent<PlayerIdentity>();

        if (identity == null)
            return;

        // 이 연막을 던진 엔트리 본인만
        if (identity.PlayerId != _ownerPlayerId)
            return;

        if (NetworkManager.Instance == null)
            return;

        // 자기 클라에서만 실제 이동 버프 적용
        if (identity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        EntrySmokeLeapBuff buff =
            identity.GetComponent<EntrySmokeLeapBuff>();

        if (buff == null)
            buff =
                identity.gameObject.AddComponent<EntrySmokeLeapBuff>();

        buff.ApplyLeapBuff(
            _buffDuration,
            _speedMultiplier,
            1.0f,
            0.0f
        );

        _ownerBuffUsed = true;

        Debug.Log(
            $"[ENTRY SMOKE BUFF TRIGGER] smokeId={_smokeId}, owner={_ownerPlayerId}"
        );
    }

}
