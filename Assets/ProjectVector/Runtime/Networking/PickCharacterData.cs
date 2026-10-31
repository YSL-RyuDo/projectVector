using System;
using UnityEngine;

[Serializable]
public class PickSkillData
{
    public string skillKey;
    public Sprite skillIcon;
    public string skillName;
    [TextArea] public string skillDescription;
}

[Serializable]
public class PickCharacterData
{
    public int characterId;

    public string characterName;
    public Sprite characterImage;

    public string roleName;
    public Sprite roleImage;

    public PickSkillData cSkill;
    public PickSkillData qSkill;
    public PickSkillData eSkill;
    public PickSkillData xSkill;
}