using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Data/visibility only. All panels, highlights and scroll content are saved in the scene.
public sealed class PickSelectionPresentation : MonoBehaviour
{
    public TMP_Text mapLabel, selectionStatus, roleCaption, skillKey;
    public TMP_Text[] teammateStates;
    public GameObject[] skillHighlights, characterHighlights;
    public ScrollRect skillScroll;
    public Image confirmSurface;

    public void SetSkill(int index, bool resetScroll)
    {
        for (int i = 0; i < skillHighlights.Length; i++) skillHighlights[i].SetActive(i == index);
        if (skillKey != null) skillKey.text = new[] { "C", "Q", "E", "X" }[Mathf.Clamp(index, 0, 3)] + "  /  스킬 상세";
        if (resetScroll && skillScroll != null)
        {
            skillScroll.StopMovement(); skillScroll.verticalNormalizedPosition = 1;
        }
    }
    public void SetCharacter(PickCharacterData data)
    {
        if (roleCaption != null) roleCaption.text = data.roleName == data.characterName ? "역할" : data.roleName;
        for (int i = 0; i < characterHighlights.Length; i++) characterHighlights[i].SetActive(i == data.characterId);
    }
    public void SetState(PickStateData state)
    {
        if (state == null) return;
        if (mapLabel != null) mapLabel.text = state.mapName;
        for (int i = 0; i < teammateStates.Length; i++) teammateStates[i].text = "참가 대기";
        foreach (var member in state.members)
        {
            if (member.slotIndex < teammateStates.Length)
                teammateStates[member.slotIndex].text = member.isLocked ? "선택 확정" : member.hoverCharacterId >= 0 ? "선택 중" : "캐릭터를 고르는 중";
            if (!member.isMe) continue;
            selectionStatus.text = member.isLocked ? "선택 완료 · 팀원의 준비를 기다립니다" : "캐릭터를 고른 뒤 선택을 확정하세요";
            confirmSurface.color = member.isLocked ? new Color32(65, 65, 65, 255) : Color.white;
        }
    }
}
