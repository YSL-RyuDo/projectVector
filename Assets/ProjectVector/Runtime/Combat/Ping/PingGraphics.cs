using UnityEngine;
using UnityEngine.UI;

namespace Game.Pings
{
    // Authored vector artwork. 0 = arc, 1 = location, 2 = danger, 3 = attack, 4 = shield.
    public sealed class PingGraphics:MaskableGraphic
    {
        public bool diamond;
        public int glyph;
        public float startDegrees,endDegrees=360,innerRadius=42,outerRadius=120;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(glyph>0)
            {
                float s=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
                Vector2 P(float x,float y)=>new Vector2(x*s,y*s);
                void L(float x,float y,float a,float b)=>Line(vh,P(x,y),P(a,b),Mathf.Max(1.7f,s*.13f));
                if(glyph==1)
                {
                    L(0,1,.75f,.25f);L(.75f,.25f,0,-1);L(0,-1,-.75f,.25f);L(-.75f,.25f,0,1);
                    L(-.22f,.22f,.22f,.22f);
                }
                else if(glyph==2)
                {
                    L(0,1,.9f,-.7f);L(.9f,-.7f,-.9f,-.7f);L(-.9f,-.7f,0,1);
                    L(0,.45f,0,-.1f);L(0,-.35f,0,-.43f);
                }
                else if(glyph==3)
                {
                    L(-.8f,.2f,0,.95f);L(0,.95f,.8f,.2f);
                    L(-.8f,-.65f,0,.1f);L(0,.1f,.8f,-.65f);
                }
                else
                {
                    L(-.8f,.8f,.8f,.8f);L(.8f,.8f,.65f,-.25f);L(.65f,-.25f,0,-.95f);
                    L(0,-.95f,-.65f,-.25f);L(-.65f,-.25f,-.8f,.8f);
                    L(0,.4f,0,-.3f);
                }
                return;
            }
            if(diamond)
            {
                var h=rectTransform.rect.size*.5f;
                Add(vh,new Vector2(0,h.y));Add(vh,new Vector2(h.x,0));Add(vh,new Vector2(0,-h.y));Add(vh,new Vector2(-h.x,0));
                vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);return;
            }
            int steps=Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(endDegrees-startDegrees)/4));
            for(int i=0;i<=steps;i++)
            {
                float a=Mathf.Lerp(startDegrees,endDegrees,i/(float)steps)*Mathf.Deg2Rad;
                var d=new Vector2(Mathf.Sin(a),Mathf.Cos(a));Add(vh,d*innerRadius);Add(vh,d*outerRadius);
                if(i==0)continue;int k=i*2;vh.AddTriangle(k-2,k-1,k);vh.AddTriangle(k,k-1,k+1);
            }
        }
        void Line(VertexHelper vh,Vector2 a,Vector2 b,float width)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=vh.currentVertCount;
            Add(vh,a+n);Add(vh,a-n);Add(vh,b-n);Add(vh,b+n);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        void Add(VertexHelper vh,Vector2 p)=>vh.AddVert(new Vector3(p.x,p.y,0),color,Vector2.zero);
    }
}
