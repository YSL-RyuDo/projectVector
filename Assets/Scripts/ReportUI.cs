using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ReportUI : MonoBehaviour
{
    public static ReportUI Instance { get; private set; }
    [SerializeField] private GameObject rootPanel, summaryView, detailView, emptyNotice, failedNotice, limitWarningNotice;
    [SerializeField] private Button toggleExpandButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;
    [SerializeField] private TextMeshProUGUI opponentInfoText, damageSummaryText, hitsDealtText, hitsTakenText;
    // Retain the original author's references. Rows are now authored in the scene before Play.
    [SerializeField] private Transform detailItemContainer;
    [SerializeField] private GameObject detailItemPrefab;
    [SerializeField] private bool spawnPerWeaponLine=true;
    [SerializeField] private float requestTimeoutSeconds=5;
    public CombatReportDetailItemUI[] detailRows;
    public TMP_Text dealtValue,takenValue,opponentCounter,pageCounter,loadingText,killerLabel;
    public Button previousOpponent,nextOpponent,previousPage,nextPage,retryButton;
    public CanvasGroup presentation;
    private bool isExpanded,hasValidData,isWaitingResponse;
    private float requestSentTime;
    private int opponentIndex,detailPage;
    private readonly List<OpponentCombatSummary> cachedSummaries=new();
    public IReadOnlyList<OpponentCombatSummary> Summaries => cachedSummaries;
    public bool IsExpanded => isExpanded;
    public int OpponentIndex => opponentIndex;
    public int DetailPage => detailPage;
    public bool IsWaiting => isWaitingResponse;
    public bool IsVisible => rootPanel && rootPanel.activeSelf;

    public static bool OverlayBlocked => GameplayActionPolicy.MenuBlocksInput ||
        (InGameScoreboardUI.Instance && InGameScoreboardUI.Instance.IsOpen()) ||
        (StorePanelUI.Instance && StorePanelUI.Instance.IsOpen());

    void Awake()
    {
        if(Instance && Instance!=this) { Destroy(gameObject); return; }
        Instance=this;
        if(toggleExpandButton) toggleExpandButton.onClick.AddListener(ToggleExpand);
        if(previousOpponent) previousOpponent.onClick.AddListener(()=>SelectOpponent(-1));
        if(nextOpponent) nextOpponent.onClick.AddListener(()=>SelectOpponent(1));
        if(previousPage) previousPage.onClick.AddListener(()=>ChangePage(-1));
        if(nextPage) nextPage.onClick.AddListener(()=>ChangePage(1));
        if(retryButton) retryButton.onClick.AddListener(Retry);
        CloseAndReset(); NetworkManager.Instance?.AttachCombatReportView();
    }
    void OnDestroy() { if(Instance==this) { Instance=null; NetworkManager.Instance?.ResetLocalCombatReport(); } }
    void Update()
    {
        if(!IsVisible) return;
        SetPresentationSuppressed(OverlayBlocked);
        if(isWaitingResponse && Time.unscaledTime-requestSentTime>=requestTimeoutSeconds) ShowFailed();
        if(OverlayBlocked || !(SpectatorManager.Instance && SpectatorManager.Instance.IsSpectating)) return;
        var keys=Keyboard.current;
        if(keys!=null) {
            if(keys.eKey.wasPressedThisFrame) ToggleExpand();
            if(keys.leftBracketKey.wasPressedThisFrame) SelectOpponent(-1);
            if(keys.rightBracketKey.wasPressedThisFrame) SelectOpponent(1);
            if(keys.rKey.wasPressedThisFrame && failedNotice && failedNotice.activeSelf) Retry();
        }
        if(isExpanded && Mouse.current!=null) {
            float scroll=Mouse.current.scroll.ReadValue().y;
            if(Mathf.Abs(scroll)>0) ChangePage(scroll<0?1:-1);
        }
    }
    public void SetPresentationSuppressed(bool suppressed)
    {
        if(presentation) { presentation.alpha=suppressed?0:1; presentation.interactable=!suppressed; presentation.blocksRaycasts=!suppressed; }
    }
    static void Active(GameObject obj,bool value) { if(obj) obj.SetActive(value); }
    public void Open()
    {
        CloseAndReset(); Active(rootPanel,true); isWaitingResponse=true; requestSentTime=Time.unscaledTime;
        if(loadingText) { loadingText.text="전투 기록 확인 중"; loadingText.gameObject.SetActive(true); }
        RefreshControls();
    }
    public void NotifyPageReceived() { if(isWaitingResponse) requestSentTime=Time.unscaledTime; }
    public void CloseAndReset()
    {
        cachedSummaries.Clear(); hasValidData=isWaitingResponse=isExpanded=false; opponentIndex=detailPage=0;
        Active(rootPanel,false); Active(summaryView,false); Active(detailView,false);
        Active(emptyNotice,false); Active(failedNotice,false); Active(limitWarningNotice,false);
        if(loadingText) loadingText.gameObject.SetActive(false);
        if(retryButton) retryButton.gameObject.SetActive(false);
        if(detailRows!=null) foreach(var row in detailRows) if(row) row.gameObject.SetActive(false);
        SetPresentationSuppressed(false);
    }
    public void ShowFailed()
    {
        if(!IsVisible) return;
        hasValidData=isWaitingResponse=false;
        if(loadingText) loadingText.gameObject.SetActive(false);
        Active(summaryView,false); Active(detailView,false); Active(emptyNotice,false); Active(failedNotice,true);
        if(retryButton) retryButton.gameObject.SetActive(true);
        RefreshControls();
    }
    void Retry() { NetworkManager.Instance?.RetryLocalCombatReport(); }
    public void ApplyData(List<CombatReportRawEntry> entries,uint killerId,bool limitReached)
    {
        if(!IsVisible) return; // A delayed response must not resurrect a closed report.
        cachedSummaries.Clear(); isWaitingResponse=false; opponentIndex=detailPage=0;
        if(loadingText) loadingText.gameObject.SetActive(false);
        Active(failedNotice,false); Active(limitWarningNotice,limitReached);
        if(retryButton) retryButton.gameObject.SetActive(false);
        var summaries=new Dictionary<uint,OpponentCombatSummary>();
        if(entries!=null) foreach(var e in entries) {
            if(!summaries.TryGetValue(e.opponentId,out var s)) {
                s=new OpponentCombatSummary { opponentId=e.opponentId,characterId=e.characterId,
                    nickname=string.IsNullOrWhiteSpace(e.nickname)?"알 수 없음":e.nickname,
                    isKiller=killerId!=0 && e.opponentId==killerId };
                summaries.Add(e.opponentId,s);
            }
            s.details.Add(e); ulong damage=(ulong)e.hpDamage+e.barrierDamage;
            if(e.direction==0) {
                s.damageDealt+=damage;
                if(e.sourceKind==0) { s.hitsDealtHead+=e.headHits; s.hitsDealtBody+=e.bodyHits; s.hitsDealtLeg+=e.legHits; }
            } else {
                s.damageTaken+=damage;
                if(e.sourceKind==0) { s.hitsTakenHead+=e.headHits; s.hitsTakenBody+=e.bodyHits; s.hitsTakenLeg+=e.legHits; }
            }
        }
        cachedSummaries.AddRange(summaries.Values);
        cachedSummaries.Sort((a,b)=> { int c=b.isKiller.CompareTo(a.isKiller); return c!=0?c:(b.damageDealt+b.damageTaken).CompareTo(a.damageDealt+a.damageTaken); });
        foreach(var s in cachedSummaries) s.details.Sort((a,b)=> {
            int c=a.direction.CompareTo(b.direction); if(c!=0)return c;
            c=a.sourceKind.CompareTo(b.sourceKind); return c!=0?c:a.sourceId.CompareTo(b.sourceId);
        });
        hasValidData=cachedSummaries.Count>0;
        Active(emptyNotice,!hasValidData);
        RefreshUI();
    }
    public void ToggleExpand() { if(!hasValidData)return; isExpanded=!isExpanded; RefreshUI(); }
    public void SelectOpponent(int delta)
    {
        if(!hasValidData)return;
        opponentIndex=(opponentIndex+delta%cachedSummaries.Count+cachedSummaries.Count)%cachedSummaries.Count;
        detailPage=0; RefreshUI();
    }
    public void ChangePage(int delta)
    {
        if(!hasValidData || !isExpanded)return;
        detailPage=Mathf.Clamp(detailPage+delta,0,PageCount-1); RefreshUI();
    }
    int Capacity => Mathf.Max(1,detailRows?.Length??0);
    int PageCount => hasValidData?Mathf.Max(1,(cachedSummaries[opponentIndex].details.Count+Capacity-1)/Capacity):1;
    void RefreshControls()
    {
        if(toggleButtonText) toggleButtonText.text=isExpanded?"E  접기":"E  상세";
        if(toggleExpandButton) toggleExpandButton.interactable=hasValidData;
        if(previousOpponent) previousOpponent.interactable=cachedSummaries.Count>1;
        if(nextOpponent) nextOpponent.interactable=cachedSummaries.Count>1;
        if(opponentCounter) opponentCounter.text=hasValidData?$"{opponentIndex+1} / {cachedSummaries.Count}":"";
        if(pageCounter) pageCounter.text=$"{detailPage+1} / {PageCount}";
        if(previousPage) previousPage.interactable=detailPage>0;
        if(nextPage) nextPage.interactable=detailPage+1<PageCount;
    }
    void RefreshUI()
    {
        Active(summaryView,hasValidData); Active(detailView,hasValidData && isExpanded); RefreshControls();
        if(!hasValidData)return;
        var s=cachedSummaries[opponentIndex];
        if(opponentInfoText) { opponentInfoText.richText=false; opponentInfoText.text=s.nickname; }
        if(killerLabel) killerLabel.text=s.isKiller?"나를 처치한 상대":"교전 상대";
        if(damageSummaryText) damageSummaryText.text="체력 + 방어막 피해";
        if(dealtValue) dealtValue.text=s.damageDealt.ToString();
        if(takenValue) takenValue.text=s.damageTaken.ToString();
        if(hitsDealtText) hitsDealtText.text=$"{s.hitsDealtHead} / {s.hitsDealtBody} / {s.hitsDealtLeg}";
        if(hitsTakenText) hitsTakenText.text=$"{s.hitsTakenHead} / {s.hitsTakenBody} / {s.hitsTakenLeg}";
        if(detailRows==null)return;
        for(int i=0;i<detailRows.Length;i++) {
            var row=detailRows[i]; if(!row)continue; int n=detailPage*Capacity+i;
            row.gameObject.SetActive(n<s.details.Count);
            if(n<s.details.Count)row.Present(s.details[n]);
        }
    }
    public static string GetSourceDisplayName(byte kind,int id,int character)
    {
        if(kind==0) {
            string[] names={"P92","P1911","R1895","UZI","UMP","S1897","AA-12","Dragunov","M416","AK47","M249","Kar98","AWM","나이프"};
            return id>=0 && id<names.Length?names[id]:"무기";
        }
        return id switch {1=>"테이저 드론",2=>"방전",3=>"블레이드",4=>"센티널 궁극기",_=>"스킬"};
    }
    [ContextMenu("[테스트] 전투 리포트")]
    public void TestWithSampleData()
    {
        if(Application.isPlaying)return;
        Open(); ApplyData(new List<CombatReportRawEntry>{new() {opponentId=1,nickname="상대",sourceId=9,direction=1,hpDamage=100,headHits=1}},1,false);
    }
}
