#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Offline presentation capture. Uses production prefabs and receive handlers, NOT a multiplayer test.
// Only the transient batch scene is edited. All HUD layout is instantiated before Play from its saved prefab.
[InitializeOnLoad]
public static class SentinelVideoCapture
{
    const string Key="Vector.SentinelVideo", Folder="C:/Users/USER/Documents/SentinelVideos-20261023";
    const string Encoder="C:/Users/USER/AppData/Local/Temp/SentinelCaptureDeps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    const string Prefabs="Assets/Prefab/Character/Sentinel/";
    static Camera camera;static SentinelCNetworkManager manager;static SentinelFeedbackUI ui;static NetworkManager network;
    static GameObject enemy;static Transform model;static AnimationClip idle;
    static RenderTexture rt;static Texture2D pixels;static System.Diagnostics.Process encoder;static FileStream sound;
    static TcpListener listener;static TcpClient peer;static NetworkClient client;
    static int clip,frame,last=-1,warmup,errors,rate,channels;static bool started,done,passed,audio;static double deadline;
    static Vector3 ground,wall,normal;static float peak;
    static readonly string[] Names={"C-scan","Q-slow","E-concussion","X-overcharge"};
    static SentinelVideoCapture(){EditorApplication.playModeStateChanged+=State;if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;}
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception))errors++;};}
    static void Field(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(o,value);
    static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(o);
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only; do not run in the user's editor.");
        Directory.CreateDirectory(Folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));
        map.GetComponent<MapInfo>().SetBarriersActive(false);
        foreach(var behaviour in map.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
        var cam=new GameObject("CaptureCamera").AddComponent<Camera>();cam.tag="MainCamera";cam.fieldOfView=48;cam.nearClipPlane=.05f;cam.farClipPlane=250;cam.gameObject.AddComponent<AudioListener>();
        cam.clearFlags=CameraClearFlags.Skybox;
        var light=new GameObject("CaptureSun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(48,-30,0);light.shadows=LightShadows.Soft;
        RenderSettings.ambientLight=new Color(.65f,.65f,.68f);
        var net=new GameObject("OfflineReceiveFixture").AddComponent<NetworkManager>();net.enabled=false; // never connects to the real server
        var owner=new GameObject("CaptureOwner").AddComponent<PlayerIdentity>();owner.PlayerId=1;owner.Team=0;owner.transform.position=new Vector3(-33,0,5);
        var mgr=new GameObject("SentinelNetworkPresentation").AddComponent<SentinelCNetworkManager>();
        foreach(var item in new[]{("trapPrefab","Sentinel_Trap"),("qTrapPrefab","SlowTrap"),("eTrapPrefab","TrapDevice"),("xAreaPrefab","X_Area")})ScannerFeedbackAuthoring.Ref(mgr,item.Item1,AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+item.Item2+".prefab"));
        var hud=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/EMP/ElectricStatusHUD.prefab"));
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.5f;
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Controller/Controller.prefab"));actor.name="CaptureEnemy";
        var body=actor.transform.Find("Model");body.gameObject.SetActive(true);
        foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))if(!(b is PlayerIdentity)&&!(b is Outline))Object.DestroyImmediate(b);
        foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;
        foreach(var l in actor.GetComponentsInChildren<AudioListener>(true))l.enabled=false;
        foreach(var r in actor.GetComponentsInChildren<Renderer>(true)){r.enabled=r.transform.IsChildOf(body);r.forceRenderingOff=false;}
        foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
        actor.GetComponent<PlayerIdentity>().PlayerId=2;actor.GetComponent<PlayerIdentity>().Team=1;
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;EditorApplication.EnterPlaymode();
    }
    static void SetCamera(Vector3 position,Vector3 target){camera.transform.position=position;camera.transform.LookAt(target);}
    static void Initialize()
    {
        camera=GameObject.Find("CaptureCamera").GetComponent<Camera>();manager=Object.FindFirstObjectByType<SentinelCNetworkManager>();ui=Object.FindFirstObjectByType<SentinelFeedbackUI>();network=Object.FindFirstObjectByType<NetworkManager>();
        enemy=GameObject.Find("CaptureEnemy");model=enemy.transform.Find("Model");idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/InGame/Pistol/Controller_Pistol_Idle.anim");
        listener=new TcpListener(IPAddress.Loopback,0);listener.Start();client=Get<NetworkClient>(network,"client");client.Connect("127.0.0.1",((IPEndPoint)listener.LocalEndpoint).Port);peer=listener.AcceptTcpClient();
        Field(network,"myPlayerId",1u);Field(network,"<IsAuthenticated>k__BackingField",true);
        Physics.SyncTransforms();ground=new Vector3(-33,0,5);
        if(Physics.Raycast(ground+Vector3.up*12,Vector3.down,out var floor,30,~0,QueryTriggerInteraction.Ignore))ground=floor.point+Vector3.up*.04f;
        bool found=false;
        foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left}){
            foreach(var h in Physics.RaycastAll(ground+Vector3.up*1.3f,direction,35,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
                if(Mathf.Abs(h.normal.y)>.1f||h.collider.GetComponentInParent<PlayerIdentity>())continue;wall=h.point+h.normal*.04f;normal=h.normal;found=true;break;}
            if(found)break;
        }
        if(!found)throw new Exception("No authored TestMap wall found");
        rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
        Time.captureFramerate=30;QualitySettings.vSyncCount=0;Application.runInBackground=true;BeginClip();started=true;
    }
    static void BeginClip()
    {
        frame=0;peak=0;ui.Clear();Field(network,"myPlayerId",1u);manager.ClearAllTraps();
        enemy.transform.SetPositionAndRotation(ground+new Vector3(1.5f,0,4),Quaternion.Euler(0,180,0));
        var cc=enemy.GetComponent<CharacterController>();if(cc){cc.enabled=false;enemy.transform.position-=Vector3.up*(cc.center.y-cc.height*.5f)*enemy.transform.lossyScale.y;}
        SetCamera(ground+new Vector3(-3,2.1f,-4),ground+new Vector3(0,.35f,2));
        if(clip==2){SetCamera(wall+normal*5+Vector3.up*1.6f,wall);enemy.transform.position=wall+normal*2-Vector3.up*1.3f;}
        if(clip==3)SetCamera(ground+new Vector3(-13,13,-15),ground+Vector3.forward*3);
        var info=new System.Diagnostics.ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{Folder}/{Names[clip]}-silent.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
        encoder=System.Diagnostics.Process.Start(info);encoder.ErrorDataReceived+=(o,e)=>{if(e.Data!=null)Debug.LogWarning(e.Data);};encoder.BeginErrorReadLine();
        var config=AudioSettings.GetConfiguration();rate=config.sampleRate;channels=config.speakerMode==AudioSpeakerMode.Mono?1:2;
        sound=new FileStream(Folder+"/"+Names[clip]+".f32le",FileMode.Create);audio=AudioRenderer.Start();
    }
    static void Track(uint id,float seconds){byte[] b=new byte[12];BitConverter.GetBytes(id).CopyTo(b,0);BitConverter.GetBytes(2u).CopyTo(b,4);BitConverter.GetBytes(seconds).CopyTo(b,8);manager.ReceiveTracking(b);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||done)return;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Capture timeout");if(!EditorApplication.isPlaying||last==Time.frameCount)return;last=Time.frameCount;
            if(!started){if(++warmup<15)return;Initialize();return;}
            if(idle)idle.SampleAnimation(model.gameObject,frame/30f);
            if(clip==0){
                if(frame==20)manager.OnSpawn(101,1,0,ground+new Vector3(-1,1.3f,0),new Vector3(1.5f,2,3),40);
                var trap=Object.FindFirstObjectByType<Sentinel_Trap>();
                if(frame==75||frame==165)Track(101,1);
                if((frame==105||frame==195)&&trap)manager.OnTrigger(101,1,0,trap.transform.position,10,2.5f);
            }
            if(clip==1){
                if(frame==20){Field(network,"myPlayerId",2u);manager.OnQSpawn(102,1,0,ground+Vector3.forward*2,40,10,10);}
                if(frame==75)manager.OnQActivate(102,6,.5f);
                if(frame==260)manager.OnQDestroy(102);
            }
            if(clip==2){
                if(frame==20)manager.OnESpawn(103,1,0,wall,normal,100,8);
                if(frame==75||frame==165)manager.OnEActivate(103,8,0,.5f,0);
                if(frame==165){Field(network,"myPlayerId",2u);byte[] b=new byte[24];BitConverter.GetBytes(103u).CopyTo(b,0);BitConverter.GetBytes(2u).CopyTo(b,4);BitConverter.GetBytes(1u).CopyTo(b,8);BitConverter.GetBytes(.5f).CopyTo(b,16);BitConverter.GetBytes(3f).CopyTo(b,20);manager.ReceiveStatus(b);}
                if(frame==270){Field(network,"myPlayerId",1u);manager.OnEDestroy(103,2,20);}
            }
            if(clip==3){
                if(frame==10){manager.OnSpawn(104,1,0,ground+Vector3.forward*2,Vector3.forward,40);manager.OnAnchor(104,ground+Vector3.forward*2,Vector3.up);manager.OnQSpawn(105,1,0,ground+new Vector3(4,0,4),40,10,10);manager.OnESpawn(106,1,0,wall,normal,100,8);}
                if(frame==65){manager.OnXSpawn(201,1,0,ground+Vector3.forward*2,10,5,.5f,10);manager.OnXSpawn(202,1,0,ground+new Vector3(4,0,4),10,5,.5f,10);manager.OnXSpawn(203,1,0,wall,8,5,.5f,10);}
                if(frame==215){manager.OnXDestroy(201);manager.OnXDestroy(202);manager.OnXDestroy(203);}
                if(frame>225)SetCamera(Vector3.Lerp(camera.transform.position,ground+new Vector3(-2,1.6f,-1),.035f),ground+Vector3.forward*2);
            }
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;
            byte[] bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(audio){int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;using(var buffer=new NativeArray<float>(count,Allocator.Temp)){AudioRenderer.Render(buffer);var samples=buffer.ToArray();foreach(var v in samples)peak=Mathf.Max(peak,Mathf.Abs(v));var data=new byte[count*4];Buffer.BlockCopy(samples,0,data,0,data.Length);sound.Write(data,0,data.Length);}}
            if(frame%75==0){File.WriteAllBytes(Folder+"/"+Names[clip]+"-"+frame+".png",pixels.EncodeToPNG());Debug.Log("SENTINEL_VIDEO_FRAME "+Names[clip]+" "+frame);}
            if(++frame<(clip==3?390:300))return;
            EndClip();if(++clip<4){BeginClip();return;}passed=errors==0;Finish();
        }catch(Exception e){Debug.LogException(e);Finish();}
    }
    static void EndClip(){if(audio){AudioRenderer.Stop();audio=false;}sound?.Dispose();sound=null;if(encoder!=null){encoder.StandardInput.Close();if(!encoder.WaitForExit(15000)||encoder.ExitCode!=0)throw new Exception("Encoder failed");encoder.Dispose();encoder=null;}
        File.WriteAllText(Folder+"/"+Names[clip]+"-capture.txt",$"fps=30\nframes={frame}\nsampleRate={rate}\nchannels={channels}\npeak={peak}\nOffline Play-mode presentation. Production prefabs and receive handlers; synthetic server events, not a multiplayer recording.\n");}
    static void Finish(){done=true;if(encoder!=null)EndClip();Time.captureFramerate=0;client?.Close();peer?.Close();listener?.Stop();File.WriteAllText(Folder+"/result.txt",$"passed={passed}\nruntimeErrors={errors}\n");Debug.Log("SENTINEL_VIDEO_FINISHED passed="+passed+" errors="+errors);EditorApplication.ExitPlaymode();}
    static void State(PlayModeStateChange state){if(SessionState.GetBool(Key,false)&&state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(passed?0:1);}}
}
#endif
