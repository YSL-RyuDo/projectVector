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

    [Header("Preset Panel")]
    [SerializeField] private GameObject _skinPanel;
    [SerializeField] private GameObject _customizePanel;
    [SerializeField] private GameObject _backgroundStoryPanel;

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

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 2f;
    private float lastMouseX;

    private int currentEquippedCharacterId = -1;
    private int currentPreviewCharacterId = -1;

    private GameObject currentSpawnedModel;
    private bool isView3D = true;

    private enum PresetState
    {
        skin,
        customize,
        backgroundStory
    }

    private enum PreviewState
    {
        character,
        primaryWepon,
        secondaryWepon,
        meleeWepon,
        tacticalItem
    }

    private PresetState _presetState = PresetState.skin;
    private PreviewState _previewState = PreviewState.character;

    void Start()
    {
        _presetState = PresetState.skin;
        _previewState = PreviewState.character;

        OnNSkinButton();
        OnSkinButton();

        DisableAllOutlines();
        SetSelectButtonsInteractable();
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

    // 캐릭터 선택
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

        ChangePreviewState();
        SelectSkill("Q");
    }

    // 장비 선택 함수들 (나중에 UI에서 무기 바꿀 때 이 함수들을 호출해주면 됨)
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
        // 실제 3D 모델 스폰 처리
        switch (_previewState)
        {
            case PreviewState.character:
                CharacterData charData = GetCharacterDataById(currentPreviewCharacterId);
                if (charData != null) SpawnPreviewObject(charData.characterPrefab);
                break;

            case PreviewState.primaryWepon:
                if (primaryWeapons != null && primaryWeapons.Length > selectedPrimaryIndex && primaryWeapons[selectedPrimaryIndex] != null)
                {
                    SpawnPreviewObject(primaryWeapons[selectedPrimaryIndex].objectPrefab);
                }
                break;

            case PreviewState.secondaryWepon:
                if (secondaryWeapons != null && secondaryWeapons.Length > selectedSecondaryIndex && secondaryWeapons[selectedSecondaryIndex] != null)
                {
                    SpawnPreviewObject(secondaryWeapons[selectedSecondaryIndex].objectPrefab);
                }
                break;

            case PreviewState.meleeWepon:
                if (meleeWeapons != null && meleeWeapons.Length > selectedMeleeIndex && meleeWeapons[selectedMeleeIndex] != null)
                {
                    SpawnPreviewObject(meleeWeapons[selectedMeleeIndex].objectPrefab);
                }
                break;

            case PreviewState.tacticalItem:
                if (tacticalItems != null && tacticalItems.Length > selectedTacticalIndex && tacticalItems[selectedTacticalIndex] != null)
                {
                    SpawnPreviewObject(tacticalItems[selectedTacticalIndex].objectPrefab);
                }
                break;
        }
    }

    private void SpawnPreviewObject(GameObject targetPrefab)
    {
        DestroyCurrentModel();

        if (targetPrefab == null || _objectSpawnPoint == null) return;

        Quaternion absolute180Rotation = Quaternion.Euler(0f, 180f, 0f);
        currentSpawnedModel = Instantiate(targetPrefab, _objectSpawnPoint.position, absolute180Rotation);
        currentSpawnedModel.transform.SetParent(_objectSpawnPoint);
        SetLayerRecursively(currentSpawnedModel, "SelectModel");
    }

    private void DestroyCurrentModel()
    {
        if (currentSpawnedModel != null)
        {
            Destroy(currentSpawnedModel);
            currentSpawnedModel = null;
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
        _previewState = PreviewState.character;
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
                break;
            case PresetState.customize:
                _skinPanel.SetActive(false);
                _customizePanel.SetActive(true);
                _backgroundStoryPanel.SetActive(false);
                break;
            case PresetState.backgroundStory:
                _skinPanel.SetActive(false);
                _customizePanel.SetActive(false);
                _backgroundStoryPanel.SetActive(true);
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
}