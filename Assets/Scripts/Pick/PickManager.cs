using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickManager : MonoBehaviour
{
    [Header("Top Info")]
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private TMP_Text timerText;

    [SerializeField] private TMP_Text ourTeamRoleText;
    [SerializeField] private TMP_Text enemyTeamRoleText;

    [SerializeField] private TMP_Text blueTeamScoreText;
    [SerializeField] private TMP_Text redTeamScoreText;

    [Header("Team Pick")]
    [SerializeField] private TeamPickSlotUI[] teamPickSlots;

    [Header("Character Select")]
    [SerializeField] private PickCharacterButtonUI[] characterButtons;
    [SerializeField] private PickCharacterData[] characters;
    [SerializeField] private Button selectButton;

    [Header("Character Info")]
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Image characterRoleImage;
    [SerializeField] private TMP_Text characterRoleText;

    [Header("Skill Buttons")]
    [SerializeField] private Button cButton;
    [SerializeField] private Button qButton;
    [SerializeField] private Button eButton;
    [SerializeField] private Button xButton;

    [SerializeField] private Image cImage;
    [SerializeField] private Image qImage;
    [SerializeField] private Image eImage;
    [SerializeField] private Image xImage;

    [Header("Skill Info")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillInfoText;

    private PickStateData currentState;
    private int currentInfoCharacterId = -1;

    private void Start()
    {
        NetworkManager.Instance.SetPickManager(this);

        InitTeamPickSlotsAsWaiting();

        InitCharacterButtons();
        InitSkillButtons();

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClickSelect);
            selectButton.interactable = false;
        }

        if (characterInfoPanel != null)
            characterInfoPanel.SetActive(false);

        NetworkManager.Instance.SendPickEnter();
    }

    private void InitCharacterButtons()
    {
        if (characterButtons == null || characters == null)
            return;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (i >= characters.Length)
                break;

            int index = i;

            if (characterButtons[i] != null)
                characterButtons[i].Init(characters[index], OnClickCharacter);
        }
    }

    private void InitSkillButtons()
    {
        if (cButton != null)
        {
            cButton.onClick.RemoveAllListeners();
            cButton.onClick.AddListener(() => ShowSkillInfo(GetCurrentCharacterData()?.cSkill));
        }

        if (qButton != null)
        {
            qButton.onClick.RemoveAllListeners();
            qButton.onClick.AddListener(() => ShowSkillInfo(GetCurrentCharacterData()?.qSkill));
        }

        if (eButton != null)
        {
            eButton.onClick.RemoveAllListeners();
            eButton.onClick.AddListener(() => ShowSkillInfo(GetCurrentCharacterData()?.eSkill));
        }

        if (xButton != null)
        {
            xButton.onClick.RemoveAllListeners();
            xButton.onClick.AddListener(() => ShowSkillInfo(GetCurrentCharacterData()?.xSkill));
        }
    }

    private void OnClickCharacter(int characterId)
    {
        PickTeamMemberData me = GetMe();

        if (me != null && me.isLocked)
            return;

        if (IsCharacterLockedByTeam(characterId))
            return;

        currentInfoCharacterId = characterId;
        ApplyCharacterInfo(characterId);

        NetworkManager.Instance.SendPickHover(characterId);
    }

    private void OnClickSelect()
    {
        PickTeamMemberData me = GetMe();

        if (me == null)
            return;

        if (me.isLocked)
            return;

        int selectCharacterId = me.hoverCharacterId;

        if (selectCharacterId < 0 && currentInfoCharacterId >= 0)
            selectCharacterId = currentInfoCharacterId;

        if (selectCharacterId < 0)
            return;

        if (IsCharacterLockedByTeam(selectCharacterId))
            return;

        NetworkManager.Instance.SendPickLock(selectCharacterId);
    }

    public void OnRecvPickState(PickStateData state)
    {
        currentState = state;

        ApplyTopInfo();
        ApplyTeamPickSlots();
        ApplyCharacterButtons();
        ApplySelectButton();

        ApplyCurrentCharacterInfoSafely();
    }

    public void OnRecvPickTimer(ushort remainSeconds)
    {
        ApplyTimer(remainSeconds);
    }

    public void OnRecvPickLockResult(byte result, int characterId)
    {
        Debug.Log($"[PICK LOCK RESULT] result={result}, characterId={characterId}");

        if (result != (byte)PickLockResult.SUCCESS)
        {
            ApplySelectButton();
            ApplyCurrentCharacterInfoSafely();
        }
    }

    private void ApplyTopInfo()
    {
        if (currentState == null)
            return;

        if (mapText != null)
            mapText.text = currentState.mapName;

        ApplyTimer(currentState.remainingSeconds);

        if (ourTeamRoleText != null)
        {
            ourTeamRoleText.text =
                RoleToText(currentState.firstRole) + " / " +
                RoleToText(currentState.secondRole);
        }

        if (enemyTeamRoleText != null)
        {
            enemyTeamRoleText.text =
                RoleToText(GetOppositeRole(currentState.firstRole)) + " / " +
                RoleToText(GetOppositeRole(currentState.secondRole));
        }

        if (blueTeamScoreText != null)
            blueTeamScoreText.text = currentState.blueScore.ToString();

        if (redTeamScoreText != null)
            redTeamScoreText.text = currentState.redScore.ToString();
    }

    private void ApplyTimer(ushort remainSeconds)
    {
        int min = remainSeconds / 60;
        int sec = remainSeconds % 60;

        if (timerText != null)
            timerText.text = $"{min:00}:{sec:00}";
    }

    private void ApplyTeamPickSlots()
    {
        if (teamPickSlots == null)
            return;

        // 슬롯 자체는 숨기지 않고 기본 상태로만 초기화
        for (int i = 0; i < teamPickSlots.Length; i++)
        {
            if (teamPickSlots[i] != null)
                teamPickSlots[i].Clear();
        }

        if (currentState == null || currentState.members == null)
            return;

        for (int i = 0; i < currentState.members.Count; i++)
        {
            PickTeamMemberData member = currentState.members[i];

            if (member.slotIndex >= teamPickSlots.Length)
                continue;

            Sprite characterSprite = null;
            int visibleCharacterId = -1;

            if (member.isLocked)
                visibleCharacterId = member.lockedCharacterId;
            else
                visibleCharacterId = member.hoverCharacterId;

            PickCharacterData character = GetCharacterData(visibleCharacterId);

            if (character != null)
                characterSprite = character.characterImage;

            if (teamPickSlots[member.slotIndex] != null)
            {
                teamPickSlots[member.slotIndex].SetData(
                    member.nickname,
                    characterSprite,
                    member.isLocked,
                    member.isMe
                );
            }
        }
    }

    private void ApplyCharacterButtons()
    {
        if (characterButtons == null || characters == null)
            return;

        PickTeamMemberData me = GetMe();
        bool myLocked = me != null && me.isLocked;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (i >= characters.Length)
                break;

            if (characterButtons[i] == null)
                continue;

            int charId = characters[i].characterId;

            PickTeamMemberData myPickMember = FindLockedMemberByCharacter(charId, true);
            PickTeamMemberData teamPickMember = FindLockedMemberByCharacter(charId, false);

            bool isMyPick = myPickMember != null;
            bool isTeamPick = teamPickMember != null;

            string nickname = "";

            if (isMyPick)
                nickname = myPickMember.nickname;
            else if (isTeamPick)
                nickname = teamPickMember.nickname;

            bool interactable = !myLocked && !isTeamPick && !isMyPick;

            characterButtons[i].SetPickState(
                isMyPick,
                isTeamPick,
                nickname,
                interactable
            );
        }
    }

    private void ApplySelectButton()
    {
        if (selectButton == null)
            return;

        PickTeamMemberData me = GetMe();

        if (me == null)
        {
            selectButton.interactable = false;
            SetSelectButtonText("Select");
            return;
        }

        if (me.isLocked)
        {
            selectButton.interactable = false;
            SetSelectButtonText("선택 완료");
            return;
        }

        SetSelectButtonText("Select");

        int candidateCharacterId = me.hoverCharacterId;

        if (candidateCharacterId < 0 && currentInfoCharacterId >= 0)
            candidateCharacterId = currentInfoCharacterId;

        if (candidateCharacterId < 0)
        {
            selectButton.interactable = false;
            return;
        }

        if (IsCharacterLockedByTeam(candidateCharacterId))
        {
            selectButton.interactable = false;
            return;
        }

        selectButton.interactable = true;
    }

    private void SetSelectButtonText(string text)
    {
        if (selectButton == null)
            return;

        TMP_Text buttonText = selectButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
            buttonText.text = text;
    }

    private void ApplyCurrentCharacterInfoSafely()
    {
        PickTeamMemberData me = GetMe();

        if (me == null)
        {
            return;
        }

        int characterId = -1;

        if (me.isLocked)
        {
            characterId = me.lockedCharacterId;
        }
        else if (me.hoverCharacterId >= 0)
        {
            characterId = me.hoverCharacterId;
        }
        else if (currentInfoCharacterId >= 0)
        {
            characterId = currentInfoCharacterId;
        }

        if (characterId < 0)
        {
            return;
        }

        currentInfoCharacterId = characterId;
        ApplyCharacterInfo(characterId);
    }

    private void ApplyCharacterInfo(int characterId)
    {
        PickCharacterData data = GetCharacterData(characterId);

        if (data == null)
            return;

        if (characterInfoPanel != null)
            characterInfoPanel.SetActive(true);

        if (characterImage != null)
        {
            characterImage.gameObject.SetActive(true);
            characterImage.enabled = true;
            characterImage.sprite = data.characterImage;
        }

        if (characterNameText != null)
            characterNameText.text = data.characterName;

        if (characterRoleImage != null)
        {
            characterRoleImage.gameObject.SetActive(true);
            characterRoleImage.enabled = true;
            characterRoleImage.sprite = data.roleImage;
        }

        if (characterRoleText != null)
            characterRoleText.text = data.roleName;

        ApplySkillButtonVisual(cImage, data.cSkill);
        ApplySkillButtonVisual(qImage, data.qSkill);
        ApplySkillButtonVisual(eImage, data.eSkill);
        ApplySkillButtonVisual(xImage, data.xSkill);

        ShowSkillInfo(data.cSkill);
    }

    private void ApplySkillButtonVisual(Image image, PickSkillData skill)
    {
        if (image == null)
            return;

        if (skill == null)
        {
            image.enabled = false;
            image.sprite = null;
            return;
        }

        image.enabled = true;
        image.gameObject.SetActive(true);
        image.sprite = skill.skillIcon;
    }

    private void ShowSkillInfo(PickSkillData skill)
    {
        if (skill == null)
            return;

        if (skillNameText != null)
            skillNameText.text = skill.skillName;

        if (skillInfoText != null)
            skillInfoText.text = skill.skillDescription;
    }

    private PickTeamMemberData GetMe()
    {
        if (currentState == null || currentState.members == null)
            return null;

        for (int i = 0; i < currentState.members.Count; i++)
        {
            if (currentState.members[i].isMe)
                return currentState.members[i];
        }

        return null;
    }

    private PickTeamMemberData FindLockedMemberByCharacter(int characterId, bool onlyMe)
    {
        if (currentState == null || currentState.members == null)
            return null;

        for (int i = 0; i < currentState.members.Count; i++)
        {
            PickTeamMemberData member = currentState.members[i];

            if (!member.isLocked)
                continue;

            if (member.lockedCharacterId != characterId)
                continue;

            if (onlyMe && !member.isMe)
                continue;

            if (!onlyMe && member.isMe)
                continue;

            return member;
        }

        return null;
    }

    private bool IsCharacterLockedByTeam(int characterId)
    {
        if (currentState == null || currentState.members == null)
            return false;

        for (int i = 0; i < currentState.members.Count; i++)
        {
            PickTeamMemberData member = currentState.members[i];

            if (member.isLocked && member.lockedCharacterId == characterId)
                return true;
        }

        return false;
    }

    private PickCharacterData GetCurrentCharacterData()
    {
        return GetCharacterData(currentInfoCharacterId);
    }

    private PickCharacterData GetCharacterData(int characterId)
    {
        if (characterId < 0)
            return null;

        if (characters == null)
            return null;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null && characters[i].characterId == characterId)
                return characters[i];
        }

        return null;
    }

    private string RoleToText(PickRole role)
    {
        if (role == PickRole.ATTACK)
            return "선 공격";

        if (role == PickRole.DEFENSE)
            return "후 수비";

        return "";
    }

    private PickRole GetOppositeRole(PickRole role)
    {
        if (role == PickRole.ATTACK)
            return PickRole.DEFENSE;

        return PickRole.ATTACK;
    }

    private void InitTeamPickSlotsAsWaiting()
    {
        if (teamPickSlots == null)
            return;

        for (int i = 0; i < teamPickSlots.Length; i++)
        {
            if (teamPickSlots[i] == null)
                continue;

            teamPickSlots[i].SetWaitingSlot("");
        }
    }
}