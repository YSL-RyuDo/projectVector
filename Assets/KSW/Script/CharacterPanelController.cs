using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public enum PreviewObjectType
{
    Character,
    PrimaryWeapon,
    SecondaryWeapon,
    MeleeWeapon,
    TacticalItem
}

[System.Serializable]
public class CharacterData
{
    public int characterId;
    public string characterName;
    public GameObject characterPrefab;
    public Sprite characterSprite;
    public bool isOwned;
    public GameObject lockImage;

    public UnityEngine.UI.Button selectButton;
    public UnityEngine.UI.Outline outline;

    [TextArea] public string qSkillName;
    [TextArea] public string qSkillDesc;

    [TextArea] public string wSkillName;
    [TextArea] public string wSkillDesc;

    [TextArea] public string eSkillName;
    [TextArea] public string eSkillDesc;

    [TextArea] public string rSkillName;
    [TextArea] public string rSkillDesc;
}

public class CharacterPanelController : MonoBehaviour
{
    [Header("Data Settings - Characters")]
    [SerializeField] private CharacterData[] characters;

    [Header("Data Settings - Equipments")]
    [SerializeField] private PreviewObjectData[] primaryWeapons;
    [SerializeField] private PreviewObjectData[] secondaryWeapons;
    [SerializeField] private PreviewObjectData[] meleeWeapons;
    [SerializeField] private PreviewObjectData[] tacticalItems;

    [Header("Currently Selected Equipment Indexes")]
    [SerializeField] private int selectedPrimaryIndex = 0;
    [SerializeField] private int selectedSecondaryIndex = 0;
    [SerializeField] private int selectedMeleeIndex = 0;
    [SerializeField] private int selectedTacticalIndex = 0;

    [Header("UI References")]
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private RawImage previewRawImage;
    [SerializeField] private Image preview2DImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescText;
    [SerializeField] private ScrollRect descriptionScrollRect;
    [SerializeField] private Button equipButton;

    [SerializeField] private GameObject _profilePanel;
    [SerializeField] private GameObject _navigationPanel;

    [Header("Preset Root")]
    [SerializeField] private GameObject _presetPanel;

    [Header("Preset Parent Panel")]
    [SerializeField] private GameObject _skinPanel;
    [SerializeField] private GameObject _customizePanel;
    [SerializeField] private GameObject _backgroundStoryPanel;

    [Header("Preset Child Panel")]
    [SerializeField] private GameObject c_skinPanel;
    [SerializeField] private GameObject c_primaryWeaponPanel;
    [SerializeField] private GameObject c_secondaryWeaponPanel;
    [SerializeField] private GameObject c_meleeWeaponPanel;
    [SerializeField] private GameObject c_tacticalItemPanel;

    [Header("Preset Navigation Button")]
    [SerializeField] private Button n_SkinButton;
    [SerializeField] private Button n_CustomizationButton;
    [SerializeField] private Button n_BackgroundStoryButton;

    [Header("Preset Button")]
    [SerializeField] private Button _skinButton;
    [SerializeField] private Button _primaryWeponButton;
    [SerializeField] private Button _secondaryWeponButton;
    [SerializeField] private Button _meleeWeponButton;
    [SerializeField] private Button _tacticalButton;

    [Header("Spawn Settings")]
    [SerializeField] private Transform _objectSpawnPoint;

    [Header("Weapon Mount & Animation Settings")]
    [Tooltip("캐릭터 뼈대 하위에 만든 무기 장착용 소켓 오브젝트 이름")]
    [SerializeField] private string weaponSocketName = "RightHandSocket";

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 2f;
    private float lastMouseX;

    private float resetRotationSpeed = 720f; // 정면 복귀 속도
    private bool isResettingRotation = false;

    private int currentEquippedCharacterId = -1;
    private int currentPreviewCharacterId = -1;

    private GameObject currentSpawnedModel;  // 스폰된 캐릭터 3D 모델
    private GameObject currentSpawnedWeapon; // 손에 장착된 무기 3D 모델
    private bool isView3D = true;

    private int currentWeaponAnimIndex = -1; // 현재 애니메이션 파라미터 상태 기록

    private enum PresetState
    {
        skin,
        customize,
        backgroundStory
    }

    private enum PreviewState
    {
        skin,
        primaryWepon,
        secondaryWepon,
        meleeWepon,
        tacticalItem
    }

    private PresetState _presetState = PresetState.skin;
    private PreviewState _previewState = PreviewState.skin;

