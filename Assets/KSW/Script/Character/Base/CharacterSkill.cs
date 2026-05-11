using UnityEngine;

public abstract class CharacterSkill : MonoBehaviour
{
    protected CharacterSkill() : base() { }

    protected abstract void UseSkill();
}