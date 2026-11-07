using UnityEngine;

// All renderers/particles are authored in Drone.prefab. No UI or hit detection here.
public sealed class DroneCombatVfx : MonoBehaviour
{
    [SerializeField] private LineRenderer _core, _halo, _arc, _impactRing;
    [SerializeField] private ParticleSystem _muzzle, _impact, _flash;
    [SerializeField] private Transform _breakup;
    [SerializeField] private AudioSource _impactAudio;
    [SerializeField, Min(.04f)] private float _shotDuration = .16f;
    private Vector3 _start, _end;
    private Vector3 _impactPosition;
    private Quaternion _impactRotation;
    private float _shotAge = 10, _hitAge = 10;
    private readonly Vector3[] _points = new Vector3[18];
    private bool _broken;

    public void Fire(Vector3 start, Vector3 end)
    {
        _start = start; _end = end; _shotAge = 0;
        if (_muzzle) { _muzzle.transform.position = start; _muzzle.transform.rotation = Quaternion.LookRotation((end-start).normalized); _muzzle.Play(true); }
        Tick(0);
    }

    // Called only after the server accepts the hit, never on the predicted trace.
    public void ConfirmImpact(Vector3 position, Vector3 direction)
    {
        _hitAge = 0;
        Quaternion rotation = Quaternion.LookRotation(direction.sqrMagnitude > .001f ? direction : Vector3.forward);
        _impactPosition = position; _impactRotation = rotation;
        if (_impact) { _impact.transform.SetPositionAndRotation(position, rotation); _impact.Play(true); }
        if (_flash) { _flash.transform.position = position; _flash.Play(true); }
        if (_impactRing) _impactRing.transform.SetPositionAndRotation(position, rotation);
        if (_impactAudio) { _impactAudio.transform.position = position; _impactAudio.Play(); }
        Tick(0);
    }

    // Detached particles survive the destroyed drone; cancellation never explodes.
    public void BreakApart()
    {
        if (_broken || !_breakup) return;
        _broken = true;
        _breakup.SetParent(null, true);
        foreach (var p in _breakup.GetComponentsInChildren<ParticleSystem>()) p.Play();
        var sound = _breakup.GetComponent<AudioSource>(); if (sound) sound.Play();
        if (Application.isPlaying) Destroy(_breakup.gameObject, 2f);
    }

    private void Update() => Tick(Time.deltaTime);
    public void Tick(float dt)
    {
        _shotAge += dt; _hitAge += dt;
        bool firing = _shotAge < _shotDuration;
        if (_core) _core.enabled = firing;
        if (_halo) _halo.enabled = firing;
        if (_arc) _arc.enabled = firing;
        if (firing && _core && _halo && _arc) {
            float fade = Mathf.Pow(1-_shotAge/_shotDuration, 1.5f);
            Vector3 axis = (_end-_start).normalized;
            Vector3 side = Vector3.Cross(axis, Vector3.up).normalized;
            if (side.sqrMagnitude < .01f) side = Vector3.right;
            Vector3 up = Vector3.Cross(side, axis);
            for (int i=0;i<_points.Length;i++) {
                float t=i/(float)(_points.Length-1);
                float envelope = Mathf.Sin(t*Mathf.PI);
                float offset = Mathf.Min(.035f, Vector3.Distance(_start,_end)*.006f)*envelope;
                _points[i]=Vector3.Lerp(_start,_end,t)+offset*(side*Mathf.Sin(i*7.31f+_shotAge*53)+up*Mathf.Cos(i*4.17f-_shotAge*37));
            }
            // Primary shape states the real shot direction. Only secondary
            // electrical detail may bend, never the apparent hit path.
            _core.positionCount=_halo.positionCount=2;
            _core.SetPosition(0,_start);_core.SetPosition(1,_end);
            _halo.SetPosition(0,_start);_halo.SetPosition(1,_end);
            _arc.positionCount=_points.Length;_arc.SetPositions(_points);
            Tint(_core,new Color(.48f,.91f,1,fade),.024f);
            Tint(_halo,new Color(.08f,.65f,1,fade*.24f),.065f);
            Tint(_arc,new Color(.20f,.85f,1,fade*.32f),.012f);
        }
        if (_impactRing) {
            _impactRing.enabled = _hitAge < .2f;
            if (_impactRing.enabled) {
                _impactRing.transform.SetPositionAndRotation(_impactPosition, _impactRotation);
                float t=_hitAge/.2f, radius=Mathf.Lerp(.04f,.30f,t);
                _impactRing.positionCount=48;
                for(int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; _impactRing.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius); }
                Tint(_impactRing,new Color(.65f,.95f,1,(1-t)*.95f),.03f*(1-t)+.004f);
            }
        }
    }
    private static void Tint(LineRenderer line, Color color, float width) { line.startColor=line.endColor=color; line.widthMultiplier=width; }
    private void OnDisable()
    {
        _shotAge=_hitAge=10;
        foreach(var line in new[]{_core,_halo,_arc,_impactRing}) if(line)line.enabled=false;
        foreach(var particle in new[]{_muzzle,_impact,_flash})if(particle)particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
