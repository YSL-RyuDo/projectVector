using UnityEngine;

// Depth-tested, short-range presentation. No outlines or through-wall tracking.
public sealed class ElectricStatusVfx : MonoBehaviour
{
    [SerializeField] private ElectricShock _status;
    [SerializeField] private LineRenderer[] _arcs = new LineRenderer[0];
    [SerializeField] private ParticleSystem _sparks;
    private bool _wasStunned;
    private float _age;
    private readonly Vector3[] _points=new Vector3[32];
    private void Update()=>Tick(Time.deltaTime);
    public void Tick(float dt)
    {
        bool active=_status && _status.IsStunned && GetComponent<PlayerHP>()?.IsDead!=true;
        if(active && !_wasStunned){_age=0;if(_sparks)_sparks.Play();}
        _wasStunned=active;_age+=dt;
        for(int n=0;n<_arcs.Length;n++){
            var arc=_arcs[n];if(!arc)continue;arc.enabled=active;
            if(!active)continue;
            for(int i=0;i<32;i++){
                // Restrained persistent status detail rather than a full-body strobe.
                float f=i/31f,a=f*Mathf.PI*2+_age*(n==0?3:-2)+n*Mathf.PI;
                float r=.28f+.012f*Mathf.Sin(i*3.6f+_age*12);
                _points[i]=new Vector3(Mathf.Cos(a)*r,.5f+f*1.05f,Mathf.Sin(a)*r);
            }
            arc.positionCount=32;arc.SetPositions(_points);arc.widthMultiplier=.009f;
            float entry=Mathf.Clamp01(1-_age/.18f);
            arc.startColor=arc.endColor=new Color(.72f,.65f,1,.34f+entry*.4f);
        }
    }
    private void OnDisable(){_wasStunned=false;foreach(var a in _arcs)if(a)a.enabled=false;if(_sparks)_sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
}
