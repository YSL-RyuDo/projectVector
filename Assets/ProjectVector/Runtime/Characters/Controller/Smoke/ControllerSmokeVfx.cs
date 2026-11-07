using UnityEngine;

// Presentation only. Server lifetime/radius and outline occlusion remain authoritative.
public sealed class ControllerSmokeVfx : MonoBehaviour
{
    [SerializeField] private Renderer _volume;
    [SerializeField] private AudioSource _audio;
    [SerializeField] private float _settleTime = .65f;
    private MaterialPropertyBlock _properties;
    private float _age, _duration = 18;
    private bool _dissolving;
    private float _fade;
    public void Begin(float duration)
    {
        _duration = Mathf.Max(.01f, duration); _age = 0; _fade = 0; _dissolving = false;
        if (_audio) _audio.Play();
        Advance(0);
    }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float delta)
    {
        _age += delta;
        if (_age >= _duration && !_dissolving) Dissolve();
        if (_dissolving) { _fade += delta; if (_fade >= .25f) { Destroy(gameObject); return; } }
        if (!_volume) return;
        _properties ??= new MaterialPropertyBlock();
        _properties.SetFloat("_CloudTime", _age);
        _properties.SetFloat("_Phase", Mathf.Clamp01(_age / _settleTime));
        // Edge turbulence winds down before expiry; core stays opaque until server removal.
        _properties.SetFloat("_End", Mathf.Clamp01((_duration - _age) / .8f));
        _properties.SetFloat("_Opacity", _dissolving ? .2f * (1 - _fade / .25f) : 1);
        _volume.SetPropertyBlock(_properties);
    }
    public void Dissolve()
    {
        if (_dissolving) return;
        _dissolving = true; _fade = 0;
        // After server expiry only a thin visual residue remains, never concealment.
        var occluder = GetComponent<SmokeOutlineOccluder>(); if (occluder) occluder.enabled = false;
        if (_volume) {
            _properties ??= new MaterialPropertyBlock();
            _properties.SetFloat("_Opacity", .2f);
            _volume.SetPropertyBlock(_properties);
        }
    }
}
