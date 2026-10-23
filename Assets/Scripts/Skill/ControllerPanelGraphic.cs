using UnityEngine;
using UnityEngine.UI;

// Saved, editable vector frame using the game's clipped-corner HUD language.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ControllerPanelGraphic : MaskableGraphic
{
    [SerializeField] private Color _line = new Color(.3f,.62f,.79f,.65f);
    [SerializeField] private float _cut = 14, _thickness = 1.5f;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;float x=r.width*.5f,y=r.height*.5f,c=Mathf.Min(_cut,Mathf.Min(x,y));
        Vector2[] p={new(-x,-y+c),new(-x+c,-y),new(x,-y),new(x,y-c),new(x-c,y),new(-x,y)};
        vh.AddVert(Vector3.zero,color,Vector2.zero);
        foreach(var v in p)vh.AddVert(v,color,Vector2.zero);
        for(int i=0;i<6;i++)vh.AddTriangle(0,1+i,1+(i+1)%6);
        for(int i=0;i<6;i++){
            int k=vh.currentVertCount;Vector2 a=p[i],b=p[(i+1)%6];Vector2 inset=new Vector2(Mathf.Max(0,1-_thickness/x),Mathf.Max(0,1-_thickness/y));
            vh.AddVert(a,_line,Vector2.zero);vh.AddVert(b,_line,Vector2.zero);vh.AddVert(b*inset,_line,Vector2.zero);vh.AddVert(a*inset,_line,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
    }
}
