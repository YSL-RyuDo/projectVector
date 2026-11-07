using UnityEngine;
using UnityEngine.UI;

// Original, resolution-independent frame. Its object and dimensions are scene-authored.
[AddComponentMenu("UI/Vector Cut Panel")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class VectorCutPanel : MaskableGraphic
{
    [Min(0)] public float corner = 14;
    [Min(0)] public float borderWidth = 1;
    public Color borderColor = new Color(.38f,.46f,.5f,1);
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var r = GetPixelAdjustedRect();
        float c = Mathf.Min(corner, Mathf.Min(r.width,r.height)*.25f);
        float b = Mathf.Min(borderWidth, Mathf.Min(r.width,r.height)*.25f);
        Vector2[] points = {
            new(r.xMin+c,r.yMax),new(r.xMax-c,r.yMax),new(r.xMax,r.yMax-c),new(r.xMax,r.yMin+c),
            new(r.xMax-c,r.yMin),new(r.xMin+c,r.yMin),new(r.xMin,r.yMin+c),new(r.xMin,r.yMax-c)
        };
        for(int i=0;i<8;i++) vh.AddVert(points[i],borderColor,Vector2.zero);
        for(int i=0;i<8;i++)
        {
            var p=points[i];
            p.x=Mathf.Lerp(r.xMin+b,r.xMax-b,Mathf.InverseLerp(r.xMin,r.xMax,p.x));
            p.y=Mathf.Lerp(r.yMin+b,r.yMax-b,Mathf.InverseLerp(r.yMin,r.yMax,p.y));
            vh.AddVert(p,color,Vector2.zero);
        }
        vh.AddVert(r.center,color,Vector2.zero);
        for(int i=0;i<8;i++)
        {
            int n=(i+1)%8;
            vh.AddTriangle(i,n,i+8); vh.AddTriangle(n,n+8,i+8);
            vh.AddTriangle(16,i+8,n+8);
        }
    }
}
