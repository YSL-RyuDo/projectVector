Shader "Vector/ControllerDecoyPulse"
{
    Properties { _Progress("Progress", Range(0,1))=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float _Progress;
            CBUFFER_END
            V vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.uv=a.uv; return o; }
            half4 frag(V i):SV_Target
            {
                float2 p=(i.uv-.5)*2;
                float r=length(p), radius=lerp(.18,.94,_Progress);
                float ring=1-smoothstep(.012,.045,abs(r-radius));
                float inner=(1-smoothstep(.015,.035,abs(r-radius*.75)))*.25;
                float segments=smoothstep(-.25,.4,sin(atan2(p.y,p.x)*3));
                return half4(.75,.84,1,(ring+inner)*segments*(1-_Progress)*.52);
            }
            ENDHLSL
        }
    }
}
