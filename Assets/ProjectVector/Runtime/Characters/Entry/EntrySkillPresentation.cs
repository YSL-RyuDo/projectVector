using UnityEngine;
using UnityEngine.UI;

// Only animates authored objects on Entry's existing skeleton. No runtime UI or rig construction.
[DefaultExecutionOrder(1320)]
public sealed class EntrySkillPresentation : MonoBehaviour
{
    public enum Cast { None, Dash, Smoke, Stealth, Blades }
    // Shared by authored first-person clips and the replicated third-person pose.
    // Ambidextrous means either hand can act; only the ultimate prepares both.
    public static bool UsesHand(Cast value, bool left)
    {
        return value==Cast.Blades || (left ? value==Cast.Stealth : value==Cast.Dash || value==Cast.Smoke);
    }
    [SerializeField] private FirstPersonSkillMotion motion;
    [SerializeField] private Transform[] localClaws, worldClaws;
    [SerializeField] private Transform[] worldArmBones;
    [SerializeField] private Transform[] clawMounts;
    [SerializeField] private GameObject heldSmoke;
    [SerializeField] private ParticleSystem dashWake, phaseFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dashSound, smokeSound, phaseSound, bladeSound;
    [SerializeField] private Canvas statusCanvas;
    [SerializeField] private Text status;
    [SerializeField] private Image[] charges;
    private PlayerIdentity identity;
    private PlayerItem item;
    private PlayerCameraController cameraController;
    private Renderer[] body;
    private bool[] bodyVisibility;
    private bool bodyHidden, controlledWeapon, hiddenRemote;
    private Transform weapon;
    private Vector3 weaponPosition;
    private Quaternion weaponRotation;
    private bool posedWeapon;
    private readonly Quaternion[] worldRotations=new Quaternion[6];
    private bool worldPosed;
    private Cast cast;
    private float began, ends, blend, lower, bladeBegan, firedAt=-10;
    private int remaining, firedIndex;
    private bool blades, firingBurst;
    public bool Casting => Time.time<ends;
    public Cast CurrentCast => cast;
    public bool Recovering => Casting || (!blades && lower>.001f);
    public bool ReadyToFire => blades && Time.time>=bladeBegan+.4f && !Casting;
    public bool IsLocal => identity && NetworkManager.Instance && identity.PlayerId==NetworkManager.Instance.MyPlayerId;

