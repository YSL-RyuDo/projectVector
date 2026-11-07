Shader "Vector/ControllerSwamp"
{
 Properties {
  _Body("Silver lavender",Color)=(.80,.85,.96,1)
  _Shadow("Flow depth",Color)=(.36,.43,.65,1)
  _Crest("Foam",Color)=(.96,.98,1,1)
  _Age("Age",Float)=1
  _Life("Life",Float)=1
  _Pulse("Contact pulse",Float)=0
  _Mode("Foot feedback",Float)=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Body,_Shadow,_Crest;
   float _Age,_Life,_Pulse,_Mode;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target {
    float2 p=i.uv*2-1;
    float r=length(p), a=atan2(p.y,p.x), t=lerp(_Age,_Time.y,step(.5,_Mode));
    float edge=1-smoothstep(.94,1,r);
    float wave=sin(p.x*8+p.y*5+t*.85+sin(p.y*7-t*.6)*1.1);
    float vein=pow(saturate(.5+.5*sin(p.x*17+p.y*9+wave*2-t*1.1)),7);
    float folds=.5+.5*sin(p.x*5-p.y*8+wave*.8+t*.45);
    float sweep=exp(-pow((r-frac(t*.24+a*.045))/.07,2))*.22;
    half3 color=lerp(_Shadow.rgb,_Body.rgb,smoothstep(.15,.85,folds));
    color=lerp(color,_Crest.rgb,saturate(vein*.47+sweep+_Pulse*.16));
    float boundary=(smoothstep(.88,.93,r)-smoothstep(.95,1,r))*(.6+.4*sin(a*7+t*2));
    color=lerp(color,_Crest.rgb,boundary*.65);
    float arrival=1-smoothstep(max(.02,saturate(_Age/.36))-.12,max(.03,saturate(_Age/.36)),r);
    float alpha=edge*(.36+arrival*.34+boundary*.18)*_Life;
    if(_Mode>.5){
     float ring=exp(-pow((r-(.55+.12*sin(a*3-t*3)))/.09,2));
     color=lerp(_Body.rgb,_Crest.rgb,ring);
     alpha=ring*edge*.7;
    }
    return half4(color,alpha);
   }
   ENDHLSL
  }
 }
}
