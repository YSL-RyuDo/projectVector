Shader "ProjectVector/ShopPreview"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor;
            CBUFFER_END
            V vert(A i)
            {
                V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);
                o.uv=TRANSFORM_TEX(i.uv,_BaseMap); return o;
            }
            half4 frag(V i):SV_Target
            {
                half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                clip(albedo.a-.1);
                float diffuse=.48+.30*saturate(dot(normalize(i.normalWS),normalize(float3(-.4,.7,-.6))));
                return half4(albedo.rgb*diffuse,1);
            }
            ENDHLSL
        }
    }
}
