using UnityEngine;

// Shared authored geometry. Presentation only, never an objective authority.
public sealed class SpikeDeviceVisual : MonoBehaviour
{
    public Transform[] petals;
    public Renderer[] lamps;
    public Transform core;
    public Renderer[] chargeSegments;
    public Renderer[] flowSegments;
    public Renderer statusLamp;
    public Transform[] stabilizers;
    public Transform dataRotor;
    public Vector3 coreClosedPosition = new Vector3(0,.19f,0);
    public Vector3 coreOpenPosition = new Vector3(0,.26f,0);
    MaterialPropertyBlock block;
    bool autoIdle=true;
    float enabledAt;
    Color signalColor;
    void OnEnable(){autoIdle=true;enabledAt=Time.unscaledTime;RenderDormant(0);}
    void Update(){if(autoIdle)RenderDormant(Time.unscaledTime-enabledAt);}
    void Light(Renderer lamp,Color color,float brightness)
    {
        if(!lamp)return;
        block??=new MaterialPropertyBlock();
        block.SetColor("_BaseColor",Color.Lerp(new Color(.025f,.045f,.06f),color,Mathf.Clamp01(brightness)));
        block.SetColor("_EmissionColor",color*brightness*1.6f);
        lamp.SetPropertyBlock(block);
    }
    public void Pose(float deployment, Color color, float pulse)
    {
        autoIdle=false;signalColor=color;
        for (int i = 0; i < (petals?.Length ?? 0); i++)
            if (petals[i]) petals[i].localRotation = Quaternion.Euler(0, i * 360f/petals.Length, -Mathf.Clamp01(deployment) * 14);
        if (core) core.localPosition = Vector3.Lerp(coreClosedPosition, coreOpenPosition, Mathf.Clamp01(deployment));
        for (int i = 0; i < (stabilizers?.Length ?? 0); i++)
            stabilizers[i].localRotation = Quaternion.Euler(0, 90 * i + Mathf.Lerp(28, 0, Mathf.Clamp01(deployment)), 0);
        if(dataRotor)dataRotor.gameObject.SetActive(false);
        foreach (var lamp in lamps)Light(lamp,color,Mathf.Lerp(.18f,.85f,pulse)*Mathf.Lerp(.1f,1,Mathf.Clamp01(deployment)));
        Light(statusLamp,color,.55f+pulse*.3f);
    }
    // Communication activity only: no countdown or exact progress exposed to players.
    public void Signal(float age, bool interrupted = false)
    {
        for (int i = 0; i < (chargeSegments?.Length ?? 0); i++)
        {
            chargeSegments[i].enabled=true;
            float step=Mathf.Repeat(age*(interrupted?-5:5)-i,chargeSegments.Length);
            Light(chargeSegments[i],signalColor,step<1.5f?.85f:.08f);
        }
        for(int i=0;i<(flowSegments?.Length??0);i++)
        {
            float step=Mathf.Repeat(age*(interrupted?-4:5)-i%6,6);
            Light(flowSegments[i],signalColor,step<1.4f?Mathf.Lerp(.9f,.2f,step/1.4f):.035f);
        }
    }
    public void RenderDormant(float age)
    {
        Pose(0,new Color(.32f,.62f,.75f),0);autoIdle=true;
        foreach(var lamp in lamps)Light(lamp,signalColor,.025f);
        Light(statusLamp,new Color(.7f,.88f,1),.15f+Mathf.Pow(Mathf.Sin(age*1.6f)*.5f+.5f,3)*.35f);
    }
    public void RenderHeld(float age)
    {
        RenderDormant(age);autoIdle=false;
        Light(statusLamp,SpikePresentation.Cyan,.55f);
        for(int i=0;i<(chargeSegments?.Length??0);i++)Light(chargeSegments[i],SpikePresentation.Cyan,Mathf.Repeat(age*5-i,7)<1.2f?.5f:.03f);
    }
    public void RenderSafe(float age)
    {
        float closing=Mathf.SmoothStep(0,1,age/.75f);
        Pose(1-closing,new Color(.22f,.9f,.58f),Mathf.Exp(-age*7));
        foreach(var lamp in lamps)Light(lamp,new Color(.22f,.9f,.58f),Mathf.Exp(-age*6)*.4f);
        Light(statusLamp,new Color(.22f,.9f,.58f),.22f);
    }
}
