using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct LobbyBackgroundPreset
{
    public Vector3 characterPosition;
    public Vector3 characterRotation;
    public Vector3 cameraPosition;
    public Vector3 cameraRotation;
}

public class LobbyPanelController : MonoBehaviour
{
    [Header("Character Popup")]
    [SerializeField] private GameObject characterSelectPopup;
    [SerializeField] private Transform characterSlotContent;
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private Sprite[] characterSprites;

    [Header("Character")]
    [SerializeField] private GameObject[] CharacterPrefebs;
    [SerializeField] private Camera lobbyCamera;
    private GameObject currentCharacter;

    [Header("Background")]
    [SerializeField] private LobbyBackgroundPreset[] backgroundPresets;

    [Header("Background Popup")]
    [SerializeField] private GameObject backgroundSelectPopup;

    private readonly List<GameObject> characterSlots = new();

    private ProfilePanelController _profilePanelController;

    void Start()
    {
        NetworkManager.Instance?.SetLobbyPanelController(this);
        _profilePanelController = GetComponent<ProfilePanelController>();

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(false);

        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(false);
    }

    public void ApplyCharacter(int characterId)
    {
        if (currentCharacter != null)
            Destroy(currentCharacter);

        if (CharacterPrefebs == null || CharacterPrefebs.Length == 0)
            return;

        if (characterId < 0 || characterId >= CharacterPrefebs.Length)
            characterId = 0;

        currentCharacter = Instantiate(CharacterPrefebs[characterId]);
    }

    public void ApplyBackgroundPreset(int backgroundId)
    {
        if (backgroundPresets == null || backgroundPresets.Length == 0)
            return;

        if (backgroundId < 0 || backgroundId >= backgroundPresets.Length)
            backgroundId = 0;

        LobbyBackgroundPreset preset = backgroundPresets[backgroundId];

        if (currentCharacter != null)
        {
            currentCharacter.transform.position = preset.characterPosition;
            currentCharacter.transform.rotation = Quaternion.Euler(preset.characterRotation);
        }

        if (lobbyCamera != null)
        {
            lobbyCamera.transform.position = preset.cameraPosition;
            lobbyCamera.transform.rotation = Quaternion.Euler(preset.cameraRotation);
        }
    }

