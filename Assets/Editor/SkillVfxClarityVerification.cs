#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SkillVfxClarityVerification
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Flags).GetValue(o);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("VFX_CLARITY_CHECK "+message);}
    public static void Run()
    {
        Check(Application.isBatchMode,"isolated batch");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        foreach(var name in new[]{"Initiator","Controller","Entry","Sentinel"}){
            var prefab=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{name}/{name}.prefab"));
            var view=prefab.GetComponent<PlayerWeaponViewController>();
            var fp=Field<GameObject>(view,"firstPersonWeaponHolder");var tp=Field<GameObject>(view,"thirdPersonWeaponHolder");
            Check(fp && tp && fp!=tp && tp.transform.IsChildOf(prefab.transform.Find("Model")),name+" has separate authored FP/TP holders");
            Check(prefab.GetComponentsInChildren<WeaponHolderModel>(true).Length==1,name+" has one third-person weapon mirror");
            int hierarchy=prefab.GetComponentsInChildren<Transform>(true).Length;
            typeof(PlayerWeaponViewController).GetMethod("Awake",Flags).Invoke(view,null);
            view.ShowFirstPerson();Check(fp.activeSelf&&!tp.activeSelf,name+" local FP only");
            view.HideAll();view.RestoreLastView();Check(fp.activeSelf&&!tp.activeSelf,name+" skill return preserves FP only");
            view.ShowThirdPerson();Check(!fp.activeSelf&&tp.activeSelf,name+" remote TP only");
            view.HideAll();view.RestoreLastView();Check(!fp.activeSelf&&tp.activeSelf,name+" remote restore preserves TP only");
            Check(prefab.GetComponentsInChildren<Transform>(true).Length==hierarchy,name+" view changes create no extra holder");
            Object.DestroyImmediate(prefab);
        }
        var drone=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Drone.prefab"));
        var fx=drone.GetComponent<DroneCombatVfx>();var core=Field<LineRenderer>(fx,"_core");var halo=Field<LineRenderer>(fx,"_halo");var impact=Field<LineRenderer>(fx,"_impactRing");
        int objects=drone.GetComponentsInChildren<Transform>(true).Length;
        foreach(var direction in new[]{Vector3.forward,Vector3.up,new Vector3(-3,1,7)}){
            var start=new Vector3(2,1,3);var end=start+direction;
            fx.Fire(start,end);
            Check(core.positionCount==2 && core.GetPosition(0)==start && core.GetPosition(1)==end,"exact straight primary shot path "+direction);
            Check(!impact.enabled,"shot alone cannot claim confirmed impact");
            Check(halo.widthMultiplier<.08f && halo.startColor.a<.3f,"secondary glow subordinate to shot path");
            fx.ConfirmImpact(end,-direction);Check(impact.enabled,"confirmation activates impact");
            fx.Tick(.21f);Check(!impact.enabled&&!core.enabled,"shot and impact expire without persistent obstruction");
        }
        Check(core.sharedMaterial.shader.name=="Vector/SkillReadableEnergy","non-additive readable primary material");
        Check(drone.GetComponentsInChildren<Transform>(true).Length==objects,"VFX updates reuse authored hierarchy");
        fx.Fire(Vector3.zero,Vector3.forward);fx.ConfirmImpact(Vector3.forward,Vector3.back);
        typeof(DroneCombatVfx).GetMethod("OnDisable",Flags).Invoke(fx,null);
        Check(!core.enabled&&!impact.enabled&&!Field<ParticleSystem>(fx,"_impact").IsAlive(true),"disable clears lines and confirmed particles");
        Object.DestroyImmediate(drone);
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Initiator.prefab"));
        var handLines=actor.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name=="EMPHandCharge").ToArray();
        Check(handLines.Length==2 && handLines.All(l=>!l.enabled && l.positionCount==0 && l.widthMultiplier<=.0041f),"idle hand charge has no default metre-wide geometry");
        var death=actor.GetComponent<PlayerDeathController>();
        typeof(PlayerDeathController).GetField("modelRenderers",Flags).SetValue(death,actor.transform.Find("Model").GetComponentsInChildren<Renderer>(true));
        death.SetModelVisible(true);
        Check(handLines.All(l=>!l.enabled),"respawn/model reveal does not activate hand VFX");
        var modelHand=handLines.First(l=>l.transform.IsChildOf(actor.transform.Find("Model")));
        modelHand.enabled=true;death.SetModelVisible(false);death.SetModelVisible(true);
        Check(modelHand.enabled && !modelHand.forceRenderingOff,"view hide/reveal preserves active effect state");
        modelHand.enabled=false;
        var match=new GameObject("VisibilityRegression").AddComponent<MatchManager>();
        typeof(MatchManager).GetMethod("SetCharacterModelVisible",Flags).Invoke(match,new object[]{actor,true});
        Check(handLines.All(l=>!l.enabled),"match model reveal does not activate hand VFX");
        foreach(var line in handLines)line.enabled=true;
        actor.GetComponent<EmpGestureRig>().Advance(0);
        Check(handLines.All(l=>!l.enabled),"idle gesture clears accidental hand VFX activation");
        Object.DestroyImmediate(match.gameObject);
        var cameraView=actor.GetComponent<PlayerCameraController>();
        typeof(PlayerCameraController).GetMethod("Awake",Flags).Invoke(cameraView,null);
        typeof(PlayerCameraController).GetField("isLocalView",Flags).SetValue(cameraView,true);
        var weaponView=actor.GetComponent<PlayerWeaponViewController>();
        var firstHolder=Field<GameObject>(weaponView,"firstPersonWeaponHolder");var thirdHolder=Field<GameObject>(weaponView,"thirdPersonWeaponHolder");
        int objectsBefore=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        var proxy=actor.GetComponent<LocalDroneBodyProxy>();
        proxy.SetVisible(true);
        Check(!firstHolder.activeSelf&&!thirdHolder.activeSelf,"drone camera hides both gun holders");
        Check(actor.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.enabled),"drone camera reuses actual player body");
        Check(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==objectsBefore,"drone body view creates no cloned character");
        proxy.SetVisible(false);
        Check(firstHolder.activeSelf&&!thirdHolder.activeSelf,"drone camera return restores FP holder only");
        Check(actor.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>(true).All(r=>!r.enabled),"drone return hides world body");
        var emp=actor.GetComponent<EmpCastVfx>();var rings=Field<LineRenderer[]>(emp,"_waveRings");var arcs=Field<LineRenderer[]>(emp,"_targetArcs");
        string server=Path.GetFullPath("../ProjectVectorServer/ProjectVectorServer/ProjectVectorServer/PacketDispatcher.cpp");
        var radius=Regex.Match(File.ReadAllText(server),@"constexpr float EMP_RADIUS = ([0-9.]+)f");
        Check(radius.Success && Mathf.Approximately(emp.Radius,float.Parse(radius.Groups[1].Value,CultureInfo.InvariantCulture)),"visual radius matches unchanged server radius");
        Vector3 origin=new Vector3(1,0,2);
        emp.Confirm(origin,Array.Empty<Vector3>());
        Check(rings[0].enabled&&arcs.All(a=>!a.enabled),"empty cast shows footprint but no claimed target");
        Check(Mathf.Abs(Vector3.Distance(new Vector3(rings[0].GetPosition(0).x,origin.y,rings[0].GetPosition(0).z),origin)-emp.Radius)<.001f,"full radius present on confirmation, no misleading travel time");
        var boundary=rings[0].GetPosition(0);emp.Tick(.12f);
        Check(Vector3.Distance(boundary,rings[0].GetPosition(0))<.001f && !rings[2].enabled,"boundary does not expand and redundant ring is hidden");
        var targets=new[]{new Vector3(3,1,4),new Vector3(-2,1,5)};emp.Confirm(origin,targets);
        Check(arcs.Count(a=>a.enabled)==2,"only supplied confirmed targets receive arcs");
        Check(arcs[0].GetPosition(63)==targets[0],"arc endpoint matches confirmed snapshot");
        targets[0]+=Vector3.right*20;emp.Tick(.01f);
        Check(arcs[0].GetPosition(63)!=targets[0],"does not track later target movement");
        emp.Tick(.5f);Check(arcs.All(a=>!a.enabled)&&rings.All(a=>!a.enabled),"pulse and target arcs expire");
        emp.Confirm(origin,targets);emp.ResetPresentation();Check(!emp.Playing&&arcs.All(a=>!a.enabled)&&rings.All(a=>!a.enabled),"cancellation/reset removes all pulse geometry");
        Check(rings[0].sharedMaterial.shader.name=="Vector/SkillReadableEnergy","range edge independent of additive bloom");
        Object.DestroyImmediate(actor);Debug.Log("VFX_CLARITY_PASS");
    }
}
#endif
