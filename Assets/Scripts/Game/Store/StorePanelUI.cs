using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StorePanelUI : MonoBehaviour
{
    public static StorePanelUI Instance;

    [Header("Root")]
    [SerializeField] private GameObject storePanelRoot;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Player Info")]
    [SerializeField] private Image playerCharacterImage;
    [SerializeField] private TMP_Text playerNicknameText;
    [SerializeField] private TMP_Text playerCreditText;

    [Header("Character Sprites")]
    [SerializeField] private Sprite[] characterSprites;

    private void Awake()
    {
        Instance = this;

        if (storePanelRoot != null)
            storePanelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (IsOpen())
                Close();
            else
                TryOpen();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsOpen())
                Close();
        }

        if (IsOpen())
        {
            RefreshPlayerInfo();

            if (!CanOpenStore())
                Close();
        }
    }

    private bool IsOpen()
    {
        return storePanelRoot != null && storePanelRoot.activeSelf;
    }

    private bool CanOpenStore()
    {
        if (RoundManager.Instance == null)
            return false;

        return RoundManager.Instance.IsBuyPhase;
    }

    public void TryOpen()
    {
        if (!CanOpenStore())
        {
            Debug.Log("[STORE] 구매 페이즈가 아니라 열 수 없습니다.");
            return;
        }

        Open();
    }

    public void Open()
    {
        if (storePanelRoot == null)
            return;

        RefreshPlayerInfo();

        storePanelRoot.SetActive(true);
        storePanelRoot.transform.SetAsLastSibling();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("[STORE] open");
    }

    public void Close()
    {
        if (storePanelRoot == null)
            return;

        storePanelRoot.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[STORE] close");
    }

    public void RefreshPlayerInfo()
    {
        if (NetworkManager.Instance == null)
            return;

        if (!NetworkManager.Instance.TryGetMyScoreboardMember(out ScoreboardMemberData myData))
            return;

        if (playerNicknameText != null)
            playerNicknameText.text = myData.nickname;

        if (playerCreditText != null)
            playerCreditText.text = myData.credits.ToString();

        if (playerCharacterImage != null)
        {
            Sprite sprite = GetCharacterSprite(myData.characterId);

            playerCharacterImage.sprite = sprite;
            playerCharacterImage.enabled = sprite != null;
        }
    }

    private Sprite GetCharacterSprite(int characterId)
    {
        if (characterSprites == null)
            return null;

        if (characterId < 0 || characterId >= characterSprites.Length)
            return null;

        return characterSprites[characterId];
    }
}