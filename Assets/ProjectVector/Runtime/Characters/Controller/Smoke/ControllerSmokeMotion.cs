using UnityEngine;

// Absolute right-hand pose authored on the neutral controller skeleton.
[DefaultExecutionOrder(1300)]
public sealed class ControllerSmokeMotion : MonoBehaviour
{
    [SerializeField] private Transform _model;
    [SerializeField] private Transform[] _bones;
    [SerializeField] private AnimationClip _clip;
    private Quaternion[] _rotations;
    private Vector3[] _positions;
    private bool _applied, _release;
    private float _age, _recovery;
    public bool Active { get; private set; }
    public void Begin() { ResetPose(); Active = true; _age = _recovery = 0; _release = false; }
    public void Release() { if (Active) { _release = true; _recovery = 0; } }
    private void Update() => Restore();
    private void LateUpdate() => Advance(Time.unscaledDeltaTime);
    public void Advance(float dt)
    {
        Restore();
        if (!Active || !_model || !_clip) return;
        _age += dt;
        if (_release) _recovery += dt;
        if (_recovery >= .45f) { Active = false; return; }
        _rotations ??= new Quaternion[_bones.Length]; _positions ??= new Vector3[_bones.Length];
        for (int i = 0; i < _bones.Length; i++) { _rotations[i] = _bones[i].localRotation; _positions[i] = _bones[i].localPosition; }
        _clip.SampleAnimation(_model.gameObject, _release ? .32f : Mathf.Min(.16f, _age));
        float weight = Mathf.SmoothStep(0, 1, _age / .22f) * (1 - Mathf.SmoothStep(0, 1, Mathf.Max(0, _recovery - .13f) / .32f));
        for (int i = 0; i < _bones.Length; i++) {
            _bones[i].localRotation = Quaternion.Slerp(_rotations[i], _bones[i].localRotation, weight);
            _bones[i].localPosition = Vector3.Lerp(_positions[i], _bones[i].localPosition, weight);
        }
        _applied = true;
    }
    private void Restore()
    {
        if (!_applied) return;
        for (int i = 0; i < _bones.Length; i++) if (_bones[i]) { _bones[i].localRotation = _rotations[i]; _bones[i].localPosition = _positions[i]; }
        _applied = false;
    }
    public void ResetPose() { Restore(); Active = false; }
    private void OnDisable() => ResetPose();
}
