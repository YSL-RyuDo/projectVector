using UnityEngine;
using UnityEngine.UI;

// Simple vector pictograms stay sharp at every Canvas scale and need no font fallback.
public sealed class HonorSymbolGraphic : MaskableGraphic
{
    public enum Symbol { Room, Trophy, Microphone, Rank, Shirt, Medal, Case }
    [SerializeField] private Symbol symbol;
    public Symbol Kind { get => symbol; set { symbol = value; SetVerticesDirty(); } }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        switch (symbol)
        {
            case Symbol.Microphone:
                Oval(mesh,.5f,.62f,.15f,.25f);
                Line(mesh,.26f,.65f,.26f,.36f,.055f);
                Line(mesh,.74f,.65f,.74f,.36f,.055f);
                Line(mesh,.26f,.36f,.5f,.23f,.055f);
                Line(mesh,.74f,.36f,.5f,.23f,.055f);
                Line(mesh,.5f,.23f,.5f,.10f,.055f);
                Line(mesh,.32f,.10f,.68f,.10f,.055f);
                break;
            case Symbol.Shirt:
                Poly(mesh,.16f,.88f,.36f,.94f,.43f,.85f,.57f,.85f,.64f,.94f,.84f,.88f,
                    .98f,.62f,.77f,.53f,.71f,.65f,.71f,.13f,.29f,.13f,.29f,.65f,.23f,.53f,.02f,.62f);
                break;
            case Symbol.Case:
                Frame(mesh,.22f,.04f,.78f,.95f,.05f);
                Line(mesh,.30f,.84f,.70f,.84f,.025f);
                Line(mesh,.30f,.14f,.70f,.14f,.025f);
                break;
            case Symbol.Trophy:
                Poly(mesh,.28f,.91f,.72f,.91f,.66f,.55f,.5f,.37f,.34f,.55f);
                Line(mesh,.5f,.37f,.5f,.12f,.08f);
                Line(mesh,.30f,.12f,.70f,.12f,.09f);
                Line(mesh,.28f,.81f,.12f,.81f,.05f);
                Line(mesh,.12f,.81f,.19f,.50f,.05f);
                Line(mesh,.19f,.50f,.40f,.41f,.05f);
                Line(mesh,.72f,.81f,.88f,.81f,.05f);
                Line(mesh,.88f,.81f,.81f,.50f,.05f);
                Line(mesh,.81f,.50f,.60f,.41f,.05f);
                break;
            case Symbol.Medal:
                Poly(mesh,.28f,.48f,.49f,.41f,.32f,.03f,.22f,.19f,.09f,.12f);
                Poly(mesh,.72f,.48f,.51f,.41f,.68f,.03f,.78f,.19f,.91f,.12f);
                Ring(mesh,.5f,.64f,.30f,.045f);
                Ring(mesh,.5f,.64f,.22f,.025f);
                Poly(mesh,.5f,.82f,.56f,.68f,.71f,.64f,.56f,.59f,.5f,.45f,.44f,.59f,.29f,.64f,.44f,.68f);
                break;
            case Symbol.Rank:
                for (int i = 0; i < 8; ++i)
                {
                    float angle = i * Mathf.PI / 4;
                    Vector2 a = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Line(mesh,.5f + a.x * .28f,.5f + a.y * .28f,.5f + a.x * .43f,.5f + a.y * .43f,.085f);
                }
                Ring(mesh,.5f,.5f,.24f,.035f);
                Oval(mesh,.5f,.5f,.15f,.15f);
                break;
            default:
                for (int i = 0; i < 3; ++i)
                {
                    float x = .26f + i * .24f, y = i == 1 ? .60f : .36f;
                    Line(mesh,x,y+.2f,x+.18f,y,.05f); Line(mesh,x+.18f,y,x,y-.2f,.05f);
                    Line(mesh,x,y-.2f,x-.18f,y,.05f); Line(mesh,x-.18f,y,x,y+.2f,.05f);
                }
                break;
        }
    }
    private void Frame(VertexHelper m, float x, float y, float xx, float yy, float w)
    { Line(m,x,y,xx,y,w); Line(m,xx,y,xx,yy,w); Line(m,xx,yy,x,yy,w); Line(m,x,yy,x,y,w); }
    private void Ring(VertexHelper m, float x, float y, float r, float w)
    {
        for (int i=0;i<32;i++)
        { float a=i*Mathf.PI/16, b=(i+1)*Mathf.PI/16; Line(m,x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r,w); }
    }
    private void Oval(VertexHelper m,float x,float y,float rx,float ry)
    {
        float[] p=new float[64];
        for(int i=0;i<32;i++) { p[2*i]=x+Mathf.Cos(i*Mathf.PI/16)*rx; p[2*i+1]=y+Mathf.Sin(i*Mathf.PI/16)*ry; }
        Poly(m,p);
    }
    private void Line(VertexHelper m,float x,float y,float xx,float yy,float w)
    {
        Vector2 n=new Vector2(y-yy,xx-x).normalized*w*.5f;
        Poly(m,x+n.x,y+n.y,xx+n.x,yy+n.y,xx-n.x,yy-n.y,x-n.x,y-n.y);
    }
    private void Poly(VertexHelper m, params float[] p)
    {
        Rect r=GetPixelAdjustedRect(); int start=m.currentVertCount;
        for(int i=0;i<p.Length;i+=2) m.AddVert(new Vector3(r.x+p[i]*r.width,r.y+p[i+1]*r.height),color,Vector2.zero);
        for(int i=1;i<p.Length/2-1;i++) m.AddTriangle(start,start+i,start+i+1);
    }
}
