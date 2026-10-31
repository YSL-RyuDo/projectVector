using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1400)]
public class ControllerSkill_X : CharacterSkill
{
    [SerializeField] private ControllerForestMotion _motion;
    private PlayerIdentity _identity;
    private float _age;
    private bool _sent, _hidden;
    public bool IsCasting { get; private set; }
#if UNITY_EDITOR
    public Action<Vector3> EditorCaptureCast;
    private bool LocalFixture => Application.isBatchMode && PracticePlayerContext.For(this) != null && EditorCaptureCast != null;
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
            if (!_sent && _age >= .8f) Release();
            if (_age >= 1.4f) Cancel();
            return;
        }
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) UseSkill();
    }
    protected override void UseSkill()
    {
        if (IsCasting || !IsMine || !GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) return;
        if (!CanUsePurchasedSkill(SkillSlot.X) || !_motion) return;
        if (!LocalFixture && (!NetworkManager.Instance || !NetworkManager.Instance.IsConnected)) return;
        GetComponentInChildren<FPSCameraLook>()?.ForceExitADS();
        IsCasting = true; _age = 0; _sent = false; _motion.Begin();
    }
    private void Release()
    {
        if (!CanUsePurchasedSkill(SkillSlot.X)) { Cancel(); return; }
        var cc = GetComponent<CharacterController>();
        Vector3 position = transform.position;
        if (cc) position = cc.transform.TransformPoint(cc.center) - Vector3.up * cc.height * Mathf.Abs(cc.transform.lossyScale.y) * .5f;
        _sent = true; _motion.ReleaseDevice();
#if UNITY_EDITOR
        if (LocalFixture) EditorCaptureCast(position);
        else
#endif
            NetworkManager.Instance.SendControllerUltimateCast(position);
    }
    private void LateUpdate()
    {
        if (!IsMine || !IsCasting) return;
        GetComponent<PlayerItem>()?.SetWeaponVisibility(false, this); _hidden = true;
    }
    public void Cancel()
    {
        if (!IsCasting && !_hidden) return;
        IsCasting = false; _motion?.ResetPose();
        if (_hidden && GetComponent<PlayerHP>()?.IsDead != true && GetComponent<PlayerDeathController>()?.IsDead != true)
            GetComponent<PlayerItem>()?.SetWeaponVisibility(true, this);
        _hidden = false;
    }
    private void OnDisable() => Cancel();
}
