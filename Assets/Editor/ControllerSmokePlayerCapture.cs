#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

// Local integration capture: the saved practice player, real PlayerMovement input,
// CharacterController collisions, weapon/view rig and production outline callbacks.
// No account/server writes, saved scene changes, free-camera interpolation or UI creation.
[InitializeOnLoad]
public static class ControllerSmokePlayerCapture
{
    const string Key="Vector.SmokePlayerCapture";
    const string Output="C:/Users/GCT/Documents/ControllerSmokeDirectional-20261023";
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    const int Fps=30, Frames=900;
    static readonly string[] Maps={"TestMap","Desert"};
    static PracticeRangeController range;
    static GameObject root, smoke;
    static Transform origin;
    static PlayerMovement movement;
    static Camera camera;
    static PlayerOutline[] enemies;
    static Keyboard keyboard;
    static Mouse mouse;
    static RenderTexture rt;
    static Texture2D pixels;
    static Process encoder;
    static int stage, index, frame, lastFrame=-1, warmup, errors, checks;
    static bool passed, done, smokeStarted, entered, exited;
    static double deadline;
    static StreamWriter telemetry;
    static ControllerSkill_E skill;
    static SmokeTargetingView targeting;
    static Camera mapCamera;
    static Camera uiCamera;
    static Canvas targetingCanvas;
    static RenderTexture mapTexture;
    static Vector2 selectionPoint;
    static int acceptedDrops;
    static Vector2 recordedPointer;
    static int lastClickFrame=-100;
    static bool rightClick;
    static readonly System.Collections.Generic.Dictionary<Graphic,Material> captureUIMaterials=new();
    static RectTransform localMapMarker;
    static string localMapMarkerPath;

