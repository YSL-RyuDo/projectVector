using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private Image keyImage;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text actionHint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string message, bool hold = false)
    {
        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        if (keyImage != null)
            keyImage.gameObject.SetActive(true);

        if (keyText) keyText.text = "F";
        if (actionHint) actionHint.text = hold ? "길게 누르기" : "누르기";
        if (promptText != null) promptText.text = message;
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}