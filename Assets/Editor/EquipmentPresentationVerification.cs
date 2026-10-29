#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class EquipmentPresentationVerification
{
    const string Key="Vector.EquipmentVerification";
    static bool failed;
    static EquipmentPresentationVerification()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("Equipment verification").AddComponent<EquipmentPresentationProbe>();
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}
        };
    }
    public static void AuthorAndRun(){SpikePresentationAuthoring.RebuildFirstPersonRigs();Run();}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        EditorSceneManager.OpenScene("Assets/Scenes/WeaponTest.unity");SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    public static void Finish(Exception error=null)
    {
        failed=error!=null;
        if(failed)Debug.LogException(error);else Debug.Log("EQUIPMENT_PRESENTATION_PASS: four roles, rifle/pistol/knife/device switching, input exclusion, grip contact during move/crouch/jump/turn, skill return, death cancellation.");
        EditorApplication.ExitPlaymode();
    }
}

[DefaultExecutionOrder(30000)]
public sealed class EquipmentPresentationProbe : MonoBehaviour
{
    PracticeRangeController range;
    Keyboard keyboard;Mouse mouse;InputDevice[] physical;
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode editorInput;
    float captureDelta,deadline;
    int role;
    System.Diagnostics.Process video;
    RenderTexture videoTarget;
    Texture2D videoFrame;
    int videoTick;
    readonly System.Collections.Generic.Stack<IEnumerator> work=new();
    string root=>Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT");
    PlayerItem item=>range.inventory;
    SpikeInteractionPresentation device=>range.player.GetComponent<SpikeInteractionPresentation>();
    FirstPersonEquipmentTransition transition=>range.player.GetComponent<FirstPersonEquipmentTransition>();
    Camera camera=>range.player.GetComponent<PracticeWeaponInput>().aimCamera;
    void Check(bool ok,string message){if(!ok)throw new Exception($"Equipment role {role}: {message}");}
    void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    void Start()
    {
        captureDelta=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;deadline=Time.realtimeSinceStartup+300;
        background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        physical=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Mouse)).ToArray();
        foreach(var d in physical)InputSystem.DisableDevice(d);
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
        work.Push(Run());
    }
    void LateUpdate()
    {
        try
        {
            while(work.Count>0)
            {
                var current=work.Peek();
                if(!current.MoveNext()){work.Pop();continue;}
                if(current.Current is IEnumerator nested){work.Push(nested);continue;}
                break;
            }
            if(video!=null && (videoTick++%2)==0)RecordFrame();
        }
        catch(Exception e){enabled=false;EquipmentPresentationVerification.Finish(e);}
    }
    IEnumerator Pause(float seconds)
    {
        float until=Time.time+seconds;
        while(Time.time<until){if(Time.realtimeSinceStartup>deadline)throw new Exception("Equipment check timed out");yield return null;}
        yield return null;
    }
    IEnumerator Select(Key key,int slot)
    {
        var old=item.GetCurrentWeapon();int ammo=old?old.CurrentAmmo:0;
        Keys(key);yield return Pause(.04f);
        Check(transition.Busy,"missing stow/draw");
        old?.OnUse();Check(!old||old.CurrentAmmo==ammo,"shot accepted during switch");
        Check(!GameplayActionPolicy.CanAct(item,GameplayAction.Reload)&&!GameplayActionPolicy.CanAct(item,GameplayAction.Skill),"reload/skill accepted during switch");
        Keys();yield return Pause(.42f);
        Check(item.CurrentIndex==slot&&!transition.Busy,"switch did not finish");
        Check(item.Items.Count(i=>i&&i.gameObject.activeInHierarchy)==(slot==4?0:1),"duplicate/hidden inventory model");
        Check(device.IsCarryingDevice==(slot==4),"device carry state mismatch");
        if(slot==4)CheckGrip();
        if(slot==2)Check(item.GetCurrentWeapon().GetComponent<FirstPersonKnifeGrip>().GripError<.001f,"knife grip detached");
    }
    void CheckGrip()
    {
        var rig=device.firstPerson;
        foreach(string side in new[]{"Left","Right"})
        {
            var hand=rig.bones.First(b=>b.name=="mixamorig:"+side+"Hand");
            var target=rig.device.transform.TransformPoint(new Vector3(side=="Left"?-.17f:.17f,.065f,-.02f));
            float error=Vector3.Distance(hand.position,target);
            Check(error<.012f,$"{side} grip error {error:F4}m");
        }
        var point=camera.WorldToViewportPoint(rig.device.transform.position+rig.device.transform.up*.15f);
        Check(point.z>0&&point.x>.1f&&point.x<.9f&&point.y>0&&point.y<.7f,"device outside lower camera frame");
    }
    IEnumerator Run()
    {
        yield return Pause(2);range=FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;
        for(role=0;role<4;role++)
        {
            range.loadout.SelectCharacter(role);yield return Pause(.5f);
            range.loadout.Confirm();range.EquipRifle();range.FreePractice();yield return Pause(1);
            Check(transition&&transition.model,"saved transition missing");
            var spike=range.player.GetComponent<PlayerSpike>();spike.hasSpike=true;
            item.GiveSpike(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Spike/Spike.prefab"));
            if(role==1)StartVideo();
            yield return Select(Key.Digit2,1);yield return Select(Key.Digit3,2);Capture("Knife");
            Keys(Key.W,Key.Space);yield return Pause(.3f);Keys();
            Check(item.GetCurrentWeapon().GetComponent<FirstPersonKnifeGrip>().GripError<.001f,"moving/jumping knife grip detached");yield return Pause(.7f);
            yield return Select(Key.Digit4,4);Capture("Device");
            var start=range.player.position;Keys(Key.W);yield return Pause(.5f);Keys();
            Check(Vector3.Distance(start,range.player.position)>.2f,"movement fixture did not move");CheckGrip();
            Keys(Key.LeftCtrl);yield return Pause(.08f);Keys();yield return Pause(.5f);CheckGrip();Capture("Crouch");
            Keys(Key.Space);yield return Pause(.08f);Keys();yield return Pause(.2f);CheckGrip();Capture("Jump");
            yield return Pause(.8f);
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(120,-35));yield return Pause(.1f);CheckGrip();
            InputSystem.QueueDeltaStateEvent(mouse.delta,Vector2.zero);
            if(role==1)
            {
                var skill=range.player.GetComponent<ControllerSkill_Q>();int throws=0;
                SkillInventoryState.ApplyServerState(2,0);
                skill.EditorCaptureThrow=(p,v)=>throws++;skill.enabled=true;
                Keys(Key.Q);yield return Pause(.18f);Keys();
                Check(skill.IsCasting&&!device.IsCarryingDevice,"skill/device overlap");Capture("Skill");
                Keys(Key.Digit3);yield return Pause(.08f);Keys();Check(item.CurrentIndex==4,"weapon changed during skill");
                yield return Pause(1);Check(throws==1&&device.IsCarryingDevice,"skill failed to restore device");CheckGrip();skill.enabled=false;
            }
            yield return Select(Key.Digit2,1);Capture("PistolReturn");
            Keys(Key.Digit3);yield return Pause(.03f);Keys(Key.Digit4);yield return Pause(.03f);Keys(Key.Digit1);yield return Pause(.03f);Keys();yield return Pause(.45f);
            Check(item.CurrentIndex==0&&!transition.Busy&&!device.firstPerson.device.activeSelf,"rapid switching retained stale equipment");
            if(role==1)StopVideo();
            Debug.Log($"EQUIPMENT_ROLE_PASS {role}");
        }
        Keys(Key.Digit4);yield return Pause(.04f);range.player.GetComponent<PlayerHP>().SetHp(0);Keys();yield return Pause(.4f);
        Check(!transition.Busy&&!device.IsCarryingDevice&&!device.firstPerson.device.activeSelf,"death retained queued device");
        enabled=false;EquipmentPresentationVerification.Finish();
    }
    void Capture(string name)
    {
        var canvas=range.menu.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;
        camera.aspect=1920f/1080;Canvas.ForceUpdateCanvases();
        var rt=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;camera.targetTexture=rt;RenderTexture.active=rt;camera.Render();
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(root,$"Equipment-{role}-{name}.png"),image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;Destroy(image);Destroy(rt);
    }
    void StartVideo()
    {
        string ffmpeg=Environment.GetEnvironmentVariable("VECTOR_FFMPEG");if(string.IsNullOrEmpty(ffmpeg))return;
        videoTarget=new RenderTexture(1920,1080,24);videoFrame=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        video=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ffmpeg,
            $"-y -loglevel error -f rawvideo -pix_fmt rgb24 -s 1920x1080 -r 30 -i pipe:0 -an -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -vf vflip -movflags +faststart \"{Path.Combine(root,"Equipment-Transitions.mp4")}\"")
            {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true});
    }
    void RecordFrame()
    {
        var previous=RenderTexture.active;camera.targetTexture=videoTarget;RenderTexture.active=videoTarget;camera.Render();
        videoFrame.ReadPixels(new Rect(0,0,1920,1080),0,0);videoFrame.Apply();
        var bytes=videoFrame.GetRawTextureData<byte>().ToArray();video.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
        camera.targetTexture=null;RenderTexture.active=previous;
    }
    void StopVideo()
    {
        if(video==null)return;video.StandardInput.Close();video.WaitForExit(10000);video.Dispose();video=null;
        Destroy(videoFrame);Destroy(videoTarget);
    }
    void OnDestroy()
    {
        StopVideo();
        if(keyboard?.added==true)InputSystem.RemoveDevice(keyboard);if(mouse?.added==true)InputSystem.RemoveDevice(mouse);
        if(physical!=null)foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);
        InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;Time.captureDeltaTime=captureDelta;
    }
}
#endif
