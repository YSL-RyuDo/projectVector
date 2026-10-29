using UnityEngine;

public sealed class ControllerSwampVfx : MonoBehaviour
{
    [SerializeField] private Renderer _surface;
    [SerializeField] private ParticleSystem _bloom;
    [SerializeField] private AudioSource _audio;
    private MaterialPropertyBlock _block;
    private float _age,_duration=5,_pulse;
    public void Begin(float duration){_age=0;_duration=duration;if(_bloom)_bloom.Play();if(_audio)_audio.Play();}
    public void Pulse(){_pulse=1;}
    private void Update()
    {
        _age+=Time.deltaTime;_pulse=Mathf.Max(0,_pulse-Time.deltaTime*2);
        if(!_surface)return;
        _block??=new MaterialPropertyBlock();
        _block.SetFloat("_Age",_age);_block.SetFloat("_Life",Mathf.Clamp01((_duration-_age)/.45f));_block.SetFloat("_Pulse",_pulse);
        _surface.SetPropertyBlock(_block);
    }
}
