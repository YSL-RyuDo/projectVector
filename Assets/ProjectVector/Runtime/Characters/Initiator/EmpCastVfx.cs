using UnityEngine;

// Authored device, emitters and lines only. This component never chooses hit targets.
public sealed class EmpCastVfx : MonoBehaviour
{
    [SerializeField] private float _radius = 7f, _duration = .65f;
    [SerializeField] private LineRenderer[] _waveRings = new LineRenderer[0], _targetArcs = new LineRenderer[0];
    [SerializeField] private Transform[] _vents = new Transform[0];
    [SerializeField] private Renderer[] _cores = new Renderer[0];
    [SerializeField] private GameObject[] _devices = new GameObject[0];
    [SerializeField] private ParticleSystem _burst;
    [SerializeField] private AudioSource _castAudio, _hitAudio;
    private readonly Vector3[] _targets = new Vector3[8];
    private readonly Vector3[] _points = new Vector3[64];
    private Vector3 _origin;
    private Vector3 _arcOrigin;
    private float _age=10f, _pending;
    private int _count;
    private MaterialPropertyBlock _properties;
    public float Radius => _radius;
    public bool Playing => _age < _duration;
    public int ConfirmedTargets => _count;

    public void Prepare() { _pending=.6f; }
    public void Confirm(Vector3 origin, Vector3[] targets)
    {
        _origin=origin;_count=Mathf.Min(targets?.Length??0,Mathf.Min(_targets.Length,_targetArcs.Length));
        for(int i=0;i<_count;i++)_targets[i]=targets[i];
        _age=0;_pending=0;
        var gesture=GetComponent<EmpGestureRig>();
        _arcOrigin=gesture && gesture.HasPulseOrigin?gesture.PulseOrigin:origin+Vector3.up*1.1f;
        if(_burst){_burst.transform.position=_arcOrigin;_burst.Play();}
        if(_castAudio)_castAudio.Play();
        if(_count>0 && _hitAudio)_hitAudio.PlayDelayed(.055f);
        Tick(0);
    }
    private void Update()
    {
        var identity=GetComponent<PlayerIdentity>();
        bool local=identity && NetworkManager.Instance && identity.PlayerId==NetworkManager.Instance.MyPlayerId;
        bool hide=local && (InitiatorSkill_Q.IsLocalPreparing || InitiatorSkill_Q.IsLocalDroneMode || InitiatorSkill_Q.IsLocalReturning);
        foreach(var device in _devices)if(device && device.activeSelf==hide)device.SetActive(!hide);
        Tick(Time.deltaTime);
    }
    public void Tick(float dt)
    {
        _age+=dt;_pending=Mathf.Max(0,_pending-dt);
        float t=Mathf.Clamp01(_age/_duration);
        float energy=Playing?Mathf.Pow(1-t,2):_pending>0?.22f:0;
        if(_properties==null)_properties=new MaterialPropertyBlock();
        _properties.SetColor("_EmissionColor",new Color(.05f,.8f,1)*(.35f+energy*5));
        foreach(var core in _cores)if(core)core.SetPropertyBlock(_properties);
        foreach(var vent in _vents)if(vent)vent.localRotation=Quaternion.Euler(0,0,energy*24);
        for(int ring=0;ring<_waveRings.Length;ring++)
        {
            var line=_waveRings[ring];if(!line)continue;
            // The server resolves the entire radius immediately. An expanding
            // wave would wrongly suggest travel time and a later dodge window.
            line.enabled=Playing && ring<2;
            if(!line.enabled)continue;
            float r=_radius;
            for(int i=0;i<64;i++){
                float a=i*Mathf.PI*2/64;
                _points[i]=_origin+new Vector3(Mathf.Cos(a)*r,.06f,Mathf.Sin(a)*r);
            }
            line.positionCount=64;line.SetPositions(_points);
            Color color=ring==0?new Color(.65f,.55f,1):new Color(.28f,.35f,.85f);
            color.a=Mathf.Pow(1-t,2)*(ring==0?.88f:.16f);line.startColor=line.endColor=color;
            line.widthMultiplier=ring==0?.055f:.13f;
        }
        for(int n=0;n<_targetArcs.Length;n++)
        {
            var line=_targetArcs[n];if(!line)continue;
            line.enabled=n<_count && _age<.18f;
            if(!line.enabled)continue;
            Vector3 start=_arcOrigin,end=_targets[n];
            var axis=(end-start).normalized;var side=Vector3.Cross(axis,Vector3.up).normalized;
            for(int i=0;i<64;i++){
                float f=i/63f, envelope=Mathf.Sin(f*Mathf.PI);
                float seed=i*127.1f+n*311.7f+Mathf.Floor(_age*24)*73.1f;
                float jitter=Mathf.Repeat(Mathf.Sin(seed)*43758.5453f,1)*2-1;
                float lift=Mathf.Repeat(Mathf.Sin(seed+43.7f)*23421.631f,1)*2-1;
                _points[i]=Vector3.Lerp(start,end,f)+envelope*.055f*(side*jitter+Vector3.up*lift);
            }
            line.positionCount=64;line.SetPositions(_points);
            line.startColor=line.endColor=new Color(.7f,.62f,1,Mathf.Clamp01(1-_age/.18f));line.widthMultiplier=.014f;
        }
    }
    public void ResetPresentation()
    {
        _age=10;_pending=0;_count=0;Tick(0);
        if(_burst)_burst.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(_castAudio)_castAudio.Stop();if(_hitAudio)_hitAudio.Stop();
    }
    private void OnDisable()=>ResetPresentation();
}
