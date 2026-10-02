Shader "ProjectVector/ResultStage"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct A { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; };
            sampler2D _BaseMap;
            float4 _BaseMap_ST, _BaseColor;
            V vert(A i)
            {
                V o; o.pos=UnityObjectToClipPos(i.vertex); o.normal=UnityObjectToWorldNormal(i.normal);
                o.uv=TRANSFORM_TEX(i.uv,_BaseMap); o.world=mul(unity_ObjectToWorld,i.vertex).xyz; return o;
            }
            fixed4 frag(V i):SV_Target
            {
                fixed4 c=tex2D(_BaseMap,i.uv)*_BaseColor; clip(c.a-.15);
                float3 n=normalize(i.normal), view=normalize(_WorldSpaceCameraPos-i.world);
                float key=saturate(dot(n,normalize(float3(-.4,.8,-.7))));
                float rim=pow(1-saturate(dot(n,view)),3)*.13;
                c.rgb*=.68+key*.38; c.rgb+=float3(.95,.80,.66)*rim;
                float fog=saturate((distance(_WorldSpaceCameraPos,i.world)-5)/22)*.45;
                return fixed4(lerp(c.rgb,float3(.64,.57,.52),fog),1);
            }
            ENDCG
        }
    }
}
