Shader "Vector/SpikeCorona"
{
    Properties { _BaseColor("Tint",Color)=(.3,.8,1,.6) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            V vert(A a)
            {
                V o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(a.normalOS); return o;
            }
            half4 frag(V i):SV_Target
            {
                float rim=1-saturate(dot(normalize(i.normalWS),GetWorldSpaceNormalizeViewDir(i.positionWS)));
                // A short pressure edge, not a solid blue orb hiding the device.
                return half4(_BaseColor.rgb*1.4,_BaseColor.a*pow(rim,3)*.7);
            }
            ENDHLSL
        }
    }
}
