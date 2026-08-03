using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class WeaponSkinSelectionPanelUI : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private CharacterPanelController characterPanelController;
    [SerializeField] private WeaponPreviewCategory weaponCategory;

    [Header("Weapon And Skin Button Templates")]
    [SerializeField] private Button[] weaponButtons;
    [SerializeField] private Button[] skinButtons;
    [SerializeField] private RectTransform skinSelectionCheckMark;

    [Header("Generated Weapon List")]
    [SerializeField] private float weaponSlotSpacing = 12f;
    [SerializeField] private Vector2 weaponIconPadding = new Vector2(8f, 8f);

    [Header("Skin Selection Colors")]
    [SerializeField] private Color normalSkinColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color selectedSkinColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("Local Selection State")]
    [SerializeField] private int selectedWeaponIndex;
    [SerializeField] private int[] selectedSkinByWeapon;

    private UnityAction[] weaponButtonActions;
    private UnityAction[] skinButtonActions;

    private Vector2 checkAnchorMin;
    private Vector2 checkAnchorMax;
    private Vector2 checkPivot;
    private Vector2 checkAnchoredPosition;
    private Vector2 checkSizeDelta;

    public int SelectedWeaponIndex => selectedWeaponIndex;
    public int SelectedServerWeaponId => GetWeaponEntry(selectedWeaponIndex)?.WeaponId ?? -1;
    public event Action<int> WeaponSelectionChanged;
    public event Action<int, int> SkinSelectionChanged;

    private void Awake()
    {
        BuildWeaponListFromCatalog();
        EnsureLocalSelectionState();
        CacheCheckMarkLayout();
        BindButtons();
        ApplyWeaponSelection(selectedWeaponIndex, false);
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void SelectWeapon(int weaponIndex)
    {
        ApplyWeaponSelection(weaponIndex, true);
    }

    public void SelectSkin(int skinIndex)
    {
        if (!IsValidWeaponIndex(selectedWeaponIndex) || !IsValidSkinIndex(skinIndex))
            return;

        bool selectionChanged = selectedSkinByWeapon[selectedWeaponIndex] != skinIndex;
        selectedSkinByWeapon[selectedWeaponIndex] = skinIndex;
        RefreshSkinSelection();

        if (selectionChanged)
            SkinSelectionChanged?.Invoke(SelectedServerWeaponId, skinIndex);
    }

    public int GetSelectedSkinIndex(int weaponIndex)
    {
        if (!IsValidWeaponIndex(weaponIndex))
            return 0;

        return selectedSkinByWeapon[weaponIndex];
    }

    public int GetSelectedSkinIndexByServerWeaponId(int weaponId)
    {
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            if (GetWeaponEntry(i)?.WeaponId == weaponId)
                return GetSelectedSkinIndex(i);
        }

        return 0;
    }

    private void ApplyWeaponSelection(int weaponIndex, bool notify)
    {
        if (!IsValidWeaponIndex(weaponIndex))
            return;

        bool selectionChanged = selectedWeaponIndex != weaponIndex;
        selectedWeaponIndex = weaponIndex;

        RefreshWeaponSelection();
        RefreshSkinSelection();
        UpdateWeaponPreview();

        if (notify && selectionChanged)
            WeaponSelectionChanged?.Invoke(SelectedServerWeaponId);
    }

    private void BuildWeaponListFromCatalog()
    {
        int weaponCount = characterPanelController != null
            ? characterPanelController.GetWeaponCount(weaponCategory)
            : 0;

        Button template = FindFirstUsableWeaponButton();
        if (weaponCount <= 0 || template == null)
        {
            weaponButtons = Array.Empty<Button>();
            return;
        }

        RectTransform viewport = template.transform.parent as RectTransform;
        RectTransform content = PrepareScrollableWeaponList(viewport);
        List<Button> generatedButtons = new List<Button>(weaponCount);

        for (int i = 0; i < weaponCount; i++)
        {
            Button button;
            if (i < weaponButtons.Length && weaponButtons[i] != null)
            {
                button = weaponButtons[i];
            }
            else
            {
                button = Instantiate(template, content);
            }

            button.transform.SetParent(content, false);
            button.gameObject.SetActive(true);
            ConfigureWeaponButton(button, GetWeaponEntry(i), i);
            generatedButtons.Add(button);
        }

        for (int i = weaponCount; i < weaponButtons.Length; i++)
        {
            if (weaponButtons[i] != null)
                weaponButtons[i].gameObject.SetActive(false);
        }

        weaponButtons = generatedButtons.ToArray();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private Button FindFirstUsableWeaponButton()
    {
        for (int i = 0; i < (weaponButtons?.Length ?? 0); i++)
        {
            if (weaponButtons[i] != null)
                return weaponButtons[i];
        }

        return null;
    }

    private RectTransform PrepareScrollableWeaponList(RectTransform viewport)
    {
        if (viewport == null)
            return null;

        VerticalLayoutGroup existingLayout = viewport.GetComponent<VerticalLayoutGroup>();
        if (existingLayout != null)
            existingLayout.enabled = false;

        RectMask2D mask = viewport.GetComponent<RectMask2D>();
        if (mask == null)
            mask = viewport.gameObject.AddComponent<RectMask2D>();

        ScrollRect scrollRect = viewport.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = viewport.gameObject.AddComponent<ScrollRect>();

        GameObject contentObject = new GameObject(
            "Generated Weapon Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));

        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = weaponSlotSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;

        return content;
    }

    private void ConfigureWeaponButton(Button button, WeaponPreviewEntry entry, int index)
    {
        string displayName = entry != null ? entry.DisplayName : $"Weapon {index + 1}";
        int weaponId = entry?.WeaponId ?? -1;
        button.gameObject.name = $"Weapon Slot {index + 1} - ID {weaponId} {displayName}";

        Image icon = GetOrCreateChildImage(button.transform, "Weapon Icon");
        icon.sprite = entry?.Icon;
        icon.enabled = icon.sprite != null;

        TextMeshProUGUI label = GetOrCreateWeaponLabel(button.transform);
        label.text = weaponId >= 0
            ? $"ID {weaponId:00}\n{displayName}"
            : displayName;
    }

    private Image GetOrCreateChildImage(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;

        if (image == null)
        {
            GameObject imageObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = weaponIconPadding;
            rect.offsetMax = -weaponIconPadding;
            image = imageObject.GetComponent<Image>();
        }

        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI GetOrCreateWeaponLabel(Transform parent)
    {
        Transform existing = parent.Find("Weapon ID Label");
        TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;

        if (label == null)
        {
            GameObject labelObject = new GameObject("Weapon ID Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 15f;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private WeaponPreviewEntry GetWeaponEntry(int categoryIndex)
    {
        return characterPanelController != null
            ? characterPanelController.GetWeaponPreviewEntry(weaponCategory, categoryIndex)
            : null;
    }

    private void EnsureLocalSelectionState()
    {
        int weaponCount = weaponButtons?.Length ?? 0;
        if (weaponCount == 0)
            return;

        if (selectedSkinByWeapon == null || selectedSkinByWeapon.Length != weaponCount)
        {
            int[] previousState = selectedSkinByWeapon;
            selectedSkinByWeapon = new int[weaponCount];

            if (previousState != null)
            {
                int copyCount = Mathf.Min(previousState.Length, selectedSkinByWeapon.Length);
                Array.Copy(previousState, selectedSkinByWeapon, copyCount);
            }
        }

        selectedWeaponIndex = Mathf.Clamp(selectedWeaponIndex, 0, weaponCount - 1);

        int maxSkinIndex = Mathf.Max(0, (skinButtons?.Length ?? 1) - 1);
        for (int i = 0; i < selectedSkinByWeapon.Length; i++)
            selectedSkinByWeapon[i] = Mathf.Clamp(selectedSkinByWeapon[i], 0, maxSkinIndex);
    }

    private void BindButtons()
    {
        weaponButtonActions = new UnityAction[weaponButtons?.Length ?? 0];
        for (int i = 0; i < weaponButtonActions.Length; i++)
        {
            int capturedIndex = i;
            weaponButtonActions[i] = () => SelectWeapon(capturedIndex);

            if (weaponButtons[i] != null)
                weaponButtons[i].onClick.AddListener(weaponButtonActions[i]);
        }

        skinButtonActions = new UnityAction[skinButtons?.Length ?? 0];
        for (int i = 0; i < skinButtonActions.Length; i++)
        {
            int capturedIndex = i;
            skinButtonActions[i] = () => SelectSkin(capturedIndex);

            if (skinButtons[i] != null)
                skinButtons[i].onClick.AddListener(skinButtonActions[i]);
        }
    }

    private void UnbindButtons()
    {
        for (int i = 0; i < (weaponButtonActions?.Length ?? 0); i++)
        {
            if (weaponButtons != null && i < weaponButtons.Length && weaponButtons[i] != null)
                weaponButtons[i].onClick.RemoveListener(weaponButtonActions[i]);
        }

        for (int i = 0; i < (skinButtonActions?.Length ?? 0); i++)
        {
            if (skinButtons != null && i < skinButtons.Length && skinButtons[i] != null)
                skinButtons[i].onClick.RemoveListener(skinButtonActions[i]);
        }
    }

    private void RefreshWeaponSelection()
    {
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            Button button = weaponButtons[i];
            if (button == null)
                continue;

            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = i == selectedWeaponIndex;
        }
    }

    private void RefreshSkinSelection()
    {
        if (!IsValidWeaponIndex(selectedWeaponIndex) || skinButtons == null || skinButtons.Length == 0)
        {
            if (skinSelectionCheckMark != null)
                skinSelectionCheckMark.gameObject.SetActive(false);

            return;
        }

        int selectedSkinIndex = selectedSkinByWeapon[selectedWeaponIndex];

        for (int i = 0; i < skinButtons.Length; i++)
        {
            Button button = skinButtons[i];
            if (button?.targetGraphic != null)
                button.targetGraphic.color = i == selectedSkinIndex ? selectedSkinColor : normalSkinColor;
        }

        MoveCheckMark(selectedSkinIndex);
    }

    private void UpdateWeaponPreview()
    {
        if (characterPanelController != null)
            characterPanelController.SelectWeapon(weaponCategory, selectedWeaponIndex);
    }

    private void CacheCheckMarkLayout()
    {
        if (skinSelectionCheckMark == null)
            return;

        checkAnchorMin = skinSelectionCheckMark.anchorMin;
        checkAnchorMax = skinSelectionCheckMark.anchorMax;
        checkPivot = skinSelectionCheckMark.pivot;
        checkAnchoredPosition = skinSelectionCheckMark.anchoredPosition;
        checkSizeDelta = skinSelectionCheckMark.sizeDelta;
    }

    private void MoveCheckMark(int skinIndex)
    {
        if (skinSelectionCheckMark == null || !IsValidSkinIndex(skinIndex))
            return;

        Button selectedButton = skinButtons[skinIndex];
        if (selectedButton == null)
        {
            skinSelectionCheckMark.gameObject.SetActive(false);
            return;
        }

        skinSelectionCheckMark.SetParent(selectedButton.transform, false);
        skinSelectionCheckMark.anchorMin = checkAnchorMin;
        skinSelectionCheckMark.anchorMax = checkAnchorMax;
        skinSelectionCheckMark.pivot = checkPivot;
        skinSelectionCheckMark.anchoredPosition = checkAnchoredPosition;
        skinSelectionCheckMark.sizeDelta = checkSizeDelta;
        skinSelectionCheckMark.SetAsLastSibling();
        skinSelectionCheckMark.gameObject.SetActive(true);
    }

    private bool IsValidWeaponIndex(int index)
    {
        return weaponButtons != null && index >= 0 && index < weaponButtons.Length;
    }

    private bool IsValidSkinIndex(int index)
    {
        return skinButtons != null && index >= 0 && index < skinButtons.Length;
    }
}
