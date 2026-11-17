using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Every object referenced here is authored in VectorSettings.prefab before Play.
[DefaultExecutionOrder(-10000)]
public sealed class VectorSettingsPanel : MonoBehaviour
{
    public static VectorSettingsPanel Instance {get;private set;}
    static int closedFrame=-10;
    public static bool BlocksInput => (Instance!=null&&Instance.IsOpen)||Time.frameCount<=closedFrame;
    public bool IsOpen => mode!=0;
    public GameObject settings, menu, notices, redeem, support, displayConfirm;
    public GameObject[] pages;
    public Button[] tabs;
    public VectorSettingRow[] rows;
    public TMP_Text descriptionTitle,descriptionBody,status,performance,confirmText,utilityStatus,noticeTitle,noticeBody,voiceDevices;
    public Button apply,defaults,back,keepDisplay,revertDisplay,copyCode,importCode,inputDevice,outputDevice;
    public TMP_InputField crosshairCode,redeemCode,supportTitle,supportBody;
    public Button redeemSubmit,supportSubmit;
    public Button[] noticeButtons;
    public RawImage backdrop;
    public Material blurMaterial;
    public ScrollRect noticeScroll;
    int mode,tab,openGeneration,captureFrame;
    int lastEscapeFrame=-1;
    string lastSyncStatus;
    CursorLockMode previousLock; bool previousVisible;
    VectorSettingRow captureRow,hoverRow;
    float confirmDeadline,replyDeadline,perfNext,voiceNext;
    int oldWidth,oldHeight; FullScreenMode oldMode;
    int acceptedMode,acceptedResolution;
    readonly List<Vector2Int> resolutions=new List<Vector2Int>();
    NetworkManager subscribedNetwork;
    ulong pendingRequest; byte pendingOperation;
    string pendingTitle,pendingBody,pendingAccount;
    bool awaitingReply;
    Texture2D snapshot;
    [Serializable] public class Notice { public string title,body; }
    [Serializable] public class NoticeList { public Notice[] entries; }
    Notice[] noticeEntries=Array.Empty<Notice>();

