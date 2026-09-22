using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// All visual objects are authored by ChipCraftHierarchyInstaller in Lobby.unity.
// This component only binds interactions and updates their existing Image/Text fields.
public class MyPageChipCraftController : MonoBehaviour
{
    private static readonly string[] MedalNames = { "브론즈", "실버", "골드", "플래티넘", "다이아몬드", "마스터" };
    private static readonly Color Selected = new Color32(193, 37, 53, 255);
    private static readonly Color Unselected = new Color32(54, 59, 65, 255);
    private ChipCraftData data;
    private ChipCraftPresetData draft;
    private GameObject panel;
    private GameObject dialog;
    private bool editing;
    private int tab;
    private int selectedMedal = -1;
    private int selectedPlaced = -1;
    private string targetUserId = "";

    private Transform Find(string name)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private void Bind(string name, UnityEngine.Events.UnityAction action)
    {
        Button button = Find(name)?.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(action);
    }

    private void Awake()
    {
        panel = Find("ChipCraftPanel")?.gameObject;
        dialog = Find("ChipCraftSaveDialog")?.gameObject;
        Bind("ChipCraftOpenButton", Open);
        Bind("ChipCraftCardOpenButton", Open);
        Bind("ChipCraftBackButton", Back);
        Bind("ChipCraftEditButton", EnterEdit);
        Bind("ChipCraftSaveButton", RequestSaveConfirmation);
        Bind("ChipCraftResetButton", ResetDraft);
        Bind("ChipCraftConfirmButton", ConfirmSave);
        Bind("ChipCraftCancelButton", () => dialog?.SetActive(false));
        Bind("ChipCraftMedalApplyButton", ApplySelectedMedal);
        Bind("ChipCraftMedalDeleteButton", DeleteSelectedMedal);
        Bind("ChipCraftMedalRotateButton", () => EditSelectedMedal(value => value.rotation = (value.rotation + 45f) % 360f));
        Bind("ChipCraftMedalFlipButton", () => EditSelectedMedal(value => value.flipped = !value.flipped));
        Bind("ChipCraftMedalLargerButton", () => EditSelectedMedal(value => value.scale = Mathf.Min(2.5f, value.scale + .15f)));
        Bind("ChipCraftMedalSmallerButton", () => EditSelectedMedal(value => value.scale = Mathf.Max(.5f, value.scale - .15f)));
        for (int i = 0; i < 3; ++i)
        {
            int index = i;
            Bind("ChipCraftTab" + i, () => SelectTab(index));
            Bind("ChipCraftCardOption" + i, () => SelectCard(index));
            Bind("ChipCraftCaseOption" + i, () => SelectCase(index));
        }
        for (int i = 0; i < 6; ++i)
        {
            int index = i;
            Bind("ChipCraftMedalOption" + i, () => SelectMedal(index));
        }
        for (int i = 0; i < 3; ++i)
        {
            int index = i;
            Bind("ChipCraftPlacedMedal" + i, () => SelectPlacedMedal(index));
        }
        if (panel != null) panel.SetActive(false);
        if (dialog != null) dialog.SetActive(false);
    }

    public void SetTarget(string userId)
    {
        targetUserId = userId ?? "";
        data = null;
        editing = false;
        if (panel != null) panel.SetActive(false);
        if (dialog != null) dialog.SetActive(false);
    }

    public void ApplyData(ChipCraftData value)
    {
        if (value == null) return;
        if (targetUserId.Length != 0 && targetUserId != value.userId) return;
        data = value;
        if (!editing) draft = value.preset.Clone();
        RefreshProfileCard();
        Refresh();
    }

    public void ApplySaveResult(bool success, ChipCraftPresetData applied)
    {
        if (!success || applied == null)
        {
            SetText("ChipCraftStatus", "저장에 실패했습니다. 보유 상태와 훈장 수를 확인하세요.");
            NetworkManager.Instance?.SendChipCraftRequest(targetUserId);
            return;
        }
        if (data == null) return;
        data.preset = applied.Clone();
        draft = applied.Clone();
        SetText("ChipCraftStatus", "프리셋이 저장되고 적용되었습니다.");
        RefreshProfileCard();
        Refresh(); // The editor intentionally remains open after saving.
    }

