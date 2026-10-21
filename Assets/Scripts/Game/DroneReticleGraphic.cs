using UnityEngine;
using UnityEngine.UI;

// Authored vector reticle; never constructs UI objects at runtime.
[RequireComponent(typeof(CanvasRenderer))]
public class DroneReticleGraphic : MaskableGraphic
{
    [SerializeField,Range(1,4)] private float thickness=1.5f;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.43f;
        for(int quadrant=0;quadrant<4;quadrant++)for(int segment=0;segment<15;segment++) {
            float a=(quadrant*90+12+segment*4.4f)*Mathf.Deg2Rad;
            float b=a+4.4f*Mathf.Deg2Rad;int start=vh.currentVertCount;
            vh.AddVert(new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),color,Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius),color,Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Cos(b)*(radius-thickness),Mathf.Sin(b)*(radius-thickness)),color,Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Cos(a)*(radius-thickness),Mathf.Sin(a)*(radius-thickness)),color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
