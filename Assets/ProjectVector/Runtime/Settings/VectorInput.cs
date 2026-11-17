using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// One binding per action, shared by the new and legacy input call sites.
public static class VectorInput
{
    public static readonly string[] Actions = { "Forward", "Back", "Left", "Right", "Jump", "Crouch", "Fire", "Aim", "Reload", "Primary", "Secondary", "Melee", "Spike", "Drop", "Interact", "SkillC", "SkillQ", "SkillE", "Ultimate", "Voice" };
    public static readonly string[] Labels = { "앞으로 이동", "뒤로 이동", "왼쪽 이동", "오른쪽 이동", "점프 / 드론 상승", "앉기 / 드론 하강", "발사 / 스킬 확정", "정조준 / 보조 공격", "재장전", "주 무기", "보조 무기", "근접 무기", "스파이크", "무기 버리기", "상호작용 / 설치 / 해체", "스킬 C", "스킬 Q", "스킬 E", "궁극기", "눌러서 말하기" };
    public static readonly string[] Defaults = { "W", "S", "A", "D", "Space", "LeftCtrl", "Mouse0", "Mouse1", "R", "Digit1", "Digit2", "Digit3", "Digit4", "G", "F", "C", "Q", "E", "X", "Backquote" };
    public static string Binding(string action) { int i = Array.IndexOf(Actions, action); return i < 0 ? "None" : PlayerPrefs.GetString("Vector.Key." + action, Defaults[i]); }
    public static string Label(string action) { string s = Binding(action); return s == "Mouse0" ? "마우스 왼쪽" : s == "Mouse1" ? "마우스 오른쪽" : s == "Mouse2" ? "마우스 가운데" : s.StartsWith("Digit") ? s.Substring(5) : s; }
    public static ButtonControl Control(string action)
    {
        string s = Binding(action);
        if (s == "Mouse0") return Mouse.current?.leftButton;
        if (s == "Mouse1") return Mouse.current?.rightButton;
        if (s == "Mouse2") return Mouse.current?.middleButton;
        if (s == "Mouse3") return Mouse.current?.backButton;
        if (s == "Mouse4") return Mouse.current?.forwardButton;
        return Enum.TryParse(s, out Key key) && key != Key.None && Enum.IsDefined(typeof(Key),key) ? Keyboard.current?[key] : null;
    }
    public static bool Held(string action) => !VectorSettingsPanel.BlocksInput && Control(action)?.isPressed == true;
    public static bool Down(string action) => !VectorSettingsPanel.BlocksInput && Control(action)?.wasPressedThisFrame == true;
    public static bool Up(string action) => VectorSettingsPanel.BlocksInput || Control(action)?.wasReleasedThisFrame == true;
    public static bool Bind(string action, string key, out string error)
    {
        error = "";
        if (Array.IndexOf(Actions, action) < 0 || key == "Escape" || key == "Enter" || key == "NumpadEnter" || key == "Tab" || key == "F10" || key == "F3" || key == "B" || key == "Z") { error = "이 키는 메뉴·채팅·상점·핑에 사용됩니다."; return false; }
        bool mouse = key == "Mouse0" || key == "Mouse1" || key == "Mouse2" || key == "Mouse3" || key == "Mouse4";
        if (!mouse && (!Enum.TryParse(key, out Key parsed) || parsed == Key.None || !Enum.IsDefined(typeof(Key),parsed))) { error = "사용할 수 없는 키입니다."; return false; }
        for (int i = 0; i < Actions.Length; i++) if (Actions[i] != action && Binding(Actions[i]) == key) { error = Labels[i] + "에 이미 사용 중인 키입니다."; return false; }
        PlayerPrefs.SetString("Vector.Key." + action, key); return true;
    }
    public static void Reset() { foreach (string a in Actions) PlayerPrefs.DeleteKey("Vector.Key." + a); }
}
