using UnityEngine;
using UnityEngine.UI;

// Resolution-independent, editable UI emblem; no runtime hierarchy creation or font glyphs.
[AddComponentMenu("UI/Honor Emblem")]
public sealed class HonorEmblemGraphic : MaskableGraphic
{
    [SerializeField, Range(1, 6)] private int grade = 3;
    public int Grade { get => grade; set { grade = Mathf.Clamp(value, 1, 6); SetVerticesDirty(); } }
    private static readonly Color32 Outline = new Color32(44, 43, 39, 255);

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Color32 metal = grade >= 4 ? new Color32(249, 203, 78, 255) : new Color32(190, 176, 146, 255);
        if (grade >= 3)
        {
            Polygon(mesh, Outline, .22f,.68f, .78f,.68f, .77f,.12f, .5f,.03f, .23f,.12f);
            Polygon(mesh, metal, .26f,.65f, .74f,.65f, .72f,.17f, .5f,.09f, .28f,.17f);
        }
        if (grade >= 4)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Wing(mesh, Outline, side, .02f, .88f, .2f, .38f);
                Wing(mesh, metal, side, .05f, .83f, .18f, .40f);
                if (grade >= 5)
                {
                    Wing(mesh, Outline, side, .07f, .64f, .18f, .25f);
                    Wing(mesh, metal, side, .10f, .61f, .17f, .28f);
                }
            }
        }
        if (grade >= 4) Star(mesh, .5f, .92f, .095f, metal);
        if (grade >= 5)
        {
            Star(mesh, .30f, .87f, .057f, metal);
            Star(mesh, .70f, .87f, .057f, metal);
        }
        Diamond(mesh, .49f, Outline);
        Diamond(mesh, .455f, new Color32(213, 210, 196, 255));
        Diamond(mesh, .413f, Outline);
        Diamond(mesh, .375f, metal);
        Diamond(mesh, .323f, Outline);
        Diamond(mesh, .285f, grade == 6 ? new Color32(122, 44, 39, 255) :
            grade == 5 ? new Color32(139, 78, 32, 255) : new Color32(70, 65, 53, 255));
    }

    private void Wing(VertexHelper mesh, Color32 tint, int side, float outer, float top, float inner, float bottom)
    {
        float X(float x) => side < 0 ? x : 1 - x;
        Polygon(mesh, tint, X(outer),top, X(.39f),.68f, X(.34f),bottom, X(inner),bottom + .12f);
    }
    private void Diamond(VertexHelper mesh, float radius, Color32 tint) =>
        Polygon(mesh, tint, .5f,.49f + radius * .84f, .5f + radius * .84f,.49f,
            .5f,.49f - radius * .84f, .5f - radius * .84f,.49f);

    private void Star(VertexHelper mesh, float x, float y, float radius, Color32 tint)
    {
        float[] points = new float[16];
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            float r = radius * (i % 2 == 0 ? 1 : .34f);
            points[i * 2] = x + Mathf.Sin(a) * r;
            points[i * 2 + 1] = y + Mathf.Cos(a) * r;
        }
        Polygon(mesh, tint, points);
    }

    private void Polygon(VertexHelper mesh, Color32 tint, params float[] points)
    {
        Rect rect = GetPixelAdjustedRect();
        int first = mesh.currentVertCount;
        Vector2 center = Vector2.zero;
        for (int i = 0; i < points.Length; i += 2) center += new Vector2(points[i], points[i + 1]);
        center /= points.Length / 2;
        Color32 final = (Color)tint * color;
        mesh.AddVert(new Vector3(rect.x + center.x * rect.width, rect.y + center.y * rect.height), final, Vector2.zero);
        int count = points.Length / 2;
        for (int i = 0; i < count; i++)
            mesh.AddVert(new Vector3(rect.x + points[i * 2] * rect.width,
                rect.y + points[i * 2 + 1] * rect.height), final, Vector2.zero);
        for (int i = 0; i < count; i++) mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % count);
    }
}
