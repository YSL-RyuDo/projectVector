using UnityEngine;

// Presentation on the existing first-person skeleton; never moves the camera or changes hit bones.
[DefaultExecutionOrder(1320)]
public sealed class SentinelCastMotion : MonoBehaviour
{
    [SerializeField] private Transform upper, forearm, hand, frame, heldDevice;
    [SerializeField] private Transform overchargeBurst;
    [SerializeField] private FirstPersonSkillMotion skillMotion;
    private Quaternion upperRotation, foreRotation, handRotation;
    private bool applied;
    private float blend, releaseUntil;
    private float burstUntil;
    private PlayerIdentity identity;
    private Renderer[] worldBody;
    private bool[] savedVisibility;
    private bool bodyHidden;
    private PlayerCameraController cameraController;
    private Transform loweredWeapon;
    private Vector3 weaponPosition;
    private Quaternion weaponRotation;
    private bool weaponPoseApplied, controlledWeapon, remoteViewHidden;
    private float lowerBlend;
    private byte remotePhase, sentPhase;
    private float remoteUntil, sentAt;
    public void ReceiveGesture(byte phase){if(identity && NetworkManager.Instance && identity.PlayerId==NetworkManager.Instance.MyPlayerId)return;remotePhase=phase;remoteUntil=Time.time+(phase==2?.6f:8f);}
    public bool ReadyToCast => lowerBlend>=.99f;
    public bool Recovering => Time.time<releaseUntil || lowerBlend>0;
    private void Awake(){identity=GetComponent<PlayerIdentity>();cameraController=GetComponent<PlayerCameraController>();var model=transform.Find("Model");worldBody=model?model.GetComponentsInChildren<Renderer>(true):new Renderer[0];savedVisibility=new bool[worldBody.Length];}
    public void Release(){releaseUntil=Time.time+.6f;}
    public void ConfirmOvercharge(){burstUntil=Time.time+.6f;}
    private void Update(){Restore();}
    private void LateUpdate()
    {
        Restore();
        // Imported body animation may re-enable mesh renderers. Keep only the local
        // first-person camera free of its own head; remote/preview/death views retain their state.
        bool hideBody=cameraController && cameraController.PlayerCamera && cameraController.PlayerCamera.enabled;
        if(hideBody){for(int i=0;i<worldBody.Length;i++)if(worldBody[i]){if(!bodyHidden)savedVisibility[i]=worldBody[i].forceRenderingOff;worldBody[i].forceRenderingOff=true;}bodyHidden=true;}
        else RestoreBodyVisibility();
        if(overchargeBurst){float left=burstUntil-Time.time;overchargeBurst.gameObject.SetActive(left>0);if(left>0)overchargeBurst.localScale=Vector3.one*Mathf.Lerp(.1f,4,1-left/.6f);}
        bool mine=identity && NetworkManager.Instance && identity.PlayerId==NetworkManager.Instance.MyPlayerId;
        bool selecting=mine && (SentinelSkill_C.IsSelectingSentinelC || SentinelSkill_E.IsSelectingSentinelE || SentinelSkill_X.IsSelectingSentinelX);
        bool releasing=mine && Time.time<releaseUntil;
        if(mine){byte phase=(byte)(selecting?1:releasing?2:0);if(phase!=sentPhase || (phase==1 && Time.time-sentAt>3)){NetworkManager.Instance.SendSentinelGesture(phase);sentPhase=phase;sentAt=Time.time;}}
        else {selecting=remotePhase==1 && Time.time<remoteUntil;releasing=remotePhase==2 && Time.time<remoteUntil;}
        if(GetComponent<PlayerHP>()?.IsDead==true){selecting=releasing=false;releaseUntil=0;}
        lowerBlend=Mathf.MoveTowards(lowerBlend,selecting||releasing?1:0,Time.deltaTime/ .18f);
        var item=GetComponent<PlayerItem>();
        // Remote view uses WeaponHolderModel's mirror, not PlayerItem's original weapon objects.
        if(!mine){
            var view=GetComponent<PlayerWeaponViewController>();
            if(lowerBlend>=.99f && !remoteViewHidden){view?.HideAll();remoteViewHidden=true;}
            else if(lowerBlend<.99f && remoteViewHidden){if(GetComponent<PlayerHP>()?.IsDead!=true)view?.RestoreLastView();remoteViewHidden=false;}
        }
        if(item && (lowerBlend>0 || controlledWeapon)){
            if(GetComponent<PlayerHP>()?.IsDead!=true)item.SetWeaponVisibility(lowerBlend<.99f);
            controlledWeapon=lowerBlend>0;
            int index=item.CurrentIndex;
            if(mine && index>=0 && index<item.Items.Length && item.Items[index]){
                loweredWeapon=item.Items[index].transform;
                weaponPosition=loweredWeapon.localPosition;weaponRotation=loweredWeapon.localRotation;
                loweredWeapon.localPosition+=new Vector3(.03f,-.45f,-.06f)*lowerBlend;
                loweredWeapon.localRotation*=Quaternion.Euler(20*lowerBlend,0,0);weaponPoseApplied=true;
            }
        }
        blend=Mathf.MoveTowards(blend,selecting||releasing?1:0,Time.deltaTime*7);
        if(heldDevice)heldDevice.gameObject.SetActive(mine && (selecting||releasing) && ReadyToCast && !SentinelSkill_X.IsChargingSentinelX);
        if(skillMotion){
            if(mine && blend>0)skillMotion.Apply(FirstPersonSkillMotion.Motion.Drone,releasing?.32f:0,blend);
            return;
        }
        if(blend<=0 || !upper || !forearm || !hand || !frame)return;
        upperRotation=upper.localRotation;foreRotation=forearm.localRotation;handRotation=hand.localRotation;
        var target=releasing?new Vector3(-.18f,-.13f,.66f):new Vector3(-.26f,-.24f,.43f);
        SkillArmPose.Reach(upper,forearm,hand,frame,target,blend);applied=true;
    }
    private void Restore(){skillMotion?.Restore(FirstPersonSkillMotion.Motion.Drone);if(weaponPoseApplied){if(loweredWeapon){loweredWeapon.localPosition=weaponPosition;loweredWeapon.localRotation=weaponRotation;}weaponPoseApplied=false;}if(!applied)return;if(upper)upper.localRotation=upperRotation;if(forearm)forearm.localRotation=foreRotation;if(hand)hand.localRotation=handRotation;applied=false;}
    private void RestoreBodyVisibility(){if(!bodyHidden)return;for(int i=0;i<worldBody.Length;i++)if(worldBody[i])worldBody[i].forceRenderingOff=savedVisibility[i];bodyHidden=false;}
    private void OnDisable(){Restore();RestoreBodyVisibility();if(GetComponent<PlayerHP>()?.IsDead!=true){if(controlledWeapon)GetComponent<PlayerItem>()?.SetWeaponVisibility(true);if(remoteViewHidden)GetComponent<PlayerWeaponViewController>()?.RestoreLastView();}controlledWeapon=remoteViewHidden=false;remotePhase=sentPhase=0;lowerBlend=0;blend=releaseUntil=burstUntil=0;if(heldDevice)heldDevice.gameObject.SetActive(false);if(overchargeBurst)overchargeBurst.gameObject.SetActive(false);}
}
