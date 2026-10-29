Shader "Vector/DroneEnergy"
{
    Properties { _Radial("Radial particle (0 = beam)", Float) = 1 }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float _Radial;
            CBUFFER_END
            Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.color=v.color; o.uv=v.uv; return o; }
            half4 frag(Varyings i):SV_Target {
                float d = _Radial > .5 ? length((i.uv-.5)*2) : abs(i.uv.y*2-1);
                float soft = pow(saturate(1-d),2);
                return half4(i.color.rgb*2, i.color.a*soft);
            }
            ENDHLSL
        }
    }
}
