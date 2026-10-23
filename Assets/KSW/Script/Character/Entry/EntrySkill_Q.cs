using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class EntrySkill_Q : CharacterSkill
{
    [Header("Throw")]
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _throwForce = 25f;

    private PlayerIdentity _playerIdentity;
    private bool _isCasting;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Start()
    {
        if (_playerCamera == null)
            _playerCamera = Camera.main;
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

        ElectricShock shock = GetComponent<ElectricShock>();

        if (shock == null)
            shock = GetComponentInChildren<ElectricShock>(true);

        if (shock != null && shock.IsStunned)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
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
        if (!GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) return;
        if (!CanUsePurchasedSkill(SkillSlot.Q))
            return;

        if (_throwPoint == null ||
            _playerCamera == null ||
            NetworkManager.Instance == null)
        {
            return;
        }

        Vector3 spawnPos = _throwPoint.position;
        Vector3 velocity = _playerCamera.transform.forward.normalized * _throwForce;

        EntrySkill_E stealth = GetComponent<EntrySkill_E>();

        if (stealth != null && stealth.IsStealth)
        {
            NetworkManager.Instance.SendBeastStealthCancel();
        }

        _isCasting = true;
        GetComponent<EntrySkillPresentation>()?.Begin(EntrySkillPresentation.Cast.Smoke, .65f);
        StartCoroutine(ThrowAfterWindup());

        Debug.Log($"[ENTRY SMOKE THROW REQUEST] pos={spawnPos}, velocity={velocity}");
    }

    private void ResetCast()
    {
        _isCasting = false;
    }
    private IEnumerator ThrowAfterWindup()
    {
        yield return new WaitForSeconds(.28f);
        if (GameplayActionPolicy.CanAct(this, GameplayAction.Skill) && NetworkManager.Instance && _playerCamera && _throwPoint)
            NetworkManager.Instance.SendEntrySmokeThrow(_throwPoint.position, _playerCamera.transform.forward.normalized * _throwForce);
        yield return new WaitForSeconds(.37f);
        ResetCast();
    }
    private void OnDisable() { StopAllCoroutines(); CancelInvoke(nameof(ResetCast)); ResetCast(); }
}
