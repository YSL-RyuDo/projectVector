using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Exercise runtime packet receivers and LateUpdate, not just authored pose sampling.
public static class SpikeRuntimeVerification
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static void Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run()
    {
        Check(Application.isBatchMode,"Batch only");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var round=new GameObject("RoundFixture").AddComponent<RoundManager>();Call(round,"Awake");
        var net=new GameObject("NetworkFixture").AddComponent<NetworkManager>();NetworkManager.Instance=net;
        Set(net,"myPlayerId",99u);
        var spawner=new GameObject("SpawnerFixture").AddComponent<SpikeSpawner>();Call(spawner,"Awake");
        spawner.spikePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Modules/Match/Resources/Prefabs/Spike/Spike.prefab");
        foreach(string role in new[]{"Initiator","Controller","Sentinel","Entry"})
        {
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ProjectVector/Shared/Characters/Prefabs/{role}/{role}.prefab"));
            var identity=actor.GetComponent<PlayerIdentity>();identity.PlayerId=1;
            var hp=actor.GetComponent<PlayerHP>();hp.SetHp(100);
            var ps=actor.GetComponent<PlayerSpike>();ps.hasSpike=false;
            var p=actor.GetComponent<SpikeInteractionPresentation>();Call(p,"Awake");
            foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
            foreach(byte action in new byte[]{1,2})
            {
                round.currentState=RoundManager.RoundState.MainRound;
                if(action==1)SpikeInteractionPresentation.ServerPlant(1,true);
                else SpikeInteractionPresentation.ServerDefuse(1,1,false);
                Set(p,"remoteStarted",Time.unscaledTime-2.8f);Call(p,"LateUpdate");
                Check(p.IsPresenting,"Remote action missing "+role);
                Check(!p.firstPerson.device.activeSelf&&!p.firstPerson.tool.activeSelf,"Remote FP equipment visible");
                // The actual completion handler formerly invoked a hard pose reset.
                if(action==1)Call(net,"HandleSpikePlanted",new byte[12]);
                else Call(net,"HandleSpikeDefused",BitConverter.GetBytes(1u));
                round.currentState=action==1?RoundManager.RoundState.SpikePlanted:RoundManager.RoundState.RoundEnd;
                Call(p,"Update");Call(p,"LateUpdate");
                Check(!p.IsPresenting&&p.IsRecovering,"Packet completion skipped stand recovery "+role+" "+action);
                Check(!p.thirdPerson.device.activeSelf&&!p.thirdPerson.tool.activeSelf,"Completion retained equipment");
                Set(p,"recoveryStarted",Time.unscaledTime-1);Call(p,"Update");Call(p,"LateUpdate");
                Check(!p.IsRecovering,"Recovery did not end");
                foreach(var spike in UnityEngine.Object.FindObjectsByType<Spike>(FindObjectsSortMode.None))
                    if(spike.isPlanted||spike==Spike.PlantedSpike)UnityEngine.Object.DestroyImmediate(spike.gameObject);
            }
            round.currentState=RoundManager.RoundState.MainRound;
            SpikeInteractionPresentation.ServerPlant(1,true);Call(p,"LateUpdate");hp.SetHp(0);Call(p,"Update");Call(p,"LateUpdate");
            Check(!p.IsPresenting&&!p.IsRecovering&&!p.thirdPerson.device.activeSelf,"Death kept device/return animation");
            hp.SetHp(100);round.currentState=RoundManager.RoundState.Preparation;Call(p,"LateUpdate");
            Check(!p.IsPresenting,"Respawn revived stale remote action");
            UnityEngine.Object.DestroyImmediate(actor);
        }
        var actorLocal=new GameObject("LocalContacts");actorLocal.AddComponent<PlayerIdentity>().PlayerId=99;
        var local=actorLocal.AddComponent<PlayerSpike>();
        local.hasSpike=true;local.plantTime=3;
        Set(local,"isPlanting",true);Set(local,"isInsidePlantZone",true);Set(local,"plantStartedAt",Time.unscaledTime);
        Call(local,"Planting");
        Check(local.PlantProgress==0,"Plant counted the frame before the action started");
        Set(local,"plantStartedAt",Time.unscaledTime-1.5f);Call(local,"Planting");
        Check(Mathf.Abs(local.PlantProgress-.5f)<.001f,"Plant progress not based on elapsed real time");
        Set(local,"isPlanting",false);
        var first=actorLocal.AddComponent<BoxCollider>();var child=new GameObject("BodyHitbox");child.transform.SetParent(actorLocal.transform);var second=child.AddComponent<BoxCollider>();
        var target=new GameObject("PlantedFixture").AddComponent<Spike>();target.isPlanted=true;
        Call(target,"OnTriggerEnter",first);Call(target,"OnTriggerEnter",second);Call(target,"OnTriggerExit",second);
        Check((bool)typeof(Spike).GetField("isPlayerInside",F).GetValue(target),"One child collider exit cancelled valid range");
        Call(target,"OnTriggerExit",first);
        Check(!(bool)typeof(Spike).GetField("isPlayerInside",F).GetValue(target),"Last collider exit retained range");
        Set(target,"isDefusing",true);Set(target,"currentDefuseTime",3.2f);target.SetHalfDefusedFromServer(true);
        Check(Mathf.Abs(target.DefuseProgress-.64f)<.001f,"Delayed checkpoint rewound progress");
        Set(target,"isDefusing",false);target.SetHalfDefusedFromServer(true);
        Check(Mathf.Abs(target.DefuseProgress-.5f)<.001f,"Cancelled checkpoint not restored");
        UnityEngine.Object.DestroyImmediate(target.gameObject);UnityEngine.Object.DestroyImmediate(actorLocal);
        UnityEngine.Object.DestroyImmediate(spawner.gameObject);UnityEngine.Object.DestroyImmediate(net.gameObject);UnityEngine.Object.DestroyImmediate(round.gameObject);
        SpikePresentationAuthoring.Verify();
        Debug.Log("DEVICE_RUNTIME_PASS: four runtime packet completions, FP isolation, round-end recovery, death/respawn, compound trigger exits, delayed half acknowledgement");
    }
}
