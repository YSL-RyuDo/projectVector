using UnityEngine;

// Authored 3D hierarchy. Coordinates come from the server and real map colliders.
public sealed class DischargeVfx : MonoBehaviour
{
    [SerializeField] private Transform[] _relays;
    [SerializeField] private LineRenderer _boundary;
    [SerializeField] private LineRenderer[] _bands, _bolts, _impactRings;
    [SerializeField] private ParticleSystem[] _sparks;
    [SerializeField] private LineRenderer[] _boltCores, _chargeRings, _shockRings, _fractures;
    [SerializeField] private ParticleSystem[] _groundSparks;
    [SerializeField] private Light[] _strikeLights;
    [SerializeField] private LineRenderer[] _fanBolts, _fanCores;
    [SerializeField] private MeshFilter[] _sectors;
    private readonly Mesh[] _sectorMeshes=new Mesh[7];
    private readonly MeshRenderer[] _sectorRenderers=new MeshRenderer[7];
    private MaterialPropertyBlock _sectorProperty;
    private readonly Vector3[] _fanEnds=new Vector3[42];
    [SerializeField] private AudioSource _audio;
    [SerializeField] private AudioClip _deploy, _strike;
    private Vector3 _origin, _direction;
    private readonly Vector3[] _ground = new Vector3[7], _hover = new Vector3[7];
    private readonly float[] _bandAge = new float[7], _boltAge = new float[13], _ringAge = new float[6];
    private float _range, _age, _warning, _interval, _finished=-1;
    private int _hits;
    private readonly Vector3[][] _ringPoints = new Vector3[7][];
    private readonly Vector3[][] _crackPoints = new Vector3[7][];
    private readonly Vector3[][] _shockOuter = new Vector3[7][];
    public Vector3[] GroundPoints => _ground;
    public Vector3[] RelayPoints => _hover;
    public static bool MapCollider(Collider c) => c && !c.isTrigger && !c.GetComponentInParent<PlayerIdentity>() &&
        !c.GetComponentInParent<CharacterController>() && !c.GetComponentInParent<DischargeVfx>();
    public static Vector3 Ground(Vector3 sample)
    {
        var hits=Physics.RaycastAll(sample+Vector3.up*1.5f,Vector3.down,12,~0,QueryTriggerInteraction.Ignore);
        float distance=float.PositiveInfinity;Vector3 point=sample;
        foreach(var h in hits)if(MapCollider(h.collider) && h.normal.y>.35f && h.distance<distance){point=h.point;distance=h.distance;}
        return point+Vector3.up*.045f;
    }
    public static Vector3 Surface(Vector3 origin,Vector3 proposed)
    {
        // Keep visual warning on this side of solid walls, not on the unseen floor beyond them.
        Vector3 start=origin+Vector3.up;Vector3 delta=proposed+Vector3.up-start;float length=delta.magnitude;
        float end=length;
        foreach(var h in Physics.RaycastAll(start,delta.normalized,length,~0,QueryTriggerInteraction.Ignore))
            if(MapCollider(h.collider) && Mathf.Abs(h.normal.y)<.7f)end=Mathf.Min(end,Mathf.Max(0,h.distance-.12f));
        return Ground(origin+delta.normalized*end);
    }
    public void Begin(Vector3 origin,Vector3 direction,float range,float warning,float interval)
    {
        _sectorProperty ??= new MaterialPropertyBlock();
        _origin=origin;_direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
        if(_direction.sqrMagnitude<.01f)_direction=Vector3.forward;
        _range=range;_warning=warning;_interval=interval;_age=0;_finished=-1;_hits=0;transform.position=origin;
        for(int i=0;i<7;i++)
        {
            _ground[i]=Surface(origin,origin+_direction*(range*(i+.5f)/7));
            Vector3 foot=_ground[i];float height=3.8f;
            foreach(var h in Physics.RaycastAll(foot+Vector3.up*.1f,Vector3.up,4.1f,~0,QueryTriggerInteraction.Ignore))
                if(MapCollider(h.collider))height=Mathf.Min(height,Mathf.Max(.35f,h.distance-.35f));
            _hover[i]=foot+Vector3.up*height;_bandAge[i]=-1;
            for(int side=0;side<6;side++){
                int k=i*6+side;float angle=-55+side*22;float depth=(side%2==0?.3f:.72f);
                _fanEnds[k]=Surface(origin,origin+Quaternion.AngleAxis(angle,Vector3.up)*_direction*(range*(i+depth)/7));
                _fanBolts[k].enabled=_fanCores[k].enabled=false;
            }
            _relays[i].position=_hover[i];_relays[i].rotation=Quaternion.LookRotation(_direction);_relays[i].localScale=Vector3.one;
            _ringPoints[i]=new Vector3[49];_crackPoints[i]=new Vector3[45];_shockOuter[i]=new Vector3[49];
            for(int j=0;j<49;j++){float a=j*Mathf.PI*2/48;_shockOuter[i][j]=Surface(foot,foot+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.42f);}
            _chargeRings[i].positionCount=49;_chargeRings[i].enabled=true;
            _shockRings[i].positionCount=49;_shockRings[i].enabled=false;
            _fractures[i].positionCount=45;_fractures[i].enabled=false;
            _strikeLights[i].intensity=0;
            _groundSparks[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            for(int j=0;j<45;j++){
                float angle=(j/5)*Mathf.PI*2/9;float radial=(j%5)/4f*.95f;
                _crackPoints[i][j]=Ground(foot+new Vector3(Mathf.Cos(angle+Mathf.Sin(j*13)*.12f)*radial,0,Mathf.Sin(angle+Mathf.Sin(j*13)*.12f)*radial));
            }
            _fractures[i].SetPositions(_crackPoints[i]);
            BuildSector(i);
            SetArc(_bands[i],range*(i+1)/7);_bands[i].enabled=true;
        }
        var points=new Vector3[51];points[0]=Ground(origin);points[50]=points[0];
        for(int j=0;j<49;j++)points[j+1]=Surface(origin,origin+Quaternion.AngleAxis(-60+j*2.5f,Vector3.up)*_direction*range);
        _boundary.positionCount=points.Length;_boundary.SetPositions(points);_boundary.enabled=true;
        for(int i=0;i<_bolts.Length;i++){_bolts[i].enabled=false;_boltCores[i].enabled=false;_boltAge[i]=-1;}
        for(int i=0;i<_impactRings.Length;i++){_impactRings[i].enabled=false;_ringAge[i]=-1;}
        if(_audio && _deploy)_audio.PlayOneShot(_deploy,.45f);
        Advance(0);
    }
    void SetArc(LineRenderer line,float distance)
    {
        var points=new Vector3[33];for(int i=0;i<33;i++)points[i]=Surface(_origin,_origin+Quaternion.AngleAxis(-60+120*i/32f,Vector3.up)*_direction*distance);
        line.positionCount=points.Length;line.SetPositions(points);
    }
    void BuildSector(int index)
    {
        // World floor-conforming temporary mesh instance; saved hierarchy and template remain intact.
        const int columns=33,rows=3;var vertices=new Vector3[columns*rows];
        for(int row=0;row<rows;row++)for(int col=0;col<columns;col++){
            float radius=_range*(index+row/(float)(rows-1))/7;
            Vector3 point=Surface(_origin,_origin+Quaternion.AngleAxis(-60+120*col/(float)(columns-1),Vector3.up)*_direction*radius);
            vertices[row*columns+col]=_sectors[index].transform.InverseTransformPoint(point+Vector3.up*.008f);
        }
        if(!_sectorMeshes[index])_sectorMeshes[index]=Instantiate(_sectors[index].sharedMesh);
        var mesh=_sectorMeshes[index];mesh.vertices=vertices;mesh.RecalculateBounds();_sectors[index].sharedMesh=mesh;
        _sectorRenderers[index]=_sectors[index].GetComponent<MeshRenderer>();_sectorRenderers[index].enabled=false;
    }
    public void Strike(int slice)
    {
        if(slice<0 || slice>=7 || _finished>=0)return;
        _bandAge[slice]=0;_boltAge[slice]=0;BoltPair(slice,_hover[slice]-Vector3.up*.12f,_ground[slice],slice);
        _groundSparks[slice].transform.position=_ground[slice];_groundSparks[slice].Play(true);
        for(int side=0;side<6;side++){int k=slice*6+side;Bolt(_fanBolts[k],_hover[slice],_fanEnds[k],k+70);Bolt(_fanCores[k],_hover[slice],_fanEnds[k],k+70);}
        _strikeLights[slice].transform.position=_ground[slice]+Vector3.up*.55f;
        _shockRings[slice].enabled=_fractures[slice].enabled=true;
        _sectorRenderers[slice].enabled=true;
        if(_audio && _strike){_audio.pitch=.82f+slice*.035f;_audio.PlayOneShot(_strike,.7f);}
    }
    public void Hit(Vector3 torso)
    {
        if(_hits>=6 || _finished>=0)return;int n=_hits++;
        int relay=Mathf.Clamp((int)(Vector3.Distance(torso-Vector3.up,_origin)/_range*7),0,6);
        _boltAge[7+n]=0;BoltPair(7+n,_hover[relay]-Vector3.up*.12f,torso,31+n);
        var ring=_impactRings[n];var points=new Vector3[33];
        for(int j=0;j<points.Length;j++){float a=j*Mathf.PI*2/32;points[j]=torso+new Vector3(Mathf.Cos(a)*.27f,Mathf.Sin(a)*.27f,0);}
        ring.positionCount=points.Length;ring.SetPositions(points);ring.enabled=true;_ringAge[n]=0;
        _sparks[n].transform.position=torso;_sparks[n].Play(true);
    }
    static void Bolt(LineRenderer line,Vector3 from,Vector3 to,int seed)
    {
        var points=new Vector3[15];Vector3 side=Vector3.Cross((to-from).normalized,Vector3.right).normalized;
        for(int i=0;i<15;i++){
            float t=i/14f;float noise=Mathf.Sin(seed*7+i*32.31f)*Mathf.Sin(t*Mathf.PI)*.32f;
            points[i]=Vector3.Lerp(from,to,t)+side*noise+Vector3.right*Mathf.Cos(i*11+seed)*Mathf.Sin(t*Mathf.PI)*.20f;
        }
        line.positionCount=15;line.SetPositions(points);line.enabled=true;
    }
    void BoltPair(int index,Vector3 from,Vector3 to,int seed)
    {
        Bolt(_bolts[index],from,to,seed);Bolt(_boltCores[index],from,to,seed);
    }
    public void Finish(bool cancelled)
    {
        if(_finished>=0)return;_finished=_age;_boundary.enabled=false;
        foreach(var b in _bands)b.enabled=false;
        foreach(var c in _chargeRings)c.enabled=false;
        if(cancelled){
            for(int i=0;i<_bolts.Length;i++){_bolts[i].enabled=_boltCores[i].enabled=false;_boltAge[i]=-1;}
            for(int i=0;i<7;i++){_bandAge[i]=1;_strikeLights[i].intensity=0;_shockRings[i].enabled=_fractures[i].enabled=false;}
            foreach(var b in _fanBolts)b.enabled=false;foreach(var b in _fanCores)b.enabled=false;
            foreach(var r in _sectorRenderers)if(r)r.enabled=false;
            foreach(var p in _sparks)p.Stop();foreach(var p in _groundSparks)p.Stop();
        }
    }
    private void Update()=>Advance(Time.unscaledDeltaTime);
    public void Advance(float delta)
    {
        _age+=delta;
        float fade=_finished>=0?1-Mathf.Clamp01((_age-_finished)/.45f):1;
        for(int i=0;i<7;i++)
        {
            float deploy=Mathf.SmoothStep(0,1,Mathf.Clamp01((_age-i*.035f)/.24f));
            _relays[i].position=Vector3.Lerp(_origin+Vector3.up*.8f,_hover[i],deploy)+Vector3.up*Mathf.Sin(_age*5+i)*.035f;
            _relays[i].localScale=Vector3.one*(Mathf.Max(.001f,deploy*fade*.9f));
            var port=_relays[i].Find("PortWing");var starboard=_relays[i].Find("StarboardWing");
            float unfold=(1-deploy)*72;float recoil=_bandAge[i]>=0?Mathf.Sin(Mathf.Clamp01(_bandAge[i]/.25f)*Mathf.PI)*24:0;
            if(port)port.localRotation=Quaternion.Euler(0,0,unfold+recoil);
            if(starboard)starboard.localRotation=Quaternion.Euler(0,180,unfold+recoil);
            float charge=Mathf.Clamp01(_age/Mathf.Max(.1f,_warning+i*_interval));
            var ring=_ringPoints[i];
            for(int j=0;j<ring.Length;j++){float a=j*Mathf.PI*2/(ring.Length-1)+_age*5;ring[j]=_relays[i].position+new Vector3(Mathf.Cos(a)*(.18f+charge*.3f),-.12f,Mathf.Sin(a)*(.18f+charge*.3f));}
            _chargeRings[i].SetPositions(ring);_chargeRings[i].enabled=_finished<0 && _bandAge[i]<0;
            _chargeRings[i].widthMultiplier=.018f+charge*.045f;
            if(_bandAge[i]>=0){
                _bandAge[i]+=delta;float t=_bandAge[i];float burst=Mathf.Clamp01(1-t/.45f);
                _bands[i].widthMultiplier=.025f+burst*.22f;_bands[i].startColor=_bands[i].endColor=new Color(.15f,.7f,1,burst);
                _relays[i].position+=Vector3.up*Mathf.Sin(Mathf.Clamp01(t/.24f)*Mathf.PI)*.28f;
                _shockRings[i].enabled=_fractures[i].enabled=t<.5f;
                for(int j=0;j<ring.Length;j++)ring[j]=Vector3.Lerp(_ground[i],_shockOuter[i][j],(.22f+Mathf.Clamp01(t/.4f)*1.2f)/1.42f);
                _shockRings[i].SetPositions(ring);_shockRings[i].widthMultiplier=.055f*burst;
                _fractures[i].widthMultiplier=.035f*burst;
                _strikeLights[i].intensity=Mathf.Max(0,1-t/.16f)*7;
                _sectorRenderers[i].enabled=t<.45f;
                _sectorProperty.SetColor("_Tint",new Color(.04f,.55f,1,.7f*burst));_sectorProperty.SetFloat("_Phase",Mathf.Clamp01(t/.3f));_sectorRenderers[i].SetPropertyBlock(_sectorProperty);
                for(int side=0;side<6;side++){
                    int k=i*6+side;_fanBolts[k].enabled=_fanCores[k].enabled=t<.32f;
                    _fanBolts[k].widthMultiplier=.23f*Mathf.Clamp01(1-t/.32f);_fanCores[k].widthMultiplier=.065f*Mathf.Clamp01(1-t/.32f);
                }
            }
        }
        for(int i=0;i<_bolts.Length;i++)if(_boltAge[i]>=0){
            _boltAge[i]+=delta;float t=_boltAge[i];float energy=Mathf.Clamp01(1-t/.32f);
            energy*=.8f+.2f*Mathf.Sin(t*130+i);_bolts[i].enabled=_boltCores[i].enabled=t<.32f;
            _bolts[i].widthMultiplier=(i<7?.35f:.2f)*energy;_boltCores[i].widthMultiplier=(i<7?.09f:.055f)*energy;
        }
        for(int i=0;i<_impactRings.Length;i++)if(_ringAge[i]>=0){_ringAge[i]+=delta;_impactRings[i].enabled=_ringAge[i]<.24f;}
        if(_finished<0 && _age>_warning+7*_interval+1)Finish(true);
        if(Application.isPlaying && _finished>=0 && _age-_finished>.65f)Destroy(gameObject);
    }
    private void OnDestroy()
    {
        foreach(var mesh in _sectorMeshes)if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
    }
}
