#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Export only: evaluates the live shader functions without changing the game material.
public static class ControllerSmokeTextureExport
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Batch export only");
        int result = 1;
        try
        {
            string source = File.ReadAllText("Assets/ProjectVector/Shared/Characters/Effects/Controller/Shaders/ControllerSmoke.shader");
            int uniforms = source.IndexOf("CBUFFER_START", StringComparison.Ordinal);
            int attributes = source.IndexOf("struct Attributes", StringComparison.Ordinal);
            int functions = source.IndexOf("float hash(", StringComparison.Ordinal);
            int fragment = source.IndexOf("half4 frag(", StringComparison.Ordinal);
            string shaderSource = "Shader \"Hidden/ControllerSmokeTextureExport\" { SubShader { Pass { Cull Off ZWrite Off ZTest Always HLSLPROGRAM\n"
                + "#pragma vertex bakeVert\n#pragma fragment bakeFrag\n"
                + "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n"
                + source.Substring(uniforms, attributes - uniforms)
                + source.Substring(functions, fragment - functions)
                + @"
                struct BakeInput { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
                struct BakeOutput { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
                BakeOutput bakeVert(BakeInput v) { BakeOutput o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o; }
                half4 bakeFrag(BakeOutput i):SV_Target {
                    float longitude=(i.uv.x-.5)*6.28318530718;
                    float latitude=(i.uv.y-.5)*3.14159265359;
                    float3 surface=.5*float3(cos(latitude)*cos(longitude),sin(latitude),cos(latitude)*sin(longitude));
                    return half4(exteriorCloud(surface,normalize(surface)),1);
                }
                ENDHLSL } } }";
            Shader shader = ShaderUtil.CreateShaderAsset(shaderSource, false);
            if (ShaderUtil.ShaderHasError(shader)) throw new Exception("Export shader compilation failed");
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/ProjectVector/Shared/Characters/Effects/Controller/Controller/SmokeVolume.mat");
            var material = new Material(shader);
            material.CopyPropertiesFromMaterial(original);
            // Capture video second 10: the smoke had already settled for one second.
            material.SetFloat("_CloudTime", 11f);
            material.SetFloat("_End", 1f);
            const int width=2048, height=1024;
            var target = new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            bool previousSrgb = GL.sRGBWrite;
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(Texture2D.whiteTexture,target,material);
            GL.sRGBWrite = previousSrgb;
            RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            RenderTexture.active=null;
            string output="C:/Users/GCT/Documents/ControllerSmokeOnly-20261023/Smoke_LongBands_BaseMap_2048.png";
            File.WriteAllBytes(output,pixels.EncodeToPNG());
            var colors=pixels.GetPixels32();int min=255,max=0;
            foreach(var color in colors){min=Math.Min(min,color.r);max=Math.Max(max,color.r);}
            if(max-min<10)throw new Exception("Bake has no visible pattern");
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(shader);
            Debug.Log("SMOKE_TEXTURE_EXPORT_PASS size=2048x1024 cloudTime=11 redRange="+min+".."+max+" path="+output);
            result=0;
        }
        catch(Exception ex){Debug.LogException(ex);}
        finally{EditorApplication.Exit(result);}
    }
}
#endif
