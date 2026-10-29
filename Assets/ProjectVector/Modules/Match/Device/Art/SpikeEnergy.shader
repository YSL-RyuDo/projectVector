Shader "Vector/SpikeEnergy"
{
    Properties
    {
        _BaseColor ("Energy", Color) = (.25,.8,1,.6)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 local : TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.local=v.positionOS.xyz;return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float rim=pow(1-saturate(dot(normalize(i.normalWS),normalize(GetWorldSpaceViewDir(i.positionWS)))),3);
                float bands=pow(saturate(sin(i.local.y*80+i.local.x*7)),18);
                float a=saturate(rim*.8+bands*.14)*_BaseColor.a;
                return half4(_BaseColor.rgb*1.5,a);
            }
            ENDHLSL
        }
    }
}
