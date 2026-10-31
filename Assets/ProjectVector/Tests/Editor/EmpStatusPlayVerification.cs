#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class EmpStatusPlayVerification
{
    const string Key="Vector.EMP.StatusTest";
    static ElectricShock shock;static EmpGestureRig gesture;static int stage;static double next,deadline;
    static EmpStatusPlayVerification(){EditorApplication.playModeStateChanged+=State;if(SessionState.GetBool(Key,false)){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+45;}}
    public static void Run(){if(!Application.isBatchMode)throw new Exception("Batch only");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+45;EditorApplication.EnterPlaymode();}
    static void State(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){stage=0;next=EditorApplication.timeSinceStartup+.3;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(stage==99?0:1);}}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("EMP_STATUS_CHECK "+message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("EMP test timeout");
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next)return;
            if(stage==0){shock=new GameObject("StatusFixture").AddComponent<ElectricShock>();gesture=shock.gameObject.AddComponent<EmpGestureRig>();gesture.Begin();shock.ApplyServerStatus(3,false,0);Check(shock.ShockCount==3&&!shock.IsStunned,"stack state");shock.ApplyServerStatus(0,true,1.5f);Check(shock.IsStunned&&shock.ShockCount==0&&shock.StunRemaining>1.3f&&shock.StunDuration==1.5f,"server stun consumes local stack and starts 1.5 second timer");Time.timeScale=0;stage=1;next=EditorApplication.timeSinceStartup+.3;}
            else if(stage==1){Check(shock.StunRemaining<1.35f,"timer continues with local pause");shock.ApplyServerStatus(1,false,0);Check(shock.IsStunned&&shock.ShockCount==1,"new stacks do not cancel an active stun");shock.ApplyServerStatus(0,false,0);Check(!shock.IsStunned&&shock.StunRemaining==0,"explicit server recovery clears animation lock immediately");shock.ApplyServerStatus(0,true,1.75f);Check(shock.StunDuration==1.75f,"fractional server stun retained");stage=2;next=EditorApplication.timeSinceStartup+1.95;}
            else if(stage==2){Check(!shock.IsStunned&&shock.StunRemaining==0,"stun expires even with timeScale zero");Check(gesture.Ready,"gesture reaches release while local time is paused");gesture.Release();stage=3;next=EditorApplication.timeSinceStartup+.5;}
            else if(stage==3){Check(!gesture.Active,"weapon recovery finishes while local time is paused");Time.timeScale=1;shock.ApplyServerStatus(0,true,2);Check(shock.StunDuration==2,"maximum 2 second server timer");shock.ClearElectricState();Check(!shock.IsStunned&&shock.StunDuration==0,"round reset clears timer");shock.ApplyServerStatus(0,true,2);gesture.Begin();shock.gameObject.SetActive(false);Check(!shock.IsStunned&&shock.StunRemaining==0&&!gesture.Active,"disable clears status and gesture");Object.Destroy(shock.gameObject);stage=99;Debug.Log("EMP_STATUS_PLAY_PASS: stacks, consumption, local pause, explicit recovery, expiry, gesture timing/recovery, round clear and disable");EditorApplication.ExitPlaymode();}
        }catch(Exception ex){Debug.LogException(ex);Time.timeScale=1;stage=-1;EditorApplication.ExitPlaymode();}
    }
}
#endif
