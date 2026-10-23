#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Actual saved practice player/input/physics on TestMap. Only server acceptance is substituted.
[InitializeOnLoad]
public static class ControllerSwampCapture
{
    const string Key="Vector.SwampCapture", Output="C:/Users/GCT/Documents/ControllerSwamp-20261023";
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    static PracticeRangeController range;
    static Camera camera;
    static ControllerSkill_Q skill;
    static ControllerSwampNetworkManager manager;
    static PlayerMovement movement;
    static Keyboard keyboard;
    static Mouse mouse;
    static RenderTexture rt;
    static Texture2D pixels;
    static System.Diagnostics.Process encoder;
    static FileStream sound;
    static StreamWriter csv;
    static int stage,frame,warmup,last=-1,checks,errors,thrown,activated,channels,rate;
    static bool done,passed,audio,observedSlow,observedRecovery,observedTargetFeedback,slopePassed;
    static Vector3 slopePoint;
    static float peak;
    static double deadline;
    static uint nextId=90000;
    static ControllerSwampCapture()
    {
        EditorApplication.playModeStateChanged+=State;
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception))errors++;};
        if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;}
    }
    static void Check(bool value,string label){if(!value)throw new Exception("SWAMP_FAIL "+label);checks++;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        ControllerSwampAuthoring.Run();Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/Scenes/WeaponTest.unity");
        Object.FindFirstObjectByType<PracticeRangeController>().persistRecords=false;
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));
        map.transform.position=new Vector3(1000,0,0);map.GetComponent<MapInfo>().SetBarriersActive(false);
        foreach(var b in map.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSwampAuthoring.Actor));actor.name="SwampTarget";
        var cc=actor.GetComponent<CharacterController>();float bottom=(cc.center.y-cc.height*.5f)*actor.transform.lossyScale.y;
        actor.transform.SetPositionAndRotation(new Vector3(968,0,10.8f)-Vector3.up*bottom,Quaternion.Euler(0,180,0));
        var model=actor.transform.Find("Model");model.gameObject.SetActive(true);
        foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(b is PlayerMovement)&&!(b is FPSCameraLook)&&!(b is PlayerIdentity))Object.DestroyImmediate(b);
        foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;
        foreach(var look in actor.GetComponentsInChildren<FPSCameraLook>(true))look.enabled=false;
        foreach(var r in actor.GetComponentsInChildren<Renderer>(true)){r.enabled=r.transform.IsChildOf(model);r.forceRenderingOff=false;}
        AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/InGame/Pistol/Controller_Pistol_Idle.anim").SampleAnimation(model.gameObject,0);
        foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
        foreach(var a in actor.GetComponentsInChildren<AudioListener>(true))a.enabled=false;
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;EditorApplication.EnterPlaymode();
    }
    static void Input()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic||keyboard==null)return;
        var keys=new KeyboardState();var m=new MouseState();
        if(stage==2&&!done){
            if(frame==45||frame==95)keys=new KeyboardState(UnityEngine.InputSystem.Key.Q);
            if(frame>=150&&frame<245)keys=new KeyboardState(UnityEngine.InputSystem.Key.W);
            if(frame>=270&&frame<300)keys=new KeyboardState(UnityEngine.InputSystem.Key.S);
        }
        keyboard.MakeCurrent();InputState.Change(keyboard,keys);mouse.MakeCurrent();InputState.Change(mouse,m);
    }
    static T Private<T>(object o,string field)=>(T)o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void TestOverlap()
    {
        var col=movement.GetComponent<CharacterController>();
        var a=new GameObject("OverlapA").AddComponent<SwampArea>();var b=new GameObject("OverlapB").AddComponent<SwampArea>();
        a.transform.position=b.transform.position=SwampSurfaceQuery.Foot(movement);
        a.InitFromServer(0,.2f);b.InitFromServer(0,.2f);
        var extra=new GameObject("ExtraCollider");extra.transform.SetParent(movement.transform,false);var second=extra.AddComponent<BoxCollider>();second.isTrigger=true;
        movement.SetExternalSpeedMultiplier(.85f);
        a.SendMessage("OnTriggerEnter",col);a.SendMessage("OnTriggerEnter",second);b.SendMessage("OnTriggerEnter",col);
        Check(Private<int>(movement,"_swampSlowCount")==2,"one slow per zone despite body colliders");
        a.SendMessage("OnTriggerExit",second);Check(Private<int>(movement,"_swampSlowCount")==2,"leaving one body collider retains zone");
        a.gameObject.SetActive(false);Check(Private<int>(movement,"_swampSlowCount")==1&&movement.IsSlowed,"other zone remains after disable");
        b.SendMessage("OnTriggerExit",col);Check(Private<int>(movement,"_swampSlowCount")==0,"last zone restores swamp multiplier");
        Check(Mathf.Approximately(Private<float>(movement,"_externalSpeedMultiplier"),.85f),"unrelated movement modifier preserved");
        movement.SetExternalSpeedMultiplier(1);Object.Destroy(a.gameObject);Object.Destroy(b.gameObject);Object.Destroy(extra);
        Debug.Log("SWAMP_OVERLAP_PASS");
    }
    static void Throw(Vector3 position,Vector3 velocity)
    {
        uint id=nextId++;thrown++;manager.OnSwampSpawn(id,0,position,velocity);
        var trap=Object.FindObjectsByType<SwampTrap>(FindObjectsSortMode.None).Single(t=>t.TrapId==id);trap.InitFromServer(id,0,true);
        trap.EditorCaptureActivate=(key,p)=>{activated++;manager.OnSwampActivate(key,0,p,Vector3.up,3,5);Debug.Log("SWAMP_LAND "+p);};
        foreach(var c in range.player.GetComponentsInChildren<Collider>(true))foreach(var t in trap.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(c,t);
        // Fixture keeps both purchased charges available, no account or server writes.
        SkillInventoryState.ApplyServerState(2,0);
    }
    static void TestSlopeThrow()
    {
        RaycastHit slope=default;bool found=false;
        for(float x=940;x<995&&!found;x+=1)
        for(float z=-5;z<35&&!found;z+=1){
            if(!Physics.Raycast(new Vector3(x,25,z),Vector3.down,out var h,50,~0,QueryTriggerInteraction.Ignore))continue;
            if(h.normal.y>.76f&&h.normal.y<.98f&&!h.collider.GetComponentInParent<CharacterController>()){
                bool room=true;
                foreach(var d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                    if(!Physics.Raycast(h.point+d*.6f+Vector3.up*3,Vector3.down,out var near,6,~0,QueryTriggerInteraction.Ignore)||Vector3.Dot(near.normal,h.normal)<.99f)room=false;
                if(room){slope=h;found=true;}
            }
        }
        Check(found,"TestMap has a usable ramp");slopePoint=slope.point;
        var trap=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSwampAuthoring.Trap),slope.point+Vector3.up*.8f,Quaternion.identity);
        var component=trap.GetComponent<SwampTrap>();component.InitFromServer(99999,0,true);trap.GetComponent<Rigidbody>().linearVelocity=Vector3.down*3;
        component.EditorCaptureActivate=(id,point)=>{
            Check(Vector3.Distance(point,slopePoint)<.5f,"real collision activates on ramp");
            var area=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSwampAuthoring.Area),point+Vector3.up*.03f,Quaternion.identity);
            area.name="SlopeVerificationArea";var visual=area.GetComponent<SwampGroundMeshVisual>();visual.Init(3,area.GetComponent<Renderer>().sharedMaterial,~0);area.GetComponent<ControllerSwampVfx>().Begin(5);
            var mesh=area.GetComponent<MeshFilter>().sharedMesh;int tested=0;
            foreach(var vertex in mesh.vertices){var world=area.transform.TransformPoint(vertex);if(Vector2.Distance(new Vector2(world.x,world.z),new Vector2(point.x,point.z))>.9f)continue;
                Check(Mathf.Abs(Vector3.Dot(world-slope.point,slope.normal)-.025f)<.055f,"ramp vertices conform to surface");tested++;}
            Check(tested>8,"multiple ramp vertices checked");slopePassed=true;
            Debug.Log($"SWAMP_SLOPE_PASS angle={Vector3.Angle(slope.normal,Vector3.up)} vertices={tested} point={point}");Object.Destroy(trap);Object.Destroy(area,5);
        };
    }
    static void TestCorner()
    {
        var root=new GameObject("L_CornerVerification");Vector3 center=new Vector3(1100,0,0);
        void Box(string name,Vector3 position,Vector3 scale){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform);g.transform.position=center+position;g.transform.localScale=scale;}
        Box("Floor",new Vector3(0,-.5f,0),new Vector3(12,1,12));
        Box("WallX",new Vector3(1.3f,1.5f,0),new Vector3(.3f,3,5));
        Box("WallZ",new Vector3(0,1.5f,1.3f),new Vector3(5,3,.3f));Physics.SyncTransforms();
        var area=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSwampAuthoring.Area),center+Vector3.up*.03f,Quaternion.identity);area.transform.SetParent(root.transform);
        area.GetComponent<SwampGroundMeshVisual>().Init(3,area.GetComponent<Renderer>().sharedMaterial,~0);
        Check(!SwampSurfaceQuery.Blocked(center,center+new Vector3(-1,0,-1),out _),"open side of corner reachable");
        Check(SwampSurfaceQuery.Blocked(center,center+new Vector3(2,0,0),out _),"right wall blocks slow");
        Check(SwampSurfaceQuery.Blocked(center,center+new Vector3(0,0,2),out _),"front wall blocks slow");
        int shortened=0;var mesh=area.GetComponent<MeshFilter>().sharedMesh;
        foreach(var v in mesh.vertices){var world=area.transform.TransformPoint(v);Check(!SwampSurfaceQuery.Blocked(center,world,out _),"visible ground vertex does not cross L walls");if(world.x>center.x+1&&world.x<center.x+1.15f)shortened++;}
        Check(shortened>10,"mesh actually trimmed along wall");
        var original=range.player.position;var cc=range.player.GetComponent<CharacterController>();float bottom=(cc.center.y-cc.height*.5f)*range.player.lossyScale.y;
        cc.enabled=false;range.player.position=center+new Vector3(2,-bottom,0);cc.enabled=true;Physics.SyncTransforms();
        var slow=area.GetComponent<SwampArea>();slow.SendMessage("OnTriggerEnter",cc);Check(slow.AffectedCount==0,"player behind wall never receives slow");
        cc.enabled=false;range.player.position=center+new Vector3(-1,-bottom,-1);cc.enabled=true;Physics.SyncTransforms();
        slow.SendMessage("OnTriggerEnter",cc);Check(slow.AffectedCount==1&&movement.IsSlowed,"player on open side receives slow");
        root.SetActive(false);Check(Private<int>(movement,"_swampSlowCount")==0,"corner teardown clears slow");
        cc.enabled=false;range.player.position=original;cc.enabled=true;movement.ResetNetworkInterpolation();Physics.SyncTransforms();Object.Destroy(root);
        Debug.Log("SWAMP_CORNER_PASS walls=2 behindWallSlow=false clippedVertices="+shortened);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||done)return;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Capture timeout");
            if(!EditorApplication.isPlaying||last==Time.frameCount)return;last=Time.frameCount;
            if(stage==0){if(++warmup<40)return;range=Object.FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;range.loadout.SelectCharacter(1);stage=1;return;}
            if(stage==1){
                if(range.loadout.Switching)return;
                range.loadout.EquipWeapon(Array.IndexOf(range.loadout.weapons,range.rifle));range.loadout.Confirm();range.spawner.StopTraining();range.menu.SetActive(false);range.trainingZones=Array.Empty<PracticeTrainingZone>();range.enabled=false;
                movement=range.player.GetComponent<PlayerMovement>();camera=range.player.GetComponent<PlayerCameraController>().PlayerCamera;
                var cc=range.player.GetComponent<CharacterController>();cc.enabled=false;range.player.position=new Vector3(967,-(cc.center.y-cc.height*.5f)*range.player.lossyScale.y+.03f,5);cc.enabled=true;
                range.player.GetComponentInChildren<FPSCameraLook>().ResetLookRotation(0);
                Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
                // Fixed initial player aim. Headless editors do not retain OS cursor capture.
                var playerLook=range.player.GetComponentInChildren<FPSCameraLook>();
                typeof(FPSCameraLook).GetField("xRotation",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(playerLook,15f);
                playerLook.transform.localRotation=Quaternion.Euler(15,0,0);
                foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
                manager=new GameObject("LocalSwampAcceptance").AddComponent<ControllerSwampNetworkManager>();var s=new SerializedObject(manager);s.FindProperty("_trapPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSwampAuthoring.Trap);s.FindProperty("_areaPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSwampAuthoring.Area);s.ApplyModifiedPropertiesWithoutUndo();
                skill=range.player.GetComponent<ControllerSkill_Q>();skill.EditorCaptureThrow=Throw;skill.enabled=true;SkillInventoryState.ApplyServerState(2,0);
                TestOverlap();
                TestCorner();
                TestSlopeThrow();
                InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onAfterUpdate+=Input;
                rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
                var start=new System.Diagnostics.ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{Output}/video.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
                encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(o,e)=>{if(e.Data!=null)Debug.LogWarning(e.Data);};encoder.BeginErrorReadLine();
                Time.captureFramerate=30;QualitySettings.vSyncCount=0;Application.runInBackground=true;
                var config=AudioSettings.GetConfiguration();rate=config.sampleRate;channels=config.speakerMode==AudioSpeakerMode.Mono?1:2;
                sound=new FileStream(Output+"/audio.f32le",FileMode.Create);audio=AudioRenderer.Start();
                csv=new StreamWriter(Output+"/checks.csv");csv.WriteLine("frame,speed,slowCount,casting,x,z");stage=2;frame=0;return;
            }
            if(skill.IsCasting){Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Fire),"no gun fire during throw");Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Reload),"no reload during throw");}
            int slow=Private<int>(movement,"_swampSlowCount");if(frame>140&&slow>0)observedSlow=true;if(observedSlow&&slow==0)observedRecovery=true;
            var victim=GameObject.Find("SwampTarget");
            if(victim&&victim.GetComponent<PlayerMovement>().IsSlowed&&victim.GetComponentInChildren<ParticleSystem>())observedTargetFeedback=true;
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;
            var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(audio){int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;using(var buffer=new NativeArray<float>(count,Allocator.Temp)){AudioRenderer.Render(buffer);var samples=buffer.ToArray();foreach(float value in samples)peak=Mathf.Max(peak,Mathf.Abs(value));byte[] data=new byte[count*4];Buffer.BlockCopy(samples,0,data,0,data.Length);sound.Write(data,0,data.Length);}}
            csv.WriteLine($"{frame},{movement.PlanarSpeed},{slow},{skill.IsCasting},{range.player.position.x},{range.player.position.z}");
            if(frame%60==0||frame==52||frame==58||frame==64){File.WriteAllBytes(Output+"/frame-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());Debug.Log($"SWAMP_FRAME {frame} slow={slow} speed={movement.PlanarSpeed} pos={range.player.position}");}
            if(++frame<360)return;
            Check(thrown==2&&activated==2,"two Q casts landed through projectile collision");Check(observedSlow&&observedRecovery,"actual player entered area and recovered");Check(observedTargetFeedback,"target ankle feedback shown while slowed");Check(slopePassed,"actual ramp collision and conforming mesh");Check(!skill.IsCasting,"weapon input recovered");Check(errors==0,"runtime errors="+errors);Check(peak>.001f,"audio recorded");
            passed=true;Finish();Debug.Log($"SWAMP_CAPTURE_PASS checks={checks} audioPeak={peak} rate={rate} channels={channels}");
        }catch(Exception ex){Debug.LogException(ex);Finish();}
    }
    static void Finish()
    {
        done=true;InputSystem.onAfterUpdate-=Input;Time.captureFramerate=0;
        if(audio){AudioRenderer.Stop();audio=false;}sound?.Dispose();csv?.Dispose();
        if(encoder!=null){encoder.StandardInput.Close();encoder.WaitForExit(15000);encoder.Dispose();encoder=null;}
        File.WriteAllText(Output+"/capture-info.txt",$"passed={passed}\nframes={frame}\nfps=30\nsampleRate={rate}\nchannels={channels}\nRuntimeErrors={errors}\nLocal Play mode, real input/physics. Server acceptance fixture only.\n");
        EditorApplication.ExitPlaymode();
    }
    static void State(PlayModeStateChange s){if(SessionState.GetBool(Key,false)&&s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(passed?0:1);}}
}
#endif
