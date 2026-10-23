Shader "Vector/DreamForestDepth"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float _ForestStrength;
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=i.texcoord;
                half4 col=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 world=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float distance=length(world-_WorldSpaceCameraPos);
                // Clear weapon/hands and close cover; far geometry disappears into silver mist.
                float fog=smoothstep(2.2,7.0,distance)*_ForestStrength;
                float3 dir=normalize(world-_WorldSpaceCameraPos);
                float flow=sin(dir.x*5+dir.y*3+_Time.y*.19)*sin(dir.z*4-dir.y*2-_Time.y*.13);
                half3 mist=lerp(half3(.57,.63,.78),half3(.82,.86,.95),saturate(.56+dir.y*.22+flow*.06));
                return half4(lerp(col.rgb,mist,fog),col.a);
            }
            ENDHLSL
        }
    }
}
