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

public class LobbyManager : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Sprite[] Icon;
    [SerializeField] private Slider expSlider;

    private int currentExp;
    private int requiredExp;

    [Header("Character")]
    [SerializeField] private GameObject[] CharacterPrefebs;
    [SerializeField] private Camera lobbyCamera;
    private GameObject currentCharacter;

    [Header("Background")]
    [SerializeField] private LobbyBackgroundPreset[] backgroundPresets;

    [Header("Character Popup")]
    [SerializeField] private GameObject characterSelectPopup;
    [SerializeField] private Transform characterSlotContent;
    [SerializeField] private GameObject characterSlotPrefab;

    [Header("Background Popup")]
    [SerializeField] private GameObject backgroundSelectPopup;

    [Header("Chat")]
    [SerializeField] private GameObject ChatPrefeb;
    [SerializeField] private Transform ChatSlotContent;
    [SerializeField] private TMP_InputField ChatInputField;

    [Header("Temp Game Room")]
    [SerializeField] private GameObject tempGameRoomPanel;
    [SerializeField] private TempGamePlayerSlotUI[] tempGamePlayerSlots; // 0,1,2
    [SerializeField] private Sprite[] tempRoomIconSprites;
    [SerializeField] private Sprite[] tempRoomBorderSprites;
    [SerializeField] private Sprite[] tempRoomRankSprites;

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

        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        HideAllTempGameSlots();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (backgroundSelectPopup != null)
                backgroundSelectPopup.SetActive(false);

            if (characterSelectPopup != null)
                characterSelectPopup.SetActive(false);
        }
    }

    public void OnRecvLobbyEnter(
        string nickname,
        int level,
        int currentExpValue,
        int requiredExpValue,
        int playerIconId,
        int lobbyCharacterModelId,
        int lobbyBackgroundId)
    {
        currentNickname = nickname;
        currentLevel = level;
        currentExp = currentExpValue;
        requiredExp = requiredExpValue;

        currentplayerIconId = playerIconId;
        currentlobbyCharacterId = lobbyCharacterModelId;
        currentlobbyBackgroundId = lobbyBackgroundId;

        if (nicknameText != null)
            nicknameText.text = nickname;

        if (levelText != null)
            levelText.text = "Lv." + level;

        if (Icon != null && Icon.Length > 0 && playerIcon != null)
        {
            if (currentplayerIconId < 0 || currentplayerIconId >= Icon.Length)
                currentplayerIconId = 0;

            playerIcon.sprite = Icon[currentplayerIconId];
        }

        ApplyExpUI(currentExp, requiredExp);
        ApplyCharacter(currentlobbyCharacterId);
        ApplyBackgroundPreset(currentlobbyBackgroundId);
    }

    private void ApplyCharacter(int characterId)
    {
        if (currentCharacter != null)
            Destroy(currentCharacter);

        if (CharacterPrefebs == null || CharacterPrefebs.Length == 0)
            return;

        if (characterId < 0 || characterId >= CharacterPrefebs.Length)
            characterId = 0;

        currentCharacter = Instantiate(CharacterPrefebs[characterId]);
    }

    private void ApplyBackgroundPreset(int backgroundId)
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

        currentlobbyCharacterId = modelId;

        ApplyCharacter(currentlobbyCharacterId);
        ApplyBackgroundPreset(currentlobbyBackgroundId);

        if (characterSelectPopup != null)
            characterSelectPopup.SetActive(false);
    }

    public void OnRecvLobbyBackgroundSelectResult(byte result, int backgroundId)
    {
        if (result != 1)
            return;

        currentlobbyBackgroundId = backgroundId;
        ApplyBackgroundPreset(currentlobbyBackgroundId);

        if (backgroundSelectPopup != null)
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
                    texts[j].text = characterId == currentlobbyCharacterId ? "¼±ÅÃÁß" : "";
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

        if (characterSelectPopup != null)
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
        if (backgroundSelectPopup != null)
            backgroundSelectPopup.SetActive(true);
    }

    public void OnClickBackgroundSelect(int backgroundId)
    {
        NetworkManager.Instance.SendLobbyBackgroundSelect(backgroundId);
    }

    public void OnRecvLobbyChat(uint playerId, string nickname, string message)
    {
        if (ChatPrefeb == null || ChatSlotContent == null)
            return;

        GameObject item = Instantiate(ChatPrefeb, ChatSlotContent);
        TMP_Text text = item.GetComponentInChildren<TMP_Text>();

        if (text != null)
            text.text = nickname + " : " + message;
    }

    public void OnClickLobbyChat()
    {
        string msg = ChatInputField.text.Trim();

        if (msg == "")
            return;

        NetworkManager.Instance.SendLobbyChat(msg);
        ChatInputField.text = "";
    }

    private void ApplyExpUI(int current, int required)
    {
        if (required <= 0)
            required = 1;

        if (current < 0)
            current = 0;

        if (current > required)
            current = required;

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = required;
            expSlider.value = current;
        }
    }

    private void HideAllTempGameSlots()
    {
        if (tempGamePlayerSlots == null)
            return;

        for (int i = 0; i < tempGamePlayerSlots.Length; i++)
        {
            if (tempGamePlayerSlots[i] != null)
                tempGamePlayerSlots[i].Hide();
        }
    }

    private void OpenTempGamePanel()
    {
        if (tempGameRoomPanel == null)
        {
            Debug.LogError("[TEMP GAME ROOM] tempGameRoomPanel is null");
            return;
        }

        tempGameRoomPanel.SetActive(true);
    }

    private void FillLocalTempGameSlot0()
    {
        if (tempGamePlayerSlots == null || tempGamePlayerSlots.Length == 0)
            return;

        TempGamePlayerSlotUI slot0 = tempGamePlayerSlots[0];
        if (slot0 == null)
            return;

        Sprite icon = GetSafeSprite(tempRoomIconSprites, currentplayerIconId);
        Sprite border = GetSafeSprite(tempRoomBorderSprites, 0);
        Sprite rank = GetSafeSprite(tempRoomRankSprites, 0);

        slot0.SetData(icon, border, rank, currentNickname);

        for (int i = 1; i < tempGamePlayerSlots.Length; i++)
        {
            if (tempGamePlayerSlots[i] != null)
                tempGamePlayerSlots[i].Hide();
        }
    }

    public void OnClickGameButton()
    {
        Debug.Log("[TEMP GAME ROOM] GameButton Click");

        OpenTempGamePanel();
        HideAllTempGameSlots();
        FillLocalTempGameSlot0();

        NetworkManager.Instance.SendTempGameRoomEnter();
    }

    public void OnClickTempGameStart()
    {
        Debug.Log("[TEMP GAME ROOM] StartButton Click - not used in prototype");
    }

    public void OnClickCloseTempGameRoom()
    {
        if (tempGameRoomPanel != null)
            tempGameRoomPanel.SetActive(false);

        HideAllTempGameSlots();
    }

    public void OnRecvTempGameRoomEnter(int playerCount, List<TempGamePlayerData> players)
    {
        Debug.Log($"[TEMP GAME ROOM] enter success, playerCount={playerCount}");

        OpenTempGamePanel();
        HideAllTempGameSlots();

        for (int i = 0; i < players.Count; i++)
        {
            TempGamePlayerData p = players[i];

            if (tempGamePlayerSlots == null)
                break;

            if (p.slotIndex < 0 || p.slotIndex >= tempGamePlayerSlots.Length)
                continue;

            if (p.slotIndex >= playerCount)
                continue;

            Sprite icon = GetSafeSprite(tempRoomIconSprites, p.playerIconId);
            Sprite border = GetSafeSprite(tempRoomBorderSprites, p.logoBorderId);
            Sprite rank = GetSafeSprite(tempRoomRankSprites, p.rankImageId);

            if (tempGamePlayerSlots[p.slotIndex] != null)
                tempGamePlayerSlots[p.slotIndex].SetData(icon, border, rank, p.nickname);
        }
    }

    public void OnRecvTempGameStartResult(byte result)
    {
        Debug.Log($"[TEMP GAME] start result={result}");
    }

    private Sprite GetSafeSprite(Sprite[] sprites, int index)
    {
        if (sprites == null || sprites.Length == 0)
            return null;

        if (index < 0 || index >= sprites.Length)
            return sprites[0];

        return sprites[index];
    }
}