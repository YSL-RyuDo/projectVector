Shader "Vector/SpikeParticle"
{
    Properties
    {
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Smoke("Smoke shading",Float)=0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination blend",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; float _Smoke;
            CBUFFER_END
            V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color*_BaseColor;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float n=noise(i.uv*7)*.6+noise(i.uv*17)*.3+noise(i.uv*37)*.1;
                float rim=1-length(p);
                float alpha=smoothstep(.02,.38,rim+(n-.5)*.38);
                float shade=lerp(.38,1.1,saturate(.65-p.y*.32+n*.35));
                float heat=pow(saturate(rim),1.6)*(1.3+n);
                return half4(i.color.rgb*lerp(heat,shade,_Smoke),i.color.a*alpha);
            }
            ENDHLSL
        }
    }
}
