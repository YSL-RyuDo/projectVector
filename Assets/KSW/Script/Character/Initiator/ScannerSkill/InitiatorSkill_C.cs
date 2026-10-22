using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1400)]
public class InitiatorSkill_C : CharacterSkill
{
    public static bool IsLocalScannerAiming { get; private set; }
    private static int _blockWeaponInputUntilFrame=-1;
    public static bool ShouldBlockWeaponInput()=>IsLocalScannerAiming || Time.frameCount<=_blockWeaponInputUntilFrame;
    [SerializeField] private float _baseForce=12f;
    [SerializeField] private int _lineResolution=32;
    [SerializeField] private float _trajectoryWidth=.018f, _maxTrajectoryTime=1.5f;
    [SerializeField] private Transform _throwSpawnPoint;
    [SerializeField] private ScannerCastRig _rig;
    private LineRenderer _lineRenderer;
    private PlayerIdentity _identity;
    private bool _aiming, _throwing, _ownsLocal, _hidWeapon;
    private int _queuedSlot=-1;
    public bool BlocksActions=>_aiming || _throwing || (_rig && _rig.Active);
    private bool Ready=>!_rig || _rig.Ready;
    private Vector3 ReleasePosition=>_rig?_rig.ReleasePosition:_throwSpawnPoint?_throwSpawnPoint.position:
        GetComponent<PlayerCameraController>().PlayerCamera.transform.TransformPoint(new Vector3(-.1f,-.2f,.4f));
    private bool IsMine()=>_identity && NetworkManager.Instance && _identity.PlayerId==NetworkManager.Instance.MyPlayerId;
    private void Awake(){_identity=GetComponent<PlayerIdentity>();_lineRenderer=GetComponent<LineRenderer>();HideTrajectory();}
    private void Update()
    {
        if(GetComponent<PlayerHP>()?.IsDead==true || (RoundManager.Instance && (RoundManager.Instance.currentState==RoundManager.RoundState.RoundEnd || RoundManager.Instance.currentState==RoundManager.RoundState.GameOver))){ResetCast();return;}
        if(!IsMine())return;
        if(BlocksActions && !GameplayActionPolicy.CanAct(this,GameplayAction.Skill,false)){ResetCast();return;}
        if(_ownsLocal)IsLocalScannerAiming=BlocksActions;
        if(_throwing && _rig && _rig.ReleaseReady){Release();return;}
        if(!BlocksActions && _queuedSlot>=0 && !GameplayActionPolicy.MenuBlocksInput){int slot=_queuedSlot;_queuedSlot=-1;GetComponent<PlayerItem>()?.TrySelectWeapon(slot);}
        if(GameplayActionPolicy.MenuBlocksInput || Keyboard.current==null || Mouse.current==null)return;
        var key=Keyboard.current;
        if(BlocksActions){
            int slot=key.digit1Key.wasPressedThisFrame?0:key.digit2Key.wasPressedThisFrame?1:key.digit3Key.wasPressedThisFrame?2:-1;
            if(slot>=0){_queuedSlot=slot;if(!_throwing)CancelAiming();return;}
            if(_aiming && (key.cKey.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)){CancelAiming();return;}
            if(_aiming && Ready && Mouse.current.leftButton.wasPressedThisFrame)UseSkill();
        }else if(key.cKey.wasPressedThisFrame && GameplayActionPolicy.CanAct(this,GameplayAction.Skill))StartAiming();
    }
    private void StartAiming()
    {
        if(!CanUsePurchasedSkill(SkillSlot.C))return;
        GetComponent<PlayerItem>()?.CancelReload(false);GetComponentInChildren<FPSCameraLook>(true)?.ForceExitADS();
        _ownsLocal=_aiming=IsLocalScannerAiming=true;_rig?.Begin();NetworkManager.Instance.SendScannerGesture(1);
    }
    private void CancelAiming()
    {
        if(_aiming && IsMine())NetworkManager.Instance.SendScannerGesture(0);
        _aiming=false;_rig?.Return();HideTrajectory();_blockWeaponInputUntilFrame=Time.frameCount+1;
    }
    protected override void UseSkill()
    {
        if(!IsMine() || !_aiming || !Ready || !CanUsePurchasedSkill(SkillSlot.C))return;
        _aiming=false;_throwing=true;_rig?.Throw();HideTrajectory();NetworkManager.Instance.SendScannerGesture(2);
        // Older non-production test prefabs retain their original immediate throw until re-authored.
        if(!_rig)Release();
    }
    private void Release()
    {
        _throwing=false;
        if(CanUsePurchasedSkill(SkillSlot.C)){
            var position=ReleasePosition;var velocity=GetThrowVelocity();
            var camera=GetComponent<PlayerCameraController>().PlayerCamera;
            var origin=camera.transform.position;var delta=position-origin;
            float distance=delta.magnitude;
            foreach(var hit in Physics.SphereCastAll(origin,.12f,delta.normalized,distance,~0,QueryTriggerInteraction.Ignore))
                if(!hit.collider.transform.IsChildOf(transform))distance=Mathf.Min(distance,Mathf.Max(0,hit.distance-.02f));
            position=origin+delta.normalized*distance;
            NetworkManager.Instance.SendScannerThrow(position.x,position.y,position.z,velocity.x,velocity.y,velocity.z);
        }
        _rig?.Return();_blockWeaponInputUntilFrame=Time.frameCount+1;
    }
    public void ReceiveGesture(byte phase)
    {
        if(IsMine() || GetComponent<PlayerHP>()?.IsDead==true)return;
        if(phase==1)_rig?.Begin();else if(phase==2)_rig?.Throw();else _rig?.Return();
    }
    public void ConfirmSpawn(){if(!IsMine())_rig?.Return();}
    private Vector3 GetThrowVelocity()
    {
        var camera=GetComponent<PlayerCameraController>()?.PlayerCamera;
        Vector3 direction=camera?camera.transform.forward:transform.forward;
        direction.y=Mathf.Max(-.25f,direction.y);direction.Normalize();
        return direction*(_baseForce+Mathf.Clamp01(direction.y)*_baseForce*.35f);
    }
    private void HideTrajectory(){if(_lineRenderer){_lineRenderer.enabled=false;_lineRenderer.positionCount=0;}}
    private void LateUpdate()
    {
        var item=GetComponent<PlayerItem>();
        if(BlocksActions){item?.SetWeaponVisibility(_rig && _rig.WeaponVisible);_hidWeapon=true;}
        else if(_hidWeapon){_hidWeapon=false;if(GetComponent<PlayerHP>()?.IsDead!=true)item?.SetWeaponVisibility(true);}
        if(!IsMine() || !_aiming || !Ready || !_lineRenderer)return;
        Vector3 start=ReleasePosition,velocity=GetThrowVelocity(),previous=start;
        _lineRenderer.useWorldSpace=true;_lineRenderer.startWidth=_lineRenderer.endWidth=_trajectoryWidth;
        _lineRenderer.enabled=true;_lineRenderer.positionCount=_lineResolution;
        float duration=Mathf.Min(.6f,_maxTrajectoryTime);
        for(int i=0;i<_lineResolution;i++){
            float t=duration*i/(_lineResolution-1);Vector3 point=start+velocity*t+Physics.gravity*(.5f*t*t);
            Vector3 delta=point-previous;float distance=delta.magnitude;bool blocked=false;
            if(distance>.0001f)foreach(var hit in Physics.SphereCastAll(previous,.12f,delta/distance,distance,~0,QueryTriggerInteraction.Ignore)){
                if(hit.collider.transform.IsChildOf(transform))continue;
                if(hit.distance<distance){distance=hit.distance;blocked=true;}
            }
            if(blocked)point=previous+delta.normalized*distance;
            _lineRenderer.SetPosition(i,point);previous=point;
            if(blocked){_lineRenderer.positionCount=i+1;break;}
        }
    }
    public void ResetCast()
    {
        bool was=BlocksActions;
        if((_aiming||_throwing) && IsMine())NetworkManager.Instance.SendScannerGesture(0);
        _aiming=_throwing=false;_queuedSlot=-1;_rig?.ResetPresentation();HideTrajectory();
        if(_ownsLocal){IsLocalScannerAiming=false;_ownsLocal=false;}
        if(was && GetComponent<PlayerHP>()?.IsDead!=true)GetComponent<PlayerItem>()?.SetWeaponVisibility(true);
        _hidWeapon=false;
    }
    private void OnDisable()=>ResetCast();
}
