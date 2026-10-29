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

// Saved player and real X input; server acceptance is explicitly a local fixture.
[InitializeOnLoad]
public static class ControllerForestCapture
{
    const string Key="Vector.ForestCapture", Output="C:/Users/GCT/Documents/ControllerForest-20261023";
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    static PracticeRangeController range;
    static Camera camera,overview;
    static ControllerSkill_X skill;
    static ControllerUltimateNetworkManager manager;
    static Keyboard keyboard;
    static Mouse mouse;
    static RenderTexture rt;
    static Texture2D pixels;
    static System.Diagnostics.Process encoder;
    static FileStream sound;
    static int stage,frame,warmup,last=-1,checks,errors,casts,channels,rate;
    static bool done,passed,audio,insideSeen,muffledSeen;
    static float peak;
    static Vector3 initialPosition;
    static AudioSource spatialProbe,uiProbe;
    static double deadline;
    static ControllerForestCapture(){EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception))errors++;};if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;}}
    static void Check(bool v,string label){if(!v)throw new Exception("FOREST_FAIL "+label);checks++;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/Scenes/WeaponTest.unity");Object.FindFirstObjectByType<PracticeRangeController>().persistRecords=false;
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));map.transform.position=new Vector3(1000,0,0);map.GetComponent<MapInfo>().SetBarriersActive(false);
        foreach(var b in map.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
        Target("NearOpponent",new Vector3(965.7f,0,8.7f),101,1);Target("FarOpponent",new Vector3(969,0,14),102,1);
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;EditorApplication.EnterPlaymode();
    }
    public static GameObject Target(string name,Vector3 foot,uint id,byte team)
    {
        var a=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerForestAuthoring.Actor));a.name=name;
        var cc=a.GetComponent<CharacterController>();a.transform.SetPositionAndRotation(foot-Vector3.up*(cc.center.y-cc.height*.5f)*a.transform.lossyScale.y,Quaternion.Euler(0,180,0));
        var model=a.transform.Find("Model");model.gameObject.SetActive(true);
        foreach(var b in a.GetComponentsInChildren<MonoBehaviour>(true))if(!(b is PlayerIdentity)&&!(b is PlayerOutline)&&!(b is Outline))Object.DestroyImmediate(b);
        foreach(var c in a.GetComponentsInChildren<Camera>(true))c.enabled=false;
        foreach(var r in a.GetComponentsInChildren<Renderer>(true)){r.enabled=r.transform.IsChildOf(model);r.forceRenderingOff=false;}
        AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/InGame/Pistol/Controller_Pistol_Idle.anim").SampleAnimation(model.gameObject,0);
        foreach(var anim in a.GetComponentsInChildren<Animator>(true))anim.enabled=false;
        foreach(var listen in a.GetComponentsInChildren<AudioListener>(true))listen.enabled=false;
        a.GetComponent<PlayerIdentity>().PlayerId=id;a.GetComponent<PlayerIdentity>().Team=team;
        return a;
    }
    static void Input()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic||keyboard==null)return;
        var keys=new KeyboardState();
        if(stage==2&&!done){if(frame==45||frame==435)keys=new KeyboardState(UnityEngine.InputSystem.Key.X);if(frame>=175&&frame<202)keys=new KeyboardState(UnityEngine.InputSystem.Key.W);if(frame>=220&&frame<247)keys=new KeyboardState(UnityEngine.InputSystem.Key.S);if(frame>=260&&frame<320)keys=new KeyboardState(UnityEngine.InputSystem.Key.A);}
        keyboard.MakeCurrent();InputState.Change(keyboard,keys);mouse.MakeCurrent();InputState.Change(mouse,new MouseState());
    }
    static void Place(Vector3 foot)
    {
        var cc=range.player.GetComponent<CharacterController>();cc.enabled=false;range.player.position=foot-Vector3.up*(cc.center.y-cc.height*.5f)*range.player.lossyScale.y+Vector3.up*.03f;cc.enabled=true;
        range.player.GetComponent<PlayerMovement>().ResetNetworkInterpolation();var look=range.player.GetComponentInChildren<FPSCameraLook>();look.ResetLookRotation(0);typeof(FPSCameraLook).GetField("xRotation",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(look,0f);look.transform.localRotation=Quaternion.identity;Physics.SyncTransforms();
    }
    static void Tests()
    {
        skill.SendMessage("UseSkill");Check(skill.IsCasting,"X starts");Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.WeaponChange),"weapon change blocked");skill.Cancel();Check(!skill.IsCasting&&!range.player.GetComponent<ControllerForestMotion>().Active&&casts==0,"cancel before release restores without cast");
        var owner=range.player.GetComponent<PlayerIdentity>();owner.Team=0;
        var test=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerForestAuthoring.Area),new Vector3(3000,0,0),Quaternion.identity);
        var fx=test.GetComponent<DreamForestPresentation>();fx.Init(owner.PlayerId,10);test.transform.localScale=Vector3.one*15;
        var viewer=new GameObject("OutlineProbe");var vi=viewer.AddComponent<PlayerIdentity>();vi.Team=0;var cam=viewer.AddComponent<Camera>();cam.enabled=false;cam.transform.position=new Vector3(3000,1.5f,0);
        Check(fx.Contains(cam.transform.position)&&!fx.Contains(new Vector3(3000,7,0)),"ellipsoid matches dome height");
        Check(!DreamForestPresentation.BlocksOutline(cam,new Vector3(3000,1.5f,3)),"owner team retains close outline");Check(DreamForestPresentation.BlocksOutline(cam,new Vector3(3000,1.5f,9)),"distant outline concealed");
        cam.transform.position=new Vector3(3000,1.5f,4);
        Check(DreamForestPresentation.BlocksOutline(cam,new Bounds(new Vector3(3000,1.5f,6.5f),new Vector3(1,2,1))),"body straddling boundary concealed");cam.transform.position=new Vector3(3000,1.5f,0);
        vi.Team=1;Check(DreamForestPresentation.BlocksOutline(cam,new Vector3(3000,1.5f,3)),"enemy visitor loses identification");
        cam.transform.position=new Vector3(3000,1.5f,-18);Check(DreamForestPresentation.BlocksOutline(cam,new Vector3(3000,1.5f,0)),"outside to inside concealed");Check(DreamForestPresentation.BlocksOutline(cam,new Vector3(3000,1.5f,18)),"through area concealed");Check(!DreamForestPresentation.BlocksOutline(cam,new Vector3(3000,1.5f,-20)),"clear outside not concealed");
        var overlap=Object.Instantiate(test);overlap.GetComponent<DreamForestPresentation>().Init(owner.PlayerId,10);overlap.transform.localScale=Vector3.one*15;
        Object.DestroyImmediate(test);Check(DreamForestPresentation.Inside(new Vector3(3000,1.5f,0)),"overlap survives first volume removal");Object.DestroyImmediate(overlap);Check(!DreamForestPresentation.Inside(new Vector3(3000,1.5f,0)),"registry cleanup");Object.DestroyImmediate(viewer);
        spatialProbe=new GameObject("SpatialFilterProbe").AddComponent<AudioSource>();spatialProbe.spatialBlend=1;var filter=spatialProbe.gameObject.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=8200;filter.enabled=true;
        uiProbe=new GameObject("NonSpatialVoiceUIProbe").AddComponent<AudioSource>();uiProbe.spatialBlend=0;
        Debug.Log("FOREST_STATIC_PASS");
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||done)return;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Capture timeout");if(!EditorApplication.isPlaying||last==Time.frameCount)return;last=Time.frameCount;
            if(stage==0){if(++warmup<40)return;range=Object.FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;range.loadout.SelectCharacter(1);stage=1;return;}
            if(stage==1){
                if(range.loadout.Switching)return;
                range.loadout.EquipWeapon(Array.IndexOf(range.loadout.weapons,range.rifle));range.loadout.Confirm();range.spawner.StopTraining();range.menu.SetActive(false);range.trainingZones=Array.Empty<PracticeTrainingZone>();range.enabled=false;
                camera=range.player.GetComponent<PlayerCameraController>().PlayerCamera;Place(new Vector3(967,0,5));initialPosition=range.player.position;
                foreach(var target in Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))if(target.PlayerId==101||target.PlayerId==102)target.GetComponentInChildren<PlayerOutline>()?.Configure(false,false);
                Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
                manager=new GameObject("LocalForestAcceptance").AddComponent<ControllerUltimateNetworkManager>();var s=new SerializedObject(manager);s.FindProperty("dreamForestPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(ControllerForestAuthoring.Area);s.ApplyModifiedPropertiesWithoutUndo();
                skill=range.player.GetComponent<ControllerSkill_X>();skill.EditorCaptureCast=p=>{casts++;manager.OnUltimateSpawn((uint)(92000+casts),range.player.GetComponent<PlayerIdentity>().PlayerId,p,7.5f,10);SkillInventoryState.ApplyServerState(8,8);};skill.enabled=true;SkillInventoryState.ApplyServerState(8,0);Tests();
                InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onAfterUpdate+=Input;
                overview=new GameObject("ExteriorCaptureCamera").AddComponent<Camera>();overview.CopyFrom(camera);overview.transform.position=new Vector3(967,13,-19);overview.transform.LookAt(new Vector3(967,2,5));overview.enabled=false;
                rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;overview.targetTexture=rt;
                var start=new System.Diagnostics.ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{Output}/video.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(o,e)=>{if(e.Data!=null)Debug.LogWarning(e.Data);};encoder.BeginErrorReadLine();
                Time.captureFramerate=30;QualitySettings.vSyncCount=0;Application.runInBackground=true;var config=AudioSettings.GetConfiguration();rate=config.sampleRate;channels=config.speakerMode==AudioSpeakerMode.Mono?1:2;sound=new FileStream(Output+"/audio.f32le",FileMode.Create);audio=AudioRenderer.Start();stage=2;frame=0;return;
            }
            if(skill.IsCasting){Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Fire),"fire locked during cast");Check(!GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Reload),"reload locked during cast");}
            if(DreamForestPresentation.Inside(camera.transform.position))insideSeen=true;if(DreamForestAudio.Instance&&DreamForestAudio.Instance.IsMuffled)muffledSeen=true;
            if(frame==150){float cutoff=spatialProbe.GetComponent<AudioLowPassFilter>().cutoffFrequency;Debug.Log("FOREST_AUDIO_PROBE main="+(Camera.main?Camera.main.transform.position.ToString():"none")+" playerView="+camera.transform.position+" filtered="+(DreamForestAudio.Instance&&DreamForestAudio.Instance.IsMuffled)+" cutoff="+cutoff);Check(cutoff<=1901,"spatial sound filtered cutoff="+cutoff);Check(!uiProbe.GetComponent<AudioLowPassFilter>(),"nonspatial voice and UI untouched");}
            if(frame==210)Check(Vector3.Distance(initialPosition,range.player.position)>1,"real movement through mist");
            if(frame==330){Check(DreamForestPresentation.Active.Count==1&&!DreamForestPresentation.Inside(camera.transform.position),"real player leaves before expiry");Check(Mathf.Abs(spatialProbe.GetComponent<AudioLowPassFilter>().cutoffFrequency-8200)<1,"leaving restores audio before expiry");}
            if(frame==400){Check(casts==1&&insideSeen&&muffledSeen,"cast fog and muffling active");Check(DreamForestPresentation.Active.Count==0&&(!DreamForestAudio.Instance||!DreamForestAudio.Instance.IsMuffled),"expiry restores mist/audio");Check(Mathf.Abs(spatialProbe.GetComponent<AudioLowPassFilter>().cutoffFrequency-8200)<1,"original audio filter restored");Place(new Vector3(967,0,5));SkillInventoryState.ApplyServerState(8,0);}
            (frame<420?camera:overview).Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(audio){int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;using(var buffer=new NativeArray<float>(count,Allocator.Temp)){AudioRenderer.Render(buffer);var samples=buffer.ToArray();foreach(float v in samples)peak=Mathf.Max(peak,Mathf.Abs(v));byte[] data=new byte[count*4];Buffer.BlockCopy(samples,0,data,0,data.Length);sound.Write(data,0,data.Length);}}
            if(frame%60==0||frame==56||frame==64||frame==70){File.WriteAllBytes(Output+"/frame-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());Debug.Log("FOREST_FRAME "+frame);}
            if(++frame<810)return;
            Check(casts==2,"two X casts");Check(!skill.IsCasting&&GameplayActionPolicy.CanAct(range.player.GetComponent<PlayerItem>(),GameplayAction.Fire),"weapon restored");Check(DreamForestPresentation.Active.Count==0,"all volumes expired");Check(errors==0,"runtime errors="+errors);Check(peak>.001f&&peak<1,"unclipped sound peak="+peak);passed=true;Finish();Debug.Log($"FOREST_CAPTURE_PASS checks={checks} peak={peak} rate={rate} channels={channels}");
        }catch(Exception e){Debug.LogException(e);Finish();}
    }
    static void Finish(){done=true;InputSystem.onAfterUpdate-=Input;Time.captureFramerate=0;if(audio){AudioRenderer.Stop();audio=false;}sound?.Dispose();if(encoder!=null){encoder.StandardInput.Close();encoder.WaitForExit(15000);encoder.Dispose();encoder=null;}File.WriteAllText(Output+"/capture-info.txt",$"passed={passed}\nframes={frame}\nfps=30\nsampleRate={rate}\nchannels={channels}\nRuntimeErrors={errors}\nReal player X input and movement, local server-acceptance fixture. Last 13 seconds exterior inspection camera.\n");EditorApplication.ExitPlaymode();}
    static void State(PlayModeStateChange s){if(SessionState.GetBool(Key,false)&&s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(passed?0:1);}}
}
#endif
