Shader "Vector/DischargeSector"
{
    Properties { _Tint("Discharge tint",Color)=(0.1,0.65,1,0.12) _Phase("Wave phase",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint; float _Phase;
            CBUFFER_END
            Varyings Vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float edge=smoothstep(0,.08,i.uv.x)*smoothstep(0,.08,1-i.uv.x);
                float streak=pow(saturate(1-abs(frac(i.uv.y*27+i.uv.x*3-_Phase*3)-.5)*2),10);
                float front=pow(saturate(1-abs(i.uv.x-_Phase)*7),3);
                return half4(_Tint.rgb,_Tint.a*edge*(.35+streak*.6+front*.7));
            }
            ENDHLSL
        }
    }
}
