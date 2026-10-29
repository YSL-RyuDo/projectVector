using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundHistoryColumnUI : MonoBehaviour
{
    [SerializeField] private GameObject topMarker;
    [SerializeField] private GameObject bottomMarker;

    [SerializeField] private Image ourTeamResultIcon;
    [SerializeField] private Image enemyTeamResultIcon;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text roundNumber;
    [SerializeField] private TMP_Text winnerText, reasonText;
    [SerializeField] private TMP_Text currentText;
    [SerializeField] private Image resultAccent;
    private int roundIndex;

    public void SetRoundNumber(int round)
    {
        roundIndex=round;
        if(roundNumber)roundNumber.text=round+"R";
    }

    public void SetEmpty()
    {
        if(background)background.color=new Color(.075f,.11f,.16f,1f);
        if(topMarker)topMarker.SetActive(false);
        if(bottomMarker)bottomMarker.SetActive(false);
        if(ourTeamResultIcon)ourTeamResultIcon.enabled=false;
        if(enemyTeamResultIcon)enemyTeamResultIcon.enabled=false;
        if(winnerText){winnerText.text="";winnerText.enabled=true;}
        if(currentText)currentText.enabled=false;
        if(resultAccent)resultAccent.enabled=false;
        if(reasonText){reasonText.text="";reasonText.enabled=false;}
    }

    public void SetCurrent(bool isCurrent)
    {
        if(topMarker)topMarker.SetActive(isCurrent);
        if(bottomMarker)bottomMarker.SetActive(isCurrent);
        if(currentText)currentText.enabled=isCurrent;
        if(winnerText)winnerText.enabled=!isCurrent;
    }

    public void SetResult(bool ourTeamWin, bool winnerAttacking, RoundWinReason reason, Sprite resultSprite)
    {
        Color accent=ourTeamWin?new Color(.62f,.82f,1f):new Color(1f,.64f,.66f);
        if(background)background.color=new Color(.075f,.11f,.16f,1f);
        if(resultAccent){resultAccent.enabled=true;resultAccent.color=accent;}
        var target=ourTeamWin?ourTeamResultIcon:enemyTeamResultIcon;
        var other=ourTeamWin?enemyTeamResultIcon:ourTeamResultIcon;
        if(target){target.sprite=resultSprite;target.color=accent;target.enabled=resultSprite;}
        if(other)other.enabled=false;
        if(roundNumber)roundNumber.text=roundIndex+"R  "+(winnerAttacking?"공격":"수비");
        if(winnerText){winnerText.text=ourTeamWin?"승":"패";winnerText.color=accent;}
        if(reasonText)reasonText.text=reason switch
        {
            RoundWinReason.ATTACK_ELIMINATION or RoundWinReason.DEFENSE_ELIMINATION=>"전멸",
            RoundWinReason.ATTACK_SPIKE_EXPLODE=>"제어권 확보",
            RoundWinReason.DEFENSE_SPIKE_DEFUSE=>"디바이스 해체",
            RoundWinReason.DEFENSE_TIME_OUT=>"시간 만료",
            _=>"라운드 종료"
        };
    }
}
