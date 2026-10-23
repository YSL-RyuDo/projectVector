using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1400)]
public class ControllerSkill_Q : CharacterSkill
{
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private float _throwForce = 15f;
    [SerializeField] private float _upwardForce = 2f;
    [SerializeField] private float _spawnForwardOffset = .6f;
    [SerializeField] private ControllerSwampMotion _motion;
    private PlayerIdentity _identity;
    private float _age;
    private bool _sent, _hidden;
    public bool IsCasting { get; private set; }
#if UNITY_EDITOR
    public Action<Vector3, Vector3> EditorCaptureThrow;
    private bool LocalFixture => Application.isBatchMode && PracticePlayerContext.For(this) != null && EditorCaptureThrow != null;
#else
    private bool LocalFixture => false;
#endif
    private void Awake() => _identity = GetComponent<PlayerIdentity>();
    private bool IsMine => LocalFixture || (_identity && NetworkManager.Instance && _identity.PlayerId == NetworkManager.Instance.MyPlayerId);
    private void Update()
    {
        if (!IsMine) return;
        if (IsCasting)
        {
            if (!GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) { Cancel(); return; }
            _age += Time.deltaTime;
            if (!_sent && _age >= .38f) Release();
            if (_age >= .9f) Cancel();
            return;
        }
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) UseSkill();
    }
    protected override void UseSkill()
    {
        if (IsCasting || !IsMine || !CanUsePurchasedSkill(SkillSlot.Q) || !_motion) return;
        if (!LocalFixture && (!NetworkManager.Instance || !NetworkManager.Instance.IsConnected)) return;
        GetComponentInChildren<FPSCameraLook>()?.ForceExitADS();
        IsCasting = true; _age = 0; _sent = false; _motion.Begin();
    }
    private void Release()
    {
        if (!CanUsePurchasedSkill(SkillSlot.Q)) { Cancel(); return; }
        var camera = GetComponent<PlayerCameraController>()?.PlayerCamera;
        Transform origin = camera ? camera.transform : (_throwPoint ? _throwPoint : transform);
        Vector3 forward = origin.forward.normalized;
        Vector3 offset = forward * _spawnForwardOffset + origin.right * .14f - origin.up * .12f;
        Vector3 position = origin.position + offset;
        var hits = Physics.SphereCastAll(origin.position, .09f, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            position = origin.position + offset.normalized * Mathf.Max(.02f, hit.distance - .025f); break;
        }
        _sent = true; _motion.ReleaseDevice();
#if UNITY_EDITOR
        if (LocalFixture) EditorCaptureThrow(position, forward * _throwForce + Vector3.up * _upwardForce);
        else
#endif
            NetworkManager.Instance.SendControllerSwampThrow(position, forward * _throwForce + Vector3.up * _upwardForce);
    }
    private void LateUpdate()
    {
        if (!IsMine || !IsCasting) return;
        GetComponent<PlayerItem>()?.SetWeaponVisibility(false); _hidden = true;
    }
    public void Cancel()
    {
        if (!IsCasting && !_hidden) return;
        IsCasting = false; _motion?.ResetPose();
        if (_hidden && GetComponent<PlayerHP>()?.IsDead != true && GetComponent<PlayerDeathController>()?.IsDead != true)
            GetComponent<PlayerItem>()?.SetWeaponVisibility(true);
        _hidden = false;
    }
    private void OnDisable() => Cancel();
}
