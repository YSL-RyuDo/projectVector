using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All UI and layout are authored in the scene. This component only binds data and visibility.
public sealed class MyPageBirthdayController : MonoBehaviour
{
    public Button entryButton, saveButton, cancelButton, closeButton;
    public TMP_Text birthdayText, message, saveLabel;
    public TMP_InputField monthInput, dayInput;
    public GameObject popup;
    MyPageProfileData profile;
    bool bound;
    static uint sequence;
    uint pending;
    string pendingUser;
    float sentAt;
    public bool IsOpen => popup != null && popup.activeSelf;
    public bool IsSaving => pending != 0;
    public bool CanEdit => profile != null && profile.isSelf;

    public static bool IsValidDate(int month, int day) => month >= 1 && month <= 12 && day >= 1 && day <= DateTime.DaysInMonth(2000, month);
    public void Initialize()
    {
        if (bound) return;
        bound = true;
        entryButton.onClick.AddListener(Open);
        saveButton.onClick.AddListener(Save);
        cancelButton.onClick.AddListener(Close);
        closeButton.onClick.AddListener(Close);
        monthInput.onValueChanged.AddListener(_ => Validate());
        dayInput.onValueChanged.AddListener(_ => Validate());
    }
    public void ResetContext()
    {
        Initialize(); profile = null; pending = 0; pendingUser = null;
        popup.SetActive(false); entryButton.interactable = false; birthdayText.raycastTarget = false;
    }
    public void ApplyProfile(MyPageProfileData data)
    {
        Initialize();
        if (profile == null || profile.userId != data.userId || profile.isSelf != data.isSelf) ResetContext();
        profile = data;
        entryButton.interactable = data.isSelf; birthdayText.raycastTarget = data.isSelf;
        RefreshCaption();
    }
    void RefreshCaption()
    {
        if (profile == null) return;
        bool unset = string.IsNullOrEmpty(profile.birthday) || profile.birthday == "----";
        birthdayText.text = profile.privacyEnabled ? "생일: 비공개" : unset ? "생일: 미설정" : $"생일: {profile.birthday}";
        if (profile.isSelf) birthdayText.text += unset ? "  [설정]" : "  [변경]";
    }
    public void Open()
    {
        if (!CanEdit) return;
        Initialize(); popup.SetActive(true);
        if (!IsSaving)
        {
            string[] values = (profile.birthday ?? "").Split('.');
            monthInput.SetTextWithoutNotify(values.Length == 2 ? values[0] : "");
            dayInput.SetTextWithoutNotify(values.Length == 2 ? values[1] : "");
        }
        Validate();
    }
    public void Close() => popup.SetActive(false);
    public bool HandleBack()
    {
        if (!IsOpen) return false;
        Close(); return true;
    }
    bool ReadDate(out int month, out int day)
    {
        month = day = 0;
        return int.TryParse(monthInput.text, out month) && int.TryParse(dayInput.text, out day) && IsValidDate(month, day);
    }
    void Validate()
    {
        bool valid = ReadDate(out _, out _);
        monthInput.interactable = dayInput.interactable = !IsSaving;
        saveButton.interactable = CanEdit && valid && !IsSaving;
        saveLabel.text = IsSaving ? "저장 중…" : "저장";
        message.text = IsSaving ? "생일을 저장하고 있습니다." : valid ? "저장 후에도 생일을 변경할 수 있습니다." : "올바른 월과 일을 입력해 주세요. (2월 29일 가능)";
    }
    public void Save()
    {
        if (!CanEdit || IsSaving || !ReadDate(out int month, out int day)) { Validate(); return; }
        var network = NetworkManager.Instance;
        if (network == null || !network.IsConnected || !network.IsAuthenticated || network.MyUserId != profile.userId)
        { message.text = "서버 연결을 확인한 뒤 다시 시도해 주세요."; return; }
        if (++sequence == 0) ++sequence;
        pending = sequence; pendingUser = profile.userId; sentAt = Time.unscaledTime;
        if (!network.SendProfileBirthday(pending, month, day))
        { pending = 0; Validate(); message.text = "저장 요청을 보내지 못했습니다. 다시 시도해 주세요."; return; }
        Validate();
    }
    public void ApplyResult(uint requestId, byte result, int month, int day)
    {
        if (pending == 0 || requestId != pending || !CanEdit || profile.userId != pendingUser) return;
        var network = NetworkManager.Instance;
        if (network != null && network.MyUserId != pendingUser) return;
        pending = 0; Validate();
        if (result != 0 || !IsValidDate(month, day))
        {
            message.text = result == 1 ? "존재하지 않는 날짜입니다. 월과 일을 확인해 주세요." : "생일을 저장하지 못했습니다. 다시 시도해 주세요.";
            return;
        }
        profile.birthday = $"{month:00}.{day:00}"; RefreshCaption();
        Close();
    }
    private void Update()
    {
        if (!IsSaving) return;
        var network = NetworkManager.Instance;
        if (Time.unscaledTime - sentAt < 10 && network != null && network.IsConnected && network.IsAuthenticated) return;
        pending = 0; Validate(); message.text = "저장 응답을 확인하지 못했습니다. 프로필을 다시 열어 확인해 주세요.";
    }
    private void OnDisable() { if (bound) ResetContext(); }
}
