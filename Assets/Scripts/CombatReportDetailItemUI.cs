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