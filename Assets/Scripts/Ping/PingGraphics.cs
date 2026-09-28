using UnityEngine;
using UnityEngine.UI;

namespace Game.Pings
{
    // 스프라이트 없이 그리는 도넛 조각과 마커 아이콘.
    public sealed class PingGraphics : MaskableGraphic
    {
        public bool diamond;
        public float startDegrees;
        public float endDegrees = 360f;
        public float innerRadius = 42f;
        public float outerRadius = 120f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (diamond)
            {
                Vector2 half = rectTransform.rect.size * 0.5f;
                Add(vh, new Vector2(0, half.y)); Add(vh, new Vector2(half.x, 0));
                Add(vh, new Vector2(0, -half.y)); Add(vh, new Vector2(-half.x, 0));
                vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
                return;
            }
            int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(endDegrees - startDegrees) / 4f));
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(startDegrees, endDegrees, i / (float)steps) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                Add(vh, dir * innerRadius); Add(vh, dir * outerRadius);
                if (i == 0) continue;
                int k = i * 2;
                vh.AddTriangle(k - 2, k - 1, k); vh.AddTriangle(k, k - 1, k + 1);
            }
        }

        private void Add(VertexHelper vh, Vector2 point) =>
            vh.AddVert(new Vector3(point.x, point.y, 0), color, Vector2.zero);
    }

    internal static class PingUI
    {
        private static Font font;
        public static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        public static Text Label(string name, Transform parent, string value, int size)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text text = Rect(name, parent, new Vector2(140, 36)).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.text = value;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.color = Color.white;
            return text;
        }
    }
}
