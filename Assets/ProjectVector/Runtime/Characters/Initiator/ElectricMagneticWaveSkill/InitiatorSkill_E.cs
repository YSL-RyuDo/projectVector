using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1400)]
public class InitiatorSkill_E : CharacterSkill
{
    [SerializeField] private EmpCastVfx _presentation;
    [SerializeField] private EmpGestureRig _gesture;
    private PlayerIdentity _identity;
    private float _nextRequest;
    private bool _preparing, _pending;
    private float _requestAt;
    private int _queuedSlot=-1;
    private bool _hidWeapon;
    public bool BlocksActions => _preparing || _pending || (_gesture && _gesture.Active);
    public bool WeaponVisible => _gesture && _gesture.WeaponVisible;
    private void Awake()
    {
        _identity=GetComponent<PlayerIdentity>();
        var legacy=GetComponent<LineRenderer>();if(legacy)legacy.enabled=false;
    }
    private bool IsMine()=>_identity && NetworkManager.Instance && _identity.PlayerId==NetworkManager.Instance.MyPlayerId;
    private void Update()
    {
        if(GetComponent<PlayerHP>()?.IsDead==true || (RoundManager.Instance && (RoundManager.Instance.currentState==RoundManager.RoundState.RoundEnd || RoundManager.Instance.currentState==RoundManager.RoundState.GameOver))) { ResetCast(); return; }
        if(!IsMine())return;
        if(BlocksActions && !GameplayActionPolicy.CanAct(this,GameplayAction.Skill,false)){ResetCast();return;}
        if(_pending && Time.unscaledTime-_requestAt>2f){_pending=false;_gesture?.Cancel();}
        if(!BlocksActions && _queuedSlot>=0 && !GameplayActionPolicy.MenuBlocksInput){int slot=_queuedSlot;_queuedSlot=-1;GetComponent<PlayerItem>()?.TrySelectWeapon(slot);}
        var key=Keyboard.current;
        if(BlocksActions && !GameplayActionPolicy.MenuBlocksInput && key!=null)
        {
            int slot=key.digit1Key.wasPressedThisFrame?0:key.digit2Key.wasPressedThisFrame?1:key.digit3Key.wasPressedThisFrame?2:-1;
            if(slot>=0 || key.eKey.wasPressedThisFrame || Mouse.current?.rightButton.wasPressedThisFrame==true)
            {
                _queuedSlot=slot;
                if(_preparing){_preparing=false;NetworkManager.Instance.SendEmpGesture(false);_gesture?.Cancel();}
                return;
            }
        }
        if(_preparing && _gesture && _gesture.Ready)
        {
            _preparing=false;_pending=true;_requestAt=Time.unscaledTime;
            NetworkManager.Instance.SendEmpCast();
        }
        if(BlocksActions || key==null || Time.unscaledTime<_nextRequest)return;
        if(Keyboard.current.eKey.wasPressedThisFrame && GameplayActionPolicy.CanAct(this,GameplayAction.Skill))UseSkill();
    }
    protected override void UseSkill()
    {
        if(!IsMine() || Time.unscaledTime<_nextRequest || !GameplayActionPolicy.CanAct(this,GameplayAction.Skill) || !CanUsePurchasedSkill(SkillSlot.E))return;
        _nextRequest=Time.unscaledTime+.3f;
        if(!_gesture || BlocksActions || InitiatorSkill_C.ShouldBlockWeaponInput() || GetComponent<InitiatorSkill_X>()?.IsCasting==true)return;
        GetComponent<PlayerItem>()?.CancelReload(false);
        GetComponentInChildren<FPSCameraLook>(true)?.ForceExitADS();
        _preparing=true;_gesture.Begin();
        NetworkManager.Instance.SendEmpGesture(true);
    }
    public void PlayEmpEffect(uint[] targetPlayerIds)
    {
        if(!_presentation)return;
        if(GetComponent<PlayerHP>()?.IsDead==true)return;
        if(RoundManager.Instance && (RoundManager.Instance.currentState==RoundManager.RoundState.RoundEnd || RoundManager.Instance.currentState==RoundManager.RoundState.GameOver))return;
        bool animate=!IsMine() || _pending;
        _pending=_preparing=false;
        if(animate)_gesture?.Release();
        var identities=FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        var points=new System.Collections.Generic.List<Vector3>();
        foreach(uint id in targetPlayerIds??System.Array.Empty<uint>()){
            foreach(var player in identities)if(player.PlayerId==id){points.Add(player.transform.position+Vector3.up*1.1f);break;}
        }
        // Empty server-confirmed casts still have a wave; no local query invents hits.
        _presentation.Confirm(transform.position,points.ToArray());
        if(IsMine())ElectricStatusUI.Instance?.ConfirmCast(targetPlayerIds?.Length??0);
    }
    public void ReceiveGesture(bool preparing)
    {
        if(IsMine() || GetComponent<PlayerHP>()?.IsDead==true)return;
        if(preparing)_gesture?.Begin();else _gesture?.Cancel();
    }
    private void LateUpdate()
    {
        var item=GetComponent<PlayerItem>();
        if(BlocksActions){item?.SetWeaponVisibility(WeaponVisible, this);_hidWeapon=true;}
        else if(_hidWeapon){_hidWeapon=false;if(GetComponent<PlayerHP>()?.IsDead!=true)item?.SetWeaponVisibility(true, this);}
    }
    public void ResetCast()
    {
        bool was=BlocksActions;
        if(_preparing && IsMine() && NetworkManager.Instance.IsConnected)NetworkManager.Instance.SendEmpGesture(false);
        _preparing=_pending=false;_queuedSlot=-1;
        _hidWeapon=false;
        _gesture?.ResetPresentation();_presentation?.ResetPresentation();
        if(was && GetComponent<PlayerHP>()?.IsDead!=true)GetComponent<PlayerItem>()?.SetWeaponVisibility(true, this);
    }
    private void OnDisable(){_nextRequest=0;ResetCast();}
}