    public void Open()
    {
        if (panel == null) return;
        if (data == null)
        {
            NetworkManager.Instance?.SendChipCraftRequest(targetUserId);
            return;
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        editing = false;
        draft = data.preset.Clone();
        selectedPlaced = -1;
        selectedMedal = -1;
        SelectTab(0);
    }

    private void Back()
    {
        if (dialog != null && dialog.activeSelf) { dialog.SetActive(false); return; }
        if (editing)
        {
            editing = false;
            draft = data?.preset.Clone();
            selectedPlaced = -1;
            SetText("ChipCraftStatus", "");
            Refresh();
        }
        else if (panel != null) panel.SetActive(false);
    }

    public bool HandleEscape()
    {
        if (panel == null || !panel.activeSelf) return false;
        Back();
        return true;
    }

    private void EnterEdit()
    {
        if (data == null || !data.isSelf) return;
        editing = true;
        draft = data.preset.Clone();
        selectedPlaced = -1;
        SetText("ChipCraftStatus", "");
        Refresh();
    }

    private void ResetDraft()
    {
        if (!editing || data == null) return;
        draft = data.preset.Clone();
        selectedPlaced = -1;
        Refresh();
    }

    private void RequestSaveConfirmation()
    {
        if (editing && data != null && data.isSelf && dialog != null) dialog.SetActive(true);
    }

    private void ConfirmSave()
    {
        if (dialog != null) dialog.SetActive(false);
        if (!editing || draft == null || !data.isSelf) return;
        NetworkManager.Instance?.SendChipCraftSave(draft);
        SetText("ChipCraftStatus", "프리셋을 저장하는 중입니다.");
    }

    private void SelectTab(int value) { tab = value; selectedMedal = -1; Refresh(); }
    private bool Owned(int mask, int index) => (mask & (1 << index)) != 0;

    private void SelectCard(int index)
    {
        if (data == null || !Owned(data.ownedCardMask, index) && editing) return;
        if (editing) draft.cardId = index;
        else draft = new ChipCraftPresetData { cardId = index, caseId = data.preset.caseId, medals = data.preset.medals.ConvertAll(m => m.Clone()) };
        Refresh();
    }

    private void SelectCase(int index)
    {
        if (data == null || !Owned(data.ownedCaseMask, index) && editing) return;
        if (editing)
        {
            draft.caseId = index;
            while (draft.medals.Count > index + 1) draft.medals.RemoveAt(draft.medals.Count - 1);
        }
        Refresh();
    }

    private void SelectMedal(int index)
    {
        if (data == null || editing && !Owned(data.ownedMedalMask, index)) return;
        selectedMedal = index;
        selectedPlaced = -1;
        Refresh();
    }

    private void ApplySelectedMedal()
    {
        if (!editing || selectedMedal < 0 || !Owned(data.ownedMedalMask, selectedMedal) ||
            draft.medals.Count >= Mathf.Min(3, draft.caseId + 1)) return;
        draft.medals.Add(new ChipCraftMedalData { medalId = selectedMedal, scale = 1f });
        selectedPlaced = draft.medals.Count - 1;
        Refresh();
    }

    private void SelectPlacedMedal(int index)
    {
        if (!editing || index >= draft.medals.Count) return;
        selectedPlaced = index;
        selectedMedal = draft.medals[index].medalId;
        Refresh();
    }

    private void DeleteSelectedMedal()
    {
        if (!editing || selectedPlaced < 0 || selectedPlaced >= draft.medals.Count) return;
        draft.medals.RemoveAt(selectedPlaced);
        selectedPlaced = -1;
        Refresh();
    }

    private void EditSelectedMedal(System.Action<ChipCraftMedalData> action)
    {
        if (!editing || selectedPlaced < 0 || selectedPlaced >= draft.medals.Count) return;
        action(draft.medals[selectedPlaced]);
        Refresh();
    }

    public void DragPlacedMedal(int index, PointerEventData eventData)
    {
        if (!editing || index >= draft.medals.Count) return;
        RectTransform art = Find("ChipCraftPreviewArt") as RectTransform;
        if (art == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
            art, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
        draft.medals[index].x = Mathf.Clamp(local.x / art.rect.width, -.5f, .5f);
        draft.medals[index].y = Mathf.Clamp(local.y / art.rect.height, -.5f, .5f);
        selectedPlaced = index;
        RefreshPreview();
    }

    private void Refresh()
    {
        if (data == null || draft == null) return;
        if (panel == null || !panel.activeSelf) return;
        SetText("ChipCraftPageTitle", editing ? "칩 크래프트 - 편집 모드" : "칩 크래프트");
        SetActive("ChipCraftEditButton", !editing && data.isSelf);
        SetActive("ChipCraftSaveButton", editing);
        SetActive("ChipCraftResetButton", editing);
        SetActive("ChipCraftEditControls", editing && tab == 2);
        SetActive("ChipCraftCardList", tab == 0);
        SetActive("ChipCraftCaseList", tab == 1);
        SetActive("ChipCraftMedalList", tab == 2);
        for (int i = 0; i < 3; ++i)
        {
            SetColor("ChipCraftTab" + i, tab == i ? Selected : Unselected);
            SetActive("ChipCraftCardOption" + i, !editing || Owned(data.ownedCardMask, i));
            SetActive("ChipCraftCaseOption" + i, i == 0 && (!editing || Owned(data.ownedCaseMask, i)));
            SetColor("ChipCraftCardOption" + i, draft.cardId == i ? Selected : Unselected);
            SetColor("ChipCraftCaseOption" + i, draft.caseId == i ? Selected : Unselected);
        }
        for (int i = 0; i < 6; ++i)
        {
            SetActive("ChipCraftMedalOption" + i, !editing || Owned(data.ownedMedalMask, i));
            SetColor("ChipCraftMedalOption" + i, selectedMedal == i ? Selected : Unselected);
        }
        SetText("ChipCraftMedalCount", $"훈장 수  {draft.medals.Count} / {Mathf.Min(3, draft.caseId + 1)}");
        string item = tab == 0 ? $"플레이어 카드 {draft.cardId + 1}"
            : tab == 1 ? $"케이스 {draft.caseId + 1}"
            : selectedMedal >= 0 ? MedalNames[selectedMedal] : "훈장을 선택하세요";
        SetText("ChipCraftItemTitle", item);
        SetText("ChipCraftItemDescription", tab == 1
            ? $"장착 가능한 훈장 수: {Mathf.Min(3, draft.caseId + 1)}개"
            : tab == 2 ? "훈장을 카드에 놓고 위치·크기·회전을 조절할 수 있습니다." : "플레이어 카드 그림을 선택하세요.");
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (draft == null) return;
        SetSprite("ChipCraftPreviewArt", Resources.Load<Sprite>($"IconCard/icon{draft.cardId}_card"));
        SetSprite("ChipCraftPreviewCase", Resources.Load<Sprite>("IconBorder/border"));
        Image cardArt = Find("ChipCraftPreviewArt")?.GetComponent<Image>();
        Image caseArt = Find("ChipCraftPreviewCase")?.GetComponent<Image>();
        // Browse tabs inspect one asset category at a time. Edit mode keeps the
        // composited card visible so medal placement has a real coordinate frame.
        if (cardArt != null) cardArt.enabled = editing || tab == 0;
        if (caseArt != null) caseArt.enabled = editing || tab == 1;
        RectTransform art = Find("ChipCraftPreviewArt") as RectTransform;
        for (int i = 0; i < 3; ++i)
        {
            Transform slot = Find("ChipCraftPlacedMedal" + i);
            if (slot == null) continue;
            bool browseMedal = !editing && tab == 2 && i == 0 && selectedMedal >= 0;
            bool visible = editing ? i < draft.medals.Count : browseMedal;
            slot.gameObject.SetActive(visible);
            if (!visible || art == null) continue;
            ChipCraftMedalData medal = browseMedal
                ? new ChipCraftMedalData { medalId = selectedMedal, scale = 1.8f }
                : draft.medals[i];
            Image image = slot.GetComponent<Image>();
            if (image != null) image.sprite = Resources.Load<Sprite>("Rank/" + new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" }[Mathf.Clamp(medal.medalId, 0, 5)]);
            RectTransform rect = slot as RectTransform;
            rect.anchoredPosition = browseMedal ? Vector2.zero :
                new Vector2(medal.x * art.rect.width, medal.y * art.rect.height);
            rect.localRotation = Quaternion.Euler(0, 0, browseMedal ? 0 : medal.rotation);
            rect.localScale = new Vector3(medal.flipped ? -medal.scale : medal.scale, medal.scale, 1);
            SetColor("ChipCraftPlacedMedal" + i, Color.white);
        }
    }

    public void RefreshProfileCard()
    {
        if (data == null || data.preset == null) return;
        SetSprite("CardIconImage", Resources.Load<Sprite>($"IconCard/icon{data.preset.cardId}_card"));
        SetSprite("CardBorderImage", Resources.Load<Sprite>("IconBorder/border"));
        RectTransform art = Find("CardIconImage") as RectTransform;
        for (int i = 0; i < 3; ++i)
        {
            Transform slot = Find("CardAppliedMedal" + i);
            if (slot == null) continue;
            bool visible = i < data.preset.medals.Count;
            slot.gameObject.SetActive(visible);
            if (!visible || art == null) continue;
            ChipCraftMedalData medal = data.preset.medals[i];
            Image image = slot.GetComponent<Image>();
            if (image != null) image.sprite = Resources.Load<Sprite>("Rank/" + new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" }[Mathf.Clamp(medal.medalId, 0, 5)]);
            RectTransform rect = slot as RectTransform;
            rect.anchoredPosition = new Vector2(medal.x * art.rect.width, medal.y * art.rect.height);
            rect.localRotation = Quaternion.Euler(0, 0, medal.rotation);
            rect.localScale = new Vector3(medal.flipped ? -medal.scale : medal.scale, medal.scale, 1);
        }
    }

    private void SetText(string name, string value)
    {
        TMP_Text text = Find(name)?.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }
    private void SetColor(string name, Color value)
    {
        Image image = Find(name)?.GetComponent<Image>();
        if (image != null) image.color = value;
    }
    private void SetSprite(string name, Sprite value)
    {
        Image image = Find(name)?.GetComponent<Image>();
        if (image == null) return;
        image.sprite = value;
        image.enabled = value != null;
    }
    private void SetActive(string name, bool value)
    {
        Transform child = Find(name);
        if (child != null) child.gameObject.SetActive(value);
    }
}

public class ChipCraftMedalDragHandle : MonoBehaviour, IDragHandler
{
    [SerializeField] private int slot;
    public void SetSlot(int value) => slot = value;
    public void OnDrag(PointerEventData eventData)
    {
        GetComponentInParent<MyPageChipCraftController>()?.DragPlacedMedal(slot, eventData);
    }
}
