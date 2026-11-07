using UnityEngine;
using UnityEngine.UI;

// Saved in the HUD hierarchy. A soft lower edge keeps controls legible on bright floors.
[RequireComponent(typeof(CanvasRenderer))]
public class DroneHudFadeGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r=rectTransform.rect;
        Color clear=new Color(color.r,color.g,color.b,0);
        vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);
        vh.AddVert(new Vector3(r.xMin,r.yMax),clear,Vector2.zero);
        vh.AddVert(new Vector3(r.xMax,r.yMax),clear,Vector2.zero);
        vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.zero);
        vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
}
