using System;
using UnityEngine;

// All bones, pose targets and emitters are saved in the character prefab.
// Overlay after the weapon aim rig, restore before the next Animator evaluation.
[DefaultExecutionOrder(1300)]
public sealed class EmpGestureRig : MonoBehaviour
{
    [Serializable] public sealed class Pose
    {
        public Transform frame, viewModel, shoulder, shoulderAnchor;
        public Transform[] bones = new Transform[0], charge = new Transform[0], release = new Transform[0];
        public Transform[] fingers = new Transform[0];
        public Quaternion[] openHand = new Quaternion[0];
        public Quaternion openWrist = Quaternion.identity;
        public LineRenderer energy;
        public Transform hand;
        [NonSerialized] public Quaternion[] rest, fingerRest;
        [NonSerialized] public Vector3 viewPosition;
    }
    [SerializeField] private Pose[] _poses = new Pose[0];
    [SerializeField] private FirstPersonSkillMotion.Motion _motionType = FirstPersonSkillMotion.Motion.Emp;
    [SerializeField, Min(.1f)] private float _windup = .5f;
    [SerializeField, Min(.1f)] private float _recovery = .38f;
    private bool _active, _released, _applied;
    private float _age, _releaseAge, _cancelWeight;
    private bool _cancelled;
    private Camera _camera;
    private FirstPersonSkillMotion _motion;
    private float _cancelSample;
    private readonly Vector3[] _energyPoints = new Vector3[24];
    public bool Active => _active;
    public bool Ready => _active && !_released && _age >= _windup;
    public float Windup => _windup;
    public bool WeaponVisible => !_active || (_released && _releaseAge > _recovery * .72f);
    public Vector3 PulseOrigin { get; private set; }
    public bool HasPulseOrigin { get; private set; }

    public void Begin()
    {
        if (_active) return;
        _camera = GetComponent<PlayerCameraController>()?.PlayerCamera;
        _motion = GetComponent<FirstPersonSkillMotion>();
        _active = true; _released = _cancelled = false; _age = _releaseAge = 0;
        HasPulseOrigin=false;
    }
    public void Release()
    {
        if (!_active) Begin();
        _age = _windup; _released = true; _releaseAge = 0; _cancelled = false;
        Advance(0);
    }
    public void Cancel()
    {
        if (!_active || _released) return;
        _cancelWeight = Mathf.SmoothStep(0, 1, _age / (_windup * .75f));
        _cancelSample = Mathf.SmoothStep(0,1,(_age/_windup-.7f)/.3f);
        _cancelled = _released = true; _releaseAge = 0;
    }
    private void Update() => Restore();
    private void LateUpdate() => Advance(Time.unscaledDeltaTime);
    public void Advance(float dt)
    {
        Restore();
        if (!_active)
        {
            foreach (var p in _poses) if (p.energy) p.energy.enabled = false;
            return;
        }
        _age += dt;
        if (_released) _releaseAge += dt;
        if ((_released && _releaseAge >= _recovery) || _age > 3f) { ResetPresentation(); return; }
        float raise = Mathf.SmoothStep(0, 1, _age / (_windup * .75f));
        float thrust = Mathf.SmoothStep(0, 1, (_age / _windup - .7f) / .3f);
        if(_cancelled)thrust=_cancelSample;
        float weight = _released ? (1 - Mathf.SmoothStep(0, 1, Mathf.Max(0, _releaseAge - .09f) / (_recovery - .09f))) : raise;
        if (_cancelled) weight *= _cancelWeight;
        foreach (var p in _poses)
        {
            p.rest = p.rest ?? new Quaternion[p.bones.Length];
            p.fingerRest = p.fingerRest ?? new Quaternion[p.fingers.Length];
            if (p.viewModel) p.viewPosition = p.viewModel.localPosition;
            for (int i=0;i<p.bones.Length;i++) p.rest[i] = p.bones[i].localRotation;
            for (int i=0;i<p.fingers.Length;i++) p.fingerRest[i] = p.fingers[i].localRotation;
            if(p.viewModel)
            {
                // Independent neutral-rig clip. Weight only blends entry/exit;
                // it never adds a rotation correction onto the firearm pose.
                _motion?.Apply(_motionType,thrust*.32f,weight);
            }
            else
            {
            for (int i=0;i<p.bones.Length;i++)
            {
                var target = Quaternion.Slerp(p.charge[i].rotation, p.release[i].rotation, thrust);
                p.bones[i].rotation = Quaternion.Slerp(p.bones[i].rotation, target, weight);
            }
            for (int i=0;i<p.fingers.Length;i++) p.fingers[i].localRotation = Quaternion.Slerp(p.fingers[i].localRotation, p.openHand[i], weight);
            }
            if (p.energy)
            {
                p.energy.enabled = !_released && raise > .1f;
                if (p.energy.enabled)
                {
                    for(int i=0;i<_energyPoints.Length;i++)
                    {
                        float f=i/23f, a=f*7+_age*18;
                        float jitter=(Mathf.Repeat(Mathf.Sin(i*117.3f+Mathf.Floor(_age*18))*43117.1f,1)-.5f)*.008f;
                        _energyPoints[i]=new Vector3(Mathf.Cos(a)*(.018f+jitter), f*.065f+jitter, Mathf.Sin(a)*(.018f+jitter));
                    }
                    p.energy.positionCount=24;p.energy.SetPositions(_energyPoints);
                    p.energy.widthMultiplier=.0025f+raise*.0015f;
                    p.energy.startColor=p.energy.endColor=new Color(.18f,.65f,.8f,raise*.65f);
                }
            }
        }
        _applied = true;
        var visiblePose=_camera && _camera.enabled?Array.Find(_poses,p=>p.viewModel && p.viewModel.gameObject.activeInHierarchy):null;
        if(visiblePose==null)visiblePose=Array.Find(_poses,p=>!p.viewModel);
        if(visiblePose?.hand){PulseOrigin=visiblePose.hand.position;HasPulseOrigin=true;}
    }
    private void Restore()
    {
        if (!_applied) return;
        foreach(var p in _poses)
        {
            for(int i=0;i<p.bones.Length;i++) if(p.bones[i]) p.bones[i].localRotation=p.rest[i];
            for(int i=0;i<p.fingers.Length;i++) if(p.fingers[i]) p.fingers[i].localRotation=p.fingerRest[i];
            if(p.viewModel) p.viewModel.localPosition=p.viewPosition;
        }
        _applied=false;
        _motion?.Restore(_motionType);
    }
    public void ResetPresentation()
    {
        Restore();_active=_released=false;
        foreach(var p in _poses) if(p.energy) p.energy.enabled=false;
    }
    private void OnDisable()=>ResetPresentation();
}
