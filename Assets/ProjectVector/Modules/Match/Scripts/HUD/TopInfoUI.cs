using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class TopInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text mapText,timerText,ourTeamRoleText,enemyTeamRoleText,gameDescriptionText;
    [SerializeField] private TMP_Text ourTeamScoreText,enemyTeamScoreText,roundLabel,phaseHint,announcementText;
    [SerializeField] private Image phaseAccent;
    public static TopInfoUI Instance;
    Coroutine timerRoutine; byte myTeam,currentRound=1; int firstHalf=5,secondHalf=5;
    bool playing,planted; string phase=""; float announcementUntil;
    static readonly Color Ice=new(.62f,.87f,1), Warm=new(1,.78f,.36f);
    void Awake(){Instance=this;}
    void OnDestroy(){if(Instance==this)Instance=null;}
    void Update(){if(announcementText && announcementText.gameObject.activeSelf && Time.unscaledTime>=announcementUntil)announcementText.gameObject.SetActive(false);}
    public void ApplyPickInfo(PickStateData state)
    {
        if(state==null)return;playing=false;SetMapText(state.mapName);SetMyTeam((byte)state.myTeam);
        ourTeamRoleText.text=myTeam==0?"선 공격 / 후 수비":"선 수비 / 후 공격";
        enemyTeamRoleText.text=myTeam==0?"선 수비 / 후 공격":"선 공격 / 후 수비";
        SetScoreByTeam(state.blueScore,state.redScore);SetGameDescription("캐릭터 선택");
    }
    public void ApplyGameStartInfo(){playing=true;currentRound=1;planted=false;SetCurrentRoundRole(1);SetGameDescription("구매 단계");}
    public void ApplyRoundRules(int first,int second){if(first<1||second<1||first+second>=255)return;firstHalf=first;secondHalf=second;if(playing)RefreshRole();}
    public void SetMyTeam(byte team){myTeam=team;if(playing)RefreshRole();}
    public void SetScoreByTeam(int a,int b){if(ourTeamScoreText)ourTeamScoreText.text=(myTeam==0?a:b).ToString();if(enemyTeamScoreText)enemyTeamScoreText.text=(myTeam==0?b:a).ToString();}
    public void SetRoundWinnerText(byte winner){planted=false;SetGameDescription(winner==myTeam?"라운드 승리":"라운드 패배");}
    public void SetMatchWinnerText(byte winner){planted=false;SetGameDescription(winner==myTeam?"최종 승리":"최종 패배");}
    public void SetMapText(string name){if(mapText)mapText.text=name;}
    public void SetGameDescription(string text)
    {
        if(text.Contains("획득")){Announce(text);return;}
        if(text=="공수 교대"){Announce(text);return;}
        if(text=="구매 단계"){planted=false;phase="구매 단계";}
        else if(text=="전투 단계"){if(planted)return;if(phase=="구매 단계")Announce("구매 종료  /  전투 시작");phase="전투 중";}
        else if(text=="디바이스 접속 중"){planted=true;phase="디바이스 설치됨";}
        else {phase=text;if(text.Contains("차단")||text.Contains("승리")||text.Contains("패배"))planted=false;}
        if(gameDescriptionText)gameDescriptionText.text=phase;
        if(phaseAccent)phaseAccent.color=planted?Warm:Ice;
        if(phaseHint)phaseHint.text=planted?"AI 제어권 접속 중":phase=="구매 단계"?"B  장비 구매":phase=="전투 중"?(OurAttack()?"디바이스를 설치하세요":"설치 구역을 방어하세요"):"";
    }
    public void Announce(string message){if(!announcementText)return;announcementText.text=message;announcementText.gameObject.SetActive(true);announcementUntil=Time.unscaledTime+4;}
    public void SetTimer(ushort seconds){if(!timerText)return;if(planted){timerText.text="접속 중";timerText.fontSize=23;return;}timerText.fontSize=34;timerText.text=$"{seconds/60:00}:{seconds%60:00}";timerText.color=seconds<=10?Warm:Color.white;}
    public void ResetMatchScore(){SetScoreByTeam(0,0);}
    public void HideRoleTextsForGame(){if(playing)RefreshRole();else {ourTeamRoleText.text="";enemyTeamRoleText.text="";}}
    public void StartLocalCountdown(ushort seconds){StopLocalCountdown();timerRoutine=StartCoroutine(Countdown(seconds));}
    public void StopLocalCountdown(){if(timerRoutine!=null)StopCoroutine(timerRoutine);timerRoutine=null;}
    IEnumerator Countdown(ushort seconds){for(int n=seconds;n>=0;n--){SetTimer((ushort)n);if(n==0)break;yield return new WaitForSecondsRealtime(1);}timerRoutine=null;}
    public bool IsATeamAttackRound(int round)=>round<=firstHalf || (round>firstHalf+secondHalf && (round-firstHalf-secondHalf-1)%2==0);
    bool OurAttack()=>myTeam==0?IsATeamAttackRound(currentRound):!IsATeamAttackRound(currentRound);
    public void SetCurrentRoundRole(byte round){bool changed=playing && currentRound+1==round && IsATeamAttackRound(currentRound)!=IsATeamAttackRound(round);currentRound=round;playing=true;RefreshRole();if(changed)Announce("공수 교대  /  "+(OurAttack()?"공격 시작":"수비 시작"));}
    void RefreshRole(){if(ourTeamRoleText)ourTeamRoleText.text=OurAttack()?"공격":"수비";if(enemyTeamRoleText)enemyTeamRoleText.text=OurAttack()?"수비":"공격";if(roundLabel)roundLabel.text=currentRound>firstHalf+secondHalf?$"연장  {currentRound-firstHalf-secondHalf}":$"ROUND  {currentRound:00}";}
    public void SetUnknownTimer(){StopLocalCountdown();if(timerText){timerText.text=planted?"접속 중":"—";timerText.fontSize=23;timerText.color=Warm;}}
}
