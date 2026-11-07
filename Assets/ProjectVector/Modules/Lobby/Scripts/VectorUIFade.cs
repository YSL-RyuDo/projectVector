using UnityEngine;
using UnityEngine.UI;

// Vertex tint, editable in the Hierarchy. Also fades a card reflection towards the floor.
[RequireComponent(typeof(Graphic))]
public sealed class VectorUIFade : BaseMeshEffect
{
    public RectTransform bounds;
    public Color top = Color.white;
    public Color bottom = Color.white;

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive()) return;
        var area = bounds != null ? bounds : (RectTransform)transform;
        var vertex = new UIVertex();
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            float y = area.InverseTransformPoint(transform.TransformPoint(vertex.position)).y;
            float t = Mathf.InverseLerp(area.rect.yMin, area.rect.yMax, y);
            vertex.color = (Color)vertex.color * Color.Lerp(bottom, top, t);
            vertices.SetUIVertex(vertex, i);
        }
    }
}
