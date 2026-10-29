Shader "Vector/SkillReadableEnergy"
{
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass {
            // Core information must survive bright sand and disabled bloom.
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float depth:TEXCOORD1; };
            Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.depth=-TransformWorldToView(TransformObjectToWorld(v.positionOS.xyz)).z;o.color=v.color;o.uv=v.uv;return o; }
            half4 frag(Varyings i):SV_Target {
                float d=abs(i.uv.y*2-1);
                float edge=smoothstep(.42,.72,d);
                float coverage=1-smoothstep(.88,1,d);
                float nearFade=smoothstep(.2,.65,i.depth);
                return half4(lerp(i.color.rgb,i.color.rgb*.22,edge),i.color.a*coverage*nearFade);
            }
            ENDHLSL
        }
    }
}
