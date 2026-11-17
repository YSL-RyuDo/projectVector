Shader "ProjectVector/SettingsBackdrop"
{
    Properties { _MainTex ("Background", 2D) = "white" {} }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            sampler2D _MainTex;float4 _MainTex_TexelSize;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target {
                fixed4 c=0;float2 d=_MainTex_TexelSize.xy*5;
                for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)c+=tex2D(_MainTex,i.uv+float2(x,y)*d);
                return fixed4(c.rgb/25*.45,1)*i.color;
            }
            ENDCG
        }
    }
}
