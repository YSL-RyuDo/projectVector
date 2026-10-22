using UnityEngine;

// Meshes, emitters and materials are authored in Scanner.prefab.
public sealed class ScannerPresentation : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private Transform[] _petals;
    [SerializeField] private LineRenderer _pulse;
    [SerializeField] private LineRenderer[] _beams, _impacts;
    [SerializeField] private ParticleSystem[] _sparks;
    [SerializeField] private ParticleSystem _breakSparks;
    [SerializeField] private AudioSource _audio;
    [SerializeField] private AudioClip _deploySound, _zapSound, _endSound;
    private float _age, _ending=-1;
    private readonly float[] _zaps={-1,-1,-1};
    private readonly Vector3[] _targets=new Vector3[3];
    private int _next;
    private bool _anchored;
    private readonly Vector3[] _circle=new Vector3[65];
    public void Anchor(){if(_anchored)return;_anchored=true;_age=0;Play(_deploySound,.5f);}
    public void Zap(Vector3 target,bool playSound=true){int i=_next++%3;_targets[i]=target;_zaps[i]=0;_sparks[i].transform.position=target;_sparks[i].Play();if(playSound)Play(_zapSound,.65f);}
    public void End(bool expired)
    {
        _ending=0;_anchored=false;_pulse.enabled=false;
        for(int i=0;i<3;i++){_beams[i].enabled=_impacts[i].enabled=false;_zaps[i]=-1;}
        if(!expired){_breakSparks.Play();Play(_endSound,.55f);}else Play(_deploySound,.2f);
    }
    private void Play(AudioClip clip,float volume){if(_audio&&clip)_audio.PlayOneShot(clip,volume);}
    private void Update()=>Advance(Time.deltaTime);
    public void Advance(float dt)
    {
        _age+=dt;
        if(_ending>=0){_ending+=dt;_visual.localScale=Vector3.one*Mathf.Max(0,1-_ending/.22f);return;}
        float open=_anchored?Mathf.SmoothStep(0,1,_age/.3f):0;
        for(int i=0;i<_petals.Length;i++)_petals[i].localRotation=Quaternion.Euler(0,i*120,0)*Quaternion.Euler(-65*(1-open),0,0);
        // The visual stays centered on its authoritative shootable collider.
        _visual.localPosition=Vector3.zero;
        if(_anchored){
            float cycle=Mathf.Repeat(_age,1.15f)/1.15f;
            // Sparse spherical-equator pulse: exact server radius, no opaque filled dome.
            Ring(_pulse,transform.position,Mathf.Lerp(.25f,6.5f,cycle),.014f,new Color(.18f,.8f,1,(1-cycle)*.24f));
        }
        for(int i=0;i<3;i++)if(_zaps[i]>=0){
            _zaps[i]+=dt;float fade=Mathf.Clamp01(1-_zaps[i]/.24f);
            var beam=_beams[i];beam.enabled=fade>0;beam.positionCount=2;beam.SetPosition(0,_visual.position);beam.SetPosition(1,_targets[i]);
            beam.widthMultiplier=.025f*fade;beam.startColor=beam.endColor=new Color(.65f,.95f,1,fade);
            Ring(_impacts[i],_targets[i],Mathf.Lerp(.04f,.28f,1-fade),.025f*fade,new Color(.45f,.9f,1,fade));
            if(fade<=0){_zaps[i]=-1;_impacts[i].enabled=false;}
        }
    }
    private void Ring(LineRenderer line,Vector3 center,float radius,float width,Color color)
    {
        for(int i=0;i<_circle.Length;i++){float a=i*2*Mathf.PI/64;_circle[i]=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
        line.positionCount=_circle.Length;line.SetPositions(_circle);line.widthMultiplier=width;line.startColor=line.endColor=color;line.enabled=color.a>.001f;
    }
}
