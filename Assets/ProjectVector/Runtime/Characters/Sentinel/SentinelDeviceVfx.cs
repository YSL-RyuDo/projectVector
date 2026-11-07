using UnityEngine;

// Temporary world presentation only. Every renderer/audio source is authored in the device prefab.
public sealed class SentinelDeviceVfx : MonoBehaviour
{
    [SerializeField] private Transform emitter;
    [SerializeField] private Renderer[] lights;
    [SerializeField] private GameObject protection;
    [SerializeField] private AudioSource detectionAudio, pulseAudio;
    private float alertUntil,pulseUntil;
    private uint owner;
    private Transform target;
    private MaterialPropertyBlock properties;
    public void Initialize(uint player){owner=player;}
    public void Track(Transform tracked,float seconds){target=tracked;alertUntil=Time.time+seconds;if(seconds>0&&detectionAudio)detectionAudio.Play();}
    public void Pulse(){pulseUntil=Time.time+.6f;target=null;if(pulseAudio)pulseAudio.Play();}
    private void Update()
    {
        bool guarded=SentinelCNetworkManager.Instance && SentinelCNetworkManager.Instance.IsSkillProtected(owner);
        if(protection)protection.SetActive(guarded);
        if(target && emitter && Time.time<alertUntil){var d=Vector3.ProjectOnPlane(target.position-emitter.position,transform.up);
            if(d.sqrMagnitude>.001f)emitter.rotation=Quaternion.Slerp(emitter.rotation,Quaternion.LookRotation(d,transform.up),Time.deltaTime*12);}
        var color=guarded?new Color(1,.8f,.25f):Time.time<alertUntil?new Color(1,.65f,.15f):new Color(.65f,.28f,1);
        float energy=Time.time<pulseUntil?3:Time.time<alertUntil?1.2f+.8f*Mathf.Sin(Time.time*24):.7f+.15f*Mathf.Sin(Time.time*3);
        properties??=new MaterialPropertyBlock();properties.SetColor("_BaseColor",color);properties.SetColor("_EmissionColor",color*energy);
        foreach(var light in lights)if(light)light.SetPropertyBlock(properties);
    }
}
