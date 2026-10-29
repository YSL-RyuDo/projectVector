#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Batch-only regression harness. No account writes or live server connection.
[InitializeOnLoad]
public static class ControllerMotionPlayVerification
{
    const string Key="Vector.Controller.MotionVerification";
    static PracticeRangeController range;
    static Mouse mouse;
    static bool aim;
    static int stage, character, weaponIndex, errors;
    static bool failed;
    static double next, deadline;
    static ControllerMotionPlayVerification()
    {
        EditorApplication.playModeStateChanged+=State;
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false) && (t==LogType.Error || t==LogType.Exception))errors++;};
        if(SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+300;EditorApplication.update+=Tick;}
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene");
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");SessionState.SetBool(Key,true);
        deadline=EditorApplication.timeSinceStartup+300;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){stage=0;next=EditorApplication.timeSinceStartup+2;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Motion test timeout");
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next)return;
            if(stage==0)
            {
                range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;
                Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
                InputSystem.settings=UnityEngine.Object.Instantiate(InputSystem.settings);
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
                mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
                InputSystem.onAfterUpdate+=DriveInput;
                range.loadout.SelectCharacter(character);stage=1;next=EditorApplication.timeSinceStartup+.5;
            }
            else if(stage==1)
            {
                if(range.loadout.Switching)return;
                range.loadout.EquipWeapon(weaponIndex);
                range.loadout.Confirm();range.ReturnToStation();stage=2;next=EditorApplication.timeSinceStartup+.4;
            }
            else if(stage==2)
            {
                var camera=range.player.GetComponent<PracticeWeaponInput>().aimCamera;
                if(range.inventory.GetCurrentWeapon().Data==range.rifle)ControllerModelIntegrationEditor.Capture(camera,"character-"+character+"-hip");
                aim=true;
                stage=3;next=EditorApplication.timeSinceStartup+.7;
            }
            else if(stage==3)
            {
                var camera=range.player.GetComponent<PracticeWeaponInput>().aimCamera;
                var held=range.inventory.GetCurrentWeapon();var data=held.Data;
                bool canAim=data.weaponType!=WeaponData.WeaponType.Pistol && data.weaponType!=WeaponData.WeaponType.Shotgun && data.weaponType!=WeaponData.WeaponType.Knife;
                Check(held.IsAiming==canAim,"Wrong ADS eligibility: "+data.name);
                Check(Mathf.Abs(camera.fieldOfView-(canAim?data.adsFOV:data.normalFOV))<.15f,$"ADS FOV role={character} weapon={data.name}: actual={camera.fieldOfView}");
                if(canAim)
                {
                    var point=held.GetComponent<WeaponADS>().SightReference;Check(point!=null,"Missing optical reference");
                    var viewport=camera.WorldToViewportPoint(point.position);
                    Check(Mathf.Abs(viewport.x-.5f)<.001f && Mathf.Abs(viewport.y-.5f)<.001f,$"Optic not centred: role={character} weapon={data.name} viewport={viewport:F6}");
                    Check(Vector3.Dot(point.forward,camera.transform.forward)>.9999f,"Optic not parallel to shot ray");
                    Debug.Log($"ADS_ALIGNMENT_PASS role={character} weapon={data.name} viewport={viewport:F6}");
                    ControllerModelIntegrationEditor.Capture(camera,"character-"+character+"-"+data.name+"-ads");
                    if(data==range.rifle)ControllerModelIntegrationEditor.Capture(camera,"character-"+character+"-ads");
                }
                aim=false;stage=4;next=EditorApplication.timeSinceStartup+.6;
            }
            else if(stage==4)
            {
                var camera=range.player.GetComponent<PracticeWeaponInput>().aimCamera;
                Check(Mathf.Abs(camera.fieldOfView-range.inventory.GetCurrentWeapon().Data.normalFOV)<.15f,"ADS exit did not converge");
                Check(!range.inventory.GetCurrentWeapon().IsAiming,"ADS flag stuck after release");
                if(++weaponIndex<range.loadout.weapons.Length){range.OpenMenu();stage=1;return;}
                weaponIndex=0;Debug.Log("CONTROLLER_ADS_PASS character="+character);
                if(++character<4){range.OpenMenu();range.loadout.SelectCharacter(character);stage=1;next=EditorApplication.timeSinceStartup+.5;return;}
                range.OpenMenu();var target=range.spawner.characterTargets[1];target.gameObject.SetActive(true);
                var animator=target.animator;animator.Rebind();animator.Update(0);
                foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    var paths=AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Transform)).Select(b=>b.path).Distinct().ToArray();
                    Check(paths.Length>0 && paths.All(p=>p.Length==0 || animator.transform.Find(p)!=null),"Unbound motion: "+clip.name);
                }
                var bones=target.model.GetComponentsInChildren<Transform>(true);
                var before=bones.Select(b=>b.localRotation).ToArray();target.SetMoving(true);animator.Update(.6f);
                Check(bones.Where((b,i)=>Quaternion.Angle(before[i],b.localRotation)>.1f).Any(),"Controller walk pose did not animate");
                InputSystem.onAfterUpdate-=DriveInput;InputSystem.RemoveDevice(mouse);Check(errors==0,"Runtime errors="+errors);
                Debug.Log("CONTROLLER_MOTION_PLAY_PASS: 4 characters x 14 weapons, 32 optical centre/ray checks, ADS restrictions and enter/exit, controller curve bindings and walking; no account writes");
                stage=99;EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception e){failed=true;Debug.LogException(e);InputSystem.onAfterUpdate-=DriveInput;if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();else EditorApplication.Exit(1);}
    }
    static void DriveInput()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic || mouse==null)return;
        mouse.MakeCurrent();
        InputState.Change(mouse,aim?new MouseState().WithButton(MouseButton.Right):new MouseState());
    }
}
#endif
