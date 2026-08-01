using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundStoryData", menuName = "UI/BackgroundStoryData")]
public class BackgroundStoryData : ScriptableObject
{
    [Header("캐릭터 ID")]
    public int characterId;

    [Header("캐릭터 스프라이트")]
    public Sprite characterSprite;

    [Header("캐릭터 이름")] public string characterName;
    [Header("성별")] public string gender;
    [Header("생일")] public string birthday;
    [Header("키")] public string stature;
    [Header("진영")] public string camp;
    [Header("취미")] public string hobby;
    [Header("식습관")] public string eatingHabits;
    [Header("개성 멘트")] public string individuality;

    [Header("소개")] [TextArea(1, 10)]
    public string intro;

    [Header("관찰 노트")] [TextArea(1, 10)]
    public string observationNotes;
}