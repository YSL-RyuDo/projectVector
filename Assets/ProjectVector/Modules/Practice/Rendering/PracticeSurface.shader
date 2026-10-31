Shader "Vector/PracticeSurface"
{
    Properties
    {
        _BaseColor("Surface", Color)=(.5,.5,.5,1)
        _DetailStrength("Weathering",Range(0,1))=.18
        _Scale("Grain scale",Float)=12
        _BaseMap("Base",2D)="white"{}
        _BumpMap("Normal",2D)="bump"{}
        _BumpScale("Normal strength",Float)=1
        _Cutoff("Cutoff",Float)=.5
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;float _DetailStrength;float _Scale;
            CBUFFER_END
            struct A { float4 positionOS:POSITION;float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float fog:TEXCOORD2; };
            V vert(A v) { V o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o; }
            float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.normal);
                float grain=.5+.25*sin(i.world.x*11.7+i.world.y*8.2)*sin(i.world.z*9.1)+.15*sin(i.world.x*3.7+i.world.z*5.3);
                float patches=sin(i.world.x*1.6+sin(i.world.z*.7))*sin(i.world.z*1.3+i.world.y*.8);
                float wear=1+_DetailStrength*((grain-.5)*.55+patches*.25);
                float footDirt=1-.12*saturate(1-i.world.y*.9)*(1-abs(n.y));
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                float diffuse=saturate(dot(n,sun.direction));
                half3 ambient=max(SampleSH(n),half3(.18,.20,.21));
                half3 lighting=ambient*.9+sun.color*diffuse*sun.shadowAttenuation;
                #ifdef _ADDITIONAL_LIGHTS
                uint count=GetAdditionalLightsCount();
                for(uint l=0;l<count;l++){Light fill=GetAdditionalLight(l,i.world);lighting+=fill.color*saturate(dot(n,fill.direction))*fill.distanceAttenuation;}
                #endif
                half3 col=_BaseColor.rgb*wear*footDirt*lighting;
                return half4(MixFog(col,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
