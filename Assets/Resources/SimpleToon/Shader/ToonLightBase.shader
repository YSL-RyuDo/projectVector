Shader "Lpk/LightModel/ToonLightBase"
{
    Properties
    {
        _BaseMap            ("Texture", 2D)                       = "white" {}
        _BaseColor          ("Color", Color)                      = (1,1,1,1)
        
        [Space]
        _ShadowColor        ("Shadow Color (Indoor Brightness)", Color) = (0.5, 0.5, 0.5, 1) 
        _ShadowStep         ("ShadowStep", Range(0, 1))           = 0.5
        _ShadowStepSmooth   ("ShadowStepSmooth", Range(0, 1))     = 0.04
        
        [Space] 
        _SpecularStep       ("SpecularStep", Range(0, 1))         = 0.6
        _SpecularStepSmooth ("SpecularStepSmooth", Range(0, 1))   = 0.05
        [HDR]_SpecularColor ("SpecularColor", Color)              = (1,1,1,1)
        
        [Space]
        _RimStep            ("RimStep", Range(0, 1))              = 0.65
        _RimStepSmooth      ("RimStepSmooth",Range(0,1))          = 0.4
        _RimColor           ("RimColor", Color)                   = (1,1,1,1)
        
        [Space]   
        _OutlineWidth      ("OutlineWidth", Range(0.0, 1.0))      = 0.15
        _OutlineColor      ("OutlineColor", Color)                = (0.0, 0.0, 0.0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
             
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _ShadowStep;
                float _ShadowStepSmooth;
                float _SpecularStep;
                float _SpecularStepSmooth;
                float4 _SpecularColor;
                float _RimStepSmooth;
                float _RimStep;
                float4 _RimColor;
                float _OutlineWidth;
                float4 _OutlineColor;
            CBUFFER_END

            struct Attributes
            {     
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            }; 

            struct Varyings
            {
                float2 uv            : TEXCOORD0;
                float3 normalWS      : TEXCOORD1;    
                float3 viewDirWS     : TEXCOORD4;
                float4 shadowCoord   : TEXCOORD5;   
                float4 fogCoord      : TEXCOORD6;   
                float3 positionWS    : TEXCOORD7;   
                float4 positionCS    : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float3 L = normalize(_MainLightPosition.xyz);
                float3 H = normalize(V+L);
                
                float NL = dot(N,L) * 0.5 + 0.5;
                float NV = dot(N,V);
                float NH = max(0, dot(N,H));

                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                float shadow = MainLightRealtimeShadow(input.shadowCoord);
                float shadowNL = smoothstep(_ShadowStep - _ShadowStepSmooth, _ShadowStep + _ShadowStepSmooth, NL);
                float combinedShadow = shadow * shadowNL;

                // 디퓨즈 (그림자 색상 보간)
                float3 diffuseColor = lerp(_ShadowColor.rgb * baseMap.rgb, baseMap.rgb, combinedShadow);
                float3 diffuse = _MainLightColor.rgb * diffuseColor * _BaseColor.rgb;
                
                // 스펙큘러
                float specularStep = smoothstep((1-_SpecularStep*0.05)-_SpecularStepSmooth*0.05, (1-_SpecularStep*0.05)+_SpecularStepSmooth*0.05, NH);
                float3 specular = _SpecularColor.rgb * combinedShadow * specularStep;
                
                // 림라이트 & 앰비언트
                float rim = smoothstep((1-_RimStep) - _RimStepSmooth * 0.5, (1-_RimStep) + _RimStepSmooth * 0.5, 0.5 - NV);
                float3 ambient = rim * _RimColor.rgb + SampleSH(N) * _BaseColor.rgb * baseMap.rgb;
            
                float3 finalColor = diffuse + ambient + specular;
                finalColor = MixFog(finalColor, input.fogCoord.x);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Cull Front // 뒷면만 그림
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 pos      : SV_POSITION;
                float fogCoord : TEXCOORD0;    
            };
            
            // 아웃라인을 위해 다시 선언
            float _OutlineWidth;
            float4 _OutlineColor;
            
            Varyings vert(Attributes v)
            {
                Varyings o;
                // 오브젝트 공간에서 노말 방향으로 정점 확장
                float3 posOS = v.positionOS.xyz + (v.normalOS * _OutlineWidth * 0.01);
                o.pos = TransformObjectToHClip(posOS);
                o.fogCoord = ComputeFogFactor(o.pos.z);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                return float4(MixFog(_OutlineColor.rgb, i.fogCoord), 1.0);
            }
            ENDHLSL
        }

        // 그림자 생성용 Pass
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}