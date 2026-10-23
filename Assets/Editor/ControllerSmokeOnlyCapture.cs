#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

// Unsaved presentation fixture, not a gameplay lifetime or camera modification.
public static class ControllerSmokeOnlyCapture
{
    const string Output="C:/Users/GCT/Documents/ControllerSmokeOnly-20261023";
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch capture only");
        Process encoder=null; RenderTexture rt=null; Texture2D pixels=null;
        int result=1;
        try
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Color background=new Color(.16f,.20f,.25f);
            var smoke=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSmokeAuthoring.Dir+"/ControllerSmoke.prefab"));
            smoke.transform.position=Vector3.zero;smoke.transform.localScale=Vector3.one*4.5f;
            var fx=smoke.GetComponent<ControllerSmokeVfx>();
            // Preview alone is held for 20 seconds; production duration is untouched.
            fx.enabled=false;fx.Begin(22);fx.Advance(1);
            var camera=new GameObject("FixedSmokeOnlyCamera").AddComponent<Camera>();
            camera.enabled=false;camera.fieldOfView=40;camera.nearClipPlane=.03f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=background;
            // No floor: show the entire sphere, including its lower hemisphere.
            camera.transform.position=new Vector3(0,0,-17.5f);camera.transform.LookAt(Vector3.zero);
            camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            rt=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            camera.targetTexture=rt;
            var start=new ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p -movflags +faststart \"{Output}/smoke-only-20s.mp4\"");
            start.UseShellExecute=false;start.CreateNoWindow=true;start.RedirectStandardInput=true;start.RedirectStandardError=true;
            encoder=new Process{StartInfo=start};encoder.ErrorDataReceived+=(s,e)=>{if(!string.IsNullOrEmpty(e.Data))Debug.LogWarning(e.Data);};
            encoder.Start();encoder.BeginErrorReadLine();
            byte[] first=null;double difference=0;
            for(int frame=0;frame<600;frame++)
            {
                if(frame>0)fx.Advance(1f/30);
                camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply(false);RenderTexture.active=null;
                byte[] bytes=pixels.GetRawTextureData<byte>().ToArray();
                encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
                if(frame==0)first=bytes;
                if(frame==300){for(int i=0;i<bytes.Length;i+=12)difference+=Math.Abs(bytes[i]-first[i]);difference/=bytes.Length/12;}
                if(frame%150==0||frame==599)File.WriteAllBytes(Output+"/smoke-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());
            }
            encoder.StandardInput.Close();
            if(!encoder.WaitForExit(30000)||encoder.ExitCode!=0)throw new Exception("Encoder failed");
            if(difference<.05)throw new Exception("Smoke animation did not visibly advance");
            Debug.Log("SMOKE_ONLY_PASS frames=600 fps=30 seconds=20 fixedCamera=true meanPixelChange="+difference);
            result=0;
        }
        catch(Exception ex){Debug.LogException(ex);}
        finally
        {
            if(encoder!=null){if(!encoder.HasExited)encoder.StandardInput.Close();encoder.Dispose();}
            RenderTexture.active=null;if(rt)Object.DestroyImmediate(rt);if(pixels)Object.DestroyImmediate(pixels);
            EditorApplication.Exit(result);
        }
    }
}
#endif
