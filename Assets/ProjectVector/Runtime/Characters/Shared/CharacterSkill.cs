using UnityEngine;

public abstract class CharacterSkill : MonoBehaviour
{
    protected CharacterSkill() : base() { }

    protected bool CanUsePurchasedSkill(SkillSlot slot)
    {
        if (!isActiveAndEnabled || !GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) return false;
        if (SkillInventoryState.CanUse(slot))
            return true;

        Debug.Log($"[SKILL BLOCKED] {slot} 스킬을 구매하지 않았거나 이미 사용했습니다.");
        return false;
    }

    protected abstract void UseSkill();
}
