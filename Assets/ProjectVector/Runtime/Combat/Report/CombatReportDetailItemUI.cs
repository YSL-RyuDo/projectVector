using TMPro;
using UnityEngine;

public class CombatReportDetailItemUI : MonoBehaviour
{
    [System.Serializable]
    private class WeaponTexts
    {
        public GameObject root;
        public TextMeshProUGUI sourceNameText;
        public TextMeshProUGUI totalDamageText;
        public TextMeshProUGUI damageBreakdownText;
        public TextMeshProUGUI hitCountText;
    }

    [System.Serializable]
    private class SkillTexts
    {
        public GameObject root;
        public TextMeshProUGUI sourceNameText;
        public TextMeshProUGUI totalDamageText;
        public TextMeshProUGUI damageBreakdownText;
        public TextMeshProUGUI hitCountText;
    }

    public TextMeshProUGUI directionText;
    public UnityEngine.UI.Image sourceIcon;
    public EquipmentIconCatalog icons;
    public void Present(CombatReportRawEntry entry)
    {
        bool received=entry.direction==1;
        string name=ReportUI.GetSourceDisplayName(entry.sourceKind,entry.sourceId,entry.characterId);
        var text=entry.sourceKind==0?weapon.sourceNameText:skill.sourceNameText;
        if(weapon.root)weapon.root.SetActive(entry.sourceKind==0);
        if(skill.root)skill.root.SetActive(entry.sourceKind==1);
        SetText(text,name);
        var damage=entry.sourceKind==0?weapon.totalDamageText:skill.totalDamageText;
        SetText(damage,((ulong)entry.hpDamage+entry.barrierDamage).ToString());
        if(damage)damage.color=received?new Color(1,.56f,.53f):new Color(.64f,.88f,1);
        SetText(directionText,received?"받은 피해":"준 피해");
        if(directionText)directionText.color=damage?damage.color:Color.white;
        SetText(entry.sourceKind==0?weapon.damageBreakdownText:skill.damageBreakdownText,$"체력 {entry.hpDamage}  /  방어막 {entry.barrierDamage}");
        SetText(entry.sourceKind==0?weapon.hitCountText:skill.hitCountText,entry.sourceKind==0?
            $"머리 {entry.headHits}   몸통 {entry.bodyHits}   다리 {entry.legHits}":$"스킬 피해 {entry.unlocalizedHits}회");
        if(sourceIcon) { sourceIcon.enabled=entry.sourceKind==0 && icons && icons.Weapon(entry.sourceId);
            if(sourceIcon.enabled)sourceIcon.sprite=icons.Weapon(entry.sourceId); }
    }

    [Header("Weapon")]
    [SerializeField] private WeaponTexts weapon = new WeaponTexts();

    [Header("Skill")]
    [SerializeField] private SkillTexts skill = new SkillTexts();

    public void SetWeaponData(
        string sourceName,
        uint totalDamage,
        uint hpDamage,
        uint barrierDamage,
        uint headHits,
        uint bodyHits,
        uint legHits)
    {
        if (weapon.root != null)
            weapon.root.SetActive(true);

        if (skill.root != null)
            skill.root.SetActive(false);

        SetText(weapon.sourceNameText, sourceName);
        SetText(weapon.totalDamageText, $"총 피해량: {totalDamage}");

        SetText(
            weapon.damageBreakdownText,
            $"체력 {hpDamage} / 보호막 {barrierDamage}"
        );

        SetText(
            weapon.hitCountText,
            $"명중: 머리 {headHits}, 몸통 {bodyHits}, 다리 {legHits}"
        );
    }

    public void SetSkillData(
        string sourceName,
        uint totalDamage,
        uint hpDamage,
        uint barrierDamage,
        uint hitCount)
    {
        if (weapon.root != null)
            weapon.root.SetActive(false);

        if (skill.root != null)
            skill.root.SetActive(true);

        SetText(skill.sourceNameText, sourceName);
        SetText(skill.totalDamageText, $"총 피해량: {totalDamage}");

        SetText(
            skill.damageBreakdownText,
            $"체력 {hpDamage} / 보호막 {barrierDamage}"
        );

        SetText(
            skill.hitCountText,
            $"피해 횟수: {hitCount}"
        );
    }

    private static void SetText(
        TextMeshProUGUI target,
        string value)
    {
        if (target != null)
            target.text = value;
    }
}