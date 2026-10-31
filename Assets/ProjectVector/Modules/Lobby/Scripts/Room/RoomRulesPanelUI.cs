using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameMapId : byte
{
    TestMap = 0,
    Desert = 1,
    Construction = 2
}

public class RoomRulesPanelUI : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button testMapButton;
    [SerializeField] private Button desertButton;
    [SerializeField] private Button constructionButton;
    [SerializeField] private TMP_Text selectedMapText;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = new Color32(58, 66, 78, 255);
    [SerializeField] private Color selectedColor = new Color32(201, 42, 58, 255);

    public Action<GameMapId> OnMapSelected;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }

        if (testMapButton != null)
        {
            testMapButton.onClick.RemoveAllListeners();
            testMapButton.onClick.AddListener(() => OnMapSelected?.Invoke(GameMapId.TestMap));
        }

        if (desertButton != null)
        {
            desertButton.onClick.RemoveAllListeners();
            desertButton.onClick.AddListener(() => OnMapSelected?.Invoke(GameMapId.Desert));
        }
        if (constructionButton != null)
        {
            constructionButton.onClick.RemoveAllListeners();
            constructionButton.onClick.AddListener(() => OnMapSelected?.Invoke(GameMapId.Construction));
        }
    }

    public void Show(string currentMapName)
    {
        gameObject.SetActive(true);
        ApplySelectedMap(currentMapName);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ApplySelectedMap(string mapName)
    {
        bool isDesert = string.Equals(mapName, "Desert", StringComparison.OrdinalIgnoreCase);
        bool isConstruction = string.Equals(mapName, "Construction", StringComparison.OrdinalIgnoreCase);
        SetButtonSelected(testMapButton, !isDesert && !isConstruction);
        SetButtonSelected(desertButton, isDesert);
        SetButtonSelected(constructionButton, isConstruction);

        if (selectedMapText != null)
            selectedMapText.text = isConstruction ? "현재 선택: 공사장" : isDesert ? "현재 선택: Desert" : "현재 선택: TestMap";
    }

    private void SetButtonSelected(Button button, bool selected)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = selected ? selectedColor : normalColor;
    }
}
