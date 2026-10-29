using UnityEngine;
using UnityEngine.UI;

// A saved UI graphic, not a layout generator. Its RectTransform is authored in the scene.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CareerStageGraphic : MaskableGraphic
{
    public enum Shape { Podium, LightColumn, Pillar }
    public Shape shape;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        if (shape == Shape.Podium)
        {
            Quad(vh, r, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.72f, 1), new Vector2(.28f, 1), color, color);
        }
        else if (shape == Shape.LightColumn)
        {
            Color edge = color; edge.a = 0;
            Quad(vh, r, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(.15f, 1), edge, color, color, edge);
            Quad(vh, r, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(.85f, 1), new Vector2(.5f, 1), color, edge, edge, color);
        }
        else
        {
            Color bottom = color; bottom.a *= .15f;
            Quad(vh, r, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, bottom, color);
        }
    }
    static void Quad(VertexHelper vh, Rect r, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color bottom, Color top) => Quad(vh, r, a, b, c, d, bottom, bottom, top, top);
    static void Quad(VertexHelper vh, Rect r, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
    {
        int start = vh.currentVertCount;
        Vector2[] points = { a, b, c, d }; Color[] colors = { ca, cb, cc, cd };
        for (int i = 0; i < 4; ++i) vh.AddVert(new Vector3(r.xMin + r.width * points[i].x, r.yMin + r.height * points[i].y), colors[i], Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start + 2, start + 3, start);
    }
}
