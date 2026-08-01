using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerSkill_C : CharacterSkill
{
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private float _throwForce = 15f;
    [SerializeField] private float _upwardForce = 2f;

    private PlayerIdentity _playerIdentity;
    private ElectricShock _electricShock;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
        _electricShock = GetComponent<ElectricShock>();
        if (_electricShock == null) _electricShock = GetComponentInChildren<ElectricShock>(true);
    }

    private void Start()
    {
        if (_throwPoint == null)
        {
            FPSCameraLook camLook = GetComponentInChildren<FPSCameraLook>(true);
            if (camLook != null) _throwPoint = camLook.transform;
        }

        if (_throwPoint == null)
            _throwPoint = transform;
    }

    private void Update()
    {
        if (!IsMine()) return;
        if (Keyboard.current == null) return;
        if (InGameChatUI.IsChatOpen) return;
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;
        if (SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen) return;
        if (ControllerSkill_E.IsSelectingSmoke || ControllerSkill_E.IsSmokeMapOpen || ControllerSkill_E.IsBlockingWeaponInput) return;
        if (InitiatorSkill_Q.IsLocalDroneMode) return;
        if (InitiatorSkill_C.ShouldBlockWeaponInput()) return;
        if (_electricShock != null && _electricShock.IsStunned) return;

        EntryDashMotor dashMotor = GetComponent<EntryDashMotor>();
        if (dashMotor != null && dashMotor.IsDashing) return;

        if (Keyboard.current.cKey.wasPressedThisFrame)
            UseSkill();
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.C))
            return;

        if (NetworkManager.Instance == null) return;

        Vector3 spawnPos = _throwPoint.position + _throwPoint.forward * 0.8f;
        Vector3 velocity = _throwPoint.forward * _throwForce + Vector3.up * _upwardForce;

        NetworkManager.Instance.SendControllerDecoyThrow(spawnPos, velocity);

        Debug.Log($"[CONTROLLER DECOY THROW REQUEST] pos={spawnPos}, velocity={velocity}");
    }

    private bool IsMine()
    {
        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }
}