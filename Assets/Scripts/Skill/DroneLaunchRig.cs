using UnityEngine;

// Models and pose targets are authored in the prefab; only presentation moves.
[DefaultExecutionOrder(1200)]
public class DroneLaunchRig : MonoBehaviour
{
    [SerializeField] private GameObject _device;
    [SerializeField] private Transform[] _fans;
    [SerializeField] private Transform _upperArm, _forearm;
    [SerializeField] private Transform _viewModel;
    [SerializeField] private Transform _shoulderAnchor;
    [SerializeField, Range(40,120)] private float _poseReferenceFieldOfView = 90f;
    private Camera _presentationCamera;
    [SerializeField] private Vector3 _preparedViewOffset;
    [SerializeField] private Transform _stowedShoulder;
    [SerializeField] private Vector3 _stowCameraOffset = new Vector3(0,-.3f,0);
    [SerializeField] private Transform[] _posedBones = new Transform[0], _poseTargets = new Transform[0];
    [SerializeField] private Transform[] _handBones = new Transform[0];
    [SerializeField] private Quaternion[] _openHandPose = new Quaternion[0];
    private Quaternion[] _handRest;
    [Header("Holster > raise/unfold > release > lower > draw")]
    [SerializeField, Min(.05f)] private float _holsterDuration = .24f;
    [SerializeField, Min(.05f)] private float _prepareDuration = .35f;
    [SerializeField, Min(.05f)] private float _launchDuration = .28f;
    [SerializeField, Min(.05f)] private float _returnDuration = .24f;
    [SerializeField, Min(.05f)] private float _drawDuration = .26f;
    [SerializeField] private Vector3 _holsterOffset = new Vector3(.08f,-.5f,-.08f);
    [SerializeField] private Vector3 _holsterEuler = new Vector3(25,-10,-12);
    [SerializeField] private Vector3 _releaseTravel = new Vector3(.05f,.18f,.85f);
    [SerializeField] private AnimationCurve _poseCurve = AnimationCurve.EaseInOut(0,0,1,1);
    private enum Phase { Idle, Holster, Raise, Ready, Release, Deployed, Lower, Draw }
    private Phase _phase;
    private float _elapsed, _weight, _lowerStart, _holsterWeight, _lowerHolster;
    private Vector3 _viewRest, _shoulderRest, _deviceRest;
    private Quaternion _viewRotation;
    private Vector3[] _fanPositions;
    private Quaternion[] _boneRest, _fanRotations;
    public float ReturnDuration => _returnDuration + _drawDuration;
    public float LaunchDuration => _launchDuration;
    public bool ReadyToLaunch => _phase == Phase.Ready;
    public bool IsReturning => _phase == Phase.Lower || _phase == Phase.Draw;
    public bool ReadyForWeaponDraw => _phase == Phase.Lower && _elapsed >= _returnDuration;
    public bool DrawAllowed { get; set; } = true;
    public bool WeaponVisible => _phase == Phase.Idle || _phase == Phase.Holster || _phase == Phase.Draw;

