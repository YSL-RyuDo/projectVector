using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkinSelectionPanelUI : MonoBehaviour
{
    [Header("Skin Items")]
    [SerializeField] private Button[] skinButtons;
    [SerializeField] private RectTransform selectionCheckMark;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private int selectedIndex;

    private Vector2 checkAnchorMin;
    private Vector2 checkAnchorMax;
    private Vector2 checkPivot;
    private Vector2 checkAnchoredPosition;
    private Vector2 checkSizeDelta;

    public int SelectedIndex => selectedIndex;
    public event Action<int> SelectionChanged;

    private void Awake()
    {
        CacheCheckMarkLayout();
        ApplySelection(selectedIndex, false);
    }

    public void SelectSkin(int index)
    {
        ApplySelection(index, true);
    }

    private void ApplySelection(int index, bool notify)
    {
        if (skinButtons == null || skinButtons.Length == 0)
        {
            Debug.LogWarning("[SKIN SELECTION] 연결된 스킨 버튼이 없습니다.", this);
            return;
        }

        if (index < 0 || index >= skinButtons.Length)
        {
            Debug.LogWarning($"[SKIN SELECTION] 잘못된 스킨 인덱스입니다. index={index}", this);
            return;
        }

        bool selectionChanged = selectedIndex != index;
        selectedIndex = index;

        for (int i = 0; i < skinButtons.Length; i++)
        {
            Button button = skinButtons[i];
            if (button == null)
                continue;

            Graphic background = button.targetGraphic;
            if (background != null)
                background.color = i == selectedIndex ? selectedColor : normalColor;
        }

        MoveCheckMarkToSelectedButton();

        if (notify && selectionChanged)
            SelectionChanged?.Invoke(selectedIndex);
    }

    private void CacheCheckMarkLayout()
    {
        if (selectionCheckMark == null)
            return;

        checkAnchorMin = selectionCheckMark.anchorMin;
        checkAnchorMax = selectionCheckMark.anchorMax;
        checkPivot = selectionCheckMark.pivot;
        checkAnchoredPosition = selectionCheckMark.anchoredPosition;
        checkSizeDelta = selectionCheckMark.sizeDelta;
    }

    private void MoveCheckMarkToSelectedButton()
    {
        if (selectionCheckMark == null)
            return;

        Button selectedButton = skinButtons[selectedIndex];
        if (selectedButton == null)
        {
            selectionCheckMark.gameObject.SetActive(false);
            return;
        }

        selectionCheckMark.SetParent(selectedButton.transform, false);
        selectionCheckMark.anchorMin = checkAnchorMin;
        selectionCheckMark.anchorMax = checkAnchorMax;
        selectionCheckMark.pivot = checkPivot;
        selectionCheckMark.anchoredPosition = checkAnchoredPosition;
        selectionCheckMark.sizeDelta = checkSizeDelta;
        selectionCheckMark.SetAsLastSibling();
        selectionCheckMark.gameObject.SetActive(true);
    }
}
