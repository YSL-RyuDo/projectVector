using UnityEngine;

// Saved clips and devices; no runtime rig or UI construction.
[DefaultExecutionOrder(1350)]
public sealed class ScannerCastRig : MonoBehaviour
{
    [SerializeField] private FirstPersonSkillMotion _motion;
    [SerializeField] private Transform _viewModel;
    [SerializeField] private Transform _firstHand, _worldHand;
    [SerializeField] private GameObject _firstDevice, _worldDevice;
    [SerializeField] private Transform[] _worldBones;
    [SerializeField] private Quaternion[] _readyPose, _releasePose;
    [SerializeField] private float _prepareTime=.32f, _throwTime=.32f, _returnTime=.3f;
    private Quaternion[] _rest;
    private int _phase;
    private float _age;
    private bool _applied;
    private Vector3 _viewPosition;
    private Quaternion _viewRotation;
    private float _returnSample;
    private float _returnWeight=1;
    private Vector3 _releasePosition;
    public bool Active=>_phase!=0;
    public bool WeaponVisible=>_phase==0 || _phase==4 || _phase==5;
    public bool Ready=>_phase==1 && _age>=_prepareTime;
    public bool ReleaseReady=>_phase==2 && _age>=_throwTime;
    public Vector3 ReleasePosition=>_releasePosition;
    private Transform Frame=>GetComponent<PlayerCameraController>()?.PlayerCamera?.transform ?? transform;
    public void Begin(){if(Active)return;_phase=4;_age=0;}
    public void Throw(){if(!Active)Begin();_phase=2;_age=0;}
    public void Return(){
        if(!Active || _phase==3 || _phase==5)return;
        if(_phase==4){_phase=5;_age=Mathf.Max(0,.18f-_age);return;}
        _returnSample=_phase==2?Mathf.Clamp01(_age/_throwTime):0;
        _returnWeight=_phase==1?Mathf.SmoothStep(0,1,_age/_prepareTime):1;
        _phase=3;_age=0;
    }
    private void Update()=>Restore();
    private void LateUpdate()=>Advance(Time.deltaTime);
    public void Advance(float dt)
    {
        Restore();if(!Active)return;_age+=dt;
        if(_phase==3 && _age>=_returnTime){_phase=5;_age=0;}
        if(_phase==5 && _age>=.18f){ResetPresentation();return;}
        if(_phase==4 && _age>=.18f){_phase=1;_age=0;}
        if(_viewModel){_viewPosition=_viewModel.localPosition;_viewRotation=_viewModel.localRotation;}
        if(_phase==4 || _phase==5){
            float lower=_phase==4?Mathf.SmoothStep(0,1,_age/.18f):1-Mathf.SmoothStep(0,1,_age/.18f);
            if(_viewModel){_viewModel.localPosition+=new Vector3(.03f,-.45f,-.06f)*lower;_viewModel.localRotation*=Quaternion.Euler(20*lower,0,0);}
            _firstDevice?.SetActive(false);_worldDevice?.SetActive(false);_applied=true;return;
        }
        float t=_phase==2?Mathf.Clamp01(_age/_throwTime):_phase==3?_returnSample:0;
        float weight=_phase==1?Mathf.SmoothStep(0,1,_age/_prepareTime):_phase==3?_returnWeight*(1-Mathf.SmoothStep(0,1,_age/_returnTime)):1;
        _motion?.Apply(FirstPersonSkillMotion.Motion.Scanner,t*.32f,weight);
        if(_worldBones!=null){
            if(_rest==null)_rest=new Quaternion[_worldBones.Length];
            for(int i=0;i<_worldBones.Length;i++){
                _rest[i]=_worldBones[i].localRotation;
                _worldBones[i].localRotation=Quaternion.Slerp(_rest[i],Quaternion.Slerp(_readyPose[i],_releasePose[i],t),weight);
            }
        }
        _applied=true;
        if(_firstHand)_releasePosition=_firstHand.position+Frame.up*.035f+Frame.forward*.045f;
        bool held=_phase!=3 && !ReleaseReady;
        var camera=GetComponent<PlayerCameraController>()?.PlayerCamera;
        bool local=!Application.isPlaying || (camera && camera.isActiveAndEnabled);
        Place(_firstDevice,_firstHand,Frame,held && local);
        Place(_worldDevice,_worldHand,transform,held && !local);
        // Lost cancel/spawn packets cannot leave a remote player permanently unarmed.
        if(_phase==2 && _age>1.5f)Return();
    }
    static void Place(GameObject device,Transform hand,Transform frame,bool visible)
    {
        if(!device)return;device.SetActive(visible);
        if(hand){device.transform.position=hand.position+frame.up*.035f+frame.forward*.045f;device.transform.rotation=frame.rotation*Quaternion.Euler(0,0,-12);}
    }
    private void Restore()
    {
        if(!_applied)return;
        if(_rest!=null)for(int i=0;i<_worldBones.Length;i++)if(_worldBones[i])_worldBones[i].localRotation=_rest[i];
        if(_viewModel){_viewModel.localPosition=_viewPosition;_viewModel.localRotation=_viewRotation;}
        _motion?.Restore(FirstPersonSkillMotion.Motion.Scanner);_applied=false;
    }
    public void ResetPresentation(){Restore();_phase=0;_firstDevice?.SetActive(false);_worldDevice?.SetActive(false);}
    private void OnDisable()=>ResetPresentation();
}
