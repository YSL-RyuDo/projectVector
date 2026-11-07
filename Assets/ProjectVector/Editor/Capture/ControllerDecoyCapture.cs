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
using Object=UnityEngine.Object;

// Actual saved player, input and collisions; only server acceptance is a local fixture.
[InitializeOnLoad]
public static class ControllerDecoyCapture
{
    const string Key="Vector.DecoyCapture", Output="C:/Users/GCT/Documents/ControllerDecoy-Manual-20261023";
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    static PracticeRangeController range;
    static Camera camera;
    static ControllerSkill_C skill;
    static ControllerDecoyNetworkManager manager;
    static Keyboard keyboard;
    static Mouse mouse;
    static RenderTexture rt;
    static Texture2D pixels;
    static System.Diagnostics.Process encoder;
    static FileStream sound;
    static int stage,frame,warmup,last=-1,checks,errors,thrown,activated,shots,channels,rate;
    static bool done,passed,audio,floorLanded,wallLanded;
    static float peak;
    static uint nextId=91000;
    static double deadline;
    static Vector3 wallStart,wallNormal;
    static DecoyTrapArea lifeTest;
    static float lifeStart;
    static bool checkedLife;
    static ControllerDecoyCapture()
    {
        EditorApplication.playModeStateChanged+=State;
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception))errors++;};
        if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;}
    }
    static void Check(bool value,string label){if(!value)throw new Exception("DECOY_FAIL "+label);checks++;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");
        Object.FindFirstObjectByType<PracticeRangeController>().persistRecords=false;
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/TestMap.prefab"));
        map.transform.position=new Vector3(1000,0,0);map.GetComponent<MapInfo>().SetBarriersActive(false);
        foreach(var b in map.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;EditorApplication.EnterPlaymode();
    }
    static void Input()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic||keyboard==null)return;
        var keys=new KeyboardState();
        if(stage==2&&!done){
            if(frame==45||frame==285||frame==305||frame==540||frame==650||frame==680)keys=new KeyboardState(UnityEngine.InputSystem.Key.C);
            if(frame>=700&&frame<708)keys=new KeyboardState(UnityEngine.InputSystem.Key.W);
            if(frame>=150&&frame<163)keys=new KeyboardState(UnityEngine.InputSystem.Key.D);
            if(frame>=180&&frame<193)keys=new KeyboardState(UnityEngine.InputSystem.Key.A);
        }
        keyboard.MakeCurrent();InputState.Change(keyboard,keys);mouse.MakeCurrent();InputState.Change(mouse,new MouseState());
    }
    static T Private<T>(object o,string field)=>(T)o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void Aim(float yaw,float pitch)
    {
        var look=range.player.GetComponentInChildren<FPSCameraLook>();look.ResetLookRotation(yaw);
        typeof(FPSCameraLook).GetField("xRotation",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(look,pitch);
        look.transform.localRotation=Quaternion.Euler(pitch,0,0);
    }
    static void Place(Vector3 foot,float yaw,float pitch)
    {
        var cc=range.player.GetComponent<CharacterController>();cc.enabled=false;
        range.player.position=foot-Vector3.up*(cc.center.y-cc.height*.5f)*range.player.lossyScale.y+Vector3.up*.03f;cc.enabled=true;
        range.player.GetComponent<PlayerMovement>().ResetNetworkInterpolation();Aim(yaw,pitch);Physics.SyncTransforms();
    }
    static void FindWall()
    {
        foreach(var dir in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
        {
            var hits=Physics.RaycastAll(new Vector3(967,1.5f,5),dir,60,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            foreach(var h in hits){
                if(Mathf.Abs(h.normal.y)>.05f||h.collider.GetComponentInParent<CharacterController>())continue;
                var p=h.point+h.normal*3.1f;
                if(!Physics.Raycast(p+Vector3.up*3,Vector3.down,out var ground,6,~0,QueryTriggerInteraction.Ignore)||ground.normal.y<.9f)continue;
                wallStart=ground.point;wallNormal=h.normal;Debug.Log("DECOY_WALL_SETUP "+wallStart+" normal="+wallNormal);return;
            }
        }
        throw new Exception("No TestMap wall suitable for capture");
    }
    static void Throw(Vector3 position,Vector3 velocity)
    {
        uint id=nextId++;thrown++;manager.OnDecoySpawn(id,0,position,velocity);
        var trap=Object.FindObjectsByType<DecoyProjectile>(FindObjectsSortMode.None).Single(t=>t.DecoyId==id);trap.InitFromServer(id,0,true);
        trap.EditorCaptureActivate=(key,p,n)=>{
            activated++;if(n.y>.8f)floorLanded=true;if(Mathf.Abs(n.y)<.1f)wallLanded=true;
            manager.OnDecoyActivate(key,0,p,n,0);
            var a=Object.FindObjectsByType<DecoyTrapArea>(FindObjectsSortMode.None).OrderBy(t=>Vector3.Distance(t.transform.position,p)).First();
            Check(Vector3.Dot(a.transform.up,n)>.999f,"device conforms to contact normal");
            var count=Object.FindObjectsByType<DecoyTrapArea>(FindObjectsSortMode.None).Length;
            manager.OnDecoyActivate(key,0,p,n,0);
            Check(Object.FindObjectsByType<DecoyTrapArea>(FindObjectsSortMode.None).Length==count,"duplicate activation ignored");
            Debug.Log("DECOY_ATTACHED "+p+" normal="+n);
        };
        foreach(var c in range.player.GetComponentsInChildren<Collider>(true))foreach(var t in trap.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(c,t);
        SkillInventoryState.ApplyServerState(1,1);
    }
    static void Tests()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ControllerDecoyAuthoring.Area);
        var gun=prefab.GetComponent<AudioSource>();
        Check(!gun.playOnAwake&&!gun.loop&&gun.spatialBlend==1,"spatial one-shot gun audio");
        Check(Private<AudioClip>(prefab.GetComponent<DecoyTrapArea>(),"_gunfireClip")==AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ProjectVector/Shared/Weapons/Assets/Pistol/P92/P92.asset").gunSound,"real P92 sound");
        // Cancel before release must not send or leave input/prop restrictions.
        skill.SendMessage("UseSkill");Check(skill.IsCasting,"C starts with purchase");
        Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.WeaponChange),"weapon change locked");
        skill.Cancel();Check(!skill.IsCasting&&!range.player.GetComponent<ControllerDecoyMotion>().Active,"cancel restores pose");
        Check(thrown==0,"cancel before release sends nothing");
        Debug.Log("DECOY_STATIC_PASS");
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
                camera=range.player.GetComponent<PlayerCameraController>().PlayerCamera;Place(new Vector3(967,0,5),0,30);
                Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
                foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
                manager=new GameObject("LocalDecoyAcceptance").AddComponent<ControllerDecoyNetworkManager>();
                var s=new SerializedObject(manager);s.FindProperty("_decoyProjectilePrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(ControllerDecoyAuthoring.Trap);s.FindProperty("_decoyAreaPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(ControllerDecoyAuthoring.Area);s.ApplyModifiedPropertiesWithoutUndo();
                skill=range.player.GetComponent<ControllerSkill_C>();skill.EditorCaptureThrow=Throw;skill.enabled=true;SkillInventoryState.ApplyServerState(1,0);
                manager.EditorCaptureTrigger=id=>{
                    var area=Object.FindObjectsByType<DecoyTrapArea>(FindObjectsSortMode.None).Single();
                    Check(!area.IsPlaying&&area.ShotsPlayed==0,"no playback before server acknowledgement");
                    Check(!SkillInventoryState.CanUse(SkillSlot.C),"reactivation works with spent charge");
                    manager.OnDecoyTrigger(id,6);manager.OnDecoyTrigger(id,6);
                    if(id==91000){lifeTest=area;lifeStart=Time.time;}
                };
                FindWall();Tests();
                InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onAfterUpdate+=Input;
                rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
                var start=new System.Diagnostics.ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{Output}/video.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
                encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(o,e)=>{if(e.Data!=null)Debug.LogWarning(e.Data);};encoder.BeginErrorReadLine();
                Time.captureFramerate=30;QualitySettings.vSyncCount=0;Application.runInBackground=true;
                var config=AudioSettings.GetConfiguration();rate=config.sampleRate;channels=config.speakerMode==AudioSpeakerMode.Mono?1:2;
                sound=new FileStream(Output+"/audio.f32le",FileMode.Create);audio=AudioRenderer.Start();stage=2;frame=0;return;
            }
            if(skill.IsCasting){
                Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Fire),"fire blocked during throw");
                Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Reload),"reload blocked during throw");
            }
            if(lifeTest&&!checkedLife&&Time.time-lifeStart>5.4f){
                Check(lifeTest&&lifeTest.IsPlaying,"server six-second lifetime survives five seconds");checkedLife=true;
            }
            if(frame==270){
                var waiting=Object.FindObjectsByType<DecoyTrapArea>(FindObjectsSortMode.None).Single();
                Check(!waiting.IsPlaying&&waiting.ShotsPlayed==0,"no auto fire after seven seconds installed");
                Check(!manager.TryTriggerOwned(777),"cannot request another owner's device");
            }
            if(frame==500){Check(!lifeTest,"six seconds after trigger cleanup");Place(wallStart,Quaternion.LookRotation(-wallNormal).eulerAngles.y,0);SkillInventoryState.ApplyServerState(1,0);}
            foreach(var area in Object.FindObjectsByType<DecoyTrapArea>(FindObjectsSortMode.None))shots=Mathf.Max(shots,area.ShotsPlayed);
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;
            var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(audio){int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;using(var buffer=new NativeArray<float>(count,Allocator.Temp)){AudioRenderer.Render(buffer);var samples=buffer.ToArray();foreach(float value in samples)peak=Mathf.Max(peak,Mathf.Abs(value));byte[] data=new byte[count*4];Buffer.BlockCopy(samples,0,data,0,data.Length);sound.Write(data,0,data.Length);}}
            if(frame%60==0||frame==52||frame==58||frame==64||frame==320){File.WriteAllBytes(Output+"/frame-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());Debug.Log("DECOY_FRAME "+frame);}
            if(++frame<900)return;
            Check(thrown==2&&activated==2,"two throws despite repeated C trigger presses");
            Check(floorLanded&&wallLanded,"floor and wall attachment");
            Check(shots>=8,"multiple pistol bursts");Check(checkedLife,"six-second lifecycle checked");
            Check(!skill.IsCasting&&GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Fire),"weapon restored");
            Check(Object.FindObjectsByType<DecoyTrapArea>(FindObjectsSortMode.None).Length==0,"no remaining effects");
            Check(errors==0,"runtime errors="+errors);Check(peak>.001f&&peak<1,"audible unclipped recording peak="+peak);
            passed=true;Finish();Debug.Log($"DECOY_CAPTURE_PASS checks={checks} audioPeak={peak} rate={rate} channels={channels}");
        }catch(Exception ex){Debug.LogException(ex);Finish();}
    }
    static void Finish()
    {
        done=true;InputSystem.onAfterUpdate-=Input;Time.captureFramerate=0;
        if(audio){AudioRenderer.Stop();audio=false;}sound?.Dispose();
        if(encoder!=null){encoder.StandardInput.Close();encoder.WaitForExit(15000);encoder.Dispose();encoder=null;}
        File.WriteAllText(Output+"/capture-info.txt",$"passed={passed}\nframes={frame}\nfps=30\nsampleRate={rate}\nchannels={channels}\nRuntimeErrors={errors}\nLocal Play mode, real player/input/physics. Server acceptance fixture only.\n");
        EditorApplication.ExitPlaymode();
    }
    static void State(PlayModeStateChange s){if(SessionState.GetBool(Key,false)&&s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(passed?0:1);}}
}
#endif
