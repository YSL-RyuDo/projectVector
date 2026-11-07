Shader "ProjectVector/ChamberMetal"
{
    Properties { _BaseColor("Metal", Color) = (.67,.73,.83,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;float3 n:NORMAL;};
            struct V {float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 w:TEXCOORD1;};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.w=TransformObjectToWorld(i.p.xyz);return o;}
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.n),v=normalize(GetWorldSpaceViewDir(i.w));
                n*=dot(n,v)<0?-1:1;
                float3 light=normalize(float3(-.4,.75,-.6));
                float diffuse=.38+.62*saturate(dot(n,light));
                float spec=pow(saturate(dot(n,normalize(light+v))),48)*.45;
                return half4(_BaseColor.rgb*diffuse+spec,1);
            }
            ENDHLSL
        }
    }
}
