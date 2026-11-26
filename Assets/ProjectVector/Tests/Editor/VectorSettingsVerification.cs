#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VectorSettingsVerification
{
    const string Active="Vector.Settings.Verification";
    const string NetworkActive="Vector.Settings.NetworkVerification";
    const string TestAccount="socialtest0";
    const string Prefab="Assets/ProjectVector/Shared/UI/Settings/VectorSettings.prefab";
    static readonly string Output=Path.Combine(Path.GetTempPath(),"VectorSettingsVerification");
    static int phase=-1;
    static int networkPhase;
    static double next,deadline;
    static Keyboard keyboard;
    static VectorSettingsPanel original;
    static bool failed;
    static void SetKeys(params Key[] keys)=>InputState.Change(keyboard,new KeyboardState(keys),InputUpdateType.Dynamic);
    static void Escape(VectorSettingsPanel panel)=>SetKeys(Key.Escape);
    // Input System has separate editor/game state buffers. Read assertions in the
    // normal Dynamic update and let the panel's real Update consume virtual keys.
    static void EditorTick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)Tick();}
    static void Subscribe(){EditorApplication.update-=EditorTick;EditorApplication.update+=EditorTick;VectorSettingsPlayDriver.Tick=Tick;}
    [Serializable] class SavedPrefs {public List<Entry> entries=new List<Entry>();}
    [Serializable] class Entry {public string key,s;public float f;public int i,type;public bool exists;}
    static VectorSettingsVerification(){if(SessionState.GetBool(Active,false)){deadline=EditorApplication.timeSinceStartup+180;Subscribe();}}
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void AuthorAndRender(){VectorSettingsAuthoring.Install();Render();}
    public static void RecoverAuthorAndRun(){RestorePrefs();AuthorAndRender();Run();}
    public static void Render()
    {
        Directory.CreateDirectory(Output);
        foreach(var item in EditorBuildSettings.scenes.Where(s=>s.enabled)){
            var scene=EditorSceneManager.OpenScene(item.path,OpenSceneMode.Single);
            var panels=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VectorSettingsPanel>(true)).ToArray();Assert(panels.Length==1,"Shared settings missing/duplicated: "+item.path);
            Assert(panels[0].pages.Length==6&&panels[0].rows.Length==VectorSettingsData.All.Length+VectorInput.Actions.Length,"Tab/row count mismatch");
            foreach(var row in panels[0].rows)Assert(row.title!=null&&row.selected!=null&&(row.binding?row.capture!=null:row.previous!=null&&row.next!=null),"Missing row reference");
            if(item.path.EndsWith("Lobby.unity")){var binder=UnityEngine.Object.FindFirstObjectByType<VectorLobbyMenuButton>();Assert(binder!=null,"Lobby button must resolve shared singleton");Assert(binder.GetComponent<Button>().onClick.GetPersistentTarget(0)==binder,"Stale scene settings target");}
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        var p=root.GetComponent<VectorSettingsPanel>();var canvas=root.GetComponent<Canvas>();var cameraObject=new GameObject("Verification camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=540;camera.transform.position=new Vector3(0,0,-10);
        var target=new RenderTexture(1920,1080,24);target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.GetComponent<CanvasScaler>().enabled=false;
        canvas.GetComponent<RectTransform>().sizeDelta=new Vector2(1920,1080);canvas.scaleFactor=1;
        p.settings.SetActive(true);
        for(int tab=0;tab<6;tab++){
            for(int i=0;i<6;i++){p.pages[i].SetActive(i==tab);p.tabs[i].GetComponent<Image>().color=i==tab?new Color(.76f,.13f,.2f):new Color(.12f,.13f,.15f);}
            var first=p.rows.First(r=>r.binding?tab==1:VectorSettingsData.Find(r.key).tab==tab);p.descriptionTitle.text=first.title.text;p.descriptionBody.text=first.binding?"버튼을 선택하고 사용할 키를 누르세요.":VectorSettingsData.Find(first.key).help;
            foreach(var row in p.rows)row.Refresh();Capture(camera,target,"settings-"+tab);
        }
        p.settings.SetActive(false);p.menu.SetActive(true);Capture(camera,target,"menu");p.menu.SetActive(false);
        p.notices.SetActive(true);p.noticeTitle.text="Project Vector 설정 안내";p.noticeBody.text="로비와 인게임은 같은 설정을 사용합니다.\n\n설정에서 감도와 조준선을 변경한 뒤 전투 화면에서 바로 확인할 수 있습니다.\n\n공지 목록을 선택하면 이 영역에 본문이 표시됩니다. 긴 본문은 스크롤해서 읽을 수 있습니다.";
        for(int i=0;i<p.noticeButtons.Length;i++){p.noticeButtons[i].gameObject.SetActive(i<2);p.noticeButtons[i].GetComponentInChildren<TMP_Text>().text=i==0?"Project Vector 설정 안내":"오류 제보와 문의 안내";}
        Capture(camera,target,"notices");p.notices.SetActive(false);p.support.SetActive(true);Capture(camera,target,"support");p.support.SetActive(false);p.redeem.SetActive(true);Capture(camera,target,"redeem");
        camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("VECTOR_SETTINGS_RENDER_OK "+Output);
    }
    static void Capture(Camera camera,RenderTexture target,string name)
    {
        Canvas.ForceUpdateCanvases();foreach(var text in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();camera.Render();
        RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=null;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);SavePrefs();EditorApplication.isPaused=false;SessionState.SetInt("Vector.Settings.OldEditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);SessionState.SetInt("Vector.Settings.OldInputFocus",(int)InputSystem.settings.backgroundBehavior);SessionState.SetBool("Vector.Settings.OldBackground",Application.runInBackground);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        new GameObject("Settings verification driver",typeof(VectorSettingsPlayDriver));
        // Supply the normal lobby dependency without starting a connection or
        // logging into any account during these isolated presentation checks.
        new GameObject("Verification network",typeof(NetworkManager)).GetComponent<NetworkManager>().enabled=SessionState.GetBool(NetworkActive,false);
        SessionState.SetBool(Active,true);deadline=EditorApplication.timeSinceStartup+180;Subscribe();EditorApplication.EnterPlaymode();
    }
    public static void RunNetworkAccount()
    {
        string root=Environment.GetEnvironmentVariable("VECTOR_SETTINGS_QA_ROOT");
        Assert(!string.IsNullOrEmpty(root)&&Path.GetFileName(root).StartsWith("VectorAccountSettingsPlay-")&&File.Exists(Path.Combine(root,"test-only.marker")),"Run only through the isolated server test launcher");
        SessionState.SetBool(NetworkActive,true);Run();
        // This emulates a PC that has never signed into the test account.
        PlayerPrefs.DeleteKey("Vector.Settings.Account."+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(TestAccount)));
    }
    static void SavePrefs()
    {
        var saved=new SavedPrefs();
        foreach(var d in VectorSettingsData.All){string key=d.key.StartsWith("MouseSensitivity_")?d.key:"Vector.Setting."+d.key;saved.entries.Add(new Entry{key=key,exists=PlayerPrefs.HasKey(key),f=PlayerPrefs.GetFloat(key)});}
        foreach(string a in VectorInput.Actions){string key="Vector.Key."+a;saved.entries.Add(new Entry{key=key,exists=PlayerPrefs.HasKey(key),s=PlayerPrefs.GetString(key),type=1});}
        foreach(string key in new[]{"Vector.Settings.LegacyOwner","Vector.Settings.LegacySnapshot","VectorVoice.InputDevice","VectorVoice.OutputDevice"})saved.entries.Add(new Entry{key=key,exists=PlayerPrefs.HasKey(key),s=PlayerPrefs.GetString(key),type=1});
        if(SessionState.GetBool(NetworkActive,false))foreach(string suffix in new[]{"",".ConflictBackup"}){string key="Vector.Settings.Account."+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(TestAccount))+suffix;saved.entries.Add(new Entry{key=key,exists=PlayerPrefs.HasKey(key),s=PlayerPrefs.GetString(key),type=1});}
        foreach(string key in new[]{"VectorVoice.Input","VectorVoice.Output","VectorVoice.Muted","VectorVoice.Automatic","Vector.Display.Width","Vector.Display.Height","Vector.Display.Mode"})saved.entries.Add(new Entry{key=key,exists=PlayerPrefs.HasKey(key),i=PlayerPrefs.GetInt(key),type=2});
        File.WriteAllText(Path.Combine(Output,"prefs-backup.json"),JsonUtility.ToJson(saved));
    }
    static void RestorePrefs()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt("Vector.Settings.OldEditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt("Vector.Settings.OldInputFocus",(int)InputSystem.settings.backgroundBehavior);Application.runInBackground=SessionState.GetBool("Vector.Settings.OldBackground",Application.runInBackground);
        var saved=JsonUtility.FromJson<SavedPrefs>(File.ReadAllText(Path.Combine(Output,"prefs-backup.json")));
        foreach(var entry in saved.entries){if(!entry.exists)PlayerPrefs.DeleteKey(entry.key);else if(entry.type==1)PlayerPrefs.SetString(entry.key,entry.s);else if(entry.type==2)PlayerPrefs.SetInt(entry.key,entry.i);else PlayerPrefs.SetFloat(entry.key,entry.f);}PlayerPrefs.Save();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false))return;
        try {
            if(phase<20&&EditorApplication.timeSinceStartup>deadline)throw new Exception("Settings Play verification timed out");
            if(!EditorApplication.isPlaying){if(phase<20)return;SessionState.SetBool(Active,false);RestorePrefs();EditorApplication.update-=EditorTick;VectorSettingsPlayDriver.Tick=null;Debug.Log(failed?"VECTOR_SETTINGS_PLAY_FAILED":"VECTOR_SETTINGS_PLAY_OK");EditorApplication.Exit(failed?1:0);return;}
            if(EditorApplication.timeSinceStartup<next)return;
            var p=VectorSettingsPanel.Instance;
            if(SessionState.GetBool(NetworkActive,false)){TickNetwork(p);return;}
            switch(phase){
                case -1:next=EditorApplication.timeSinceStartup+1;phase=0;break;
                case 0:
                    Assert(p!=null&&!p.IsOpen,"Prefab startup");original=p;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;Escape(p);next=EditorApplication.timeSinceStartup+.3;phase=1;break;
                case 1:
                    Assert(p.IsOpen&&p.settings.activeSelf&&GameplayActionPolicy.MenuBlocksInput,"ESC opens shared settings and blocks gameplay");Assert(Cursor.visible,"Settings cursor");SetKeys();
                    VectorSettingsData.Set("MouseSensitivity_General",1.37f);Assert(Mathf.Approximately(MouseSettings.GetSensitivity(SensitivityType.General),1.37f),"Legacy sensitivity linkage");
                    string code=VectorCrosshairCode.Export();VectorSettingsData.Set("CrossGap",13);Assert(VectorCrosshairCode.Import(code)&&VectorCrosshairCode.Export()==code,"Crosshair round trip");Assert(!VectorCrosshairCode.Import(code+"X"),"Corrupt crosshair rejected");
                    VectorInput.Reset();Assert(!VectorInput.Bind("Reload","W",out _),"Duplicate binding rejected");Assert(VectorInput.Bind("Reload","T",out _),"Binding saved");
                    SetKeys(Key.T);next=EditorApplication.timeSinceStartup+.3;phase=2;break;
                case 2:
                    Assert(!VectorInput.Held("Reload"),"No input leaks while open");p.Close();SetKeys();next=EditorApplication.timeSinceStartup+.3;phase=3;break;
                case 3:
                    Assert(!VectorSettingsPanel.BlocksInput,"Closed-frame input guard expires");SetKeys(Key.T);next=EditorApplication.timeSinceStartup+.3;phase=4;break;
                case 4:
                    Assert(VectorInput.Held("Reload"),"Remapped key reaches gameplay");SetKeys();SceneManager.LoadScene("WeaponTest");next=EditorApplication.timeSinceStartup+4;phase=5;break;
                case 5:
                    Assert(p==original,"Singleton survives practice scene");Assert(Mathf.Approximately(MouseSettings.GetSensitivity(SensitivityType.General),1.37f),"Settings persist across scenes");Assert(UnityEngine.Object.FindObjectsByType<VectorSettingsPanel>(FindObjectsSortMode.None).Length==1,"No duplicate global settings");
                    Assert(UnityEngine.Object.FindObjectsByType<VectorCrosshairGraphic>(FindObjectsSortMode.None).Any(g=>!g.preview),"Practice uses authored custom crosshair");
                    Escape(p);next=EditorApplication.timeSinceStartup+.4;phase=6;break;
                case 6:
                    Assert(p.IsOpen,"Practice ESC opens common settings");SetKeys();p.Close();SceneManager.LoadScene("Lobby");next=EditorApplication.timeSinceStartup+4;phase=7;break;
                case 7:
                    Assert(p==original,"Singleton survives lobby return");var binder=UnityEngine.Object.FindFirstObjectByType<VectorLobbyMenuButton>();Assert(binder!=null,"Lobby binder");binder.GetComponent<Button>().onClick.Invoke();next=EditorApplication.timeSinceStartup+.4;phase=8;break;
                case 8:
                    Assert(p.IsOpen&&p.menu.activeSelf,"Lobby menu button still works after duplicate destruction");p.OpenSettings();Assert(p.settings.activeSelf,"Lobby settings route");Assert(Mathf.Approximately(MouseSettings.GetSensitivity(SensitivityType.General),1.37f),"Lobby reads in-game settings");p.Close();InputSystem.RemoveDevice(keyboard);phase=20;EditorApplication.ExitPlaymode();break;
            }
        }catch(Exception error){Debug.LogException(error);failed=true;phase=20;SessionState.SetBool(NetworkActive,false);NetworkManager.Instance?.AccountSettings?.Detach();if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();else{SessionState.SetBool(Active,false);RestorePrefs();EditorApplication.Exit(1);}}
    }
    static void TickNetwork(VectorSettingsPanel panel)
    {
        var net=NetworkManager.Instance;if(net==null||panel==null)return;
        if(networkPhase==0){
            if(!net.IsConnected)return;
            net.SendLogin(TestAccount,"WrongTestPassword");Assert(!net.AccountSettings.CanEdit,"Settings wait during authentication");networkPhase=10;
        }else if(networkPhase==10){
            if(!net.AccountSettings.CanEdit)return;
            Assert(!net.IsAuthenticated,"Wrong password must not authenticate or load the account profile");
            net.SendLogin(TestAccount,"TestOnly123");Assert(!net.AccountSettings.CanEdit,"Real login must wait for cloud profile");networkPhase=1;
        }else if(networkPhase==1){
            if(!net.IsAuthenticated||!net.AccountSettings.CanEdit)return;
            Assert(VectorSettingsData.Get("CrossGap")==12&&VectorInput.Binding("Reload")=="T"&&Mathf.Approximately(MouseSettings.GetSensitivity(SensitivityType.General),1.37f),"Fresh PC real login loads server profile");
            panel.OpenSettings();VectorSettingsData.Set("CrossGap",29);panel.apply.onClick.Invoke();networkPhase=2;
        }else if(networkPhase==2){
            if(net.AccountSettings.Status!="계정에 설정을 저장했습니다.")return;
            Assert(VectorSettingsData.Get("CrossGap")==29,"Real saved setting stays applied");
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("VECTOR_SETTINGS_QA_ROOT"),"unity-passed.marker"),"login-load-and-save");
            panel.Close();net.SendMessage("OnApplicationQuit");net.AccountSettings.Detach();net.enabled=false;
            SessionState.SetBool(NetworkActive,false);phase=-1;Debug.Log("VECTOR_ACCOUNT_SETTINGS_NETWORK_OK: real login, fresh-PC profile, UI apply, server acknowledgement");
        }
    }
}
#endif
