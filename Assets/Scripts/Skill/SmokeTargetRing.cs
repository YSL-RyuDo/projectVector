using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SmokeTargetRing : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var rect = rectTransform.rect;
        for (int i = 0; i < 64; i++) {
            float a = i * Mathf.PI / 32, b = (i + 1) * Mathf.PI / 32;
            Vector2 outer = rect.size * .5f, inner = Vector2.Max(Vector2.zero, outer - Vector2.one * 2);
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(Mathf.Cos(a)*outer.x,Mathf.Sin(a)*outer.y),color,Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Cos(b)*outer.x,Mathf.Sin(b)*outer.y),color,Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Cos(b)*inner.x,Mathf.Sin(b)*inner.y),color,Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Cos(a)*inner.x,Mathf.Sin(a)*inner.y),color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
