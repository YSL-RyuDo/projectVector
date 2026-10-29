#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class InitiatorBalanceVerification
{
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Debug.Log("BALANCE_CHECK "+text);}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only, never replaces an open user scene");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var drone=new GameObject("DurabilityFixture").AddComponent<Drone>();
        Check(drone.CurrentHp==1 && drone.MaxHp==1,"drone defaults match one-hit server durability");
        drone.ApplyHp(0,1);Check(drone.CurrentHp==0 && drone.MaxHp==1,"terminal HP packet supported");
        Object.DestroyImmediate(drone.gameObject);
        foreach(string name in new[]{"TestMap","Desert"}){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ProjectVector/Shared/Maps/Prefabs/{name}.prefab"));
            var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);Physics.SyncTransforms();
            var fx=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Characters/Effects/Initiator/Discharge/DischargeSequence.prefab"))).GetComponent<DischargeVfx>();
            var spawn=info.attackSpawnPoints[0];fx.Begin(spawn.position,spawn.forward,18,2,.15f);
            fx.Advance(1.99f);
            Check(Field<float>(fx,"_finished")<0 && Field<LineRenderer>(fx,"_boundary").enabled,name+" warning remains visible for full 2 seconds");
            Check(Field<LineRenderer[]>(fx,"_bolts").All(b=>!b.enabled),name+" no locally invented early strike");
            fx.Advance(.01f);
            for(int band=0;band<7;band++){
                fx.Strike(band);fx.Advance(.01f);
                Check(Field<LineRenderer[]>(fx,"_bolts")[band].enabled,name+" authoritative band "+band+" displayed");
                fx.Advance(.14f);
                Check(Field<float>(fx,"_finished")<0,name+" no old 1.3 second cleanup timeout");
            }
            fx.Finish(false);fx.Advance(.7f);Check(Field<LineRenderer[]>(fx,"_bolts").All(b=>!b.enabled),name+" strike cleanup");
        }
        Debug.Log("INITIATOR_BALANCE_VISUAL_PASS: one-hit HP, both maps 2 second warning / .15 second waves / cleanup");
        EmpStatusPlayVerification.Run();
    }
}
#endif
