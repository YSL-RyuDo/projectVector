using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPickButton : MonoBehaviour
{
    [Header("Data Source")]
    public CharacterPickSO characterData;
    public Transform characterSpawnPoint;

    [Header("UI References")]
    public Image myPickImage;
    public GameObject characterInfoPanel;
    public TextMeshProUGUI characterRoleText;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI descriptionDisplayText;

    [Header("Skill Buttons")]
    public CharacterInfoButton roleButton;
    public CharacterInfoButton skillCButton;
    public CharacterInfoButton skillQButton;
    public CharacterInfoButton skillEButton;
    public CharacterInfoButton skillXButton;

    [Header("My Pick Image")]
    public Transform spawnPoint;
    private static GameObject currentPreviewModel;

    private void Start()
    {
        if (GetComponent<Image>() != null && characterData != null)
        {
            GetComponent<Image>().sprite = characterData.characterIcon;
        }
    }

    public void OnClick()
    {
        if (characterData == null) return;

        // 1. UI 활성화 및 기본 정보 세팅
        characterInfoPanel.SetActive(true);
        myPickImage.sprite = characterData.characterIcon;
        characterNameText.text = characterData.characterName;

        // Enum을 문자열로 변환하여 역할 표시
        characterRoleText.text = characterData.role.ToString();

        // 2. 하단 정보창 초기화 (클릭 전 기본 안내)
        descriptionDisplayText.text = characterData.synopsis;

        // 3. 각 정보 버튼(역할 및 스킬) 초기화
        // CharacterInfoButton의 Info 필드에 descriptionDisplayText를 연결해줘야 합니다.
        roleButton.Initialize(characterData.characterRoleIcon, "", characterData.synopsis);
        skillCButton.Initialize(characterData.skillC.skillIcon, characterData.skillC.skillName, characterData.skillC.description);
        skillQButton.Initialize(characterData.skillQ.skillIcon, characterData.skillQ.skillName, characterData.skillQ.description);
        skillEButton.Initialize(characterData.skillE.skillIcon, characterData.skillE.skillName, characterData.skillE.description);
        skillXButton.Initialize(characterData.skillX.skillIcon, characterData.skillX.skillName, characterData.skillX.description);

        // 4. 캐릭터 3D 모델 프리뷰 생성
        UpdateCharacterModel();
    }

    private void UpdateCharacterModel()
    {
        // 기존에 생성된 모델이 있다면 파괴
        if (currentPreviewModel != null)
        {
            Destroy(currentPreviewModel);
        }

        // 새로운 모델 생성
        if (characterData.characterModel != null && spawnPoint != null)
        {
            currentPreviewModel = Instantiate(characterData.characterModel, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
