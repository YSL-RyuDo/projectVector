using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickCharacterButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image characterImage;

    [SerializeField] private GameObject myPickObject;
    [SerializeField] private GameObject teamPickObject;
    [SerializeField] private TMP_Text nicknameText;

    private int characterId;
    private Action<int> onClick;

    public bool IsPickActive { get; private set; }

    private void OnEnable()
    {
        IsPickActive = true;
    }

    private void OnDisable()
    {
        IsPickActive = false;
    }

    public void Init(PickCharacterData data, Action<int> callback)
    {
        characterId = data.characterId;
        onClick = callback;

        if (button == null)
            button = GetComponent<Button>();

        if (characterImage != null)
            characterImage.sprite = data.characterImage;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                onClick?.Invoke(characterId);

            });
        }

        SetPickState(false, false, "", true);
    }

    public void SetPickState(bool myPick, bool teamPick, string nickname, bool interactable)
    {
        if (myPickObject != null)
            myPickObject.SetActive(myPick);

        if (teamPickObject != null)
            teamPickObject.SetActive(teamPick);

        if (nicknameText != null)
        {
            nicknameText.gameObject.SetActive(myPick || teamPick);
            nicknameText.text = nickname;
        }

        if (button != null)
            button.interactable = interactable;
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }
}
