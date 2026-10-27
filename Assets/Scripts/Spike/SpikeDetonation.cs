using UnityEngine;

public sealed class SpikeDetonation : MonoBehaviour
{
    public Transform shockRing, innerRing, core;
    public Renderer[] fading;
    public ParticleSystem sparks;
    public AudioSource source;
    public AudioClip burst;
    public ParticleSystem[] layers;
    public Light flash;
    float started;
    MaterialPropertyBlock block;
    public void Begin()
    {
        started = Time.unscaledTime;
        if(layers!=null)foreach (var p in layers) { if (p) p.Play(); }
        else if(sparks)sparks.Play();
        if (source && burst) source.PlayOneShot(burst);
        Destroy(gameObject, 6);
    }
    void Update() => Render(Time.unscaledTime - started);
    public void Render(float age)
    {
        float t = Mathf.Clamp01(age / 1.7f);
        if (flash) flash.intensity = Mathf.Exp(-age * 17) * 18;
        if (shockRing) shockRing.localScale = Vector3.one * Mathf.Lerp(.2f, 8, 1 - Mathf.Pow(1 - t, 3));
        if (innerRing) innerRing.localScale = Vector3.one * Mathf.Lerp(.1f, 4.8f, 1 - Mathf.Pow(1 - t, 2));
        if (core) core.localScale = Vector3.one * Mathf.Max(.001f, (1 - Mathf.Clamp01(age / .22f)) * (1 + age * 26));
        block ??= new MaterialPropertyBlock();
        block.SetColor("_BaseColor", new Color(.3f, .8f, 1f, Mathf.Pow(1 - t, 2) * .75f));
        foreach (var r in fading) if (r) r.SetPropertyBlock(block);
    }
    public void SampleParticles(float age)
    {
        if(layers!=null)foreach (var p in layers) { if (p) p.Simulate(Mathf.Max(0, age), false, true, true); }
        else if(sparks)sparks.Simulate(Mathf.Max(0,age),false,true,true);
    }
}