    private void Awake()
    {
        identity=GetComponent<PlayerIdentity>();item=GetComponent<PlayerItem>();cameraController=GetComponent<PlayerCameraController>();
        var model=transform.Find("Model");body=model?model.GetComponentsInChildren<Renderer>(true):new Renderer[0];
        bodyVisibility=new bool[body.Length];
    }
    public void Begin(Cast value,float duration)
    {
        cast=value;began=Time.time;ends=began+duration;
        Play(value==Cast.Dash?dashSound:value==Cast.Smoke?smokeSound:phaseSound);
    }
    public void DashConfirmed()
    {
        if(!Casting)Begin(Cast.Dash,.42f);
        if(dashWake)dashWake.Play(true);
    }
    public void StealthConfirmed(bool active)
    {
        if(active){Begin(Cast.Stealth,.55f);if(phaseFlash && IsLocal)phaseFlash.Play(true);}
    }
    public void SetBlades(bool active,int count)
    {
        bool entered=active&&!blades;blades=active;remaining=Mathf.Clamp(count,0,6);
        if(entered){cast=Cast.Blades;bladeBegan=Time.time;firedAt=-10;Play(phaseSound);}
        UpdateClaws();
    }
    public Vector3 FireOrigin(Vector3 fallback)
    {
        int index=Mathf.Clamp(6-remaining,0,5);
        var claws=IsLocal?localClaws:worldClaws;
        return claws!=null && index<claws.Length && claws[index]?claws[index].position:fallback;
    }
    public void Fired(int count)
    {
        firingBurst=Time.time-firedAt<.035f;
        firedIndex=Mathf.Clamp(6-remaining,0,5);firedAt=Time.time;
        remaining=Mathf.Clamp(count,0,6);UpdateClaws();Play(bladeSound);
    }
    private void Play(AudioClip clip){if(audioSource && clip){audioSource.spatialBlend=IsLocal?0:1;audioSource.PlayOneShot(clip,.45f);}}
    private void UpdateClaws()
    {
        // A normal skill can temporarily interrupt the ultimate's hand presentation,
        // without consuming or resetting its remaining ammunition.
        bool presentingBlades=(blades||Time.time-firedAt<.32f)&&!Casting;
        if(clawMounts!=null)for(int i=0;i<clawMounts.Length;i++)if(clawMounts[i])clawMounts[i].gameObject.SetActive(presentingBlades && (i<2)==IsLocal);
        for(int i=0;i<6;i++){
            bool show=presentingBlades && i>=6-remaining;
            if(localClaws!=null && i<localClaws.Length && localClaws[i])localClaws[i].gameObject.SetActive(show && IsLocal);
            if(worldClaws!=null && i<worldClaws.Length && worldClaws[i])worldClaws[i].gameObject.SetActive(show && !IsLocal);
        }
    }
    private void Update()=>RestorePose();
    private void LateUpdate()
    {
        RestorePose();
        bool dead=GetComponent<PlayerHP>()?.IsDead==true;
        if(dead){ends=0;blades=false;remaining=0;}
        UpdateClaws();
        bool mine=IsLocal;
        if(cameraController && cameraController.PlayerCamera && cameraController.PlayerCamera.enabled){
            for(int i=0;i<body.Length;i++)if(body[i]){if(!bodyHidden)bodyVisibility[i]=body[i].forceRenderingOff;body[i].forceRenderingOff=true;}bodyHidden=true;
        }else RestoreBody();
        bool show=(!dead && (Casting||blades||Time.time-firedAt<.32f));
        lower=Mathf.MoveTowards(lower,show?1:0,Time.deltaTime/.16f);
        blend=Mathf.MoveTowards(blend,show?1:0,Time.deltaTime/.18f);
        if(item && (lower>0||controlledWeapon)){
            if(!dead)item.SetWeaponVisibility(lower<.99f, this);controlledWeapon=lower>0;
            int index=item.CurrentIndex;
            if(mine && index>=0 && index<item.Items.Length && item.Items[index]){
                weapon=item.Items[index].transform;weaponPosition=weapon.localPosition;weaponRotation=weapon.localRotation;
                weapon.localPosition+=new Vector3(0,-.42f,-.08f)*lower;weapon.localRotation*=Quaternion.Euler(25*lower,0,0);posedWeapon=true;
            }
        }
        if(!mine){var view=GetComponent<PlayerWeaponViewController>();
            if(lower>=.99f&&!hiddenRemote){view?.HideAll(this);hiddenRemote=true;}
            else if(lower<.99f&&hiddenRemote){if(!dead)view?.RestoreLastView(this);hiddenRemote=false;}}
        if(heldSmoke)heldSmoke.SetActive(mine && Casting && cast==Cast.Smoke && Time.time-began<.28f && lower>=.99f);
        if(mine && motion && blend>0){
            var kind=cast==Cast.Dash?FirstPersonSkillMotion.Motion.Drone:cast==Cast.Smoke?FirstPersonSkillMotion.Motion.Emp:FirstPersonSkillMotion.Motion.Scanner;
            float sample=Mathf.Min(Time.time-began,.55f);
            if(!Casting && (blades||cast==Cast.Blades||Time.time-firedAt<.32f)){
                kind=FirstPersonSkillMotion.Motion.Ultimate;sample=Mathf.Min(Time.time-bladeBegan,.4f);
                if(Time.time-firedAt<.32f)sample=(firingBurst?1.0f:firedIndex%2==0?.4f:.7f)+(Time.time-firedAt)/.32f*.3f;
            }
            motion.Apply(kind,sample,blend);
        }
        else if(!mine && blend>0 && worldArmBones!=null && worldArmBones.Length==6){
            for(int i=0;i<6;i++)worldRotations[i]=worldArmBones[i].localRotation;
            float strike=Mathf.Sin(Mathf.Clamp01((Time.time-firedAt)/.32f)*Mathf.PI);
            var poseCast=blades&&!Casting?Cast.Blades:cast;
            for(int side=0;side<2;side++){
                // Keep the unused hand down through recovery too. Testing only
                // Casting made it pop back up before the gun had returned.
                bool idleHand=!UsesHand(poseCast,side==0);
                if(idleHand){var upper=worldArmBones[side*3];var fore=worldArmBones[side*3+1];var hand=worldArmBones[side*3+2];
                    upper.rotation=Quaternion.Slerp(upper.rotation,Quaternion.FromToRotation(fore.position-upper.position,transform.TransformDirection(new Vector3(side==0?-.1f:.1f,-1,0)))*upper.rotation,blend);
                    fore.rotation=Quaternion.Slerp(fore.rotation,Quaternion.FromToRotation(hand.position-fore.position,-transform.up)*fore.rotation,blend);continue;}
                float reach=(firingBurst||firedIndex%2==side)?strike:0;
                var target=new Vector3(side==0?-.22f:.22f,1.0f+reach*.12f,.25f+reach*.3f);
                if(Casting&&cast==Cast.Smoke&&side==1)target=new Vector3(.2f,1.3f,.65f);
                SkillArmPose.Reach(worldArmBones[side*3],worldArmBones[side*3+1],worldArmBones[side*3+2],transform,target,blend);
                if(poseCast==Cast.Blades && clawMounts!=null && clawMounts.Length==4 && clawMounts[side+2]){
                    var hand=worldArmBones[side*3+2];var mount=clawMounts[side+2];
                    var direction=transform.TransformDirection(new Vector3(side==0?-.18f:.12f,-.12f*(1-reach),1));
                    var wrist=Quaternion.LookRotation(direction,transform.up)*Quaternion.Inverse(mount.rotation)*hand.rotation;
                    hand.rotation=Quaternion.Slerp(hand.rotation,wrist,blend);
                }
            }
            worldPosed=true;
        }
        var stealth=GetComponent<EntrySkill_E>();
        if(statusCanvas)statusCanvas.enabled=mine&&!dead&&(blades||Casting||stealth?.IsStealth==true);
        if(status)status.text=blades?$"야수 여제  {remaining}/6":Casting?(cast==Cast.Dash?"기습 준비":cast==Cast.Smoke?"암습":"야수화"):"야수화 · 가까운 적에게 노출";
        if(charges!=null)for(int i=0;i<charges.Length;i++)if(charges[i]){charges[i].enabled=blades;charges[i].color=i<remaining?new Color(.2f,.9f,1):new Color(.3f,.35f,.38f,.5f);}
    }
    private void RestorePose(){if(worldPosed){for(int i=0;i<6;i++)if(worldArmBones[i])worldArmBones[i].localRotation=worldRotations[i];worldPosed=false;}if(motion){motion.Restore(FirstPersonSkillMotion.Motion.Drone);motion.Restore(FirstPersonSkillMotion.Motion.Emp);motion.Restore(FirstPersonSkillMotion.Motion.Scanner);motion.Restore(FirstPersonSkillMotion.Motion.Ultimate);}if(posedWeapon){if(weapon){weapon.localPosition=weaponPosition;weapon.localRotation=weaponRotation;}posedWeapon=false;}}
    private void RestoreBody(){if(!bodyHidden)return;for(int i=0;i<body.Length;i++)if(body[i])body[i].forceRenderingOff=bodyVisibility[i];bodyHidden=false;}
    private void OnDisable()
    {
        RestorePose();RestoreBody();
        if(GetComponent<PlayerHP>()?.IsDead!=true){if(controlledWeapon)item?.SetWeaponVisibility(true, this);if(hiddenRemote)GetComponent<PlayerWeaponViewController>()?.RestoreLastView(this);}
        controlledWeapon=hiddenRemote=blades=false;lower=blend=ends=0;firedAt=-10;remaining=0;UpdateClaws();
        if(heldSmoke)heldSmoke.SetActive(false);if(statusCanvas)statusCanvas.enabled=false;
    }
}
