using UnityEngine;

namespace Game.Pings
{
    public sealed class PingWheel : MonoBehaviour
    {
        private PingGraphics[] segments;
        private PingOption[] options;
        private RectTransform pointer;
        private Vector2 cursorPosition;
        private PingGraphics cancelCircle;
        private float deadZone;
        private float radius;
        public int SelectedIndex { get; private set; } = -1;

        public void Initialize(PingOption[] choices, float wheelRadius, float selectionDeadZone)
        {
            options = choices; radius = Mathf.Max(60, wheelRadius);
            deadZone = Mathf.Clamp(selectionDeadZone, 12f, radius * 0.3f);
            segments = new PingGraphics[choices.Length];
            float step = 360f / choices.Length;
            for (int i = 0; i < choices.Length; i++)
            {
                PingGraphics segment = PingUI.Rect("Sector", transform, Vector2.one * radius * 2)
                    .gameObject.AddComponent<PingGraphics>();
                segment.raycastTarget = false;
                segment.startDegrees = i * step - step * 0.5f + 2f;
                segment.endDegrees = i * step + step * 0.5f - 2f;
                segment.innerRadius = radius * 0.32f; segment.outerRadius = radius;
                segments[i] = segment;
                float a = i * step * Mathf.Deg2Rad;
                var text = PingUI.Label("Option", transform, choices[i].label, 15);
                text.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius * 0.68f;
            }
            cancelCircle = PingUI.Rect("CancelCircle", transform, Vector2.one * deadZone * 2)
                .gameObject.AddComponent<PingGraphics>();
            cancelCircle.raycastTarget = false;
            cancelCircle.innerRadius = 0;
            cancelCircle.outerRadius = deadZone;
            var cancelLabel = PingUI.Label("CancelLabel", transform, "X", 20);
            cancelLabel.rectTransform.sizeDelta = Vector2.one * deadZone * 2;
            pointer = PingUI.Rect("Pointer", transform, Vector2.one * 7);
            var graphic = pointer.gameObject.AddComponent<PingGraphics>();
            graphic.diamond = true; graphic.raycastTarget = false;
            gameObject.SetActive(false);
        }

        public void Open()
        {
            cursorPosition = Vector2.zero; SelectedIndex = -1;
            gameObject.SetActive(true); Move(Vector2.zero);
        }

        public void Move(Vector2 delta)
        {
            // 실제 선택 좌표도 화면 포인터와 같이 제한합니다.
            // 큰 마우스 이동 후에도 중앙 취소 원으로 바로 돌아올 수 있습니다.
            cursorPosition = Vector2.ClampMagnitude(cursorPosition + delta, radius * 0.9f);
            pointer.anchoredPosition = cursorPosition;
            SelectedIndex = -1;
            if (cursorPosition.sqrMagnitude > deadZone * deadZone)
            {
                float angle = Mathf.Repeat(Mathf.Atan2(cursorPosition.x, cursorPosition.y) * Mathf.Rad2Deg, 360);
                float step = 360f / options.Length;
                SelectedIndex = Mathf.FloorToInt((angle + step * 0.5f) / step) % options.Length;
            }
            cancelCircle.color = SelectedIndex < 0
                ? new Color(0.9f, 0.22f, 0.24f, 0.95f)
                : new Color(0.15f, 0.17f, 0.21f, 0.9f);
            for (int i = 0; i < segments.Length; i++)
            {
                Color c = i == SelectedIndex ? options[i].color : new Color(0.08f, 0.1f, 0.15f);
                c.a = i == SelectedIndex ? 0.92f : 0.84f;
                segments[i].color = c;
            }
        }

        public void Close() => gameObject.SetActive(false);
    }
}
