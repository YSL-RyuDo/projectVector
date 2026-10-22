using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1400)]
public class InitiatorSkill_X : CharacterSkill
{
    [SerializeField] private EmpGestureRig _gesture;
    private bool _preparing, _pending, _hidWeapon;
    private float _requestedAt, _nextRequest;
    private int _queuedSlot = -1;
    public bool IsCasting => _preparing || _pending || (_gesture && _gesture.Active);
    private bool Mine => NetworkManager.Instance && GetComponent<PlayerIdentity>()?.PlayerId == NetworkManager.Instance.MyPlayerId;
    private void Update()
    {
        if (GetComponent<PlayerHP>()?.IsDead == true || (RoundManager.Instance &&
            (RoundManager.Instance.currentState == RoundManager.RoundState.RoundEnd || RoundManager.Instance.currentState == RoundManager.RoundState.GameOver)))
        { ResetCast(); return; }
        if (!Mine) return;
        if (IsCasting && !GameplayActionPolicy.CanAct(this, GameplayAction.Skill, false)) { ResetCast(); return; }
        if (_pending && Time.unscaledTime - _requestedAt > 2) { _pending = false; _gesture?.Cancel(); }
        if (!IsCasting && _queuedSlot >= 0 && !GameplayActionPolicy.MenuBlocksInput)
        { int slot = _queuedSlot; _queuedSlot = -1; GetComponent<PlayerItem>()?.TrySelectWeapon(slot); }
        var key = Keyboard.current;
        if (IsCasting && key != null && !GameplayActionPolicy.MenuBlocksInput)
        {
            int slot = key.digit1Key.wasPressedThisFrame ? 0 : key.digit2Key.wasPressedThisFrame ? 1 : key.digit3Key.wasPressedThisFrame ? 2 : -1;
            if (slot >= 0 || key.xKey.wasPressedThisFrame || Mouse.current?.rightButton.wasPressedThisFrame == true)
            {
                _queuedSlot = slot;
                if (_preparing) { _preparing = false; NetworkManager.Instance.SendDischargeGesture(false); _gesture?.Cancel(); }
                return;
            }
        }
        if (_preparing && _gesture && _gesture.Ready)
        {
            var camera = GetComponent<PlayerCameraController>()?.PlayerCamera;
            Vector3 direction = camera ? camera.transform.forward : transform.forward;
            direction.y = 0; if (direction.sqrMagnitude < .001f) direction = transform.forward;
            _preparing = false; _pending = true; _requestedAt = Time.unscaledTime;
            NetworkManager.Instance.SendUltimateCast(direction.normalized);
        }
        if (!IsCasting && key != null && key.xKey.wasPressedThisFrame) UseSkill();
    }
    protected override void UseSkill()
    {
        if (!Mine || !_gesture || IsCasting || Time.unscaledTime < _nextRequest ||
            !GameplayActionPolicy.CanAct(this, GameplayAction.Skill) || !CanUsePurchasedSkill(SkillSlot.X)) return;
        _nextRequest = Time.unscaledTime + .3f;
        GetComponent<PlayerItem>()?.CancelReload(false);
        GetComponentInChildren<FPSCameraLook>(true)?.ForceExitADS();
        _preparing = true; _gesture.Begin(); NetworkManager.Instance.SendDischargeGesture(true);
    }
    public void ReceiveGesture(bool preparing)
    {
        if (Mine || GetComponent<PlayerHP>()?.IsDead == true) return;
        if (preparing) _gesture?.Begin(); else _gesture?.Cancel();
    }
    public void ConfirmBegin()
    {
        bool animate = !Mine || _pending;
        _pending = _preparing = false;
        if (animate && GetComponent<PlayerHP>()?.IsDead != true) _gesture?.Release();
    }
    // Legacy packet compatibility: no invented local damage/strike sequence.
    public void PlayUltimateEffect(uint[] ids, Vector3 direction) => ConfirmBegin();
    private void LateUpdate()
    {
        if (IsCasting) { GetComponent<PlayerItem>()?.SetWeaponVisibility(_gesture && _gesture.WeaponVisible); _hidWeapon = true; }
        else if (_hidWeapon) { _hidWeapon = false; RestoreWeapon(); }
    }
    private void RestoreWeapon()
    {
        if (GetComponent<PlayerHP>()?.IsDead != true) GetComponent<PlayerItem>()?.SetWeaponVisibility(true);
    }
    public void ResetCast()
    {
        bool was = IsCasting || _hidWeapon;
        if (_preparing && Mine) NetworkManager.Instance.SendDischargeGesture(false);
        _preparing = _pending = _hidWeapon = false; _queuedSlot = -1;
        _gesture?.ResetPresentation(); if (was) RestoreWeapon();
    }
    private void OnDisable() => ResetCast();
}
