using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CharacterData
{
    public int characterId;
    public string characterName;
    public GameObject characterPrefab;
    public Sprite characterSprite;       // 2D 일러스트용 스프라이트 파일
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
    [Header("--- Data Settings ---")]
    public CharacterData[] characters; // 캐릭터 데이터 배열

    [Header("--- UI References ---")]
    public GameObject characterPanel;
    public RawImage previewRawImage; // 3D 모델을 보여주는 RawImage (렌더텍스처용)
    public Image preview2DImage; // 2D 일러스트를 보여줄 전용 UI Image 컴포넌트 참조
    public TextMeshProUGUI characterNameText; // 캐릭터 이름 텍스트
    public TextMeshProUGUI skillNameText; // 스킬 이름 표시 UI
    public TextMeshProUGUI skillDescText; // 스킬 설명 표시 UI
    public ScrollRect descriptionScrollRect; // 스킬 설명창의 ScrollRect 컴포넌트
    public Button equipButton; // 장착 버튼 컴포넌트 참조 추가

    [Header("--- Spawn Settings ---")]
    public Transform spawnPoint; // 3D 캐릭터가 생성될 위치

    private int currentEquippedCharacterId = -1; // 현재 최종 장착된 캐릭터 ID
    private int currentPreviewCharacterId = -1;  // 현재 중앙 프리뷰에 보이는 캐릭터 ID

    private GameObject currentSpawnedModel; // 현재 생성되어 있는 캐릭터 모델 오브젝트
    private bool isView3D = true; // 현재 상태가 3D 모드인지 체크하는 플래그 (기본값 3D)

    void Start()
    {
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
        if (currentSpawnedModel != null)
        {
            Destroy(currentSpawnedModel);
            currentSpawnedModel = null;
        }

        if (characterPanel != null)
        {
            characterPanel.SetActive(false);
        }
    }

    // 캐릭터 변경
    public void SelectCharacter(int index)
    {
        if (index < 0 || index >= characters.Length) return;

        CharacterData data = characters[index];

        UpdateOutline(index);

        // 현재 프리뷰 ID 갱신
        currentPreviewCharacterId = data.characterId;

        // 기존 3D 모델 정리
        if (currentSpawnedModel != null)
        {
            Destroy(currentSpawnedModel);
        }

        // 3D 모델 생성 및 세팅
        if (data.characterPrefab != null && spawnPoint != null)
        {
            Quaternion absolute180Rotation = Quaternion.Euler(0f, 180f, 0f);
            currentSpawnedModel = Instantiate(data.characterPrefab, spawnPoint.position, absolute180Rotation);
            currentSpawnedModel.transform.SetParent(spawnPoint);
            SetLayerRecursively(currentSpawnedModel, "CharacterSelectModel");
        }

        // 2D 전용 이미지 컴포넌트에 스프라이트 교체 교체 ★
        if (preview2DImage != null)
        {
            preview2DImage.sprite = data.characterSprite;
        }

        // 새로 고른 캐릭터도 현재 보고 있던 모드(2D 혹은 3D) 상태에 맞춰서 켜고 꺼줌 ★
        UpdateVisibilityByMode();

        // 캐릭터 이름 UI 업데이트
        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }

        // 캐릭터가 바뀌면 기본으로 Q스킬 정보를 먼저 보여줌
        SelectSkill("Q");
    }

    // 프리뷰 변경 전환 버튼에 연결할 함수
    public void ToggleDimensionView()
    {
        isView3D = !isView3D;
        UpdateVisibilityByMode();
    }

    // 3D 오브젝트들과 2D 이미지의 시각적 활성화 상태를 일괄 제어하는 함수
    private void UpdateVisibilityByMode()
    {
        // 3D 화면 제어
        if (previewRawImage != null)
        {
            previewRawImage.gameObject.SetActive(isView3D);
        }

        // 2D 일러스트 화면 제어
        if (preview2DImage != null)
        {
            preview2DImage.gameObject.SetActive(!isView3D);
        }
    }

    // 장착 버튼을 누르면 현재 선택 캐릭터 ID만 로컬에 저장
    public void ClickEquipButton()
    {
        currentEquippedCharacterId = currentPreviewCharacterId;
        Debug.Log($"캐릭터 장착 완료: 장착된 캐릭터 ID: {currentEquippedCharacterId}");
    }

    // 스킬 버튼 입력 함수 (Q, C, E, X 스킬 선택 버튼에 연결)
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
}