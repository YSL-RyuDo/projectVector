using UnityEngine;

// Presentation only: no raycasts, damage, hitmarkers or enemy information.
// All recipients start the same shot pattern from authoritative activation.
public class DecoyTrapArea : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _gunfireClip;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private float _duration = 6f;
    [SerializeField] private ControllerDecoyVfx _vfx;
    private bool _isPlaying;
    private float _age, _nextShot;
    private int _burstRemaining;
    private System.Random _pattern;
    public int ShotsPlayed { get; private set; }
    public bool IsPlaying => _isPlaying;

    public void Arm()
    {
        StopPresentation(); ShotsPlayed = 0;
        if (_vfx) _vfx.Arm();
    }

    public void PlayDecoy() => PlayDecoy(_duration, 1);
    public void PlayDecoy(float duration, uint id)
    {
        if (_isPlaying) return;
        _duration = Mathf.Max(.01f, duration);
        _pattern = new System.Random(unchecked((int)id));
        _age = 0; _nextShot = .22f; _burstRemaining = 3; ShotsPlayed = 0;
        _isPlaying = true;
        if (_audioSource) { _audioSource.Stop(); _audioSource.loop = false; }
        if (_vfx) _vfx.Begin(_duration);
    }
    private void Update()
    {
        if (!_isPlaying) return;
        _age += Time.deltaTime;
        if (_age >= _duration) { StopPresentation(); return; }
        if (_age < _nextShot) return;
        // P92 cadence with pauses. Do not catch up many shots in a late frame.
        if (_audioSource && _gunfireClip) _audioSource.PlayOneShot(_gunfireClip);
        if (_muzzleFlash) _muzzleFlash.Play();
        if (_vfx) _vfx.Shot();
        ShotsPlayed++;
        if (--_burstRemaining <= 0)
        {
            _burstRemaining = _pattern.Next(2, 5);
            _nextShot = _age + .65f + (float)_pattern.NextDouble() * .4f;
        }
        else _nextShot = _age + .2f;
    }
    private void StopPresentation()
    {
        _isPlaying = false;
        if (_audioSource) _audioSource.Stop();
        if (_muzzleFlash) _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_vfx) _vfx.Stop();
    }
    private void OnDisable() => StopPresentation();
}
