using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class VectorCrosshairGraphic : MaskableGraphic
{
    public bool preview;
    protected override void OnEnable() { base.OnEnable(); raycastTarget=false; VectorSettingsData.Changed+=Refresh; Refresh(); }
    protected override void OnDisable() { VectorSettingsData.Changed-=Refresh; base.OnDisable(); }
    void Refresh() => SetVerticesDirty();
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); if(VectorSettingsData.Get("CrossEnabled")==0)return;
        float G(string k)=>VectorSettingsData.Get("Cross"+k);
        var ink=new Color(G("R")/255f,G("G")/255f,G("B")/255f,G("Alpha")/100f);
        float border=G("Outline")==1?G("OutlineSize"):0;
        var center=rectTransform.rect.center;
        void Quad(float x,float y,float w,float h,Color c) {
            int i=vh.currentVertCount; var v=UIVertex.simpleVert; v.color=c;
            v.position=new Vector2(x,y)+center;vh.AddVert(v); v.position=new Vector2(x,y+h)+center;vh.AddVert(v);
            v.position=new Vector2(x+w,y+h)+center;vh.AddVert(v);v.position=new Vector2(x+w,y)+center;vh.AddVert(v);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i+2,i+3,i);
        }
        void Bar(float x,float y,float w,float h) { if(w<=0||h<=0)return; if(border>0)Quad(x-border,y-border,w+2*border,h+2*border,new Color(0,0,0,ink.a)); Quad(x,y,w,h,ink); }
        void Lines(float gap,float len,float thick) {
            Bar(gap,-thick/2,len,thick);Bar(-gap-len,-thick/2,len,thick);
            Bar(-thick/2,-gap-len,thick,len);if(G("Top")==1)Bar(-thick/2,gap,thick,len);
        }
        Lines(G("Gap"),G("Length"),G("Thickness"));
        if(G("Outer")==1)Lines(G("OuterGap"),G("OuterLength"),G("Thickness"));
        if(G("Dot")==1){float s=G("DotSize");Bar(-s/2,-s/2,s,s);}
    }
}

public static class VectorCrosshairCode
{
    static uint Hash(byte[] bytes,int length) { uint h=2166136261; for(int i=0;i<length;i++)h=unchecked((h^bytes[i])*16777619);return h; }
    static VectorSetting[] Fields => Array.FindAll(VectorSettingsData.All,d=>d.tab==3);
    public static string Export()
    {
        using var stream=new MemoryStream(); using var writer=new BinaryWriter(stream);
        writer.Write((byte)1);foreach(var d in Fields)writer.Write((byte)VectorSettingsData.Get(d.key));
        byte[] payload=stream.ToArray();writer.Write(Hash(payload,payload.Length));
        return "PV1-"+Convert.ToBase64String(stream.ToArray()).TrimEnd('=').Replace('+','-').Replace('/','_');
    }
    public static bool Import(string code)
    {
        if(string.IsNullOrWhiteSpace(code)||code.Length>160)return false;
        code=code.Trim();if(!code.StartsWith("PV1-",StringComparison.Ordinal))return false;
        try {
            string s=code.Substring(4).Replace('-','+').Replace('_','/');s=s.PadRight((s.Length+3)/4*4,'=');
            byte[] bytes=Convert.FromBase64String(s);var fields=Fields;
            if(bytes.Length!=fields.Length+5||bytes[0]!=1||BitConverter.ToUInt32(bytes,bytes.Length-4)!=Hash(bytes,bytes.Length-4))return false;
            for(int i=0;i<fields.Length;i++)if(bytes[i+1]<fields[i].min||bytes[i+1]>fields[i].max)return false;
            for(int i=0;i<fields.Length;i++)VectorSettingsData.Set(fields[i].key,bytes[i+1]);
            return true;
        } catch(FormatException){return false;}
    }
}
