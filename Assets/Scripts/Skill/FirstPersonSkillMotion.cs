using UnityEngine;

// Absolute, neutral-rig-authored animation clips, not additive weapon corrections.
// Restore before Animator evaluation so firearm animation remains the base state.
[DefaultExecutionOrder(1100)]
public sealed class FirstPersonSkillMotion : MonoBehaviour
{
    public enum Motion { Drone, Emp, Scanner, Ultimate }
    [SerializeField] private Transform _model;
    [SerializeField] private Transform[] _bones;
    [SerializeField] private AnimationClip _drone, _emp, _scanner, _ultimate;
    [SerializeField] private SkinnedMeshRenderer[] _ultimateRenderers;
    [SerializeField] private Mesh[] _ultimateLeftMeshes;
    private Mesh[] _originalMeshes;
    private bool _swappedMeshes;
    private Quaternion[] _rotations;
    private Vector3[] _positions;
    private bool _applied;
    private Motion _owner;

    private void Update() => Restore();
    public void Apply(Motion motion, float time, float weight)
    {
        Restore();
        var clip=motion==Motion.Drone?_drone:motion==Motion.Emp?_emp:motion==Motion.Scanner?_scanner:_ultimate;
        if(!_model || !clip || _bones==null || weight<=0)return;
        if(motion==Motion.Ultimate && _ultimateRenderers!=null && _ultimateLeftMeshes!=null && _ultimateRenderers.Length==_ultimateLeftMeshes.Length){
            if(_originalMeshes==null || _originalMeshes.Length!=_ultimateRenderers.Length)_originalMeshes=new Mesh[_ultimateRenderers.Length];
            for(int i=0;i<_ultimateRenderers.Length;i++)if(_ultimateRenderers[i] && _ultimateLeftMeshes[i]){_originalMeshes[i]=_ultimateRenderers[i].sharedMesh;_ultimateRenderers[i].sharedMesh=_ultimateLeftMeshes[i];}
            _swappedMeshes=true;
        }
        if(_rotations==null || _rotations.Length!=_bones.Length){
            _rotations=new Quaternion[_bones.Length];_positions=new Vector3[_bones.Length];
        }
        for(int i=0;i<_bones.Length;i++){
            _rotations[i]=_bones[i].localRotation;_positions[i]=_bones[i].localPosition;
        }
        clip.SampleAnimation(_model.gameObject,Mathf.Clamp(time,0,clip.length));
        weight=Mathf.Clamp01(weight);
        for(int i=0;i<_bones.Length;i++){
            _bones[i].localRotation=Quaternion.Slerp(_rotations[i],_bones[i].localRotation,weight);
            _bones[i].localPosition=Vector3.Lerp(_positions[i],_bones[i].localPosition,weight);
        }
        _owner=motion;_applied=true;
    }
    public void Restore(Motion owner){if(_owner==owner)Restore();}
    private void Restore()
    {
        if(_swappedMeshes){for(int i=0;i<_ultimateRenderers.Length;i++)if(_ultimateRenderers[i] && _originalMeshes[i])_ultimateRenderers[i].sharedMesh=_originalMeshes[i];_swappedMeshes=false;}
        if(!_applied)return;
        for(int i=0;i<_bones.Length;i++)if(_bones[i]){
            _bones[i].localRotation=_rotations[i];_bones[i].localPosition=_positions[i];
        }
        _applied=false;
    }
    private void OnDisable()=>Restore();
}
