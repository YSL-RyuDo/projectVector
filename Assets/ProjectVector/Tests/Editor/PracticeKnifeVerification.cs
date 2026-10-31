#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Opt-in Play-mode check against the saved training scene and its actual rigs.
// No account data or scene changes are saved.
[InitializeOnLoad]
public static class PracticeKnifeVerification
{
    const string Active="Vector.Practice.KnifeVerification";
    static bool failed;
    static PracticeKnifeVerification()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(!SessionState.GetBool(Active,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("Knife verification").AddComponent<PracticeKnifeProbe>();
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Active,false);
                if(!failed && SessionState.GetBool(Active+".Build",false))
                {
                    SessionState.SetBool(Active+".Build",false);
                    try{SpikeRuntimeVerification.Run();RankedCaptureTestEditor.Build();}catch(Exception e){Debug.LogException(e);failed=true;}
                }
                EditorApplication.Exit(failed?1:0);
            }
        };
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene first");
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");
        SessionState.SetBool(Active,true);EditorApplication.EnterPlaymode();
    }
    public static void RunAndBuild(){SessionState.SetBool(Active+".Build",true);Run();}
    public static void Finish(Exception error=null)
    {
        failed=error!=null;if(error!=null)Debug.LogException(error);
        else Debug.Log("PRACTICE_KNIFE_PASS: all four saved character rigs, real slot 3 input, palm grip, cutting edge forward, attack, slot 2 return. No scene/account writes.");
        EditorApplication.ExitPlaymode();
    }
}

[DefaultExecutionOrder(30000)]
public sealed class PracticeKnifeProbe : MonoBehaviour
{
    PracticeRangeController range;
    Keyboard keyboard;
    Mouse mouse;
    InputDevice[] physical;
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode editorInput;
    int role,stage;
    float next,deadline;
    float captureDelta;
    Weapon knife;
    FirstPersonKnifeGrip grip;
    Quaternion idle;
    Transform rig;
    Vector3 originalRigPosition;
    void Start()
    {
        captureDelta=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;
        background=InputSystem.settings.backgroundBehavior;
        editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        physical=InputSystem.devices.Where(d=>d.enabled && (d is Keyboard || d is Mouse)).ToArray();
        foreach(var d in physical)InputSystem.DisableDevice(d);
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();next=Time.time+2;deadline=Time.realtimeSinceStartup+180;
    }
    void Press(Key key)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
    void Release()=>InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    void Check(bool ok,string message){if(!ok)throw new Exception($"Practice role {role}: {message}");}
    void LateUpdate()
    {
        try
        {
            if(Time.realtimeSinceStartup>deadline)throw new Exception("Training knife verification timed out");
            if(Time.time<next)return;
            switch(stage)
            {
                case 0:
                    range=FindFirstObjectByType<PracticeRangeController>();Check(range!=null,"training scene missing");
                    range.persistRecords=false;range.loadout.SelectCharacter(role);stage=1;next=Time.time+.3f;break;
                case 1:
                    if(range.loadout.Switching)return;
                    range.loadout.Confirm();range.EquipPistol();range.FreePractice();stage=2;next=Time.time+1;break;
                case 2:
                    rig=((GameObject)new SerializedObject(range.player.GetComponent<PlayerMovement>()).FindProperty("firstPersonArmModel").objectReferenceValue).transform;
                    originalRigPosition=rig.localPosition;
                    Press(Key.Digit3);stage=3;next=Time.time+.1f;break;
                case 3: Release();stage=4;next=Time.time+.4f;break;
                case 4:
                    knife=range.inventory.GetCurrentWeapon();Check(knife!=null && knife.GetWeaponType()==WeaponData.WeaponType.Knife,$"slot 3 did not equip knife (slot={range.inventory.CurrentIndex}, menu={range.MenuOpen}, allowed={GameplayActionPolicy.CanAct(range.inventory,GameplayAction.WeaponChange)}, keyboard={Keyboard.current?.name})");
                    grip=knife.GetComponent<FirstPersonKnifeGrip>();Check(grip!=null && grip.GripError<.001f,"knife handle detached from palm");
                    var cam=range.player.GetComponent<PracticeWeaponInput>().aimCamera;
                    cam.aspect=1920f/1080;
                    var body=knife.GetComponentsInChildren<MeshFilter>().First().transform;
                    Check(Vector3.Dot(body.right,cam.transform.forward)>.7f,"cutting edge is not forward");
                    Capture("Idle");
                    Check(knife.GetComponentsInChildren<Renderer>().Any(r=>{var p=cam.WorldToViewportPoint(r.bounds.center);Debug.Log($"PRACTICE_KNIFE_BOUNDS {role} {p} cam={cam.transform.position} knife={knife.transform.position} scale={knife.transform.lossyScale}");return r.enabled&&p.z>0&&p.x>0&&p.x<1&&p.y>0&&p.y<1;}),"knife outside player camera");
                    idle=knife.transform.rotation;InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));stage=5;next=Time.time+.12f;break;
                case 5:
                    Check(grip.GripError<.001f,"handle detached during attack");
                    Check(Quaternion.Angle(idle,knife.transform.rotation)>2,"attack did not animate");Capture("Attack");InputSystem.QueueStateEvent(mouse,new MouseState());
                    stage=6;next=Time.time+.6f;break;
                case 6: Press(Key.Digit2);stage=7;next=Time.time+.1f;break;
                case 7: Release();stage=8;next=Time.time+1;break;
                case 8:
                    Check(!knife.gameObject.activeInHierarchy,"knife remains visible after switching");
                    Check(range.inventory.GetCurrentWeapon().gameObject.activeInHierarchy,"pistol not restored");
                    // The existing weapon pose eases toward its pistol root.
                    Check(Vector3.Distance(rig.localPosition,originalRigPosition)<.005f,$"knife pose shifted firearm rig: before={originalRigPosition:F4}, after={rig.localPosition:F4}");
                    Debug.Log($"PRACTICE_KNIFE_ROLE_PASS {role}");
                    if(++role==4){enabled=false;PracticeKnifeVerification.Finish();return;}
                    stage=0;break;
            }
        }
        catch(Exception e){enabled=false;PracticeKnifeVerification.Finish(e);}
    }
    void Capture(string phase)
    {
        var cam=range.player.GetComponent<PracticeWeaponInput>().aimCamera;
        var canvas=range.menu.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=cam.nearClipPlane+1;
        Canvas.ForceUpdateCanvases();
        var rt=new RenderTexture(1920,1080,24);cam.targetTexture=rt;var previous=RenderTexture.active;RenderTexture.active=rt;cam.Render();
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT"),$"Practice-Knife-{role}-{phase}.png"),image.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=previous;Destroy(image);Destroy(rt);
    }
    void OnDestroy()
    {
        if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
        if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);
        if(physical!=null)foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);
        InputSystem.settings.backgroundBehavior=background;
        InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
        Time.captureDeltaTime=captureDelta;
    }
}
#endif
