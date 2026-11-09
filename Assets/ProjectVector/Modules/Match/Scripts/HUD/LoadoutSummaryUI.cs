using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadoutSummaryUI : MonoBehaviour
{
    public EquipmentIconCatalog icons;
    public Image primary,pistol,knife,armorIcon;
    public TMP_Text armorText,ultimateText;
    public void SetData(ScoreboardMemberData member)
    {
        bool known=member!=null && member.hasLoadout;
        Set(primary,known?icons.Weapon(member.primaryWeaponId):null);
        Set(pistol,known?icons.Weapon(member.pistolWeaponId):null);
        Set(knife,known?icons.Weapon(member.knifeWeaponId):null);
        Set(armorIcon,known && member.maxArmor>0?icons.Armor(member.maxArmor>=100?1:0):null);
        if(armorText)armorText.text=known?"방어막  "+member.armor+" / "+member.maxArmor:"방어막  —";
        if(ultimateText)ultimateText.text=known?"궁극기  "+member.ultimatePoints+" / "+member.ultimateRequired:"궁극기  —";
    }
    static void Set(Image image,Sprite sprite){if(!image)return;image.sprite=sprite;image.enabled=sprite!=null;image.preserveAspect=true;}
}