    void Awake()
    {
        if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);
        foreach(var root in new[]{settings,menu,notices,redeem,support,displayConfirm})root.SetActive(false);
        backdrop.gameObject.SetActive(false);InitDisplayChoices();
        for(int i=0;i<tabs.Length;i++){int index=i;tabs[i].onClick.AddListener(()=>SelectTab(index));}
        foreach(var row in rows)row.Bind(this);
        apply.onClick.AddListener(Apply);defaults.onClick.AddListener(()=>{if(!NetworkManager.CanEditAccountSettings)return;VectorSettingsData.ResetTab(tab);RefreshRows();Edited();});back.onClick.AddListener(Close);
        keepDisplay.onClick.AddListener(KeepDisplay);revertDisplay.onClick.AddListener(RevertDisplay);
        copyCode.onClick.AddListener(()=>{crosshairCode.text=VectorCrosshairCode.Export();GUIUtility.systemCopyBuffer=crosshairCode.text;status.text="조준선 코드를 복사했습니다.";});
        importCode.onClick.AddListener(()=>{if(!NetworkManager.CanEditAccountSettings)return;bool ok=VectorCrosshairCode.Import(crosshairCode.text);status.text=ok?"조준선을 가져왔습니다.":"올바른 Project Vector 조준선 코드가 아닙니다.";RefreshRows();});
        inputDevice.onClick.AddListener(()=>VectorVoiceChat.Instance?.CycleDevice(true));outputDevice.onClick.AddListener(()=>VectorVoiceChat.Instance?.CycleDevice(false));
        redeemSubmit.onClick.AddListener(()=>SendUtility(1,redeemCode.text.Trim(),""));
        supportSubmit.onClick.AddListener(()=>SendUtility(2,supportTitle.text.Trim(),supportBody.text.Trim()));
        for(int i=0;i<noticeButtons.Length;i++){int n=i;noticeButtons[i].onClick.AddListener(()=>ShowNotice(n));}
        SceneManager.sceneLoaded+=SceneLoaded;
        VectorSettingsData.ApplyRuntime();SelectTab(0);
    }
    void InitDisplayChoices()
    {
        resolutions.Clear();foreach(var r in Screen.resolutions){var v=new Vector2Int(r.width,r.height);if(!resolutions.Contains(v))resolutions.Add(v);}
        var current=new Vector2Int(Screen.width,Screen.height);if(!resolutions.Contains(current))resolutions.Add(current);
        var d=VectorSettingsData.Find("Resolution");d.choices=resolutions.Select(x=>x.x+" × "+x.y).ToArray();d.max=resolutions.Count-1;
        var savedSize=new Vector2Int(PlayerPrefs.GetInt("Vector.Display.Width",current.x),PlayerPrefs.GetInt("Vector.Display.Height",current.y));
        int savedIndex=resolutions.IndexOf(savedSize);
        int currentMode=Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen?0:Screen.fullScreenMode==FullScreenMode.Windowed?2:1;
        if(PlayerPrefs.HasKey("Vector.Display.Mode")&&savedIndex>=0){currentMode=Mathf.Clamp(PlayerPrefs.GetInt("Vector.Display.Mode"),0,2);current=savedSize;Screen.SetResolution(current.x,current.y,currentMode==0?FullScreenMode.ExclusiveFullScreen:currentMode==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}
        VectorSettingsData.Set("Resolution",resolutions.IndexOf(current));
        VectorSettingsData.Set("DisplayMode",currentMode);acceptedMode=currentMode;acceptedResolution=resolutions.IndexOf(current);
    }
    void Update()
    {
        if(Instance!=this)return;
        string syncStatus=NetworkManager.Instance?.AccountSettings?.Status;
        if(syncStatus!=lastSyncStatus){lastSyncStatus=syncStatus;RefreshRows();if(mode==1&&!string.IsNullOrEmpty(syncStatus))status.text=syncStatus;}
        if(subscribedNetwork!=NetworkManager.Instance){if(subscribedNetwork!=null)subscribedNetwork.LobbyUtilityReceived-=ReceiveUtility;subscribedNetwork=NetworkManager.Instance;if(subscribedNetwork!=null)subscribedNetwork.LobbyUtilityReceived+=ReceiveUtility;}
        if(confirmDeadline>0){confirmText.text="이 화면 설정을 유지할까요?  "+Mathf.CeilToInt(confirmDeadline-Time.unscaledTime)+"초";if(Time.unscaledTime>=confirmDeadline)RevertDisplay();}
        if(awaitingReply&&Time.unscaledTime>replyDeadline){awaitingReply=false;utilityStatus.text="응답이 지연되고 있습니다. 다시 시도하면 같은 요청을 확인합니다.";SetSubmitEnabled(true);}
        if(Time.unscaledTime>=perfNext){perfNext=Time.unscaledTime+.3f;bool fps=VectorSettingsData.Get("ShowFPS")==1,ms=VectorSettingsData.Get("ShowFrameTime")==1;performance.gameObject.SetActive(fps||ms);performance.text=(fps?Mathf.RoundToInt(1f/Mathf.Max(.0001f,Time.smoothDeltaTime))+" FPS":"")+(fps&&ms?"  |  ":"")+(ms?(Time.smoothDeltaTime*1000).ToString("F1")+" ms":"");}
        if(mode==1&&Time.unscaledTime>=voiceNext){voiceNext=Time.unscaledTime+.5f;var v=VectorVoiceChat.Instance;voiceDevices.text=v==null?"음성 연결 대기":v.InputDevice+"\n"+v.OutputDevice;inputDevice.interactable=outputDevice.interactable=v?.Ready==true;}
        var keyboard=Keyboard.current;
        if(captureRow!=null){CaptureBinding();return;}
        if(keyboard?.escapeKey.wasPressedThisFrame!=true)return;
        if(lastEscapeFrame==Time.frameCount)return;
        lastEscapeFrame=Time.frameCount;
        if(confirmDeadline>0){RevertDisplay();return;}
        if(IsOpen){Close();return;}
        if(VectorVoicePanel.IsAnyOpen||VectorVoicePanel.BlocksGameplayInput)return;
        if(InGameChatUI.TryCloseChatByEscape())return;
        OpenSettings();
    }
    void LateUpdate(){if(IsOpen){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
    void OnApplicationFocus(bool focus){VectorSettingsData.ApplyRuntime();}
    void SceneLoaded(Scene s,LoadSceneMode load){if(IsOpen)Close(false);}
    void OnDestroy(){if(Instance!=this)return;SceneManager.sceneLoaded-=SceneLoaded;if(subscribedNetwork!=null)subscribedNetwork.LobbyUtilityReceived-=ReceiveUtility;if(snapshot)Destroy(snapshot);VectorSettingsData.Release();PlayerPrefs.Save();Instance=null;}
    public void OpenSettings(){Open(1);RefreshRows();status.text=string.IsNullOrEmpty(NetworkManager.Instance?.AccountSettings?.Status)?"변경 내용은 즉시 반영됩니다. 화면 모드와 해상도는 적용 후 확인합니다.":NetworkManager.Instance.AccountSettings.Status;}
    public void ReloadAccountSettings(){captureRow=null;confirmDeadline=0;displayConfirm.SetActive(false);InitDisplayChoices();RefreshRows();}
    public void OpenMenu()=>Open(2);
    public void OpenNotices(){Open(3);SetNotices(Array.Empty<Notice>());SendUtility(0,"","");}
    public void OpenRedeem(){Open(4);SetSubmitEnabled(!awaitingReply);utilityStatus.text="교환 코드를 입력해 주세요.";}
    public void OpenSupport(){Open(5);SetSubmitEnabled(!awaitingReply);utilityStatus.text="발생 상황과 재현 방법을 적어 주세요. 접수 후 확인 번호가 표시됩니다.";}
    public void QuitGame(){PlayerPrefs.Save();Application.Quit();}
    void Open(int target)
    {
        if(!IsOpen){previousLock=Cursor.lockState;previousVisible=Cursor.visible;}
        mode=target;openGeneration++;captureRow=null;EventSystem.current?.SetSelectedGameObject(null);
        settings.SetActive(target==1);menu.SetActive(false);notices.SetActive(false);redeem.SetActive(false);support.SetActive(false);
        backdrop.gameObject.SetActive(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(target!=1)StartCoroutine(OpenUtilityAfterCapture(target,openGeneration));
        utilityStatus.gameObject.SetActive(target>=3);
    }
    IEnumerator OpenUtilityAfterCapture(int target,int generation)
    {
        if(!Application.isBatchMode){
            yield return new WaitForEndOfFrame();
            if(generation!=openGeneration)yield break;
            if(snapshot)Destroy(snapshot);
            snapshot=ScreenCapture.CaptureScreenshotAsTexture();backdrop.texture=snapshot;backdrop.material=blurMaterial;
        }
        if(generation!=openGeneration)yield break;
        backdrop.gameObject.SetActive(true);menu.SetActive(target==2);notices.SetActive(target==3);redeem.SetActive(target==4);support.SetActive(target==5);
    }
    public void Close()=>Close(true);
    void Close(bool restoreCursor)
    {
        if(!IsOpen)return;
        if(confirmDeadline>0)RevertDisplay();
        VectorSettingsData.Set("DisplayMode",acceptedMode);VectorSettingsData.Set("Resolution",acceptedResolution);
        utilityStatus.gameObject.SetActive(false);mode=0;openGeneration++;captureRow=null;closedFrame=Time.frameCount+1;
        foreach(var root in new[]{settings,menu,notices,redeem,support,displayConfirm})root.SetActive(false);
        backdrop.gameObject.SetActive(false);if(snapshot){Destroy(snapshot);snapshot=null;}EventSystem.current?.SetSelectedGameObject(null);PlayerPrefs.Save();
        NetworkManager.Instance?.AccountSettings?.Flush();
        if(restoreCursor){if(GameController.Instance!=null&&ProjectVector.Core.SceneCatalog.IsMatch(SceneManager.GetActiveScene().name)){if(GameController.Instance.CanOpenSettings)GameController.Instance.LockGameplayCursor();else GameController.Instance.ShowUICursor();}else{Cursor.lockState=previousLock;Cursor.visible=previousVisible;}}
    }
    public void SelectTab(int selectedTab)
    {
        captureRow=null;tab=selectedTab;
        for(int i=0;i<pages.Length;i++){pages[i].SetActive(i==tab);tabs[i].GetComponent<Image>().color=i==tab?new Color(.75f,.13f,.2f):new Color(.12f,.13f,.15f);}
        var first=Array.Find(rows,r=>r.binding?tab==1:VectorSettingsData.Find(r.key).tab==tab);if(first!=null)Describe(first);
    }
    public void Describe(VectorSettingRow row)
    {
        if(hoverRow!=null&&hoverRow.selected!=null)hoverRow.selected.color=new Color(.45f,.5f,.55f,.13f);hoverRow=row;
        if(row.selected!=null)row.selected.color=new Color(.5f,.55f,.6f,.3f);
        descriptionTitle.text=row.title.text;
        descriptionBody.text=row.binding?"버튼을 선택한 뒤 사용할 키나 마우스 버튼을 누르세요.\n\nESC: 변경 취소\n다른 동작에 이미 지정한 키는 사용할 수 없습니다.\n\n고정 단축키\nEnter: 채팅  ·  Tab: 점수판\nB: 상점  ·  Z: 핑\nF3: 연습 설정  ·  F10: 음성 패널":VectorSettingsData.Find(row.key).help;
    }
    public void Edited(){status.text=NetworkManager.Instance?.IsAuthenticated==true?"변경했습니다. 계정에 자동 저장합니다.":"변경했습니다. 이 PC에 저장됩니다.";}
    public void BeginBinding(VectorSettingRow row){if(!NetworkManager.CanEditAccountSettings)return;captureRow=row;captureFrame=Time.frameCount;status.text="새 키를 누르세요. ESC로 취소합니다.";EventSystem.current?.SetSelectedGameObject(null);}
    void CaptureBinding()
    {
        if(!NetworkManager.CanEditAccountSettings){captureRow=null;return;}
        if(Time.frameCount<=captureFrame)return;
        string key=null;if(Keyboard.current!=null)foreach(var k in Keyboard.current.allKeys)if(k.wasPressedThisFrame){key=k.keyCode.ToString();break;}
        var m=Mouse.current;if(m!=null){if(m.leftButton.wasPressedThisFrame)key="Mouse0";else if(m.rightButton.wasPressedThisFrame)key="Mouse1";else if(m.middleButton.wasPressedThisFrame)key="Mouse2";else if(m.backButton.wasPressedThisFrame)key="Mouse3";else if(m.forwardButton.wasPressedThisFrame)key="Mouse4";}
        if(key==null)return;if(key=="Escape"){captureRow=null;status.text="키 변경을 취소했습니다.";return;}
        if(VectorInput.Bind(captureRow.key,key,out string error)){captureRow.Refresh();captureRow=null;Edited();}else status.text=error;
    }
    void RefreshRows(){foreach(var row in rows)row.Refresh();}
    void Apply()
    {
        if(!NetworkManager.CanEditAccountSettings)return;
        NetworkManager.Instance?.AccountSettings?.Flush();
        if(confirmDeadline>0)return;
        int m=(int)VectorSettingsData.Get("DisplayMode"),r=(int)VectorSettingsData.Get("Resolution");
        if(m!=acceptedMode||r!=acceptedResolution){oldWidth=Screen.width;oldHeight=Screen.height;oldMode=Screen.fullScreenMode;var size=resolutions[r];Screen.SetResolution(size.x,size.y,m==0?FullScreenMode.ExclusiveFullScreen:m==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);confirmDeadline=Time.unscaledTime+15;displayConfirm.SetActive(true);}
        else{PlayerPrefs.Save();status.text=NetworkManager.Instance?.IsAuthenticated==true?NetworkManager.Instance.AccountSettings.Status:"이 PC에 설정을 저장했습니다.";}
    }
    void KeepDisplay(){acceptedMode=(int)VectorSettingsData.Get("DisplayMode");acceptedResolution=(int)VectorSettingsData.Get("Resolution");confirmDeadline=0;displayConfirm.SetActive(false);PlayerPrefs.SetInt("Vector.Display.Width",resolutions[acceptedResolution].x);PlayerPrefs.SetInt("Vector.Display.Height",resolutions[acceptedResolution].y);PlayerPrefs.SetInt("Vector.Display.Mode",acceptedMode);PlayerPrefs.Save();NetworkManager.Instance?.AccountSettings?.Flush();status.text="화면 설정을 저장했습니다.";}
    void RevertDisplay(){if(confirmDeadline>0)Screen.SetResolution(oldWidth,oldHeight,oldMode);confirmDeadline=0;displayConfirm.SetActive(false);VectorSettingsData.Set("DisplayMode",acceptedMode);VectorSettingsData.Set("Resolution",acceptedResolution);RefreshRows();status.text="이전 화면 설정으로 복원했습니다.";}
    void SetSubmitEnabled(bool value){redeemSubmit.interactable=value;supportSubmit.interactable=value;}
    void SendUtility(byte op,string title,string body)
    {
        if(awaitingReply){utilityStatus.text="이전 요청의 응답을 기다리고 있습니다.";return;}
        if(op==2&&(string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(body))){utilityStatus.text="제목과 내용을 입력해 주세요.";return;}
        var net=NetworkManager.Instance;
        if(net==null||!net.IsAuthenticated){utilityStatus.text="서버에 로그인한 뒤 이용할 수 있습니다.";return;}
        if(op!=pendingOperation||title!=pendingTitle||body!=pendingBody||pendingAccount!=net.MyUserId||pendingRequest==0)pendingRequest=(ulong)DateTime.UtcNow.Ticks;
        pendingOperation=op;pendingTitle=title;pendingBody=body;pendingAccount=net.MyUserId;
        if(!net.SendLobbyUtility(op,pendingRequest,title,body)){utilityStatus.text="연결 상태와 입력 길이를 확인한 뒤 다시 시도해 주세요.";return;}
        awaitingReply=true;replyDeadline=Time.unscaledTime+10;SetSubmitEnabled(false);utilityStatus.text="서버에서 확인 중입니다…";
    }
    void ReceiveUtility(LobbyUtilityReply reply)
    {
        if(reply.request!=pendingRequest||reply.operation!=pendingOperation||pendingAccount!=NetworkManager.Instance?.MyUserId)return;
        awaitingReply=false;SetSubmitEnabled(true);
        if(reply.status!=0){utilityStatus.text=reply.status switch{1=>"로그인이 필요합니다.",2=>"입력한 내용을 확인해 주세요. 코드가 없거나 사용할 수 없습니다.",3=>"이미 사용한 교환 코드입니다.",4=>"교환 코드 사용 기간이 아닙니다.",6=>"잠시 후 다시 접수해 주세요. 문의는 1분에 한 번 보낼 수 있습니다.",_=>"저장하지 못했습니다. 잠시 후 다시 시도해 주세요."};return;}
        if(reply.operation==0){
            try{var data=string.IsNullOrWhiteSpace(reply.text)?null:JsonUtility.FromJson<NoticeList>(reply.text);SetNotices(data?.entries??Array.Empty<Notice>());utilityStatus.text=noticeEntries.Length==0?"등록된 공지가 없습니다.":"";}catch(ArgumentException){SetNotices(new[]{new Notice{title="Project Vector 공지",body=reply.text}});utilityStatus.text="";}
        }else if(reply.operation==1){utilityStatus.text="교환이 완료되었습니다. 재화가 반영되었습니다.";redeemCode.text="";}
        else {utilityStatus.text="문의가 접수되었습니다. 확인 번호: "+reply.text;supportTitle.text="";supportBody.text="";}
        pendingRequest=0;
    }
    void SetNotices(Notice[] entries)
    {
        noticeEntries=entries.Take(noticeButtons.Length).Where(n=>n!=null&&!string.IsNullOrWhiteSpace(n.title)).ToArray();
        for(int i=0;i<noticeButtons.Length;i++){noticeButtons[i].gameObject.SetActive(i<noticeEntries.Length);if(i<noticeEntries.Length)noticeButtons[i].GetComponentInChildren<TMP_Text>().text=noticeEntries[i].title;}
        noticeTitle.text="공지";noticeBody.text="";if(noticeEntries.Length>0)ShowNotice(0);
    }
    void ShowNotice(int index){if(index<0||index>=noticeEntries.Length)return;noticeTitle.text=noticeEntries[index].title;noticeBody.text=noticeEntries[index].body??"";for(int i=0;i<noticeButtons.Length;i++)noticeButtons[i].GetComponent<Image>().color=i==index?new Color(.75f,.13f,.2f):new Color(.35f,.37f,.4f);Canvas.ForceUpdateCanvases();noticeScroll.verticalNormalizedPosition=1;}
}
