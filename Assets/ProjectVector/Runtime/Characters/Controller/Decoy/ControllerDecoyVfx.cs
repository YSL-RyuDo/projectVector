using UnityEngine;

public sealed class ControllerDecoyVfx : MonoBehaviour
{
    [SerializeField] private Transform[] _panels;
    [SerializeField] private Renderer[] _lenses;
    [SerializeField] private Renderer _wave;
    [SerializeField] private ParticleSystem _landing;
    [SerializeField] private AudioSource _attachSound;
    private MaterialPropertyBlock _block, _waveBlock;
    private float _age, _duration, _shotAge = 100;
    private bool _active;
    public void Arm()
    {
        _age = 0; _duration = float.PositiveInfinity; _shotAge = 100; _active = true;
        if (_landing) _landing.Play();
        if (_attachSound) _attachSound.Play();
    }
    public void Begin(float duration)
    {
        _age = 0; _duration = duration; _active = true;
    }
    public void Shot() => _shotAge = 0;
    public void Stop()
    {
        _active = false;
        if (_wave) _wave.enabled = false;
        if (_attachSound) _attachSound.Stop();
        if (_landing) _landing.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    private void Update()
    {
        if (!_active) return;
        _age += Time.deltaTime; _shotAge += Time.deltaTime;
        float unfold = Mathf.SmoothStep(0, 1, _age / .3f);
        float ending = Mathf.Clamp01((_duration - _age) / .28f);
        for (int i = 0; i < _panels.Length; i++)
            if (_panels[i]) _panels[i].localRotation = Quaternion.Euler(-32 * unfold * ending, i * 120, 0);
        _block ??= new MaterialPropertyBlock();
        float pulse = Mathf.Exp(-_shotAge * 23);
        _block.SetColor("_EmissionColor", new Color(.32f, .44f, .8f) * (.35f + pulse * 2.1f) * ending);
        foreach (var lens in _lenses) if (lens) lens.SetPropertyBlock(_block);
        if (_wave)
        {
            _wave.enabled = _shotAge < .32f;
            _waveBlock ??= new MaterialPropertyBlock();
            _waveBlock.SetFloat("_Progress", Mathf.Clamp01(_shotAge / .32f));
            _wave.SetPropertyBlock(_waveBlock);
        }
    }
    private void OnDisable() => Stop();
}
