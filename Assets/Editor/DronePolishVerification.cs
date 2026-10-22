#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

public static class DronePolishVerification
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string Output=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"../Downloads/Vector_Drone_20261021");
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("DRONE_PASS "+message);}
    static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Flags).GetValue(o);
    public static void Run()
    {
        Check(Application.isBatchMode,"batch only");Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var self=new GameObject("DroneSafetyFixture");self.AddComponent<SphereCollider>().radius=.48f;
        Vector3 p=Vector3.zero;
        for(int i=0;i<3000;i++)p+=DroneFlightSafety.Constrain(p,Vector3.up*.16f,DroneFlightSafety.MaxClimb,.48f,self.transform);
        Check(Mathf.Abs(p.y-DroneFlightSafety.MaxClimb)<.001f,"open sky stays at launch + 1.25m after sustained ascent");
        var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(0,2,0);ceiling.transform.localScale=new Vector3(6,.05f,6);Physics.SyncTransforms();
        var delta=DroneFlightSafety.Constrain(Vector3.zero,Vector3.up*4,8,.48f,self.transform);
        Check(delta.y>1 && delta.y<1.5f,"thin physical ceiling stops flight before camera/body clips");
        Check(DroneFlightSafety.Constrain(new Vector3(0,1,0),Vector3.down,8,.48f,self.transform).y<-.9f,"descent remains available below ceiling");
        Object.DestroyImmediate(ceiling);Object.DestroyImmediate(self);
        foreach(string map in new[]{"TestMap","Desert"}) {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{map}.prefab"));root.transform.position=Vector3.zero;Physics.SyncTransforms();
            int count=0;
            foreach(var c in root.GetComponentsInChildren<Collider>()) {
                if(c.isTrigger || !c.enabled || c.bounds.size.x<1.2f || c.bounds.size.z<1.2f)continue;
                var origin=new Vector3(c.bounds.center.x,c.bounds.min.y-2,c.bounds.center.z);
                if(Physics.Raycast(origin,Vector3.up,out var hit,3,~0,QueryTriggerInteraction.Ignore) && hit.normal.y<-.5f) {
                    var constrained=DroneFlightSafety.Constrain(origin,Vector3.up*3,origin.y+8,.48f,null);
                    Check(constrained.y<=Mathf.Max(0,hit.distance-.47f),map+" actual underside clearance "+c.name);if(++count>=12)break;
                }
            }
            Check(count>0,map+" physical geometry fixtures");Object.DestroyImmediate(root);
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Drone.prefab");
        var gun=prefab.GetComponent<TaserGun>();
        Check(Field<AudioClip>(gun,"_fireSound") && Field<AudioSource>(gun,"_audioSource") && Field<ParticleSystem>(gun,"_muzzleSpark") && Field<ParticleSystem>(gun,"_impactSpark"),"authored sound and muzzle/impact references");
        var actor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Initiator.prefab");
        Check(actor.GetComponent<DroneLaunchRig>() && Field<DroneLaunchRig>(actor.GetComponent<InitiatorSkill_Q>(),"_launchRig"),"authored forearm deployment rig");
        foreach(var visual in new[]{prefab.transform.Find("drone"),actor.GetComponentsInChildren<Transform>(true).First(t=>t.name=="DroneLaunchDevice")})
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
                Check(renderer.sharedMaterials.All(m=>m && m.shader && m.shader.name!="Standard"),"drone materials have no legacy or missing slots: "+renderer.name);
        var actorInstance=(GameObject)PrefabUtility.InstantiatePrefab(actor);
        var weaponModel=actorInstance.transform.Find("CameraRoot/Controller Model_Riging (1)");
        var neutralBones=weaponModel.GetComponentsInChildren<Transform>(true);
        var neutralUpper=neutralBones.First(t=>t.name=="mixamorig:LeftArm");
        var neutralFore=neutralBones.First(t=>t.name=="mixamorig:LeftForeArm");
        var neutralHand=neutralBones.First(t=>t.name=="mixamorig:LeftHand");
        var shoulderPosition=neutralUpper.position+actorInstance.GetComponentInChildren<Camera>(true).transform.TransformVector(new Vector3(-.34f,-.10f,.13f));
        float upperLength=Vector3.Distance(neutralUpper.position,neutralFore.position),foreLength=Vector3.Distance(neutralFore.position,neutralHand.position);
        AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/AR.anim").SampleAnimation(weaponModel.gameObject,0);
        var rig=actorInstance.GetComponent<DroneLaunchRig>();Call(rig,"Awake");
        var view=Field<Transform>(rig,"_viewModel");var restPosition=view.localPosition;
        var bones=Field<Transform[]>(rig,"_posedBones");var rotations=bones.Select(b=>b.localRotation).ToArray();
        var upper=Field<Transform>(rig,"_upperArm");var fore=Field<Transform>(rig,"_forearm");var hand=Field<Transform>(rig,"_hand");
        for(int cycle=0;cycle<3;cycle++) {
            rig.SetPrepared(true);Call(rig,"ApplyPose",1f);
            Check(Vector3.Distance(view.localPosition,restPosition)<.4f && Vector3.Distance(upper.position,shoulderPosition)<.0001f,"authored left-side framing translates the whole rig without stretching shoulder");
            Check(Mathf.Abs(Vector3.Distance(upper.position,fore.position)-upperLength)<.0001f && Mathf.Abs(Vector3.Distance(fore.position,hand.position)-foreLength)<.0001f,"preparation preserves both arm segment lengths");
            var fingerRotations=Field<Transform[]>(rig,"_handBones").Select(b=>b.localRotation).ToArray();
            for(int i=0;i<100;i++)Call(rig,"ApplyPose",1f);
            Check(Field<Transform[]>(rig,"_handBones").Select((b,i)=>Quaternion.Angle(b.localRotation,fingerRotations[i])<.02f).All(v=>v),"held pose does not accumulate finger rotation");
            rig.SetPrepared(false);Call(rig,"OnDisable");
            Check(Vector3.Distance(view.localPosition,restPosition)<.0001f && bones.Select((b,i)=>Quaternion.Angle(b.localRotation,rotations[i])).All(a=>a<.02f),"cancel/disable restores original weapon rig");
        }
        var device=Field<GameObject>(rig,"_device");
        rig.SetPrepared(true);
        Check(rig.WeaponVisible && !device.activeSelf && !rig.ReadyToLaunch,"holster starts with gun visible and drone hidden");
        Call(rig,"Advance",.12f);
        Check(view.localPosition.y<restPosition.y && rig.WeaponVisible,"gun visibly lowers before hiding");
        for(int i=0;i<90;i++)Call(rig,"Advance",1f/120);
        Check(rig.ReadyToLaunch && !rig.WeaponVisible && device.activeSelf,"arm preparation completes before deploy unlock");
        var poseCamera=actorInstance.GetComponentInChildren<Camera>(true);poseCamera.fieldOfView=90;Call(rig,"Advance",1f/120);
        Check(Vector3.Distance(upper.position,shoulderPosition)<.0001f,"ready pose preserves authored shoulder framing");
        var deviceViewport=poseCamera.WorldToViewportPoint(device.GetComponent<Renderer>().bounds.center);
        Check(deviceViewport.x>0 && deviceViewport.x<.5f && deviceViewport.y>.1f && deviceViewport.y<.5f,"prepared drone stays visible below-left without covering reticle");
        poseCamera.fieldOfView=60;Call(rig,"Advance",1f/120);
        var gameplayViewport=poseCamera.WorldToViewportPoint(device.GetComponent<Renderer>().bounds.center);
        Check(gameplayViewport.x>0 && gameplayViewport.x<.5f && gameplayViewport.y>.05f && gameplayViewport.y<.55f,"actual 60 degree gameplay FOV keeps drone visible below-left: "+gameplayViewport);
        var elbowViewport=poseCamera.WorldToViewportPoint(fore.position);
        var wristViewport=poseCamera.WorldToViewportPoint(hand.position);
        Check(elbowViewport.x<wristViewport.x-.15f && elbowViewport.y<wristViewport.y,
            "reference composition: forearm enters from lower-left, not bottom-center: elbow="+elbowViewport+" wrist="+wristViewport);
        Check(wristViewport.x>.35f && wristViewport.x<.6f && wristViewport.y>.1f && wristViewport.y<.35f,
            "reference composition: wrist remains in lower central band without hiding aim");
        Check(Vector3.Distance(upper.position,shoulderPosition)<.0001f,"changing FOV preserves authored shoulder framing");
        poseCamera.fieldOfView=90;Call(rig,"Advance",1f/120);
        var cameraRoot=poseCamera.transform.parent;
        var cameraPosition=cameraRoot.localPosition;var cameraRotation=cameraRoot.localRotation;
        cameraRoot.localPosition+=Vector3.up*.2f;cameraRoot.localRotation*=Quaternion.Euler(15,0,0);
        Call(rig,"Advance",1f/120);
        var correctedViewport=poseCamera.WorldToViewportPoint(device.GetComponent<Renderer>().bounds.center);
        Check(correctedViewport.z>.05f && correctedViewport.x>0 && correctedViewport.x<.6f && correctedViewport.y>0 && correctedViewport.y<.6f,"camera offset and pitch keep drone visible within natural arm reach");
        Check(Field<Transform[]>(rig,"_handBones").Length>5,"authored open wrist and finger pose replaces firearm grip");
        cameraRoot.localPosition=cameraPosition;cameraRoot.localRotation=cameraRotation;Call(rig,"Advance",1f/120);
        var releaseOrigin=device.transform.position;rig.BeginLaunch();Call(rig,"Advance",.14f);
        Check(Vector3.Distance(device.transform.position,releaseOrigin)>.1f,"device travels off forearm during release");
        for(int i=0;i<45;i++)Call(rig,"Advance",1f/120);
        Check(!device.activeSelf && !rig.WeaponVisible,"deployed rig hides device and weapon");
        rig.SetPrepared(false);
        for(int i=0;i<90;i++)Call(rig,"Advance",1f/120);
        Check(!rig.IsReturning && rig.WeaponVisible && Vector3.Distance(view.localPosition,restPosition)<.0001f,"lower and redraw restores rig without pose drift");
        Check(Vector3.Distance(device.transform.localScale,Field<Vector3>(rig,"_deviceBaseScale"))<.0001f,"held-device presentation scale restores after return");
        rig.SetDeployed(); rig.SetPrepared(false); rig.DrawAllowed=false;
        for(int i=0;i<180;i++)Call(rig,"Advance",1f/120);
        Check(rig.ReadyForWeaponDraw && rig.IsReturning && !rig.WeaponVisible,"server exit delay holds gun below frame before redraw");
        rig.DrawAllowed=true;
        for(int i=0;i<90;i++)Call(rig,"Advance",1f/120);
        Check(!rig.IsReturning && Vector3.Distance(view.localPosition,restPosition)<.0001f,"reconnected drone return captures and restores original rig");
        foreach(string character in new[]{"Initiator","Controller","Entry","Sentinel"}) {
            var target=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{character}/{character}.prefab");
            Check(target.GetComponent<ElectricShock>() && Field<ParticleSystem>(target.GetComponent<ElectricShock>(),"_shockEffect"),character+" has authored server shock receiver and particles");
        }
        Object.DestroyImmediate(actorInstance);
        var inputFixture=new GameObject("DroneInputFixture");
        Check(GameplayActionPolicy.CanAct(inputFixture.transform,GameplayAction.Fire),"input baseline is unlocked");
        var returning=typeof(InitiatorSkill_Q).GetField("<IsLocalReturning>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static);
        try {
            returning.SetValue(null,true);
            foreach(var action in new[]{GameplayAction.Fire,GameplayAction.Reload,GameplayAction.WeaponChange,GameplayAction.Skill,GameplayAction.Interact})
                Check(!GameplayActionPolicy.CanAct(inputFixture.transform,action),"return transition blocks "+action);
        }finally{returning.SetValue(null,false);Object.DestroyImmediate(inputFixture);}
        var menu=new GameObject("MenuFixture").AddComponent<SettingsMenu>();Call(menu,"Awake");
        var panel=new GameObject("Panel");typeof(SettingsMenu).GetField("settingsPanel",Flags).SetValue(menu,panel);
        panel.SetActive(true);Check(DroneMovement.InputBlocked,"settings block drone look/move/fire");panel.SetActive(false);
        Object.DestroyImmediate(menu.gameObject);Object.DestroyImmediate(panel);
        EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var hud=Object.FindFirstObjectByType<DroneHUDUI>(FindObjectsInactive.Include);
        Check(hud.gameObject.name=="DroneHUDController" && hud.gameObject.activeInHierarchy,"HUD controller survives hidden root");
        Call(hud,"Awake");hud.SetVisible(true);hud.SetTaserReady(false);hud.PlayTaserFireFeedback();
        Check(Field<DroneReticleGraphic>(hud,"_reticle").GetComponent<CanvasRenderer>(),"aim ring has an authored canvas renderer");
        Check(!Field<Image>(hud,"_hpFill").sprite && Field<Image>(hud,"_hpFill").type==Image.Type.Simple,
            "integrity fill is a straight quad without rounded sprite borders");
        Check(Field<float>(hud,"_hitUntil")==0,"local fire does not show confirmed hit");
        hud.PlayTaserHitConfirm();Check(Field<float>(hud,"_hitUntil")>0,"server-confirmed hit has distinct feedback");
        hud.SetDroneHp(100,100);hud.SetDroneHp(80,100);Check(Field<float>(hud,"_damagePulse")>0,"damage feedback only on HP decrease");
        Check(Mathf.Abs(Field<Image>(hud,"_hpFill").rectTransform.localScale.x-.8f)<.001f,"integrity quad scales to authoritative health");
        hud.ShowPreparation(false);Check(!Field<GameObject>(hud,"_flightInstruments").activeSelf,"preparation does not show flight instrumentation");
        hud.SetSpectatorDroneMode(true);Check(Field<TMP_Text>(hud,"_controlGuideText").text.Contains("관전"),"spectator does not see pilot commands");
        hud.SetVisible(false);Check(!Field<GameObject>(hud,"_root").activeSelf,"exit clears drone overlay");
        InteractionLifecycleVerification.RunRegression();
        Debug.Log("DRONE_CLIENT_PASS");
    }
    public static void Capture()
    {
        Directory.CreateDirectory(Output);
        foreach(string map in new[]{"TestMap","Desert"}) {
            EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
            var hud=Object.FindFirstObjectByType<DroneHUDUI>(FindObjectsInactive.Include);
            var root=Field<GameObject>(hud,"_root");
            foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())go.SetActive(false);
            var canvas=new GameObject("ReviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=1;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            root.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var mapRoot=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{map}.prefab"));mapRoot.transform.position=Vector3.zero;
            var info=mapRoot.GetComponent<MapInfo>();if(info.barriers)info.barriers.SetActive(false);
            var camera=new GameObject("ReviewCamera").AddComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.03f;camera.farClipPlane=400;
            var start=info.GetSpawnPoint(0,0);camera.transform.position=start.position+Vector3.up*2;
            Vector3 target=info.GetSpawnPoint(1,0).position+Vector3.up*1.8f;camera.transform.LookAt(target);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.42f,.55f,.65f);canvas.worldCamera=camera;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.7f,.72f,.76f);
            var light=new GameObject("ReviewSun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-35,0);
            Call(hud,"Awake");hud.SetVisible(true);hud.SetTelemetry(2.4f,3.6f,false);
            Save(camera,map+"-DroneHUD.png");
            if(map=="TestMap") {hud.SetDroneHp(57,100);hud.SetTaserReady(false);hud.PlayTaserHitConfirm();Call(hud,"Update");Save(camera,"Drone-HitFeedback.png");}
        }
        Debug.Log("DRONE_CAPTURE_PASS "+Output);
    }
    static void Save(Camera camera,string name)
    {
        var rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(Output,name),texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);
    }
}
#endif
