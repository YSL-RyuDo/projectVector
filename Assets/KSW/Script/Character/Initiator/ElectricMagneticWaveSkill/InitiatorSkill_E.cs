using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InitiatorSkill_E : CharacterSkill
{
    [Header("Visual")]
    [SerializeField] private float _effectDuration = 0.15f;
    [SerializeField] private int _lineSegmentCount = 2;

    private LineRenderer _lineRenderer;
    private bool _isCasting;
    private PlayerIdentity _playerIdentity;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
        _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
            _lineRenderer.useWorldSpace = true;
        }
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (_isCasting)
            return;

        if (Keyboard.current == null)
            return;

        if (InGameChatUI.IsChatOpen)
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
            return;

        if (SettingsMenu.Instance != null &&
            SettingsMenu.Instance.IsOpen)
            return;

        ElectricShock myShock =
            GetComponent<ElectricShock>();

        if (myShock != null && myShock.IsStunned)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    private bool IsMine()
    {
        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.E))
            return;

        if (NetworkManager.Instance == null)
            return;

        _isCasting = true;

        NetworkManager.Instance.SendEmpCast();

        StartCoroutine(CastDelayRoutine());

        Debug.Log("[EMP CAST REQUEST]");
    }

    private IEnumerator CastDelayRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        _isCasting = false;
    }

    public void PlayEmpEffect(uint[] targetPlayerIds)
    {
        if (_lineRenderer == null)
            return;

        StopCoroutine(nameof(EmpEffectRoutine));
        StartCoroutine(EmpEffectRoutine(targetPlayerIds));
    }

    private IEnumerator EmpEffectRoutine(uint[] targetPlayerIds)
    {
        if (targetPlayerIds == null || targetPlayerIds.Length == 0)
        {
            _lineRenderer.enabled = false;
            yield break;
        }

        int lineCount = targetPlayerIds.Length;
        _lineRenderer.positionCount = lineCount * 2;
        _lineRenderer.enabled = true;

        Vector3 startPos = transform.position + Vector3.up * 1.2f;

        for (int i = 0; i < targetPlayerIds.Length; i++)
        {
            PlayerIdentity target =
                FindPlayerIdentityById(targetPlayerIds[i]);

            Vector3 endPos = startPos;

            if (target != null)
                endPos = target.transform.position + Vector3.up * 1.2f;

            _lineRenderer.SetPosition(i * 2, startPos);
            _lineRenderer.SetPosition(i * 2 + 1, endPos);
        }

        yield return new WaitForSeconds(_effectDuration);

        _lineRenderer.enabled = false;
    }

    private PlayerIdentity FindPlayerIdentityById(uint playerId)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < identities.Length; i++)
        {
            if (identities[i] != null &&
                identities[i].PlayerId == playerId)
            {
                return identities[i];
            }
        }

        return null;
    }
}