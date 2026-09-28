using UnityEngine;
using UnityEngine.UI;

namespace Game.Pings
{
    public sealed class PingMarker : MonoBehaviour
    {
        private RectTransform root;
        private Text label;
        private string caption;
        private Vector3 worldPosition;
        public float ExpiresAt { get; private set; }

        public void Initialize(Vector3 position, PingOption option, float expiresAt)
        {
            root = (RectTransform)transform; worldPosition = position; ExpiresAt = expiresAt;
            var icon = PingUI.Rect("Icon", transform, Vector2.one * 18).gameObject.AddComponent<PingGraphics>();
            icon.diamond = true; icon.color = option.color; icon.raycastTarget = false;
            caption = option.label;
            label = PingUI.Label("Label", transform, caption, 14);
            label.rectTransform.anchoredPosition = new Vector2(0, -28);
            label.color = option.color;
        }

        public void Refresh(Camera camera, RectTransform canvas, float padding)
        {
            bool available = camera != null && camera.isActiveAndEnabled;
            gameObject.SetActive(available);
            if (!available) return;

            Vector3 projected = camera.WorldToScreenPoint(worldPosition);
            Rect viewport = camera.pixelRect;
            Vector2 center = viewport.center;
            Vector2 screen = new Vector2(projected.x, projected.y);
            float margin = Mathf.Min(padding, Mathf.Min(viewport.width, viewport.height) * 0.45f);
            Vector2 extents = new Vector2(viewport.width * 0.5f - margin, viewport.height * 0.5f - margin);
            Vector2 offset = screen - center;
            bool outside = projected.z <= 0 || Mathf.Abs(offset.x) > extents.x || Mathf.Abs(offset.y) > extents.y;
            if (outside)
            {
                if (projected.z <= 0) offset = -offset;
                if (offset.sqrMagnitude < 0.001f) offset = Vector2.down;
                float scale = Mathf.Min(extents.x / Mathf.Max(Mathf.Abs(offset.x), 0.001f),
                    extents.y / Mathf.Max(Mathf.Abs(offset.y), 0.001f));
                screen = center + offset * scale;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local);
            root.anchoredPosition = local;
            int distance = Mathf.RoundToInt(Vector3.Distance(camera.transform.position, worldPosition));
            label.text = caption + " " + distance + "m";
        }
    }
}
