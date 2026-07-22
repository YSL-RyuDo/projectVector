using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerSkill_Q : CharacterSkill
{
    [Header("References")]
    [SerializeField] private Transform _throwPoint;

    [Header("Throw")]
    [SerializeField] private float _throwForce = 15f;
    [SerializeField] private float _upwardForce = 2f;
    [SerializeField] private float _spawnForwardOffset = 0.6f;

    private PlayerIdentity _playerIdentity;
    private ElectricShock _electricShock;

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
        if (_throwPoint == null)
        {
            Transform found =
                transform.Find("TrapPoint");

            if (found != null)
                _throwPoint = found;
        }

        if (_throwPoint == null)
        {
            Transform found =
                transform.Find("CameraRoot/WeaponHolder/DropPoint");

            if (found != null)
                _throwPoint = found;
        }
    }

    private void Update()
    {
        if (!IsMine())
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

        if (EntrySkill_X.IsLocalBladeMode)
            return;

        EntryDashMotor dashMotor =
            GetComponent<EntryDashMotor>();

        if (dashMotor != null &&
            dashMotor.IsDashing)
            return;

        if (_electricShock != null &&
            _electricShock.IsStunned)
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
               _playerIdentity.PlayerId ==
               NetworkManager.Instance.MyPlayerId;
    }

    protected override void UseSkill()
    {
        if (NetworkManager.Instance == null)
            return;

        Transform origin =
            _throwPoint != null
                ? _throwPoint
                : transform;

        Vector3 forward =
            origin.forward.normalized;

        Vector3 spawnPos =
            origin.position +
            forward * _spawnForwardOffset;

        Vector3 velocity =
            forward * _throwForce +
            Vector3.up * _upwardForce;

        NetworkManager.Instance.SendControllerSwampThrow(
            spawnPos,
            velocity
        );

        Debug.Log(
            $"[CONTROLLER Q REQUEST] pos={spawnPos}, velocity={velocity}"
        );
    }
}