    private void Awake()
    {
        _presentationCamera=GetComponent<PlayerCameraController>()?.PlayerCamera;
        _fanPositions=new Vector3[_fans.Length];
        _fanRotations=new Quaternion[_fans.Length];
        for(int i=0;i<_fans.Length;i++) {
            _fanPositions[i]=_fans[i].localPosition; _fanRotations[i]=_fans[i].localRotation;
        }
        if (_device) { _deviceRest=_device.transform.localPosition; _device.SetActive(false); }
    }
    private void Enter(Phase phase) { _phase=phase; _elapsed=0; }
    public void SetPrepared(bool value)
    {
        if (value) {
            if (_phase != Phase.Idle) return;
            if (_viewModel) { _viewRest=_viewModel.localPosition; _viewRotation=_viewModel.localRotation; }
            if (_stowedShoulder) _shoulderRest=_stowedShoulder.localPosition;
            _boneRest=new Quaternion[_posedBones.Length];
            for(int i=0;i<_posedBones.Length;i++)_boneRest[i]=_posedBones[i].localRotation;
            _handRest=new Quaternion[_handBones.Length];
            for(int i=0;i<_handBones.Length;i++)_handRest[i]=_handBones[i].localRotation;
            _weight=0; Enter(Phase.Holster);
        } else {
            if (_phase == Phase.Idle || IsReturning) return;
            _lowerStart=_weight; _lowerHolster=_holsterWeight; Enter(Phase.Lower);
        }
    }
    public void BeginLaunch() { if (ReadyToLaunch) Enter(Phase.Release); }
    public void SetDeployed()
    {
        // Reconnect may restore a live drone without a local preparation animation.
        if (_phase == Phase.Idle) SetPrepared(true);
        Enter(Phase.Deployed);
        if (_device) { _device.SetActive(false); _device.transform.localPosition=_deviceRest; }
    }
    private void LateUpdate() => Advance(Time.deltaTime);
    // Deterministic step also used by editor regression checks.
    private void Advance(float delta)
    {
        if (_phase == Phase.Idle) return;
        _elapsed+=delta;
        float t;
        switch(_phase) {
            case Phase.Holster:
                t=Smooth(_elapsed/_holsterDuration); ApplyPose(0); ApplyHolster(t);
                if(t>=1) Enter(Phase.Raise);
                break;
            case Phase.Raise:
                t=Smooth(_elapsed/_prepareDuration); ApplyPose(t); ApplyHolster(1-t);
                if(t>=1) Enter(Phase.Ready);
                break;
            case Phase.Ready: ApplyPose(1); break;
            case Phase.Release:
                t=Mathf.Clamp01(_elapsed/_launchDuration); ApplyPose(1-.25f*t);
                if(_device && _viewModel) _device.transform.position+=_viewModel.parent.TransformVector(_releaseTravel)*(t*t);
                if(t>=1) SetDeployed();
                break;
            case Phase.Deployed: ApplyPose(0); ApplyHolster(1); break;
            case Phase.Lower:
                t=Smooth(_elapsed/_returnDuration); ApplyPose(_lowerStart*(1-t)); ApplyHolster(Mathf.Lerp(_lowerHolster,1,t));
                if(t>=1 && DrawAllowed) Enter(Phase.Draw);
                break;
            case Phase.Draw:
                t=Smooth(_elapsed/_drawDuration); ApplyPose(0); ApplyHolster(1-t);
                if(t>=1) { RestorePose(); Enter(Phase.Idle); }
                break;
        }
        bool deviceVisible=_phase==Phase.Raise || _phase==Phase.Ready || _phase==Phase.Release || (_phase==Phase.Lower && _weight>.02f);
        if(_device)_device.SetActive(deviceVisible);
        for(int i=0;i<_fans.Length;i++) {
            _fans[i].localPosition=_fanPositions[i]*Mathf.Lerp(.3f,1,_weight);
            if(deviceVisible)_fans[i].Rotate(Vector3.forward,700*delta,Space.Self);
        }
    }
    private float Smooth(float t) => _poseCurve.Evaluate(Mathf.Clamp01(t));
    private void ApplyHolster(float weight)
    {
        _holsterWeight=weight;
        if(!_viewModel)return;
        _viewModel.localPosition+=_holsterOffset*weight;
        _viewModel.localRotation=_viewRotation*Quaternion.Slerp(Quaternion.identity,Quaternion.Euler(_holsterEuler),weight);
    }
    private void ApplyPose(float weight)
    {
        _weight=weight;
        _holsterWeight=0;
        if (_device) _device.transform.localPosition=_deviceRest;
        if (_viewModel) { _viewModel.localPosition=_viewRest+_preparedViewOffset*weight; _viewModel.localRotation=_viewRotation; }
        if (_stowedShoulder && _viewModel)
            _stowedShoulder.position=_stowedShoulder.parent.TransformPoint(_shoulderRest)+_viewModel.parent.TransformVector(_stowCameraOffset)*weight;
        if(_boneRest==null)return;
        for(int i=0;i<_posedBones.Length;i++) {
            var bone=_posedBones[i];
            Quaternion target=_poseTargets[i].rotation;
            if(_presentationCamera && _viewModel)
                target=_presentationCamera.transform.rotation*Quaternion.Inverse(_viewModel.parent.rotation)*target;
            bone.rotation=Quaternion.Slerp(bone.parent.rotation*_boneRest[i],target,weight);
        }
        // Keep the open sleeve/shoulder outside the frame irrespective of the
        // current weapon's animator pose; wrist/device then enter from below-left.
        if(_viewModel && _upperArm && _shoulderAnchor) {
            Vector3 anchor=_shoulderAnchor.position;
            if(_presentationCamera)anchor=_presentationCamera.transform.TransformPoint(_viewModel.parent.InverseTransformPoint(anchor));
            _viewModel.position+=(anchor-_upperArm.position)*weight;
        }
        for(int i=0;i<_handBones.Length && i<_openHandPose.Length;i++)
            _handBones[i].localRotation=Quaternion.Slerp(_handBones[i].localRotation,_openHandPose[i],weight);
        // Gameplay uses a different FOV from the authoring camera. Preserve the
        // authored screen position without altering the player's camera or aim.
        if(weight>0 && _presentationCamera && _viewModel && _device) {
            var renderer=_device.GetComponent<Renderer>();
            if(renderer) {
                Vector3 center=renderer.bounds.center;
                Vector3 local=_presentationCamera.transform.InverseTransformPoint(center);
                if(local.z>.03f) {
                    float ratio=Mathf.Tan(_presentationCamera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Tan(_poseReferenceFieldOfView*Mathf.Deg2Rad*.5f);
                    Vector3 framed=_presentationCamera.transform.TransformPoint(new Vector3(local.x*ratio,local.y*ratio,local.z));
                    _viewModel.position+=(framed-center)*weight;
                }
            }
        }
    }
    private void RestorePose()
    {
        ApplyPose(0);
        if(_handRest!=null)for(int i=0;i<_handBones.Length;i++)_handBones[i].localRotation=_handRest[i];
        for(int i=0;i<_fans.Length;i++) { _fans[i].localPosition=_fanPositions[i]; _fans[i].localRotation=_fanRotations[i]; }
        if(_device)_device.SetActive(false);
    }
    public void ResetPresentation()
    {
        if (_phase!=Phase.Idle) RestorePose();
        Enter(Phase.Idle);
        DrawAllowed=true;
        if (_device) _device.SetActive(false);
    }
    private void OnDisable() => ResetPresentation();
}
