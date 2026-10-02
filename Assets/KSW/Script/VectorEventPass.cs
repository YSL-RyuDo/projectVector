using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VectorEventPass : MonoBehaviour
{
    public GameObject rewardsPanel, questsPanel;
    public TMP_Text levelLabel, statusLabel, pageLabel, questGroupLabel;
    public Button rewardTab, questTab, weeklyTab, cumulativeTab;
    public Button rewardsNavigation, questsNavigation;
    public Slider progressBar;
    public Button[] rewardButtons, questButtons;
    public TMP_Text[] rewardLabels, questLabels;
    public TMP_Text[] rewardLevels, rewardStates, questStates;
    public Image[] questProgress;
    public Image[] rewardIcons;
    public Sprite currency0Icon, currency1Icon;
    private int level = 1, rewardMask, questMask, page, questGroup;
    private long week;
    private bool ready, pending;
    private float sentAt, refreshedAt;
    private readonly int[] progress = new int[6];
    private static readonly int[] Targets = {3,3,7,10,30,50};
    private static readonly string[] Names = {"이벤트 3일 방문", "전투 3회 완료", "전투 7회 완료", "전투 누적 10회 완료", "전투 누적 30회 완료", "처치·어시스트 누적 50회"};
    private static readonly Color Selected = new Color32(195,35,61,255), Idle = new Color32(60,79,115,255);
    private void Awake()
    {
        for(int i=0;i<rewardButtons.Length;i++){ int slot=i; rewardButtons[i].onClick.AddListener(()=>ClaimReward(slot)); }
        for(int i=0;i<questButtons.Length;i++){ int slot=i; questButtons[i].onClick.AddListener(()=>Send(1,questGroup*3+slot)); }
    }
    private void OnEnable(){ ready=false; pending=false;levelLabel.text="Lv.— / 30";progressBar.value=0; ShowRewards(); Refresh(); Send(0,0); }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)){ Close(); return; }
        if(pending && Time.unscaledTime-sentAt>8){ pending=false; ready=false; statusLabel.text="응답을 받지 못했습니다. 새로고침해 주세요."; Refresh(); }
        if(ready && !pending && Time.unscaledTime-refreshedAt>30) Send(0,0);
    }
    private void OnDisable(){ pending=false; ready=false; }
    public void Open(){gameObject.SetActive(true); transform.SetAsLastSibling();}
    public void Close()=>gameObject.SetActive(false);
    public void Reload()=>Send(0,0);
    public void Previous(){page=Mathf.Max(0,page-1);Refresh();}
    public void Next(){page=Mathf.Min(5,page+1);Refresh();}
    public void ShowRewards(){rewardsPanel.SetActive(true);questsPanel.SetActive(false);rewardTab.image.color=Selected;questTab.image.color=Idle;if(rewardsNavigation!=null)rewardsNavigation.image.color=Selected;if(questsNavigation!=null)questsNavigation.image.color=Idle;}
    public void ShowQuests(){rewardsPanel.SetActive(false);questsPanel.SetActive(true);rewardTab.image.color=Idle;questTab.image.color=Selected;if(rewardsNavigation!=null)rewardsNavigation.image.color=Idle;if(questsNavigation!=null)questsNavigation.image.color=Selected; Refresh();}
    public void ShowWeekly(){questGroup=0;Refresh();}
    public void ShowCumulative(){questGroup=1;Refresh();}
    private void ClaimReward(int slot)=>Send(2,page*5+slot+1);
    private void Send(int operation,int index)
    {
        if(pending || (operation!=0 && !ready))return;
        if(NetworkManager.Instance==null || !NetworkManager.Instance.IsConnected){ready=false;Refresh();statusLabel.text="서버 연결 후 이용할 수 있습니다.";return;}
        pending=true;sentAt=Time.unscaledTime;statusLabel.text="불러오는 중…";Refresh();
        NetworkManager.Instance.SendEventPass(operation,index,week);
    }
    public void ApplyState(byte[] body)
    {
        if(body==null || body.Length!=60)return;
        pending=false; refreshedAt=Time.unscaledTime;
        int result=BitConverter.ToInt32(body,0);
        if(result==1 || result==3){ready=false;statusLabel.text="정보를 불러오지 못했습니다. 새로고침해 주세요.";Refresh();return;}
        level=BitConverter.ToInt32(body,4); int xp=BitConverter.ToInt32(body,8);
        rewardMask=BitConverter.ToInt32(body,12);questMask=BitConverter.ToInt32(body,16);week=BitConverter.ToInt64(body,28);
        for(int i=0;i<6;i++)progress[i]=BitConverter.ToInt32(body,36+i*4);
        MoneyManager.Instance?.SetServerBalances(BitConverter.ToInt32(body,20),BitConverter.ToInt32(body,24));
        ready=true;levelLabel.text=$"Lv.{level} <size=55%>/ 30</size>";progressBar.value=level==30?1:(xp%100)/100f;
        statusLabel.text=result==2?"수령 조건 또는 이미 받은 보상인지 확인해 주세요.":"퀘스트를 완료하고 패스 레벨을 올려 보세요.";
        Refresh();
    }
    private void Refresh()
    {
        for(int i=0;i<rewardButtons.Length;i++)
        {
            int n=page*5+i+1; bool claimed=(rewardMask&(1<<(n-1)))!=0;
            rewardLabels[i].text=n%5==0?"재화 1\n×3":"재화 0\n×300";
            if(rewardLevels!=null && i<rewardLevels.Length)rewardLevels[i].text=$"Lv.{n}";
            if(rewardStates!=null && i<rewardStates.Length)rewardStates[i].text=!ready?"—":claimed?"수령 완료":n<=level?"받기":"잠김";
            rewardIcons[i].sprite=n%5==0?currency1Icon:currency0Icon;
            rewardButtons[i].interactable=ready&&!pending&&!claimed&&n<=level;
            rewardButtons[i].image.color=claimed?new Color32(174,193,188,255):new Color32(240,230,215,255);
        }
        pageLabel.text=$"{page+1} / 6";
        weeklyTab.image.color=questGroup==0?Selected:Idle;cumulativeTab.image.color=questGroup==1?Selected:Idle;
        questGroupLabel.text=questGroup==0?"매주 월요일 09:00 갱신 (한국 시간)":"이벤트 첫 방문 이후의 기록";
        for(int i=0;i<questButtons.Length;i++)
        {
            int q=questGroup*3+i;bool claimed=(questMask&(1<<q))!=0;
            questLabels[i].text=$"{Names[q]}\n<size=75%>{(ready?Mathf.Min(progress[q],Targets[q]).ToString():"—")} / {Targets[q]}     패스 경험치 +100</size>";
            questButtons[i].interactable=ready&&!pending&&!claimed&&progress[q]>=Targets[q];
            if(questStates!=null && i<questStates.Length)questStates[i].text=!ready?"—":claimed?"수령 완료":progress[q]>=Targets[q]?"받기":"진행 중";
            if(questProgress!=null && i<questProgress.Length)questProgress[i].fillAmount=ready?Mathf.Clamp01((float)progress[q]/Targets[q]):0;
        }
    }
}