    void Start()
    {
        _presetState = PresetState.skin;
        _previewState = PreviewState.skin;

        OnNSkinButton();
        OnSkinButton();

        DisableAllOutlines();
        SetSelectButtonsInteractable();
    }

    private void Update()
    {
        // 정면 리셋 애니메이션 처리
        if (isResettingRotation && currentSpawnedModel != null)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, 180f, 0f);

            // 목표 회전값으로 부드럽게 회전
            currentSpawnedModel.transform.rotation = Quaternion.RotateTowards(
    currentSpawnedModel.transform.rotation,
    targetRotation,
    resetRotationSpeed * Time.deltaTime
);

            // 거의 목표 각도에 도달했으면 리셋 종료 (오차 0.1도 이하)
            if (Quaternion.Angle(currentSpawnedModel.transform.rotation, targetRotation) < 0.1f)
            {
                currentSpawnedModel.transform.rotation = targetRotation;
                isResettingRotation = false;
            }
        }
    }

    /// <summary>
    /// 현재 스폰된 캐릭터 모델을 정면(-180도)으로 부드럽게 회전시킵니다.
    /// </summary>
    public void ResetCharacterRotationSmooth()
    {
        if (currentSpawnedModel == null) return;

        // 정면 리셋 플래그 활성화
        isResettingRotation = true;
    }

    public void OpenCharacterPanel()
    {
        if (characterPanel != null)
        {
            characterPanel.SetActive(true);
            SelectCharacter(0);
        }
    }

    public void CloseCharacterPanel()
    {
        DestroyCurrentModel();

        if (characterPanel != null)
        {
            characterPanel.SetActive(false);
        }
    }

    public void SelectCharacter(int index)
    {
        if (index < 0 || index >= characters.Length) return;

        CharacterData data = characters[index];

        UpdateOutline(index);
        currentPreviewCharacterId = data.characterId;

        if (preview2DImage != null)
        {
            preview2DImage.sprite = data.characterSprite;
        }

        UpdateVisibilityByMode();

        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }

        SpawnCharacterModel(data.characterPrefab);
        SelectSkill("Q");
    }

    public void SelectPrimaryWeapon(int index)
    {
        if (index >= 0 && index < primaryWeapons.Length)
        {
            selectedPrimaryIndex = index;
            if (_previewState == PreviewState.primaryWepon) ChangePreviewState();
        }
    }

    public void SelectSecondaryWeapon(int index)
    {
        if (index >= 0 && index < secondaryWeapons.Length)
        {
            selectedSecondaryIndex = index;
            if (_previewState == PreviewState.secondaryWepon) ChangePreviewState();
        }
    }

    public void SelectMeleeWeapon(int index)
    {
        if (index >= 0 && index < meleeWeapons.Length)
        {
            selectedMeleeIndex = index;
            if (_previewState == PreviewState.meleeWepon) ChangePreviewState();
        }
    }

    public void SelectTacticalItem(int index)
    {
        if (index >= 0 && index < tacticalItems.Length)
        {
            selectedTacticalIndex = index;
            if (_previewState == PreviewState.tacticalItem) ChangePreviewState();
        }
    }

    public void ToggleDimensionView()
    {
        isView3D = !isView3D;
        UpdateVisibilityByMode();
    }

    private void UpdateVisibilityByMode()
    {
        if (previewRawImage != null) previewRawImage.gameObject.SetActive(isView3D);
        if (preview2DImage != null) preview2DImage.gameObject.SetActive(!isView3D);
    }

    public void ClickPresetButton()
    {
        _presetPanel.SetActive(true);
        _profilePanel.SetActive(false);
        _navigationPanel.SetActive(false);

        _presetState = PresetState.skin;
        ChangePresetState();
    }

    public void SelectSkill(string skillType)
    {
        CharacterData data = GetCharacterDataById(currentPreviewCharacterId);
        if (data == null) return;

        switch (skillType.ToUpper())
        {
            case "Q":
                skillNameText.text = data.qSkillName;
                skillDescText.text = data.qSkillDesc;
                break;
            case "C":
                skillNameText.text = data.wSkillName;
                skillDescText.text = data.wSkillDesc;
                break;
            case "E":
                skillNameText.text = data.eSkillName;
                skillDescText.text = data.eSkillDesc;
                break;
            case "X":
                skillNameText.text = data.rSkillName;
                skillDescText.text = data.rSkillDesc;
                break;
        }

        ResetScrollPosition();
    }

    private CharacterData GetCharacterDataById(int id)
    {
        foreach (var charData in characters)
        {
            if (charData.characterId == id) return charData;
        }
        return null;
    }

    private void ResetScrollPosition()
    {
        if (descriptionScrollRect != null)
        {
            descriptionScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void SetLayerRecursively(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layerName);
        }
    }

    private void UpdateOutline(int selectedIndex)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].outline == null) continue;
            characters[i].outline.enabled = (i == selectedIndex);
        }
    }

    private void DisableAllOutlines()
    {
        foreach (CharacterData data in characters)
        {
            if (data.outline != null)
            {
                data.outline.enabled = false;
            }
        }
    }

    private void SetSelectButtonsInteractable()
    {
        foreach (var data in characters)
        {
            if (data.selectButton != null)
            {
                data.selectButton.interactable = data.isOwned;
            }

            if (data.lockImage != null)
            {
                data.lockImage.SetActive(!data.isOwned);
            }
        }
    }

    public void BeginDrag(PointerEventData eventData)
    {
        lastMouseX = eventData.position.x;
    }

    public void Drag(PointerEventData eventData)
    {
        if (currentSpawnedModel == null) return;

        float deltaX = eventData.position.x - lastMouseX;
        currentSpawnedModel.transform.Rotate(
            Vector3.up,
            -deltaX * rotationSpeed,
            Space.World);

        lastMouseX = eventData.position.x;
    }

    #region Character Panel Button
    public void OnPresetBackButton()
    {
        _presetPanel.SetActive(false);
        _profilePanel.SetActive(true);
        _navigationPanel.SetActive(true);

        OnSkinButton();
    }

    public void OnPresetButton() { }
    public void OnScreenshotButton() { }
    #endregion

    #region PresetPanelButton

    private void ChangePreviewState()
    {
        UpdatePreviewPanels();

        if (currentSpawnedModel == null) return;

        GameObject targetWeaponPrefab = null;
        int weaponAnimIndex = 0;

        switch (_previewState)
        {
            case PreviewState.skin:
                weaponAnimIndex = 0;
                break;

            case PreviewState.primaryWepon:
                if (primaryWeapons != null && primaryWeapons.Length > selectedPrimaryIndex && primaryWeapons[selectedPrimaryIndex] != null)
                {
                    targetWeaponPrefab = primaryWeapons[selectedPrimaryIndex].objectPrefab;
                }
                weaponAnimIndex = 1;
                break;

            case PreviewState.secondaryWepon:
                if (secondaryWeapons != null && secondaryWeapons.Length > selectedSecondaryIndex && secondaryWeapons[selectedSecondaryIndex] != null)
                {
                    targetWeaponPrefab = secondaryWeapons[selectedSecondaryIndex].objectPrefab;
                }
                weaponAnimIndex = 2;
                break;

            case PreviewState.meleeWepon:
                if (meleeWeapons != null && meleeWeapons.Length > selectedMeleeIndex && meleeWeapons[selectedMeleeIndex] != null)
                {
                    targetWeaponPrefab = meleeWeapons[selectedMeleeIndex].objectPrefab;
                }
                weaponAnimIndex = 3;
                break;

            case PreviewState.tacticalItem:
                if (tacticalItems != null && tacticalItems.Length > selectedTacticalIndex && tacticalItems[selectedTacticalIndex] != null)
                {
                    targetWeaponPrefab = tacticalItems[selectedTacticalIndex].objectPrefab;
                }
                weaponAnimIndex = 4;
                break;
        }

        EquipWeaponToCharacter(targetWeaponPrefab);
        UpdateCharacterAnimation(weaponAnimIndex);
    }

    private void UpdatePreviewPanels()
    {
        if (c_skinPanel != null)
            c_skinPanel.SetActive(_previewState == PreviewState.skin);

        if (c_primaryWeaponPanel != null)
            c_primaryWeaponPanel.SetActive(_previewState == PreviewState.primaryWepon);

        if (c_secondaryWeaponPanel != null)
            c_secondaryWeaponPanel.SetActive(_previewState == PreviewState.secondaryWepon);

        if (c_meleeWeaponPanel != null)
            c_meleeWeaponPanel.SetActive(_previewState == PreviewState.meleeWepon);

        if (c_tacticalItemPanel != null)
            c_tacticalItemPanel.SetActive(_previewState == PreviewState.tacticalItem);
    }

    private void SpawnCharacterModel(GameObject charPrefab)
    {
        DestroyCurrentModel();

        if (charPrefab == null || _objectSpawnPoint == null) return;

        Quaternion absolute180Rotation = Quaternion.Euler(0f, 180f, 0f);
        currentSpawnedModel = Instantiate(charPrefab, _objectSpawnPoint.position, absolute180Rotation);
        currentSpawnedModel.transform.SetParent(_objectSpawnPoint);
        SetLayerRecursively(currentSpawnedModel, "SelectModel");

        currentWeaponAnimIndex = -1; // 초기화
        ChangePreviewState();
    }

    private void EquipWeaponToCharacter(GameObject weaponPrefab)
    {
        DestroyCurrentWeapon();

        if (weaponPrefab == null || currentSpawnedModel == null) return;

        Transform socket = FindChildRecursive(currentSpawnedModel.transform, weaponSocketName);

        if (socket == null)
        {
            socket = currentSpawnedModel.transform;
            Debug.LogWarning($"[{weaponSocketName}] 소켓을 찾지 못해 캐릭터 루트 위치에 무기를 생성합니다.");
        }

        currentSpawnedWeapon = Instantiate(weaponPrefab, socket.position, socket.rotation, socket);
        currentSpawnedWeapon.transform.localPosition = Vector3.zero;
        currentSpawnedWeapon.transform.localRotation = Quaternion.identity;

        SetLayerRecursively(currentSpawnedWeapon, "SelectModel");
    }

    private void UpdateCharacterAnimation(int weaponAnimIndex)
    {
        if (currentSpawnedModel == null) return;

        if (currentWeaponAnimIndex == weaponAnimIndex) return;
        currentWeaponAnimIndex = weaponAnimIndex;

        Animator animator = currentSpawnedModel.GetComponent<Animator>();
        if (animator == null)
        {
            animator = currentSpawnedModel.GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            string prefix = "";
            switch (currentPreviewCharacterId)
            {
                case 0: prefix = "Initiator"; break;
                case 1: prefix = "Controller"; break;
                case 2: prefix = "Sentinel"; break;
                case 3: prefix = "Entry"; break;
                default: prefix = "Initiator"; break;
            }

            // 무기 종류에 따른 Suffix 접미사 결정
            string suffix = "";
            switch (weaponAnimIndex)
            {
                case 0: suffix = "Idle"; break;
                case 1: suffix = "Preset_AK47"; break;
                case 2: suffix = "Preset_Pistol"; break;
                case 3: suffix = "Preset_Melee"; break;
                case 4: suffix = "Preset_Item"; break;
            }

            // Prefix + "_" + Suffix 조합 (예: "Controller_Preset_AK47")
            string stateName = $"{prefix}_{suffix}";

            animator.CrossFadeInFixedTime(stateName, 0.15f);
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void DestroyCurrentModel()
    {
        DestroyCurrentWeapon();

        if (currentSpawnedModel != null)
        {
            Destroy(currentSpawnedModel);
            currentSpawnedModel = null;
        }
    }

    private void DestroyCurrentWeapon()
    {
        if (currentSpawnedWeapon != null)
        {
            Destroy(currentSpawnedWeapon);
            currentSpawnedWeapon = null;
        }
    }

    private void UpdatePresetButtonOutlines(Button selectedButton)
    {
        Button[] presetButtons = new Button[]
        {
            _skinButton,
            _primaryWeponButton,
            _secondaryWeponButton,
            _meleeWeponButton,
            _tacticalButton
        };

        foreach (Button btn in presetButtons)
        {
            if (btn == null) continue;

            UnityEngine.UI.Outline outline = btn.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.enabled = (btn == selectedButton);
            }
        }
    }

    public void OnSkinButton()
    {
        _previewState = PreviewState.skin;
        UpdatePresetButtonOutlines(_skinButton);
        ChangePreviewState();
    }

    public void OnPrimaryWeaponButton()
    {
        _previewState = PreviewState.primaryWepon;
        UpdatePresetButtonOutlines(_primaryWeponButton);
        ChangePreviewState();
    }

    public void OnSecondaryWeaponButton()
    {
        _previewState = PreviewState.secondaryWepon;
        UpdatePresetButtonOutlines(_secondaryWeponButton);
        ChangePreviewState();
    }

    public void OnMeleeWeaponButton()
    {
        _previewState = PreviewState.meleeWepon;
        UpdatePresetButtonOutlines(_meleeWeponButton);
        ChangePreviewState();
    }

    public void OnTacticalItemButton()
    {
        _previewState = PreviewState.tacticalItem;
        UpdatePresetButtonOutlines(_tacticalButton);
        ChangePreviewState();
    }
    #endregion

    #region Preset Navigation Buttons
    private void ChangePresetState()
    {
        switch (_presetState)
        {
            case PresetState.skin:
                _skinPanel.SetActive(true);
                _customizePanel.SetActive(false);
                _backgroundStoryPanel.SetActive(false);
                UpdateNavButtonOutlines(n_SkinButton);
                break;
            case PresetState.customize:
                _skinPanel.SetActive(false);
                _customizePanel.SetActive(true);
                _backgroundStoryPanel.SetActive(false);
                UpdateNavButtonOutlines(n_CustomizationButton);
                break;
            case PresetState.backgroundStory:
                _skinPanel.SetActive(false);
                _customizePanel.SetActive(false);
                _backgroundStoryPanel.SetActive(true);
                UpdateNavButtonOutlines(n_BackgroundStoryButton);
                PrintCharacterBackgroundStory(currentPreviewCharacterId);
                break;
        }
    }

    private void UpdateNavButtonOutlines(Button selectedButton)
    {
        Button[] navButtons = new Button[]
        {
            n_SkinButton,
            n_CustomizationButton,
            n_BackgroundStoryButton
        };

        foreach (Button btn in navButtons)
        {
            if (btn == null) continue;

            UnityEngine.UI.Outline outline = btn.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.enabled = (btn == selectedButton);
            }
        }
    }

    public void OnNSkinButton()
    {
        _presetState = PresetState.skin;
        UpdateNavButtonOutlines(n_SkinButton);
        ChangePresetState();
    }

    public void OnNCustomizationButton()
    {
        _presetState = PresetState.customize;
        UpdateNavButtonOutlines(n_CustomizationButton);
        ChangePresetState();
    }

    public void OnNBackgroundStoryButton()
    {
        _presetState = PresetState.backgroundStory;
        UpdateNavButtonOutlines(n_BackgroundStoryButton);
        ChangePresetState();
    }
    #endregion

    #region Character Background Story UI
    [Header("Character Sprite")]
    [SerializeField] private Image _characterStoryImage;

    [Header("Character Background Story UI")]
    [SerializeField] private TextMeshProUGUI _characterNameText;
    [SerializeField] private TextMeshProUGUI _characterGenderText;
    [SerializeField] private TextMeshProUGUI _characterBirhdayText;
    [SerializeField] private TextMeshProUGUI _characterStatureText;
    [SerializeField] private TextMeshProUGUI _characterCampText;
    [SerializeField] private TextMeshProUGUI _characterHobbyText;
    [SerializeField] private TextMeshProUGUI _characterEatingHabitsText;
    [SerializeField] private TextMeshProUGUI _characterIndividualityText;
    [SerializeField] private TextMeshProUGUI _characterIntroText;
    [SerializeField] private TextMeshProUGUI _characterObservationNotesText;

    [Header("Character Background Story Data")]
    [SerializeField] private BackgroundStoryData[] _characterData;

    public void PrintCharacterBackgroundStory(int previewCharacterId)
    {
        CharacterData charData = GetCharacterDataById(previewCharacterId);
        if (charData == null) return;

        if (_characterNameText != null)
            _characterNameText.text = charData.characterName;

        BackgroundStoryData storyData = GetBackgroundStoryDataById(previewCharacterId);
        if (storyData == null) return;

        if (_characterGenderText != null) _characterGenderText.text = storyData.gender;
        if (_characterBirhdayText != null) _characterBirhdayText.text = storyData.birthday;
        if (_characterStatureText != null) _characterStatureText.text = storyData.stature;
        if (_characterCampText != null) _characterCampText.text = storyData.camp;
        if (_characterHobbyText != null) _characterHobbyText.text = storyData.hobby;
        if (_characterEatingHabitsText != null) _characterEatingHabitsText.text = storyData.eatingHabits;
        if (_characterIndividualityText != null) _characterIndividualityText.text = storyData.individuality;
        if (_characterIntroText != null) _characterIntroText.text = storyData.intro;
        if (_characterObservationNotesText != null) _characterObservationNotesText.text = storyData.observationNotes;

        if (_characterStoryImage != null && storyData.characterSprite != null)
        {
            _characterStoryImage.sprite = storyData.characterSprite;
            _characterStoryImage.gameObject.SetActive(true);
        }
    }

    private BackgroundStoryData GetBackgroundStoryDataById(int id)
    {
        if (_characterData == null) return null;

        foreach (var data in _characterData)
        {
            if (data != null && data.characterId == id)
                return data;
        }
        return null;
    }
    #endregion

    public bool IsCharacterFacingFront
    {
        get
        {
            if (currentSpawnedModel == null)
                return true;

            return Quaternion.Angle(
                currentSpawnedModel.transform.rotation,
                Quaternion.Euler(0f, 180f, 0f)
            ) < 0.1f;
        }
    }
}