#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PracticeUpgradeVerification
{
    const string Key="Vector.Practice.UpgradeTest";
    static PracticeRangeController range;
    static int stage, character, weapon, errors, shots;
    static double next, deadline;
    static bool failed;
    static PracticeUpgradeVerification()
    {
        EditorApplication.playModeStateChanged+=State;
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false) && (t==LogType.Error || t==LogType.Exception))errors++;};
        if(SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+150;EditorApplication.update+=Tick;}
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene");
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");SessionState.SetBool(Key,true);
        deadline=EditorApplication.timeSinceStartup+150;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
    }
    static void State(PlayModeStateChange value)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(value==PlayModeStateChange.EnteredPlayMode){stage=0;next=EditorApplication.timeSinceStartup+2;}
        if(value==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Upgrade Play timeout");
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next)return;
            if(stage==0)
            {
                range=UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;
                Check(range.loadout!=null && range.MenuOpen && !range.spawner.Running,"Character selection not required on entry");
                Capture("selection",1920,1080);Capture("selection-4x3",1440,1080);Capture("selection-ultrawide",2560,1080);
                range.loadout.SelectCharacter(0);stage=1;next=EditorApplication.timeSinceStartup+.3;
            }
            else if(stage==1)
            {
                var l=range.loadout;if(l.Switching)return;
                Check(l.players.Count(p=>p.gameObject.activeInHierarchy)==1,"Multiple player actors active");
                Check(l.players.SelectMany(p=>p.GetComponentsInChildren<AudioListener>(true)).Count(a=>a.enabled && a.gameObject.activeInHierarchy)==1,"Audio listener count");
                Check(range.player.localScale==range.spawner.characterTargets[character].gameplayPrefab.transform.localScale,"Player scale mismatch");
                Check(!range.player.GetComponentsInChildren<CharacterSkill>(true).Any(s=>s.enabled),"Skill enabled");
                l.EquipWeapon(weapon);stage=2;next=EditorApplication.timeSinceStartup+.12;
            }
            else if(stage==2)
            {
                var l=range.loadout;var held=range.inventory.GetCurrentWeapon();
                Check(held!=null && held.Data==l.weapons[weapon] && held.gameObject.activeInHierarchy,$"Weapon {weapon} unavailable on character {character}");
                Check(range.inventory.Items.Count(i=>i!=null && i.gameObject.activeInHierarchy)==1,"Multiple held items visible");
                l.Confirm();
                Check(range.player.GetComponent<PracticeWeaponInput>().TryFire(),$"Weapon {weapon} cannot fire on character {character}");
                range.OpenMenu();
                if(++weapon<l.weapons.Length){stage=1;return;}
                weapon=0;
                if(++character<4){l.SelectCharacter(character);stage=1;next=EditorApplication.timeSinceStartup+.3;return;}
                l.SelectCharacter(0);stage=3;next=EditorApplication.timeSinceStartup+.3;
            }
            else if(stage==3)
            {
                var l=range.loadout;if(l.Switching)return;l.Confirm();
                range.SetMode(1);range.spawner.realisticHealth=true;range.spawner.targetArmor=50;range.FreePractice();range.spawner.Advance(1);
                var sp=range.spawner;
                Check(!sp.RegisterDamage(20,0,100) && sp.TargetBarrier==30 && sp.TargetHP==100,"Barrier damage");
                Check(!sp.RegisterDamage(40,2,100) && sp.TargetBarrier==0 && sp.TargetHP==90,"Split barrier damage / leg hit");
                Check(sp.Hits==1,"Shotgun pellets counted as separate accuracy shots");
                Check(sp.RegisterDamage(999,1,101) && sp.TotalDamage==150 && sp.LastDamage==90,"Overkill/kill accounting");
                Check(!sp.RegisterDamage(999,1,102) && sp.Score==1,"Duplicate kill");
                sp.StopTraining();range.FreePractice();sp.Advance(1);int generation=sp.Generation;
                var zone=sp.enemyObject.GetComponentsInChildren<PracticeTargetZone>().Single(z=>!z.isHead && !z.isLegs);
                zone.Hit(999,1,generation-1);Check(sp.TargetHP==100 && sp.TargetBarrier==50,"Previous run bullet leaked");
                range.OpenMenu();zone.Hit(999,1,generation);Check(sp.TargetHP==100,"Paused hit applied");
                l.EquipWeapon(Array.FindIndex(l.weapons,w=>w==range.rifle));range.infiniteAmmo=false;l.autoReload=false;
                range.FreePractice();sp.Advance(1);shots=0;stage=4;next=EditorApplication.timeSinceStartup+.2;
            }
            else if(stage==4)
            {
                var sp=range.spawner;
                if(sp.Score>0){Check(sp.TotalDamage==150,"Physical bullet damage total");stage=5;return;}
                Check(shots++<10,"Physical rifle target not killed");
                var zone=sp.enemyObject.GetComponentsInChildren<PracticeTargetZone>().Single(z=>!z.isHead&&!z.isLegs);
                var input=range.player.GetComponent<PracticeWeaponInput>();
                input.aimCamera.transform.position=zone.transform.position+Vector3.back;
                input.aimCamera.transform.LookAt(zone.transform.position);Physics.SyncTransforms();
                Check(input.TryFire(),"Rifle fire blocked");next=EditorApplication.timeSinceStartup+.2;
            }
            else if(stage==5)
            {
                range.OpenMenu();range.EquipPistol();range.Resume();
                range.inventory.GetCurrentWeapon().SetAmmo(0,3);range.inventory.SendMessage("TryStartReload");
                Check(range.inventory.IsReloading,"Reload failed");stage=6;next=EditorApplication.timeSinceStartup+range.pistol.reloadTime+.4;
            }
            else if(stage==6)
            {
                var held=range.inventory.GetCurrentWeapon();
                Check(held.CurrentAmmo==3 && held.ReserveAmmo==0,"Reload minted reserve ammo");range.loadout.Refill();
                Check(held.CurrentAmmo==held.MagazineCapacity && held.ReserveAmmo==held.Data.maxReserveAmmo,"Refill failed");
                range.SetMode(2);range.spawner.irregularMovement=true;range.FreePractice();range.spawner.Advance(1);
                Vector3 original=range.spawner.enemyObject.transform.position;
                for(int i=0;i<100;i++){range.spawner.Advance(.1f);Check(Mathf.Abs(range.spawner.enemyObject.transform.position.x-original.x)<=range.spawner.movementWidth+.01f,"Target escaped lane");}
                range.SetMode(1);range.FreePractice();range.spawner.Advance(1);range.ReturnToStation();
                range.player.GetComponentInChildren<FPSCameraLook>()?.ResetLookRotation(0);Capture("distance-gameplay",1920,1080);
                range.SetMode(4);range.FreePractice();range.spawner.Advance(1);Capture("cover-gameplay",1920,1080);
                range.OpenMenu();Capture("settings",1920,1080);
                range.Resume();var cam=range.player.GetComponent<PracticeWeaponInput>().aimCamera;
                cam.transform.position=new Vector3(194,1.8f,-11);cam.transform.LookAt(new Vector3(190,1.5f,-6));Capture("armory-detail",1920,1080);
                Check(errors==0,"Runtime errors: "+errors);
                Debug.Log("PRACTICE_UPGRADE_PLAY_PASS: entry selection, 4 actors x 14 weapons, exact scales, skill isolation, health/armor/regions, pellet accuracy, real projectile kill, reserve reload/refill, movement bounds, aspect captures; no account writes");
                stage=99;EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception e){failed=true;Debug.LogException(e);if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();else EditorApplication.Exit(1);}
    }
    static void Capture(string name,int width,int height)
    {
        var cam=range.player.GetComponent<PracticeWeaponInput>().aimCamera;
        var canvas=range.menu.GetComponentInParent<Canvas>();var mode=canvas.renderMode;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=cam.nearClipPlane+1;
        Canvas.ForceUpdateCanvases();range.loadout.Refresh();
        var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;var old=RenderTexture.active;RenderTexture.active=rt;cam.Render();
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        string dir=Path.Combine(Path.GetTempPath(),"VectorPracticeUpgrade");Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,name+".png"),image.EncodeToPNG());
        canvas.renderMode=mode;cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);
    }
}
#endif
