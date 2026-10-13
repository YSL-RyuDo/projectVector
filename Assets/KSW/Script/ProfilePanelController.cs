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
    public int currentLogoBorderId;
    public int currentlobbyCharacterId;
    public int currentlobbyBackgroundId;

    private int currentExp;
    private int requiredExp;

    private LobbyPanelController lobbyPanelController;

    private void Start()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SetProfilePanelController(this);
        else
            Debug.LogWarning("[PROFILE] NetworkManager is not available in this scene run.");

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

        ApplyPlayerIcon(playerIconId);

        ApplyExpUI(currentExp, requiredExp);
        lobbyPanelController.ApplyCharacter(currentlobbyCharacterId);
        lobbyPanelController.ApplyBackgroundPreset(currentlobbyBackgroundId);
    }

    public void ApplyPlayerIcon(int iconId)
    {
        currentplayerIconId = Mathf.Clamp(iconId, 0, 2);
        VectorVoiceChat.Instance?.ObserveProfile(currentNickname, currentplayerIconId);
        FindFirstObjectByType<FriendPanelController>(FindObjectsInactive.Include)?.RefreshMyProfile();
        if (playerIcon == null) return;
        Sprite sprite = Resources.Load<Sprite>($"PlayerIcon/{currentplayerIconId}");
        if (sprite == null && Icon != null && currentplayerIconId < Icon.Length)
            sprite = Icon[currentplayerIconId];
        playerIcon.sprite = sprite;
        playerIcon.enabled = sprite != null;
        playerIcon.color = Color.white;
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
