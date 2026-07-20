using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePanelController : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Sprite[] Icon;
    [SerializeField] private Slider expSlider;

    public string currentNickname;
    public int currentLevel;
    public int currentplayerIconId;
    public int currentlobbyCharacterId;
    public int currentlobbyBackgroundId;

    private int currentExp;
    private int requiredExp;

    private LobbyPanelController lobbyPanelController;

    private void Start()
    {
        NetworkManager.Instance.SetProfilePanelController(this);

        lobbyPanelController = GetComponent<LobbyPanelController>();
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
        lobbyPanelController.ApplyCharacter(currentlobbyCharacterId);
        lobbyPanelController.ApplyBackgroundPreset(currentlobbyBackgroundId);
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
}