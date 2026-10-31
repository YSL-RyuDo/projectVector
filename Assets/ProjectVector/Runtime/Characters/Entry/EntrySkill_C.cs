using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class EntrySkill_C : CharacterSkill
{
    [Header("References")]
    [SerializeField] private Transform _cameraTransform;

    [Header("Cast")]
    [SerializeField] private float _castLockTime = 0.25f;

    private PlayerIdentity _playerIdentity;
    private ElectricShock _electricShock;

    private bool _isCasting;

    private void Awake()
    {
        _playerIdentity =
            GetComponent<PlayerIdentity>();

        _electricShock =
            GetComponent<ElectricShock>();

        if (_electricShock == null)
            _electricShock =
                GetComponentInChildren<ElectricShock>(true);
    }

    private void Start()
    {
        if (_cameraTransform == null)
        {
            Camera cam =
                Camera.main;

            if (cam != null)
                _cameraTransform = cam.transform;
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

        if (InitiatorSkill_Q.IsLocalDroneMode)
            return;

        if (InitiatorSkill_C.ShouldBlockWeaponInput())
            return;

        if (_electricShock != null &&
            _electricShock.IsStunned)
            return;

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    private bool IsMine()
    {
        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId ==
               NetworkManager.Instance.MyPlayerId;
    }

    protected override void UseSkill()
    {
        if (!GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) return;
        if (!CanUsePurchasedSkill(SkillSlot.C))
            return;

        if (NetworkManager.Instance == null)
            return;

        Vector3 dir;

        if (_cameraTransform != null)
            dir = _cameraTransform.forward;
        else
            dir = transform.forward;

        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = transform.forward;

        dir.Normalize();

        EntrySkill_E stealth =
            GetComponent<EntrySkill_E>();

        if (stealth != null &&
            stealth.IsStealth)
        {
            NetworkManager.Instance.SendBeastStealthCancel();
        }

        _isCasting = true;
        GetComponent<EntrySkillPresentation>()?.Begin(EntrySkillPresentation.Cast.Dash, .65f);
        StartCoroutine(PrepareDash(dir));

        Debug.Log(
            $"[ENTRY DASH REQUEST] dir={dir}"
        );
    }

    private void ResetCast()
    {
        _isCasting = false;
    }
    private IEnumerator PrepareDash(Vector3 direction)
    {
        yield return new WaitForSeconds(.28f);
        if (GameplayActionPolicy.CanAct(this, GameplayAction.Skill) && NetworkManager.Instance)
            NetworkManager.Instance.SendEntryDashCast(direction);
        yield return new WaitForSeconds(Mathf.Max(.25f, _castLockTime));
        ResetCast();
    }
    private void OnDisable() { StopAllCoroutines(); CancelInvoke(nameof(ResetCast)); ResetCast(); }
}
