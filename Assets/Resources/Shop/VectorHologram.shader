Shader "ProjectVector/ShopHologram"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _GlowColor("Hologram Glow", Color) = (.75, .34, .035, 1)
        _Reveal("Reveal", Range(0,1)) = 0
        _GridScale("Grid Scale", Float) = 35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionOS:TEXCOORD0; float2 uv:TEXCOORD1; float3 normalWS:TEXCOORD2; float3 positionWS:TEXCOORD3; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _GlowColor, _BaseColor;
            float _Reveal, _GridScale;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.positionOS = input.positionOS.xyz;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float3 grid = abs(frac(i.positionOS * _GridScale) - .5);
                float gridLine = 1 - smoothstep(.015, .04, min(grid.x, min(grid.y, grid.z)));
                float rim = pow(1 - saturate(dot(normalize(i.normalWS), normalize(GetWorldSpaceViewDir(i.positionWS)))), 2);
                float noise = frac(sin(dot(floor(i.positionOS * 18), float3(12.9898,78.233,37.719))) * 43758.5453);
                float solid = smoothstep(noise, noise + .08, _Reveal * 1.1);
                float edge = saturate(1 - abs(noise - _Reveal) * 25);
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                baseColor.rgb *= .48 + .30 * saturate(dot(normalize(i.normalWS), normalize(float3(-.4,.7,-.6))));
                float scan = .8 + .2 * sin(i.positionOS.y * 160 - _Time.y * 12);
                float3 glow = _GlowColor.rgb * saturate(gridLine * .55 + rim * .35 + edge * .25) * scan;
                return half4(saturate(lerp(glow, baseColor.rgb, solid) + edge * _GlowColor.rgb * .08 * (1-solid)), lerp(saturate(gridLine + rim) * .65, baseColor.a, solid));
            }
            ENDHLSL
        }
    }
}
