using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickManager : MonoBehaviour
{
    public static PickManager Instance;

    [Header("Top Info")]
    [SerializeField] private TopInfoUI topInfoUI;

    [Header("Authored Match Map Background")]
    [SerializeField] private PickMapBackdrop mapBackdrop;
    [SerializeField] private PickSelectionPresentation presentation;

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

    [Header("Character Preview Model")]
    [SerializeField] private Transform previewSpawnPoint;
    [SerializeField] private GameObject[] characterModelPrefabs;
    [SerializeField] private GameObject[] authoredPreviewModels;
    [SerializeField] private Vector3 previewLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 previewLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 previewLocalScale = Vector3.one;

    private GameObject currentPreviewModel;
    private int currentPreviewCharacterId = -1;

    private PickStateData currentState;
    private int currentInfoCharacterId = -1;
    private int displayedInfoCharacterId = -1;
    private int selectedSkillIndex;

    private bool isInputLockedByPhase = true;
    private PickPhase currentPhase = PickPhase.PICKING;
    [SerializeField] private MatchLoadingPanelUI matchLoadingPanelUI;

    private void Awake()
    {
        Instance = this;
    }

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

        ApplyTimer(20);

        if (characterInfoPanel != null)
            characterInfoPanel.SetActive(false);

        NetworkManager.Instance.SendPickEnter();
    }

    private void OnDestroy()
    {
        ClearPreviewModel();

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SetPickManager(null);
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
            cButton.onClick.AddListener(() => SelectSkill(0));
        }

        if (qButton != null)
        {
            qButton.onClick.RemoveAllListeners();
            qButton.onClick.AddListener(() => SelectSkill(1));
        }

        if (eButton != null)
        {
            eButton.onClick.RemoveAllListeners();
            eButton.onClick.AddListener(() => SelectSkill(2));
        }

        if (xButton != null)
        {
            xButton.onClick.RemoveAllListeners();
            xButton.onClick.AddListener(() => SelectSkill(3));
        }
    }

    private void OnClickCharacter(int characterId)
    {
        if (isInputLockedByPhase)
            return;

        PickTeamMemberData me = GetMe();

        if (me != null && me.isLocked)
            return;

        if (IsCharacterLockedByTeam(characterId))
            return;

        currentInfoCharacterId = characterId;

        ApplyCharacterInfo(characterId);
        ShowPreviewModel(characterId);

        NetworkManager.Instance.SendPickHover(characterId);
    }

    private void OnClickSelect()
    {
        if (isInputLockedByPhase)
            return;

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
        if (mapBackdrop != null) mapBackdrop.ApplyMap(state != null ? state.mapName : null);
        if (presentation != null) presentation.SetState(state);

        ApplyTopInfo();
        ApplyTeamPickSlots();
        ApplyCharacterButtons();
        ApplySelectButton();
        ApplyCurrentCharacterInfoSafely();
    }

    public void OnRecvPickTimer(PickPhase phase, ushort remainSeconds)
    {
        currentPhase = phase;
        ApplyTimer(remainSeconds);

        if (phase == PickPhase.PICKING)
        {
            isInputLockedByPhase = false;

            ApplyCharacterButtons();
            ApplySelectButton();
            return;
        }

        if (phase == PickPhase.PREPARE)
        {
            isInputLockedByPhase = true;

            //if (preparePanel != null)
            //    preparePanel.SetActive(true);

            // 여기서 matchLoadingPanelUI.Show() 호출하면 안 됨
            ApplyCharacterButtons();
            ApplySelectButton();
            ApplyCurrentCharacterInfoSafely();
            return;
        }

        if (phase == PickPhase.COMPLETE)
        {
            isInputLockedByPhase = true;

            ApplyCharacterButtons();
            ApplySelectButton();
            ApplyCurrentCharacterInfoSafely();
            return;
        }
    }
    public void OnRecvPickLockResult(byte result, int characterId)
    {
        Debug.Log($"[PICK LOCK RESULT] result={result}, characterId={characterId}");

        ApplySelectButton();
        ApplyCurrentCharacterInfoSafely();
    }

    private void ApplyTopInfo()
    {
        if (topInfoUI != null)
            topInfoUI.ApplyPickInfo(currentState);
    }

    private void ApplyTimer(ushort remainSeconds)
    {
        if (topInfoUI != null)
            topInfoUI.SetTimer(remainSeconds);
    }

    private void ApplyTeamPickSlots()
    {
        if (teamPickSlots == null)
            return;

        for (int i = 0; i < teamPickSlots.Length; i++)
        {
            if (teamPickSlots[i] != null)
                teamPickSlots[i].SetWaitingSlot("");
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

            bool interactable =
                !isInputLockedByPhase &&
                !myLocked &&
                !isTeamPick &&
                !isMyPick;

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

        if (isInputLockedByPhase)
        {
            selectButton.interactable = false;

            if (currentPhase == PickPhase.PREPARE)
                SetSelectButtonText("선택 완료");

            return;
        }

        if (me == null)
        {
            selectButton.interactable = false;
            SetSelectButtonText("선택 확정");
            return;
        }

        if (me.isLocked)
        {
            selectButton.interactable = false;
            SetSelectButtonText("선택 완료");
            return;
        }

        SetSelectButtonText("선택 확정");

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
            return;

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
            return;

        currentInfoCharacterId = characterId;
        ApplyCharacterInfo(characterId);
        ShowPreviewModel(characterId);
    }

    private void ApplyCharacterInfo(int characterId)
    {
        PickCharacterData data = GetCharacterData(characterId);

        if (data == null)
            return;

        // Team hover/lock snapshots must never reset the skill the local player is reading.
        bool characterChanged = displayedInfoCharacterId != characterId;
        if (characterChanged) selectedSkillIndex = 0;
        displayedInfoCharacterId = characterId;

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

        ShowSkillInfo(GetSkill(data, selectedSkillIndex));
        if (presentation != null)
        {
            presentation.SetCharacter(data);
            presentation.SetSkill(selectedSkillIndex, characterChanged);
        }
    }

    private static PickSkillData GetSkill(PickCharacterData data, int index)
    {
        if (data == null) return null;
        return index == 1 ? data.qSkill : index == 2 ? data.eSkill : index == 3 ? data.xSkill : data.cSkill;
    }

    private void SelectSkill(int index)
    {
        if (index < 0 || index > 3) return;
        var data = GetCurrentCharacterData();
        if (data == null) return;
        bool changed = selectedSkillIndex != index;
        selectedSkillIndex = index;
        ShowSkillInfo(GetSkill(data, index));
        if (presentation != null) presentation.SetSkill(index, changed);
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
        if (skillNameText != null)
            skillNameText.text = skill != null ? skill.skillName : "스킬 정보 없음";

        if (skillInfoText != null)
            skillInfoText.text = skill != null ? skill.skillDescription : "";
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

    public PickCharacterData GetCharacterData(int characterId)
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

    public void OnRecvMatchLoadingInfo(MatchLoadingInfoData data)
    {
        Debug.Log("[PICK MANAGER] Match loading info received");

        if (matchLoadingPanelUI != null)
            matchLoadingPanelUI.Show(data);
        else
            Debug.LogError("[PICK MANAGER] matchLoadingPanelUI is null");
    }

    public void OnRecvMatchStart(uint roomId)
    {
        if (matchLoadingPanelUI != null)
            matchLoadingPanelUI.OnMatchStart();
    }

    private void ShowPreviewModel(int characterId)
    {
        if (authoredPreviewModels != null && characterId >= 0 && characterId < authoredPreviewModels.Length && authoredPreviewModels[characterId] != null)
        {
            if (currentPreviewModel == authoredPreviewModels[characterId]) return;
            ClearPreviewModel();
            currentPreviewModel = authoredPreviewModels[characterId];
            currentPreviewCharacterId = characterId;
            currentPreviewModel.SetActive(true);
            var animator = currentPreviewModel.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.Rebind(); animator.Play("Idle", 0, 0); animator.Update(0);
            }
            return;
        }
        if (previewSpawnPoint == null)
        {
            Debug.LogError("[PICK PREVIEW] previewSpawnPoint is null");
            return;
        }

        if (!previewSpawnPoint.gameObject.activeInHierarchy)
        {
            Debug.LogError("[PICK PREVIEW] previewSpawnPoint is not active in hierarchy");
            return;
        }

        if (characterModelPrefabs == null)
        {
            Debug.LogError("[PICK PREVIEW] characterModelPrefabs is null");
            return;
        }

        if (characterId < 0 || characterId >= characterModelPrefabs.Length)
        {
            Debug.LogError($"[PICK PREVIEW] invalid characterId={characterId}");
            return;
        }

        GameObject prefab = characterModelPrefabs[characterId];

        if (prefab == null)
        {
            Debug.LogError($"[PICK PREVIEW] prefab is null. characterId={characterId}");
            return;
        }

        if (currentPreviewModel != null && currentPreviewCharacterId == characterId)
            return;

        ClearPreviewModel();

        currentPreviewModel = Instantiate(prefab);
        currentPreviewCharacterId = characterId;

        currentPreviewModel.transform.SetParent(previewSpawnPoint, false);
        currentPreviewModel.transform.localPosition = previewLocalPosition;
        currentPreviewModel.transform.localRotation = Quaternion.Euler(previewLocalEulerAngles);
        currentPreviewModel.transform.localScale = previewLocalScale;

        currentPreviewModel.SetActive(true);

        DisablePreviewControlComponents(currentPreviewModel);

        Debug.Log(
            $"[PICK PREVIEW] Show model. " +
            $"characterId={characterId}, " +
            $"prefab={prefab.name}, " +
            $"spawnPoint={previewSpawnPoint.name}"
        );
    }

    private void DisablePreviewControlComponents(GameObject target)
    {
        if (target == null)
            return;

        PlayerMovement movement = target.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerItem item = target.GetComponent<PlayerItem>();
        if (item != null)
            item.enabled = false;

        PlayerSpike spike = target.GetComponent<PlayerSpike>();
        if (spike != null)
            spike.enabled = false;

        UnityEngine.InputSystem.PlayerInput input =
            target.GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (input != null)
            input.enabled = false;

        Camera camera = target.GetComponentInChildren<Camera>();
        if (camera != null)
            camera.gameObject.SetActive(false);

        Light[] lights = target.GetComponentsInChildren<Light>(true);
        foreach (Light previewLight in lights)
        {
            if (previewLight != null)
                previewLight.enabled = false;
        }

        AudioListener listener = target.GetComponentInChildren<AudioListener>();
        if (listener != null)
            listener.enabled = false;
    }

    private void ClearPreviewModel()
    {
        if (currentPreviewModel != null)
        {
            if (authoredPreviewModels != null && System.Array.IndexOf(authoredPreviewModels, currentPreviewModel) >= 0)
                currentPreviewModel.SetActive(false);
            else
                Destroy(currentPreviewModel);
            currentPreviewModel = null;
        }

        currentPreviewCharacterId = -1;
    }
}
