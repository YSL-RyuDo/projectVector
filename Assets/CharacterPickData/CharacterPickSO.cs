using UnityEngine;

public enum CharacterRole
{
    Initiator, // 이니시에이터
    Duelist,   // 엔트리
    Controller, // 컨트롤러
    Sentinel    // 센티널
}

[CreateAssetMenu(menuName = "Character/CharacterData")]
public class CharacterPickSO : ScriptableObject
{
    [Header("기본 정보")]
    public string characterName;        // 캐릭터 이름
    public CharacterRole role;          // 캐릭터 역할
    [TextArea(3, 10)]
    public string synopsis;             // 캐릭터 시놉시스

    [Header("시각 정보")]
    public GameObject characterModel;    // 캐릭터 프리팹 (선택창 표시용)
    public Sprite characterRoleIcon;        // 캐릭터 역할 스프라이트
    public Sprite characterIcon;        // 선택창 아이콘 (추가 권장)

    [Header("스킬 정보 (C, Q, E, X 순서)")]
    public SkillData skillC;
    public SkillData skillQ;
    public SkillData skillE;
    public SkillData skillX;
}

[System.Serializable]
public struct SkillData
{
    public string skillName;            // 스킬 이름
    [TextArea(2, 5)]
    public string description;          // 스킬 설명
    public Sprite skillIcon;            // 스킬 아이콘
}