    public void OnRecvLobbyCharacterSelectResult(byte result, int modelId)
    {
        if (result != 1)
            return;

        _profilePanelController.currentlobbyCharacterId = modelId;

        ApplyCharacter(_profilePanelController.currentlobbyCharacterId);
        ApplyBackgroundPreset(_profilePanelController.currentlobbyBackgroundId);

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyBackgroundSelectResult(byte result, int backgroundId)
    {
        if (result != 1)
            return;

        _profilePanelController.currentlobbyBackgroundId = backgroundId;
        ApplyBackgroundPreset(_profilePanelController.currentlobbyBackgroundId);

        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyCharacterList(
    List<LobbyCharacterData> list,
    int currentCharacterModelId)
    {
        _profilePanelController.currentlobbyCharacterId = currentCharacterModelId;

        // 기존에 생성된 캐릭터 버튼 제거
        for (int i = 0; i < characterSlots.Count; i++)
        {
            if (characterSlots[i] != null)
                Destroy(characterSlots[i]);
        }

        characterSlots.Clear();

        if (list == null)
        {
            Debug.LogError("[CHARACTER LIST] 캐릭터 목록이 null입니다.");
            return;
        }

        if (characterSlotPrefab == null)
        {
            Debug.LogError("[CHARACTER LIST] characterSlotPrefab이 연결되지 않았습니다.");
            return;
        }

        if (characterSlotContent == null)
        {
            Debug.LogError("[CHARACTER LIST] characterSlotContent가 연결되지 않았습니다.");
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            LobbyCharacterData characterData = list[i];

            // 보유하지 않은 캐릭터는 버튼 생성하지 않음
            if (!characterData.owned)
                continue;

            int characterId = characterData.characterId;

            // 캐릭터 버튼 생성
            GameObject slot = Instantiate(
                characterSlotPrefab,
                characterSlotContent
            );

            characterSlots.Add(slot);

            // 생성된 프리팹에서 Button 찾기
            Button button = slot.GetComponent<Button>();

            if (button == null)
                button = slot.GetComponentInChildren<Button>(true);

            if (button == null)
            {
                Debug.LogError(
                    $"[CHARACTER SLOT] Button 컴포넌트를 찾지 못했습니다. " +
                    $"characterId={characterId}"
                );

                continue;
            }

            // 캐릭터 번호에 맞는 버튼 이미지 적용
            if (characterSprites == null ||
                characterSprites.Length == 0)
            {
                Debug.LogError(
                    "[CHARACTER SLOT] characterSprites 배열이 비어 있습니다."
                );
            }
            else if (characterId < 0 ||
                     characterId >= characterSprites.Length)
            {
                Debug.LogError(
                    $"[CHARACTER SLOT] 캐릭터 번호가 스프라이트 배열 범위를 벗어났습니다. " +
                    $"characterId={characterId}, " +
                    $"spriteCount={characterSprites.Length}"
                );
            }
            else if (characterSprites[characterId] == null)
            {
                Debug.LogError(
                    $"[CHARACTER SLOT] characterSprites[{characterId}]에 " +
                    "스프라이트가 등록되지 않았습니다."
                );
            }
            else
            {
                Image buttonImage = button.image;

                // Button의 Target Graphic이 연결되지 않은 경우
                if (buttonImage == null)
                    buttonImage = button.GetComponent<Image>();

                if (buttonImage != null)
                {
                    buttonImage.sprite = characterSprites[characterId];
                    buttonImage.enabled = true;
                    buttonImage.preserveAspect = true;
                    buttonImage.color = Color.white;

                    Debug.Log(
                        $"[CHARACTER SLOT] 버튼 이미지 적용 완료. " +
                        $"characterId={characterId}, " +
                        $"sprite={characterSprites[characterId].name}"
                    );
                }
                else
                {
                    Debug.LogError(
                        $"[CHARACTER SLOT] 버튼에 Image 컴포넌트가 없습니다. " +
                        $"characterId={characterId}"
                    );
                }
            }

            // 슬롯 내부 텍스트 설정
            TMP_Text[] texts =
                slot.GetComponentsInChildren<TMP_Text>(true);

            if (texts.Length > 0)
            {
                texts[0].text = "";
            }

            if (texts.Length > 1)
            {
                texts[1].text =
                    characterId == _profilePanelController.currentlobbyCharacterId
                        ? "선택중"
                        : "";
            }

            // 버튼 클릭 이벤트 연결
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(() =>
            {
                OnClickCharacterSelect(characterId);
            });
        }

        if (characterSelectPopup == null)
        {
            Debug.LogError(
                "[CHARACTER LIST] characterSelectPopup이 연결되지 않았습니다."
            );
        }
    }

    public void OnClickCharacterSelect(int characterId)
    {
        NetworkManager.Instance.SendLobbyCharacterSelect(characterId);
    }

    public void OnClickOpenCharacterSelect()
    {
        if (characterSelectPopup == null)
            return;

        bool shouldOpen = !characterSelectPopup.activeSelf;
        characterSelectPopup.SetActive(shouldOpen);

        if (shouldOpen)
            NetworkManager.Instance.SendLobbyCharacterList();
    }

    public void OnClickOpenBackgroundSelect()
    {
        if (backgroundSelectPopup == null)
            return;

        backgroundSelectPopup.SetActive(!backgroundSelectPopup.activeSelf);
    }

    public void OnClickBackgroundSelect(int backgroundId)
    {
        NetworkManager.Instance.SendLobbyBackgroundSelect(backgroundId);
    }

    private void OnDestroy()
    {
        NetworkManager.Instance?.ClearLobbyPanelController(this);
    }
}
