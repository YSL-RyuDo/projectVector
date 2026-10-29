#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug=UnityEngine.Debug;
using Object=UnityEngine.Object;

// Isolated, unsaved Play-mode capture. No input automation or gameplay scene edits.
[InitializeOnLoad]
public static class ControllerSmokeVideoCapture
{
    const string Key="Vector.ControllerSmokeVideo", Output="C:/Users/GCT/Documents/ControllerSmokeVideo-20261023";
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    const int Fps=30,Frames=540;
    static readonly string[] Maps={"TestMap","Desert"};
    static GameObject root;
    static Camera camera;
    static Transform outside,inside;
    static RenderTexture rt;
    static Texture2D pixels;
    static Process encoder;
    static int mapIndex,frame,lastUnityFrame=-1;
    static bool ready,done,passed;
    static double deadline;
    static ControllerSmokeVideoCapture()
    {
        EditorApplication.playModeStateChanged+=State;
        if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;}
    }
    static GameObject Load(string path)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch capture only");
        if(!File.Exists(Encoder))throw new FileNotFoundException("Video encoder missing",Encoder);
        Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.white*.7f;
        var sun=new GameObject("CaptureSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.4f;sun.transform.rotation=Quaternion.Euler(48,-30,0);
        foreach(string name in Maps)Prepare(name);
        var c=new GameObject("SmokeCaptureCamera").AddComponent<Camera>();c.enabled=false;c.fieldOfView=80;c.nearClipPlane=.03f;c.farClipPlane=500;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.28f,.38f,.48f);c.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+600;
        EditorApplication.EnterPlaymode();
    }
    static void Prepare(string name)
    {
        var holder=new GameObject("SmokeCapture_"+name);
        var map=Load("Assets/Prefab/Maps/"+name+".prefab");map.transform.SetParent(holder.transform);
        var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);
        var spawn=info.attackSpawnPoints[0];var center=spawn.position+spawn.forward*6;
        Physics.SyncTransforms();if(Physics.Raycast(center+Vector3.up*12,Vector3.down,out var hit,40,~0,QueryTriggerInteraction.Ignore))center=hit.point;
        Vector3 forward=Vector3.forward;
        for(int angle=0;angle<360;angle+=30){var candidate=Quaternion.Euler(0,angle,0)*Vector3.forward;
            if(!Physics.Linecast(center-candidate*9+Vector3.up*2.6f,center+Vector3.up*2.6f,~0,QueryTriggerInteraction.Ignore)){forward=candidate;break;}}
        var smoke=Load(ControllerSmokeAuthoring.Dir+"/ControllerSmoke.prefab");smoke.transform.SetParent(holder.transform);smoke.transform.position=center;smoke.transform.localScale=Vector3.one*4.5f;
        var actor=Load(ControllerSmokeAuthoring.Actor);var model=actor.transform.Find("Model");
        var person=Object.Instantiate(model.gameObject);Object.DestroyImmediate(actor);person.name="VisibleInsideCharacter";person.transform.SetParent(holder.transform);
        person.transform.SetPositionAndRotation(center+forward*1.1f,Quaternion.LookRotation(-forward));person.SetActive(true);
        foreach(var r in person.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.forceRenderingOff=false;}
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/InGame/Pistol/Controller_Pistol_Idle.anim");clip.SampleAnimation(person,0);
        foreach(var animator in person.GetComponentsInChildren<Animator>(true))animator.enabled=false;
        var bounds=person.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds;
        foreach(var skin in person.GetComponentsInChildren<SkinnedMeshRenderer>())bounds.Encapsulate(skin.bounds);
        float eye=Mathf.Clamp(bounds.size.y*.78f,1.6f,2.8f);
        var outView=new GameObject("OutsideView").transform;outView.SetParent(holder.transform);outView.position=center-forward*9+Vector3.up*eye;outView.LookAt(bounds.center);
        var inView=new GameObject("InsideView").transform;inView.SetParent(holder.transform);inView.position=center-forward*2+Vector3.up*eye;inView.LookAt(bounds.center);
        // The presentation fixture contains only map geometry, the posed model,
        // and the real smoke behaviour. No network/player gameplay is simulated.
        foreach(var b in holder.GetComponentsInChildren<MonoBehaviour>(true))if(!(b is ControllerSmokeVfx)&&!(b is SmokeOutlineOccluder))Object.DestroyImmediate(b);
        holder.SetActive(false);
    }
    static void BeginMap()
    {
        root=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="SmokeCapture_"+Maps[mapIndex]);
        outside=root.transform.Find("OutsideView");inside=root.transform.Find("InsideView");
        root.SetActive(true);root.GetComponentInChildren<ControllerSmokeVfx>().Begin(18);
        frame=0;
        var start=new ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate {Fps} -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p -movflags +faststart \"{Output}/{Maps[mapIndex]}-smoke-walkthrough.mp4\"");
        start.UseShellExecute=false;start.CreateNoWindow=true;start.RedirectStandardInput=true;start.RedirectStandardError=true;
        encoder=new Process{StartInfo=start};encoder.ErrorDataReceived+=(s,e)=>{if(!string.IsNullOrEmpty(e.Data))Debug.LogWarning("ENCODER "+e.Data);};encoder.Start();encoder.BeginErrorReadLine();
        Debug.Log("SMOKE_VIDEO_BEGIN "+Maps[mapIndex]);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||done)return;
        if(EditorApplication.timeSinceStartup>deadline){Fail(new Exception("Capture timeout"));return;}
        if(!EditorApplication.isPlaying||Time.frameCount==lastUnityFrame)return;
        lastUnityFrame=Time.frameCount;
        try{
            if(!ready){Time.captureFramerate=Fps;Application.runInBackground=true;QualitySettings.vSyncCount=0;
                camera=GameObject.Find("SmokeCaptureCamera").GetComponent<Camera>();rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;ready=true;BeginMap();return;}
            float t=frame/(float)Fps;
            float enter=t<4?0:t<7?Mathf.SmoothStep(0,1,(t-4)/3):t<12?1:t<15?1-Mathf.SmoothStep(0,1,(t-12)/3):0;
            camera.transform.SetPositionAndRotation(Vector3.Lerp(outside.position,inside.position,enter),Quaternion.Slerp(outside.rotation,inside.rotation,enter));
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;
            var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            if(frame%150==0){File.WriteAllBytes(Output+"/"+Maps[mapIndex]+"-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());var properties=new MaterialPropertyBlock();root.GetComponentInChildren<ControllerSmokeVfx>().GetComponentInChildren<MeshRenderer>().GetPropertyBlock(properties);Debug.Log("SMOKE_VIDEO_FRAME "+Maps[mapIndex]+" "+frame+" effectAge="+properties.GetFloat("_CloudTime"));}
            frame++;
            if(frame==Frames){encoder.StandardInput.Close();if(!encoder.WaitForExit(45000)||encoder.ExitCode!=0)throw new Exception("Encoder failed");encoder.Dispose();encoder=null;root.SetActive(false);
                Debug.Log("SMOKE_VIDEO_MAP_PASS "+Maps[mapIndex]+" frames="+frame);
                mapIndex++;if(mapIndex<Maps.Length){BeginMap();return;}
                done=passed=true;Time.captureFramerate=0;Debug.Log("SMOKE_VIDEO_PASS");EditorApplication.ExitPlaymode();}
        }catch(Exception ex){Fail(ex);}
    }
    static void Fail(Exception ex){Debug.LogException(ex);done=true;passed=false;if(encoder!=null&&!encoder.HasExited){encoder.StandardInput.Close();}EditorApplication.ExitPlaymode();}
    static void State(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(passed?0:1);}}
}
#endif
