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

// Local rendering/input verification. Enemy cast packets are a fixture, not a network match.
[InitializeOnLoad]
public static class ControllerForestDesertCapture
{
    const string Key="Vector.ForestDesert", Output="C:/Users/GCT/Documents/ControllerForest-Desert-20261023";
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    static PracticeRangeController range;
    static Transform route;
    static Camera camera,overview;
    static ControllerSkill_X skill;
    static ControllerUltimateNetworkManager manager;
    static PlayerOutline enemy,ally,far;
    static Keyboard keyboard;
    static Mouse mouse;
    static RenderTexture rt;
    static Texture2D pixels;
    static System.Diagnostics.Process encoder;
    static FileStream sound;
    static StreamWriter telemetry;
    static int stage,frame,warmup,last=-1,checks,errors,casts,channels,rate,phase;
    static bool done,passed,audio,entered;
    static float peak;
    static double deadline;
    static ControllerForestDesertCapture(){EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception))errors++;};if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;}}
    static void Check(bool v,string label){if(!v)throw new Exception("FOREST_DESERT_FAIL "+label);checks++;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");Object.FindFirstObjectByType<PracticeRangeController>().persistRecords=false;
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/Desert.prefab"));map.transform.position=new Vector3(1000,0,0);var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);Physics.SyncTransforms();
        FindRoute(map,info);
        Target("ForestCaster",new Vector3(-1.3f,0,1.1f),101,0);Target("VisitorAlly",new Vector3(.15f,0,2.4f),102,1);Target("FarCasterAlly",new Vector3(1.3f,0,6.5f),103,0);
        foreach(var b in map.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;EditorApplication.EnterPlaymode();
    }
    static bool Ground(Vector3 p,out Vector3 result){if(Physics.Raycast(p+Vector3.up*4,Vector3.down,out var h,9,~0,QueryTriggerInteraction.Ignore)&&h.normal.y>.97f){result=h.point;return true;}result=default;return false;}
    static void FindRoute(GameObject map,MapInfo info)
    {
        var seeds=info.attackSpawnPoints.Concat(info.defendSpawnPoints).Concat(map.GetComponentsInChildren<SpikePlantZone>().Select(z=>z.transform));
        foreach(var seed in seeds)for(int radius=0;radius<=20;radius+=4)for(int offset=0;offset<8;offset++){
            if(!Ground(seed.position+Quaternion.Euler(0,offset*45,0)*Vector3.forward*radius,out var g))continue;
            for(int angle=0;angle<360;angle+=30){
                var f=Quaternion.Euler(0,angle,0)*Vector3.forward;var right=Vector3.Cross(Vector3.up,f);bool clear=true;
                for(int z=-9;z<=7&&clear;z+=2)foreach(float x in new[]{-1.5f,0,1.5f})if(!Ground(g+f*z+right*x,out var floor)||Mathf.Abs(floor.y-g.y)>.25f){clear=false;break;}
                foreach(float x in new[]{-1.5f,0,1.5f})if(Physics.Linecast(g-f*9+right*x+Vector3.up*1.5f,g+f*7+right*x+Vector3.up*1.5f,~0,QueryTriggerInteraction.Ignore))clear=false;
                if(!clear)continue;
                route=new GameObject("ForestDesertRoute").transform;route.SetPositionAndRotation(g,Quaternion.LookRotation(f));Debug.Log($"FOREST_DESERT_ROUTE center={g} forward={f} seed={seed.name}");return;
            }
        }
        throw new Exception("No walkable Desert route");
    }
    static void Target(string name,Vector3 offset,uint id,byte team){var a=ControllerForestCapture.Target(name,route.TransformPoint(offset),id,team);a.transform.rotation=Quaternion.LookRotation(-route.forward);}
    static void Input()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic||keyboard==null)return;
        var keys=new KeyboardState();
        if(stage==2&&!done){if(phase==0&&frame==45)keys=new KeyboardState(UnityEngine.InputSystem.Key.X);if(phase==1&&frame>=110&&frame<250&&route.InverseTransformPoint(range.player.position).z< -1.8f)keys=new KeyboardState(UnityEngine.InputSystem.Key.W);}
        keyboard.MakeCurrent();InputState.Change(keyboard,keys);mouse.MakeCurrent();InputState.Change(mouse,new MouseState());
    }
    static void Place(float z)
    {
        var cc=range.player.GetComponent<CharacterController>();cc.enabled=false;range.player.position=route.TransformPoint(new Vector3(0,0,z))-Vector3.up*(cc.center.y-cc.height*.5f)*range.player.lossyScale.y+Vector3.up*.03f;cc.enabled=true;
        range.player.GetComponent<PlayerMovement>().ResetNetworkInterpolation();var look=range.player.GetComponentInChildren<FPSCameraLook>();look.ResetLookRotation(route.eulerAngles.y);typeof(FPSCameraLook).GetField("xRotation",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(look,0f);look.transform.localRotation=Quaternion.identity;Physics.SyncTransforms();
    }
    static void StartRecording()
    {
        string name=phase==0?"Desert-Size":"Desert-Enemy";
        var start=new System.Diagnostics.ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{Output}/{name}-silent.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(o,e)=>{if(e.Data!=null)Debug.LogWarning(e.Data);};encoder.BeginErrorReadLine();
        sound=new FileStream(Output+"/"+name+".f32le",FileMode.Create);audio=AudioRenderer.Start();
    }
    static void StopRecording(){if(audio){AudioRenderer.Stop();audio=false;}sound?.Dispose();sound=null;if(encoder!=null){encoder.StandardInput.Close();encoder.WaitForExit(15000);encoder.Dispose();encoder=null;}}
    static Vector3 OverviewPosition()
    {
        int best=-1;Vector3 position=route.position+Vector3.up*23;
        for(int angle=0;angle<360;angle+=30){
            var p=route.position+Quaternion.Euler(0,angle,0)*new Vector3(0,19,-21);int score=0;
            foreach(float x in new[]{-5f,0,5})foreach(float z in new[]{-5f,0,5}){
                var point=route.TransformPoint(new Vector3(x,2,z));
                if(!Physics.Linecast(p,point,~0,QueryTriggerInteraction.Ignore))score++;
            }
            if(score>best){best=score;position=p;}
        }
        Debug.Log($"FOREST_DESERT_OVERVIEW clear={best}/9 pos={position}");return position;
    }
    static bool Suppressed(PlayerOutline p)=>(bool)typeof(Outline).GetField("smokeSuppressed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p.GetComponentInChildren<Outline>());
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||done)return;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Capture timeout");if(!EditorApplication.isPlaying||last==Time.frameCount)return;last=Time.frameCount;
            if(stage==0){if(++warmup<40)return;range=Object.FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;range.loadout.SelectCharacter(1);stage=1;return;}
            if(stage==1){
                if(range.loadout.Switching)return;
                route=GameObject.Find("ForestDesertRoute").transform;range.loadout.EquipWeapon(Array.IndexOf(range.loadout.weapons,range.rifle));range.loadout.Confirm();range.spawner.StopTraining();range.menu.SetActive(false);range.trainingZones=Array.Empty<PracticeTrainingZone>();range.enabled=false;
                camera=range.player.GetComponent<PlayerCameraController>().PlayerCamera;Place(0);range.player.GetComponent<PlayerIdentity>().Team=0;
                enemy=GameObject.Find("ForestCaster").GetComponentInChildren<PlayerOutline>();ally=GameObject.Find("VisitorAlly").GetComponentInChildren<PlayerOutline>();far=GameObject.Find("FarCasterAlly").GetComponentInChildren<PlayerOutline>();
                enemy.Configure(false,true);ally.Configure(false,false);far.Configure(false,true);
                Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
                manager=new GameObject("LocalForestAcceptance").AddComponent<ControllerUltimateNetworkManager>();var s=new SerializedObject(manager);s.FindProperty("dreamForestPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(ControllerForestAuthoring.Area);s.ApplyModifiedPropertiesWithoutUndo();
                skill=range.player.GetComponent<ControllerSkill_X>();skill.EditorCaptureCast=p=>{casts++;manager.OnUltimateSpawn(94000,range.player.GetComponent<PlayerIdentity>().PlayerId,p,7.5f,10);SkillInventoryState.ApplyServerState(8,8);};skill.enabled=true;SkillInventoryState.ApplyServerState(8,0);
                InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onAfterUpdate+=Input;
                overview=new GameObject("ExteriorCaptureCamera").AddComponent<Camera>();overview.CopyFrom(camera);overview.transform.position=OverviewPosition();overview.transform.LookAt(route.position+Vector3.up);overview.enabled=false;
                rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;overview.targetTexture=rt;
                Time.captureFramerate=30;QualitySettings.vSyncCount=0;Application.runInBackground=true;var config=AudioSettings.GetConfiguration();rate=config.sampleRate;channels=config.speakerMode==AudioSpeakerMode.Mono?1:2;StartRecording();telemetry=new StreamWriter(Output+"/checks.csv");telemetry.WriteLine("phase,frame,z,inside,enemySuppressed,allySuppressed,farSuppressed");stage=2;frame=0;return;
            }
            if(phase==1&&frame==45)manager.OnUltimateSpawn(94001,101,route.position,7.5f,10);
            var view=phase==0?overview:camera;view.Render();
            bool inside=DreamForestPresentation.Inside(camera.transform.position),eh=Suppressed(enemy),ah=Suppressed(ally),fh=Suppressed(far);
            telemetry.WriteLine($"{phase},{frame},{route.InverseTransformPoint(range.player.position).z:F3},{inside},{eh},{ah},{fh}");
            if(phase==0&&frame==180){var area=DreamForestPresentation.Active.Single();Check(Mathf.Abs(area.Radius-7.5f)<.01f,"Desert actual radius 7.5");}
            if(phase==1){
                if(frame==30)Check(!inside&&!eh&&!ah,"outside before cast both identification outlines visible");
                if(frame==105)Check(!inside&&eh&&ah&&fh,"outside shell blocks outlines");
                if(inside)entered=true;
                if(frame==260){Check(entered&&inside,"real enemy walks into forest");Check(eh&&ah&&fh,"enemy loses both ally and enemy outline cues");Check(enemy.GetComponentInChildren<SkinnedMeshRenderer>().enabled&&ally.GetComponentInChildren<SkinnedMeshRenderer>().enabled,"bodies remain rendered");Check(DreamForestAudio.Instance&&DreamForestAudio.Instance.IsMuffled,"enemy audio affected");}
                if(frame==390){Check(DreamForestPresentation.Active.Count==0&&!eh&&!ah&&!fh,"expiry restores all outlines");Check(!DreamForestAudio.Instance||!DreamForestAudio.Instance.IsMuffled,"audio restored");}
            }
            RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(audio){int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;using(var buffer=new NativeArray<float>(count,Allocator.Temp)){AudioRenderer.Render(buffer);var samples=buffer.ToArray();foreach(float v in samples)peak=Mathf.Max(peak,Mathf.Abs(v));byte[] data=new byte[count*4];Buffer.BlockCopy(samples,0,data,0,data.Length);sound.Write(data,0,data.Length);}}
            if(frame%60==0||frame==30||frame==105||frame==260||frame==390){File.WriteAllBytes(Output+$"/phase-{phase}-frame-{frame:D4}.png",pixels.EncodeToPNG());Debug.Log($"FOREST_DESERT_FRAME {phase}:{frame}");}
            if(++frame<(phase==0?420:450))return;
            if(phase==0){Check(casts==1,"real caster X accepted");Check(DreamForestPresentation.Active.Count==0,"first cast expired");StopRecording();phase=1;frame=0;Place(-9);range.player.GetComponent<PlayerIdentity>().Team=1;enemy.Configure(false,false);ally.Configure(false,true);far.Configure(false,false);StartRecording();return;}
            Check(errors==0,"runtime errors="+errors);Check(peak>.001f&&peak<1,"unclipped audio");passed=true;Finish();Debug.Log($"FOREST_DESERT_PASS checks={checks} peak={peak}");
        }catch(Exception e){Debug.LogException(e);Finish();}
    }
    static void Finish(){done=true;InputSystem.onAfterUpdate-=Input;Time.captureFramerate=0;StopRecording();telemetry?.Dispose();File.WriteAllText(Output+"/capture-info.txt",$"passed={passed}\nchecks={checks}\nerrors={errors}\nrate={rate}\nchannels={channels}\nLocal Desert, saved player/real input/physics, enemy team 1 versus caster team 0, server packet fixture. No accounts or saved scenes changed.\n");EditorApplication.ExitPlaymode();}
    static void State(PlayModeStateChange s){if(SessionState.GetBool(Key,false)&&s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(passed?0:1);}}
}
#endif
