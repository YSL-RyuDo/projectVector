using UnityEngine;
using UnityEngine.UI;

// Saved UI geometry. Changing a value redraws this graphic, never the Hierarchy.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HudShapeGraphic : MaskableGraphic
{
    public enum Shape { CutPanel, SegmentedArc, Diamond, ChargeRail, FadePlate, HeadingArrow }
    public Shape shape;
    public float corner = 12, thickness = 3;
    public int segments = 7, filled;
    public Color emptyColor = new Color(.28f,.36f,.43f,.85f);
    public void SetCharge(int value,int total)
    {
        total=Mathf.Clamp(total,1,30);value=Mathf.Clamp(value,0,total);
        if(filled==value && segments==total)return;
        filled=value;segments=total;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=GetPixelAdjustedRect();
        if(shape==Shape.HeadingArrow)
        {vh.AddVert(new Vector2(r.center.x,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);vh.AddTriangle(0,1,2);return;}
        if(shape==Shape.FadePlate)
        {
            var clear=color;clear.a=0;
            vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),clear,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),clear,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);return;
        }
        if(shape==Shape.ChargeRail)
        {
            int n=Mathf.Clamp(segments,1,30);float cell=r.width/n,gap=Mathf.Min(3,cell*.25f);
            for(int i=0;i<n;i++){float x=r.xMin+i*cell;Quad(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),new Vector2(x+cell-gap,r.yMax),new Vector2(x+cell-gap,r.yMin),i<filled?color:emptyColor);}return;
        }
        if(shape==Shape.SegmentedArc)
        {
            float radius=Mathf.Min(r.width,r.height)*.5f,inner=radius-thickness;
            int count=Mathf.Clamp(segments,1,30);
            for(int i=0;i<count;i++)for(int j=0;j<8;j++)
            {
                float a=(215-i*250f/count-j*(250f/count-4)/8)*Mathf.Deg2Rad;
                float b=(215-i*250f/count-(j+1)*(250f/count-4)/8)*Mathf.Deg2Rad;
                Vector2 u=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                Quad(vh,r.center+u*inner,r.center+u*radius,r.center+v*radius,r.center+v*inner,i<filled?color:emptyColor);
            }
            return;
        }
        if(shape==Shape.Diamond){Quad(vh,new Vector2(r.xMin,r.center.y),new Vector2(r.center.x,r.yMax),new Vector2(r.xMax,r.center.y),new Vector2(r.center.x,r.yMin),color);return;}
        float c=Mathf.Min(corner,Mathf.Min(r.width,r.height)*.5f);
        Vector2[] p={new(r.xMin,r.yMin+c),new(r.xMin,r.yMax),new(r.xMax-c,r.yMax),new(r.xMax,r.yMax-c),new(r.xMax,r.yMin),new(r.xMin+c,r.yMin)};
        vh.AddVert(r.center,color,Vector2.zero);
        foreach(var q in p)vh.AddVert(q,color,Vector2.zero);
        for(int i=0;i<p.Length;i++)vh.AddTriangle(0,i+1,(i+1)%p.Length+1);
    }
    static void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
    {int n=v.currentVertCount;v.AddVert(a,tint,Vector2.zero);v.AddVert(b,tint,Vector2.zero);v.AddVert(c,tint,Vector2.zero);v.AddVert(d,tint,Vector2.zero);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);}
}
