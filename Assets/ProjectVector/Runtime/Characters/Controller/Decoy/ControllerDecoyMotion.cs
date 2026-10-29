using UnityEngine;

// Absolute poses on the authored first-person skeleton, not offsets piled onto the gun pose.
[DefaultExecutionOrder(1350)]
public sealed class ControllerDecoyMotion : MonoBehaviour
{
    [SerializeField] private Transform _model;
    [SerializeField] private Transform[] _bones;
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private GameObject _device;
    [SerializeField] private AudioSource _audio;
    private Quaternion[] _rotations;
    private Vector3[] _positions;
    private float _age;
    private bool _applied;
    public bool Active { get; private set; }
    public void Begin() { ResetPose(); Active = true; _age = 0; if (_device) _device.SetActive(true); }
    public void ReleaseDevice() { if (_device) _device.SetActive(false); if (_audio) _audio.Play(); }
    private void Update() => Restore();
    private void LateUpdate()
    {
        Restore();
        if (!Active || !_model || !_clip) return;
        _age += Time.deltaTime;
        _rotations ??= new Quaternion[_bones.Length]; _positions ??= new Vector3[_bones.Length];
        for (int i=0;i<_bones.Length;i++) { _rotations[i]=_bones[i].localRotation; _positions[i]=_bones[i].localPosition; }
        _clip.SampleAnimation(_model.gameObject, Mathf.Min(_age, .72f));
        float weight = Mathf.SmoothStep(0,1,_age/.13f) * (1-Mathf.SmoothStep(0,1,(_age-.65f)/.25f));
        for (int i=0;i<_bones.Length;i++) {
            _bones[i].localRotation=Quaternion.Slerp(_rotations[i],_bones[i].localRotation,weight);
            _bones[i].localPosition=Vector3.Lerp(_positions[i],_bones[i].localPosition,weight);
        }
        _applied=true;
    }
    private void Restore() { if (!_applied) return; for(int i=0;i<_bones.Length;i++) if(_bones[i]){_bones[i].localRotation=_rotations[i];_bones[i].localPosition=_positions[i];} _applied=false; }
    public void ResetPose() { Restore(); Active=false; if(_device)_device.SetActive(false); }
    private void OnDisable() => ResetPose();
}
