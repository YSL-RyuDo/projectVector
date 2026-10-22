#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

// Existing saved VFX only: no poses, UI, gameplay or server settings.
public static class SkillVfxClarityAuthoring
{
    static void Burst(Transform root,string path,int count,float size,float life,Color color)
    {
        var t=root.Find(path);if(!t)throw new Exception("Missing authored VFX: "+path);
        var p=t.GetComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=p.main;m.startSize=new ParticleSystem.MinMaxCurve(size*.55f,size);m.startLifetime=new ParticleSystem.MinMaxCurve(life*.55f,life);m.startColor=color;m.maxParticles=48;
        var emission=p.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
        var colors=p.colorOverLifetime;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(color,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});
        colors.enabled=true;colors.color=gradient;
    }
    static void Line(Transform root,string path,Material mat){var t=root.Find(path);if(!t)throw new Exception("Missing line "+path);t.GetComponent<LineRenderer>().sharedMaterial=mat;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch only");
        const string path="Assets/Materials/Drone/ReadableEnergy.mat";
        var shader=Shader.Find("Vector/SkillReadableEnergy");if(!shader)throw new Exception("Missing readable energy shader");
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}else{material.shader=shader;EditorUtility.SetDirty(material);}
        string dronePath="Assets/Prefab/Character/Initiator/Drone.prefab";
        var drone=PrefabUtility.LoadPrefabContents(dronePath);
        try{
            Line(drone.transform,"CombatFX/DischargeCore",material);Line(drone.transform,"CombatFX/ConfirmedImpactRing",material);
            var cyan=new Color(.25f,.85f,1);
            Burst(drone.transform,"CombatFX/MuzzleDischarge",7,.045f,.11f,cyan);
            Burst(drone.transform,"CombatFX/MuzzleDischarge/Flash",1,.20f,.055f,cyan);
            Burst(drone.transform,"CombatFX/ConfirmedSparks",16,.04f,.28f,cyan);
            Burst(drone.transform,"CombatFX/ConfirmedFlash",1,.45f,.11f,new Color(.85f,1,1));
            Burst(drone.transform,"BreakupFX/ElectricalFlash",1,.55f,.10f,cyan);
            Burst(drone.transform,"BreakupFX/HotFragments",16,.035f,.50f,new Color(1,.53f,.18f));
            Burst(drone.transform,"BreakupFX/ResidualCharge",8,.04f,.25f,cyan);
            var data=new SerializedObject(drone.GetComponent<DroneCombatVfx>());data.FindProperty("_shotDuration").floatValue=.12f;data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(drone,dronePath);
        }finally{PrefabUtility.UnloadPrefabContents(drone);}
        foreach(string name in new[]{"Initiator","Controller","Entry","Sentinel"}){
            string actorPath=$"Assets/Prefab/Character/{name}/{name}.prefab";var actor=PrefabUtility.LoadPrefabContents(actorPath);
            try{
                Burst(actor.transform,"ElectricStatusFX/StunDischarge",14,.045f,.28f,new Color(.78f,.7f,1));
                if(name=="Initiator"){
                    foreach(var line in actor.GetComponentsInChildren<LineRenderer>(true))
                        if(line.name=="EMPHandCharge"){line.enabled=false;line.positionCount=0;line.widthMultiplier=.004f;}
                    Line(actor.transform,"EMP/Wave_0",material);
                    for(int i=0;i<8;i++)Line(actor.transform,"EMP/ConfirmedArc_"+i,material);
                    Burst(actor.transform,"EMP/CastSparks",14,.03f,.22f,new Color(.65f,.56f,1));
                    var data=new SerializedObject(actor.GetComponent<EmpCastVfx>());data.FindProperty("_duration").floatValue=.4f;data.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(actor,actorPath);
            }finally{PrefabUtility.UnloadPrefabContents(actor);}
        }
        AssetDatabase.SaveAssets();Debug.Log("VFX_CLARITY_AUTHOR_PASS");
    }
    public static void ApplyAndCapture(){Run();SkillVfxClarityVerification.Run();SkillVfxClarityReview.After();}
}
#endif