    static ControllerSmokePlayerCapture()
    {
        EditorApplication.playModeStateChanged+=State;
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception))errors++;};
        if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+900;}
    }
    static void Check(bool ok,string label){if(!ok)throw new Exception("SMOKE_PLAYER_FAIL "+label);checks++;}
    static string Role(PlayerOutline actor) => actor.GetComponentInParent<CharacterController>().name;
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only: never replace an interactive scene");
        Directory.CreateDirectory(Output);
        var source=EditorSceneManager.OpenScene(ControllerSmokeAuthoring.ScenePath);
        int savedMapMask=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MiniMapCamera>(true)).Single().GetComponent<Camera>().cullingMask;
        Check(savedMapMask==LayerMask.GetMask("MiniMap"),"saved placement camera renders only map geometry");
        var savedView=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SmokeTargetingView>(true)).Single();
        var savedManager=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MiniMapManager>(true)).Single();
        var savedMarker=(RectTransform)new SerializedObject(savedManager).FindProperty("_overlayPlayerMarker").objectReferenceValue;
        localMapMarkerPath=AnimationUtility.CalculateTransformPath(savedMarker,savedView.transform);
        SessionState.SetString(Key+".LocalMarker",localMapMarkerPath);
        var copy=Object.Instantiate(savedView.gameObject);copy.name="CaptureSavedSmokeTargeting";copy.SetActive(false);
        // Reuse the authored targeting controls, never author replacement runtime UI.
        var canvas=copy.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
        var scaler=copy.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        foreach(var animator in copy.GetComponentsInChildren<UIAnimator>(true))animator.enabled=false;
        var group=copy.GetComponent<CanvasGroup>();if(group)group.alpha=1;
        var practiceScene=EditorSceneManager.OpenScene("Assets/Scenes/WeaponTest.unity",OpenSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(copy,practiceScene);EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(practiceScene);
        var minimap=new GameObject("CaptureMiniMapCamera").AddComponent<Camera>();minimap.enabled=false;minimap.orthographic=true;minimap.orthographicSize=32;minimap.nearClipPlane=.1f;minimap.farClipPlane=300;minimap.cullingMask=savedMapMask;
        var ui=new GameObject("CaptureOverlayCamera").AddComponent<Camera>();ui.enabled=false;ui.clearFlags=CameraClearFlags.Depth;ui.cullingMask=1<<5;
        var r=Object.FindFirstObjectByType<PracticeRangeController>();r.persistRecords=false;
        for(int i=0;i<Maps.Length;i++)Prepare(Maps[i],i);
        SessionState.SetBool(Key,true);deadline=EditorApplication.timeSinceStartup+900;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
    }
    static GameObject Load(string path)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    static bool Ground(Vector3 point,out Vector3 result)
    {
        if(Physics.Raycast(point+Vector3.up*4,Vector3.down,out var hit,10,~0,QueryTriggerInteraction.Ignore)&&hit.normal.y>.97f)
        {result=hit.point;return true;}result=default;return false;
    }
    static void Prepare(string name,int mapIndex)
    {
        var holder=new GameObject("SmokePlayer_"+name);
        var map=Load("Assets/Prefab/Maps/"+name+".prefab");map.transform.SetParent(holder.transform);
        map.transform.SetPositionAndRotation(new Vector3(1000+mapIndex*1000,0,0),Quaternion.identity);
        var info=map.GetComponent<MapInfo>();info.SetBarriersActive(false);Physics.SyncTransforms();
        Vector3 center=default,forward=default;bool found=false;
        foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
        {
            for(int radius=0;radius<=20&&!found;radius+=4)
            for(int offset=0;offset<8&&!found;offset++)
            {
                var point=spawn.position+Quaternion.Euler(0,offset*45,0)*Vector3.forward*radius;
                if(!Ground(point,out var g))continue;
                for(int angle=0;angle<360&&!found;angle+=30)
                {
                    var f=Quaternion.Euler(0,angle,0)*Vector3.forward;var right=Vector3.Cross(Vector3.up,f);bool clear=true;
                    for(int z=-8;z<=8&&clear;z+=2)
                    foreach(float x in new[]{-1.5f,0,1.5f})
                    {
                        if(!Ground(g+f*z+right*x,out var floor)||Mathf.Abs(floor.y-g.y)>.25f){clear=false;break;}
                    }
                    foreach(float x in new[]{-1.5f,0,1.5f})
                        if(Physics.Linecast(g-f*8+right*x+Vector3.up*1.5f,g+f*8+right*x+Vector3.up*1.5f,~0,QueryTriggerInteraction.Ignore))clear=false;
                    if(clear){center=g;forward=f;found=true;}
                }
            }
            if(found)break;
        }
        Check(found,name+" flat walkable corridor");
        var anchor=new GameObject("RouteOrigin").transform;anchor.SetParent(holder.transform);anchor.SetPositionAndRotation(center,Quaternion.LookRotation(forward));
        var effect=Load(ControllerSmokeAuthoring.Dir+"/ControllerSmoke.prefab");effect.name="Smoke";effect.transform.SetParent(holder.transform);effect.transform.position=center;effect.transform.localScale=Vector3.one*4.5f;effect.SetActive(false);
        Enemy(holder.transform,anchor.TransformPoint(new Vector3(-1.3f,0,0)),forward,"EnemyInside");
        Enemy(holder.transform,anchor.TransformPoint(new Vector3(-1f,0,8)),forward,"EnemyBehind");
        Enemy(holder.transform,anchor.TransformPoint(new Vector3(1.3f,0,0)),forward,"AllyInside");
        Enemy(holder.transform,anchor.TransformPoint(new Vector3(1f,0,8)),forward,"AllyBehind");
        Debug.Log($"SMOKE_PLAYER_ROUTE {name} center={center} forward={forward}");
        foreach(var b in map.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
        holder.SetActive(false);
    }
    static void Enemy(Transform parent,Vector3 position,Vector3 forward,string name)
    {
        var actor=Load(ControllerSmokeAuthoring.Actor);actor.name=name;actor.transform.SetParent(parent);
        var cc=actor.GetComponent<CharacterController>();float bottom=(cc.center.y-cc.height*.5f)*actor.transform.lossyScale.y;
        actor.transform.SetPositionAndRotation(position-Vector3.up*bottom,Quaternion.LookRotation(-forward));
        var model=actor.transform.Find("Model");model.gameObject.SetActive(true);
        foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))if(!(b is PlayerOutline)&&!(b is Outline))Object.DestroyImmediate(b);
        foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.gameObject.SetActive(false);
        foreach(var r in actor.GetComponentsInChildren<Renderer>(true)){r.enabled=r.transform.IsChildOf(model);r.forceRenderingOff=false;}
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character(New)/Controller/Animation/InGame/Pistol/Controller_Pistol_Idle.anim");clip.SampleAnimation(model.gameObject,0);
        foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
        foreach(var listener in actor.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;
    }
    static void DriveInput()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic||keyboard==null)return;
        KeyboardState state=new KeyboardState();
        var mouseState=new MouseState();
        if(stage==3&&!done)
        {
            float t=frame/(float)Fps;
            float z=origin.InverseTransformPoint(range.player.position).z;
            if(frame==60)state=new KeyboardState(UnityEngine.InputSystem.Key.E);
            if(frame>=61&&frame<=165)
                mouseState.position=selectionPoint+Vector2.Lerp(new Vector2(0,-42),Vector2.zero,Mathf.SmoothStep(0,1,(frame-65)/50f));
            if(frame>125&&frame<150)mouseState.position=selectionPoint+new Vector2((frame-125)*1.5f,0);
            if(frame==120)mouseState=new MouseState{position=selectionPoint}.WithButton(MouseButton.Left);
            if(frame==150)mouseState=new MouseState{position=selectionPoint}.WithButton(MouseButton.Right);
            if(t>=8&&t<12&&z< -2.1f)state=new KeyboardState(UnityEngine.InputSystem.Key.W);
            if(t>=14&&t<18&&z<5f)state=new KeyboardState(UnityEngine.InputSystem.Key.W);
            if(t>=19&&t<20)
            {
                float sensitivity=.2f*MouseSettings.GetSensitivity(SensitivityType.General)*MouseSettings.GetSensitivity(SensitivityType.Horizontal);
                mouseState.delta=new Vector2(180f/(Fps*Mathf.Max(.0001f,sensitivity)),0);
            }
        }
        keyboard.MakeCurrent();InputState.Change(keyboard,state);
        mouse.MakeCurrent();InputState.Change(mouse,mouseState);
        recordedPointer=mouse.position.ReadValue();
        if(mouse.leftButton.isPressed||mouse.rightButton.isPressed)
        {
            lastClickFrame=frame;rightClick=mouse.rightButton.isPressed;
            Debug.Log($"SMOKE_CAPTURE_POINTER frame={frame} position={recordedPointer} button={(rightClick?"RMB":"LMB")}");
        }
    }
    static void BeginMap()
    {
        root=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="SmokePlayer_"+Maps[index]);root.SetActive(true);
        origin=root.transform.Find("RouteOrigin");smoke=root.transform.Find("Smoke").gameObject;
        enemies=root.GetComponentsInChildren<PlayerOutline>();foreach(var e in enemies)e.Configure(false,Role(e).StartsWith("Ally"));
        var cc=range.player.GetComponent<CharacterController>();cc.enabled=false;
        float bottom=(cc.center.y-cc.height*.5f)*range.player.lossyScale.y;
        range.player.position=origin.TransformPoint(new Vector3(0,-bottom+.03f,-8));cc.enabled=true;
        range.player.GetComponentInChildren<FPSCameraLook>().ResetLookRotation(origin.eulerAngles.y);
        var mapController=mapCamera.GetComponent<MiniMapCamera>();mapController.ClearSmokeHeading();mapController.SetOverlayMapCenter(origin);mapController.SetOverlayMode(true);mapCamera.Render();
        VerifyMapPrivacy();
        SkillInventoryState.ApplyServerState(4,0);acceptedDrops=0;lastClickFrame=-100;
        Physics.SyncTransforms();smokeStarted=entered=exited=false;frame=0;warmup=20;stage=2;
        telemetry=new StreamWriter(Output+"/"+Maps[index]+"-checks.csv");telemetry.WriteLine("frame,time,playerZ,speed,cameraX,cameraY,cameraZ,smokeActive,enemyInsideHidden,enemyBehindHidden,allyInsideHidden,allyBehindHidden");
        var start=new ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate {Fps} -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p -movflags +faststart \"{Output}/{Maps[index]}-player-smoke.mp4\"")
        {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
        encoder=new Process{StartInfo=start};encoder.ErrorDataReceived+=(s,e)=>{if(!string.IsNullOrEmpty(e.Data))Debug.LogWarning("ENCODER "+e.Data);};encoder.Start();encoder.BeginErrorReadLine();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||done)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Capture timeout");
            if(!EditorApplication.isPlaying||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            if(stage==0)
            {
                if(++warmup<40)return;
                range=Object.FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;
                InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onAfterUpdate+=DriveInput;
                Time.captureFramerate=Fps;QualitySettings.vSyncCount=0;Application.runInBackground=true;
                range.loadout.SelectCharacter(1);stage=1;return;
            }
            if(stage==1)
            {
                if(range.loadout.Switching)return;
                range.loadout.EquipWeapon(Array.IndexOf(range.loadout.weapons,range.rifle));range.loadout.Confirm();
                range.spawner.StopTraining();range.menu.SetActive(false);range.trainingZones=Array.Empty<PracticeTrainingZone>();range.enabled=false;
                movement=range.player.GetComponent<PlayerMovement>();
                camera=range.player.GetComponent<PlayerCameraController>().PlayerCamera;
                Check(movement.IsMine&&camera,"actual local player and camera");
                camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
                rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
                // Hide training instruction panels only in this unsaved diagnostic scene.
                foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))c.enabled=false;
                targeting=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="CaptureSavedSmokeTargeting").GetComponent<SmokeTargetingView>();
                localMapMarker=targeting.transform.Find(SessionState.GetString(Key+".LocalMarker","")) as RectTransform;
                GameObject.Find("CaptureOverlayCamera").SetActive(false);
                uiCamera=camera;camera.cullingMask|=1<<5;
                targetingCanvas=targeting.GetComponent<Canvas>();targetingCanvas.enabled=true;targetingCanvas.worldCamera=camera;targetingCanvas.planeDistance=.4f;
                // Render the saved overlay into the offscreen player view. Preserve
                // ScreenSpaceOverlay ordering above world outlines in this capture only.
                mapCamera=GameObject.Find("CaptureMiniMapCamera").GetComponent<Camera>();mapTexture=new RenderTexture(1024,1024,24);mapCamera.targetTexture=mapTexture;targeting.Map.GetComponent<RawImage>().texture=mapTexture;
                var mapController=mapCamera.gameObject.AddComponent<MiniMapCamera>();mapController.enabled=false;mapController.SetTarget(range.player);mapController.SetMapFraming(32,32,100,100);
                skill=range.player.GetComponent<ControllerSkill_E>();skill.EditorCaptureDrop=AcceptLocalDrop;skill.enabled=true;
                var skillData=new SerializedObject(skill);skillData.FindProperty("_miniMapCamera").objectReferenceValue=mapCamera;skillData.ApplyModifiedPropertiesWithoutUndo();
                BeginMap();return;
            }
            if(stage==2){if(--warmup>0)return;stage=3;Debug.Log("SMOKE_PLAYER_BEGIN "+Maps[index]);}
            float t=frame/(float)Fps;
            uiCamera.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
            if(targeting.gameObject.activeSelf)
            {
                mapCamera.Render();
                if(localMapMarker)
                {
                    var playerUV=mapCamera.WorldToViewportPoint(range.player.position);var parent=(RectTransform)localMapMarker.parent;
                    localMapMarker.anchoredPosition=new Vector2((playerUV.x-.5f)*parent.rect.width,(playerUV.y-.5f)*parent.rect.height);
                }
                foreach(var text in targeting.GetComponentsInChildren<TMPro.TMP_Text>(true))text.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();var uv=mapCamera.WorldToViewportPoint(origin.position);
                var rect=targeting.Map.rect;var local=new Vector3(Mathf.Lerp(rect.xMin,rect.xMax,uv.x),Mathf.Lerp(rect.yMin,rect.yMax,uv.y),0);
                selectionPoint=RectTransformUtility.WorldToScreenPoint(uiCamera,targeting.Map.TransformPoint(local));
                // Fallback glyph submeshes are created by TMP after text changes.
                // Include them, with the authored sibling order, above world outlines.
                int order=0;
                foreach(var graphic in targeting.GetComponentsInChildren<Graphic>(true))
                {
                    if(!captureUIMaterials.TryGetValue(graphic,out var material))
                    {
                        var source=graphic is TMPro.TMP_Text label?label.fontSharedMaterial:
                            graphic is TMPro.TMP_SubMeshUI glyphs?glyphs.sharedMaterial:graphic.material;
                        material=new Material(source);captureUIMaterials[graphic]=material;
                    }
                    material.renderQueue=4000+order++;
                    if(graphic is TMPro.TMP_SubMeshUI sub)sub.sharedMaterial=material;
                    else if(graphic is TMPro.TMP_Text text)text.fontSharedMaterial=material;
                    else graphic.material=material;
                }
            }
            if(frame==90)Check(ControllerSkill_E.IsSelectingSmoke&&range.player.GetComponent<ControllerSmokeMotion>().Active,"E input opened authored map and hand gesture");
            if(frame==100)
            {
                var hover=targeting.GetComponentInChildren<SmokeTargetRing>();
                Check(hover,"hover footprint visible before left click");
                Check(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(uiCamera,hover.transform.position),recordedPointer)<2,"footprint tracks actual pointer");
            }
            if(frame==135)Check(targeting.GetComponentsInChildren<SmokeTargetRing>().Length==1,"left click selected visible drop footprint");
            if(frame==180)Check(acceptedDrops==1&&!ControllerSkill_E.IsBlockingWeaponInput,"right click accepted once and restored weapon input");
            camera.Render();
            RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;
            // Recording annotation only: camera render textures omit the OS pointer.
            // Draw the actual InputSystem coordinates, never an inferred click target.
            if(frame>=62&&frame<=165)AnnotatePointer();
            var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            float z=origin.InverseTransformPoint(range.player.position).z;
            if(t>=8&&z> -2.3f)entered=true;if(t>=14&&z>4.8f)exited=true;
            bool active=smoke&&smoke.activeInHierarchy&&smoke.GetComponent<SmokeOutlineOccluder>().enabled;
            var states=enemies.Select(e=>e.GetComponentInChildren<Outline>(true).SmokeSuppressed).ToArray();
            if(frame%30==0)
            {
                foreach(var e in enemies)
                {
                    var o=e.GetComponentInChildren<Outline>(true);Check(o.enabled,"outline remains enabled for visibility callback");
                    bool viewerInside=active&&(camera.transform.position-smoke.transform.position).sqrMagnitude<4.5f*4.5f;
                    bool expectedHidden=false;
                    if(active&&!Role(e).StartsWith("Ally"))
                        expectedHidden=Role(e)=="EnemyInside"?!viewerInside:z<4.5f;
                    Check(o.SmokeSuppressed==expectedHidden,Maps[index]+" "+Role(e)+" suppression at "+t);
                    foreach(var r in o.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
                        Check(r.sharedMaterials.Count(m=>m&&m.name.StartsWith("Outline"))==(expectedHidden?0:2),"actual outline material passes at "+t);
                }
            }
            var p=camera.transform.position;telemetry.WriteLine($"{frame},{t:F3},{z:F3},{movement.PlanarSpeed:F3},{p.x:F3},{p.y:F3},{p.z:F3},{active},{string.Join(",",states)}");
            if(frame%90==0||frame==125||frame==155){File.WriteAllBytes(Output+"/"+Maps[index]+"-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());Debug.Log($"SMOKE_PLAYER_FRAME {Maps[index]} {frame} z={z:F2} speed={movement.PlanarSpeed:F2} suppressed={string.Join(",",states)}");}
            if(++frame<Frames)return;
            Check(smokeStarted&&entered&&exited,"real movement reached inside and returned outside");
            telemetry.Dispose();telemetry=null;encoder.StandardInput.Close();Check(encoder.WaitForExit(45000)&&encoder.ExitCode==0,"encoder");encoder.Dispose();encoder=null;
            root.SetActive(false);Debug.Log("SMOKE_PLAYER_MAP_PASS "+Maps[index]);
            if(++index<Maps.Length){BeginMap();return;}
            Check(errors==0,"no runtime errors: "+errors);passed=done=true;InputSystem.onAfterUpdate-=DriveInput;Time.captureFramerate=0;
            Debug.Log("SMOKE_PLAYER_PASS checks="+checks);EditorApplication.ExitPlaymode();
        }
        catch(Exception ex){done=true;Debug.LogException(ex);telemetry?.Dispose();if(encoder!=null&&!encoder.HasExited)encoder.StandardInput.Close();InputSystem.onAfterUpdate-=DriveInput;EditorApplication.ExitPlaymode();}
    }
    static void VerifyMapPrivacy()
    {
        var actors=root.GetComponentsInChildren<CharacterController>().SelectMany(c=>c.GetComponentsInChildren<Renderer>(true))
            .Concat(range.player.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();
        foreach(var r in actors)Check((mapCamera.cullingMask&(1<<r.gameObject.layer))==0,"character/weapon excluded from placement map: "+r.name);
        var before=ReadMapPixels();var flags=actors.Select(r=>r.forceRenderingOff).ToArray();
        try
        {
            foreach(var r in actors)r.forceRenderingOff=true;
            mapCamera.Render();var withoutActors=ReadMapPixels();
            Check(before.SequenceEqual(withoutActors),Maps[index]+" map pixels unchanged when actors are removed");
        }
        finally {for(int i=0;i<actors.Length;i++)actors[i].forceRenderingOff=flags[i];mapCamera.Render();}
    }
    static byte[] ReadMapPixels()
    {
        var old=RenderTexture.active;var readback=new Texture2D(mapTexture.width,mapTexture.height,TextureFormat.RGB24,false);
        RenderTexture.active=mapTexture;readback.ReadPixels(new Rect(0,0,mapTexture.width,mapTexture.height),0,0);readback.Apply(false);RenderTexture.active=old;
        var result=readback.GetRawTextureData<byte>().ToArray();Object.DestroyImmediate(readback);return result;
    }
    static void AnnotatePointer()
    {
        int cx=Mathf.RoundToInt(recordedPointer.x),cy=Mathf.RoundToInt(recordedPointer.y);
        int clickAge=frame-lastClickFrame;
        float radius=16+clickAge*.85f;
        var clickColor=rightClick?new Color(1,.68f,.16f):new Color(.15f,.95f,1);
        for(int y=-36;y<=36;y++)for(int x=-36;x<=36;x++)
        {
            int px=cx+x,py=cy+y;if(px<0||py<0||px>=pixels.width||py>=pixels.height)continue;
            if(clickAge>=0&&clickAge<15&&Mathf.Abs(Mathf.Sqrt(x*x+y*y)-radius)<2)
                pixels.SetPixel(px,py,clickColor);
            // High contrast arrow, tip exactly at the recorded input position.
            int down=-y;
            if(down>=0&&down<=26&&x>=0&&x<=down*.62f)
                pixels.SetPixel(px,py,x<2||down>23||x>down*.62f-2?Color.black:Color.white);
        }
        pixels.Apply(false);
    }
    static void AcceptLocalDrop(Vector3 position)
    {
        Check(Vector3.Distance(position,origin.position)<.2f,"selected ground matches real map click");
        Check(!smokeStarted&&acceptedDrops==0,"drop requested exactly once");
        acceptedDrops++;SkillInventoryState.ApplyServerState(4,4);
        smoke.transform.position=position;smoke.SetActive(true);smoke.GetComponent<ControllerSmokeVfx>().Begin(18);smokeStarted=true;
        Debug.Log("SMOKE_PLAYER_LOCAL_DROP "+position+" (local acceptance fixture, no server)");
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(passed?0:1);}
    }
}
#endif
