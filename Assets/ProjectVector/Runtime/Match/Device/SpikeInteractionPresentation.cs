using System;
using UnityEngine;

// Absolute neutral-rig clips, sampled after firearm animation and restored before
// its next evaluation. No changes to joint lengths or gameplay camera transforms.
[DefaultExecutionOrder(1800)]
public sealed class SpikeInteractionPresentation : MonoBehaviour
{
    [Serializable] public sealed class PoseFrame
    {
        public float time;
        public Vector3[] positions;
        public Quaternion[] rotations;
    }
    [Serializable] public sealed class Rig
    {
        public Transform model;
        public Transform[] bones;
        public AnimationClip plant, defuse;
        public GameObject device, tool;
        public PoseFrame[] plantFrames, defuseFrames;
        [NonSerialized] public Vector3[] positions;
        [NonSerialized] public Quaternion[] rotations;
        [NonSerialized] public bool sampled;
        [NonSerialized] public Vector3[] recoveryPositions;
        [NonSerialized] public Quaternion[] recoveryRotations;
        [NonSerialized] public bool[] recoveryBones;
        [NonSerialized] public Transform[] recoveryFeet;
        [NonSerialized] public Vector3[] recoveryFootPoints, recoveryFootTargets;
    }
    public Rig firstPerson, thirdPerson;
    public AudioSource source;
    public AudioClip equipSound, inputSound;
    int lastInput = -1;
    byte remoteAction;
    float remoteStarted;
    bool shown;
    bool recovering;
    bool suppressRecovery;
    float recoveryStarted;
    public const float StandRecoveryDuration = .55f;
    PlayerSpike spike;
    PlayerItem inventory;
    public bool IsPresenting { get; private set; }
    public bool IsRecovering => recovering;
    public bool IsCarryingDevice { get; private set; }
    void Awake() { spike = GetComponent<PlayerSpike>(); inventory = GetComponent<PlayerItem>(); }
    void Update() { Restore(firstPerson); Restore(thirdPerson); }
    void LateUpdate()
    {
        if (!spike) return;
        bool local = spike.IsLocalPlayer() || PracticePlayerContext.For(spike)!=null;
        byte action = local ? (spike.IsPlanting ? (byte)1 : spike.IsDefusing ? (byte)2 : (byte)0) : remoteAction;
        bool interrupted = GetComponent<PlayerHP>()?.IsDead == true || GetComponent<PlayerDeathController>()?.IsDead == true ||
            (RoundManager.Instance && (RoundManager.Instance.currentState == RoundManager.RoundState.Preparation || RoundManager.Instance.currentState == RoundManager.RoundState.GameOver));
        if(interrupted){action=0;remoteAction=0;recovering=false;}
        // A successful defuse is immediately followed by RoundEnd. Finish the
        // lower-body return there; only death/respawn hard-reset the pose.
        if(RoundManager.Instance && RoundManager.Instance.currentState == RoundManager.RoundState.RoundEnd)action=0;
        if (!local && Time.unscaledTime - remoteStarted > 8) action = 0;
        float elapsed = local ? (action == 1 ? spike.PlantProgress * spike.plantTime : spike.DefuseProgress * 5) : Time.unscaledTime - remoteStarted;
        if (action != 0)
        {
            recovering=false;
            if (!shown && source && equipSound && NetworkManager.Instance?.ReconnectBusy != true) source.PlayOneShot(equipSound, .5f);
            if(action==1)
            {
                int key=elapsed<2.77f?-1:0;
                if(key>lastInput && source && inputSound)source.PlayOneShot(inputSound,.25f);
                lastInput=key;
            }
            Sample(firstPerson, local ? action : (byte)0, elapsed);
            Sample(thirdPerson, action, elapsed);
        }
        else
        {
            Hide(firstPerson); Hide(thirdPerson);
            if(shown&&!interrupted&&!suppressRecovery){recovering=true;recoveryStarted=Time.unscaledTime;}
            if(recovering)
            {
                float recoveryTime=Time.unscaledTime-recoveryStarted;
                SampleRecovery(thirdPerson,recoveryTime);
                if(recoveryTime>=StandRecoveryDuration)recovering=false;
            }
            remoteAction = 0;
            lastInput = -1;
        }
        // Slot 4 uses the authored two-hand carry pose. The inventory's old
        // world-model offset is not a first-person pose and can sit off-screen.
        IsCarryingDevice = action == 0 && local && spike.hasSpike && inventory && inventory.CurrentIndex == 4 && firstPerson?.device && firstPerson.model &&
            GameplayActionPolicy.CanAct(inventory, GameplayAction.WeaponChange, false);
        if(IsCarryingDevice)
        {
            Sample(firstPerson, 1, .65f);
            firstPerson.device.GetComponent<SpikeDeviceVisual>()?.RenderHeld(Time.unscaledTime);
        }
        inventory?.SetWeaponVisibility(action == 0 && !IsCarryingDevice, this);
        IsPresenting = action != 0; shown = IsPresenting;suppressRecovery=false;
    }
    static void Restore(Rig rig)
    {
        if (rig == null || !rig.sampled) return;
        for (int i = 0; i < rig.bones.Length; i++) if (rig.bones[i])
        { rig.bones[i].localPosition = rig.positions[i]; rig.bones[i].localRotation = rig.rotations[i]; }
        rig.sampled = false;
    }
    static void Hide(Rig rig)
    {
        Restore(rig);
        if (rig?.device) rig.device.SetActive(false);
        if (rig?.tool) rig.tool.SetActive(false);
    }
    public static void Sample(Rig rig, byte action, float elapsed)
    {
        if (rig == null || !rig.model) return;
        Restore(rig);
        if (rig.device) rig.device.SetActive(action == 1);
        if (rig.tool) rig.tool.SetActive(action == 2);
        if (action == 0) return;
        var clip = action == 1 ? rig.plant : rig.defuse;
        if (!clip) return;
        int n = rig.bones.Length;
        rig.positions ??= new Vector3[n]; rig.rotations ??= new Quaternion[n];
        for (int i = 0; i < n; i++) { rig.positions[i] = rig.bones[i].localPosition; rig.rotations[i] = rig.bones[i].localRotation; }
        var frames=action==1?rig.plantFrames:rig.defuseFrames;
        if(frames!=null&&frames.Length>0)
        {
            // Apply the authored local poses directly. SampleAnimation on a humanoid
            // can also apply its avatar's root offset (notably Entry), lowering the body twice.
            int next=0;while(next<frames.Length-1&&frames[next].time<elapsed)next++;
            var a=frames[Mathf.Max(0,next-1)];var b=frames[next];
            float blend=a==b?0:Mathf.InverseLerp(a.time,b.time,elapsed);
            for(int i=0;i<n;i++)if(rig.bones[i])
            {rig.bones[i].localPosition=Vector3.Lerp(a.positions[i],b.positions[i],blend);rig.bones[i].localRotation=Quaternion.Slerp(a.rotations[i],b.rotations[i],blend);}
        }
        else clip.SampleAnimation(rig.model.gameObject, Mathf.Clamp(elapsed, 0, clip.length));
        if(action==1&&rig.device)rig.device.GetComponent<SpikeDeviceVisual>()?.RenderHeld(elapsed);
        rig.recoveryPositions ??= new Vector3[n];rig.recoveryRotations ??= new Quaternion[n];
        for(int i=0;i<n;i++){rig.recoveryPositions[i]=rig.bones[i].localPosition;rig.recoveryRotations[i]=rig.bones[i].localRotation;}
        if(rig.recoveryFeet==null)
        {
            var feet=new System.Collections.Generic.List<Transform>();
            foreach(var b in rig.bones)if(b&&(b.name.EndsWith("Foot")||b.name=="FootL"||b.name=="FootR"))feet.Add(b);
            rig.recoveryFeet=feet.ToArray();rig.recoveryFootPoints=new Vector3[feet.Count];rig.recoveryFootTargets=new Vector3[feet.Count];
        }
        for(int i=0;i<rig.recoveryFeet.Length;i++)rig.recoveryFootPoints[i]=rig.model.parent.InverseTransformPoint(rig.recoveryFeet[i].position);
        rig.sampled = true;
    }
    // Only the pelvis and legs blend out. Firearm aiming, weapon visibility,
    // player input and the first-person camera return immediately as before.
    public static void SampleRecovery(Rig rig,float elapsed)
    {
        Hide(rig);
        if(rig?.recoveryPositions==null||elapsed>=StandRecoveryDuration)return;
        int n=rig.bones.Length;
        if(rig.recoveryBones==null)
        {
            rig.recoveryBones=new bool[n];
            foreach(var foot in rig.bones)
            {
                if(!foot||!(foot.name.EndsWith("Foot")||foot.name=="FootL"||foot.name=="FootR"))continue;
                for(int i=0;i<n;i++)
                    if(rig.bones[i]==foot.parent||rig.bones[i]==foot.parent.parent||rig.bones[i]==foot.parent.parent.parent||rig.bones[i].IsChildOf(foot))rig.recoveryBones[i]=true;
            }
        }
        float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/StandRecoveryDuration));
        for(int i=0;i<rig.recoveryFeet.Length;i++)
            rig.recoveryFootTargets[i]=Vector3.Lerp(rig.model.parent.TransformPoint(rig.recoveryFootPoints[i]),rig.recoveryFeet[i].position,blend);
        for(int i=0;i<n;i++)
        {
            var bone=rig.bones[i];if(!bone)continue;
            rig.positions[i]=bone.localPosition;rig.rotations[i]=bone.localRotation;
            if(!rig.recoveryBones[i])continue;
            bone.localPosition=Vector3.Lerp(rig.recoveryPositions[i],rig.positions[i],blend);
            bone.localRotation=Quaternion.Slerp(rig.recoveryRotations[i],rig.rotations[i],blend);
        }
        // Interpolating hip height and joint angles independently sinks the
        // planted foot. Solve each existing leg toward its grounded return path.
        for(int i=0;i<rig.recoveryFeet.Length;i++)
        {
            var foot=rig.recoveryFeet[i];var rotation=foot.rotation;
            SkillArmPose.Reach(foot.parent.parent,foot.parent,foot,rig.model.parent,rig.model.parent.InverseTransformPoint(rig.recoveryFootTargets[i]),1);
            foot.rotation=rotation;
        }
        rig.sampled=true;
    }
    public static void ServerPlant(uint actor, bool active)
    {
        foreach (var p in FindObjectsByType<SpikeInteractionPresentation>(FindObjectsSortMode.None))
            if (p.GetComponent<PlayerIdentity>()?.PlayerId == actor)
            {
                if (active) { p.remoteAction = 1; p.remoteStarted = Time.unscaledTime; }
                else { p.remoteAction = 0; if (p.spike?.IsLocalPlayer() == true) p.spike.ForceCancelPlant(); }
            }
    }
    public static void ServerDefuse(uint actor, byte state, bool half)
    {
        foreach (var p in FindObjectsByType<SpikeInteractionPresentation>(FindObjectsSortMode.None))
        {
            if (state == 0 && p.remoteAction == 2) p.remoteAction = 0;
            if (state == 1 && p.GetComponent<PlayerIdentity>()?.PlayerId == actor)
            { p.remoteAction = 2; p.remoteStarted = Time.unscaledTime - (half ? 2.5f : 0); }
        }
    }
    public static void StopAll(bool immediate = true)
    {
        foreach (var p in FindObjectsByType<SpikeInteractionPresentation>(FindObjectsSortMode.None))
        {
            p.remoteAction=0;p.IsPresenting=false;
            if(immediate){p.recovering=false;p.suppressRecovery=true;}
            // Preserve shown until LateUpdate restores weapon visibility.
            Hide(p.firstPerson);Hide(p.thirdPerson);
        }
    }
    void OnDisable()
    {
        Hide(firstPerson); Hide(thirdPerson);
        if(shown && inventory && GetComponent<PlayerHP>()?.IsDead != true && GetComponent<PlayerDeathController>()?.IsDead != true)
            inventory.SetWeaponVisibility(true, this);
        remoteAction=0; recovering=false;suppressRecovery=false;lastInput=-1; shown=false;IsPresenting=false;IsCarryingDevice=false;
    }
}
