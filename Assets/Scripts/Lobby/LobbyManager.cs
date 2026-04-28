using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

[System.Serializable]
public struct LobbyBackgroundPreset
{
    public Vector3 characterPosition;
    public Vector3 characterRotation;
    public Vector3 cameraPosition;
    public Vector3 cameraRotation;
}

public class LobbyManager : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Sprite[] Icon;

    private GameObject currentCharacter;
    [Header("Character")]
    [SerializeField] private GameObject[] CharacterPrefebs;
    [SerializeField] private Camera lobbyCamera;

    [Header("Background")]
    [SerializeField] private LobbyBackgroundPreset[] backgroundPresets;

    [Header("Character Popup")]
    [SerializeField] private GameObject characterSelectPopup;
    [SerializeField] private Transform characterSlotContent;
    [SerializeField] private GameObject characterSlotPrefab;

    [Header("Background Popup")]
    [SerializeField] private GameObject backgroundSelectPopup;

    [SerializeField] private GameObject ChatPrefeb;
    [SerializeField] private Transform ChatSlotContent;
    [SerializeField] private TMP_InputField ChatInputField;

    private GameObject currentcharacter;
    private readonly List<GameObject> characterSlots = new();

    private string currentNickname;
    private int currentLevel;
    private int currentplayerIconId;
    private int currentlobbyCharacterId;
    private int currentlobbyBackgroundId;

    public static LobbyManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        NetworkManager.Instance.SetLobbyManager(this);
        NetworkManager.Instance.SendLobbyEnter();

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(false);

        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(false);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            backgroundSelectPopup.SetActive(false);
            characterSelectPopup.SetActive(false);

        }
    }
    public void OnRecvLobbyEnter(string nickname, int level, int playerIconId, int lobbyCharacterModelId, int lobbyBackgroundId)
    {
        currentNickname = nickname;
        currentLevel = level;
        currentplayerIconId = playerIconId;
        currentlobbyCharacterId = lobbyCharacterModelId;
        currentlobbyBackgroundId = lobbyBackgroundId;

        nicknameText.text = nickname;
        levelText.text = "Lv." + level;
        playerIcon.sprite = Icon[currentplayerIconId];

        ApplyCharacter(currentlobbyCharacterId);
        ApplyBackgroundPreset(currentlobbyBackgroundId);
    }

    private void ApplyCharacter(int characterId)
    {
        if (currentCharacter != null)
            Destroy(currentCharacter);

        currentCharacter = Instantiate(CharacterPrefebs[characterId]);
    }

    private void ApplyBackgroundPreset(int backgroundId)
    {
        LobbyBackgroundPreset preset = backgroundPresets[backgroundId];

        currentCharacter.transform.position = preset.characterPosition;
        currentCharacter.transform.rotation = Quaternion.Euler(preset.characterRotation);

        lobbyCamera.transform.position = preset.cameraPosition;
        lobbyCamera.transform.rotation = Quaternion.Euler(preset.cameraRotation);
    }


    public void OnRecvLobbyCharacterSelectResult(byte result, int modelId)
    {
        if (result != 1)
            return;

        currentlobbyCharacterId = modelId;

        ApplyCharacter(currentlobbyCharacterId);
        ApplyBackgroundPreset(currentlobbyBackgroundId);

        characterSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyBackgroundSelectResult(byte result, int backgroundId)
    {
        if (result != 1)
            return;

        currentlobbyBackgroundId = backgroundId;

        ApplyBackgroundPreset(currentlobbyBackgroundId);

        backgroundSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyCharacterList(List<LobbyCharacterData> list, int currentCharacterModelId)
    {

        currentlobbyCharacterId = currentCharacterModelId;

        for (int i = 0; i < characterSlots.Count; i++)
            Destroy(characterSlots[i]);

        characterSlots.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].owned)
                continue;

            int characterId = list[i].characterId;

            GameObject slot = Instantiate(characterSlotPrefab, characterSlotContent);
            characterSlots.Add(slot);

            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);
            for (int j = 0; j < texts.Length; j++)
            {
                if (j == 0)
                    texts[j].text = "Character " + characterId;
                else if (j == 1)
                    texts[j].text = characterId == currentlobbyCharacterId ? "선택중" : "";
            }

            Button button = slot.GetComponent<Button>();
            if (button == null)
                button = slot.GetComponentInChildren<Button>(true);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClickCharacterSelect(characterId));
            }
        }

        characterSelectPopup.SetActive(true);
    }

    public void OnClickOpenCharacterSelect()
    {
        
        NetworkManager.Instance.SendLobbyCharacterList();
    }

    public void OnClickCharacterSelect(int characterId)
    {
       
        NetworkManager.Instance.SendLobbyCharacterSelect(characterId);
    }

    public void OnClickOpenBackgroundSelect()
    {
        backgroundSelectPopup.SetActive(true);
    }

    public void OnClickBackgroundSelect(int backgroundId)
    {
        NetworkManager.Instance.SendLobbyBackgroundSelect(backgroundId);
    }

    public void OnRecvLobbyChat(uint playerId, string nickname, string message)
    {
        Debug.Log($"[LOBBY CHAT UI] {nickname} : {message}");

        if (ChatPrefeb == null)
        {
            Debug.LogError("ChatPrefeb is null");
            return;
        }

        if (ChatSlotContent == null)
        {
            Debug.LogError("ChatSlotContent is null");
            return;
        }

        GameObject item = Instantiate(ChatPrefeb, ChatSlotContent);
        TMP_Text text = item.GetComponentInChildren<TMP_Text>();

        if (text == null)
        {
            Debug.LogError("ChatPrefeb 안에 TMP_Text가 없음");
            return;
        }

        text.text = nickname + " : " + message;
    }

    public void OnClickLobbyChat ()
    {
        string msg = ChatInputField.text.Trim();

        if (msg == "")
            return;

        NetworkManager.Instance.SendLobbyChat(msg);
        ChatInputField.text = "";
    }
}
