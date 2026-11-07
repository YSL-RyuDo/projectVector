using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Pings
{
    internal static class PingInputCompat
    {
        // 두 입력 백엔드가 모두 켜져 있으면 새 Input System만 사용하여 중복 입력 방지.
        public static bool Held(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            // 일반 키 이름은 두 API가 동일하며 숫자 키는 별도 매핑합니다.
            string name = key.ToString();
            if (name.StartsWith("Alpha")) name = "Digit" + name.Substring(5);
            if (key == KeyCode.Return) name = "Enter";
            if (key == KeyCode.LeftControl) name = "LeftCtrl";
            if (key == KeyCode.RightControl) name = "RightCtrl";
            if (!System.Enum.TryParse(name, out Key mapped) || mapped == Key.None) return false;
            return Keyboard.current[mapped].isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        public static Vector2 MouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            // 기존 Input Manager에 기본 Mouse X / Mouse Y 축이 있어야 합니다.
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 20f;
#else
            return Vector2.zero;
#endif
        }
    }
}
