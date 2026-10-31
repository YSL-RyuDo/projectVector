using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class KillLogEntryUI : MonoBehaviour
{
    [SerializeField] private Image killerImage,deathImage,weaponImage,teamAccent;
    [SerializeField] private TMP_Text killerNicknameText,deathNicknameText,assistText,sourceText,headshotText;
    [SerializeField] private EquipmentIconCatalog equipmentIcons;
    public void Initialize(Sprite killer,Sprite dead,string killerName,string deadName){killerImage.sprite=killer;killerImage.enabled=killer;deathImage.sprite=dead;deathImage.enabled=dead;killerNicknameText.text=SafeName(killerName);deathNicknameText.text=SafeName(deadName);SetDetails(-2,false,"");}
    public void SetTeam(bool ours){var c=ours?new Color(.56f,.88f,1):new Color(1,.48f,.49f);if(teamAccent)teamAccent.color=c;killerNicknameText.color=c;}
    public void SetDetails(int weaponId,bool headshot,string assists)
    {
        if(weaponImage){weaponImage.sprite=equipmentIcons?equipmentIcons.Weapon(weaponId):null;weaponImage.enabled=weaponImage.sprite;}
        if(sourceText)sourceText.text=weaponId==-1?"스킬":weaponId==-2?"—":weaponImage && weaponImage.sprite?"":"처치";
        if(headshotText)headshotText.text=headshot?"HS":"";
        if(assistText)assistText.text=string.IsNullOrEmpty(assists)?"":"도움  "+assists;
    }
    public static string SafeName(string value)=>string.IsNullOrWhiteSpace(value)?"알 수 없음":value.Replace("<","‹").Replace(">","›");
}
