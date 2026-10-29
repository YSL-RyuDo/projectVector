#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Opt-in recording fixture, editor-only. It runs the actual scene/weapon/physics paths.
// No scene, prefab, UI layout or account data is saved by this recorder.
[InitializeOnLoad]
public static class PracticeRangeVideoRecorder
{
    const string Active="Vector.Practice.Video";
    static double deadline, startAfter;
    static bool started;
    static PracticeRangeVideoRecorder()
    {
        EditorApplication.playModeStateChanged+=State;
        if(SessionState.GetBool(Active,false)){deadline=EditorApplication.timeSinceStartup+600;EditorApplication.update+=Tick;}
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save/close existing work before recording");
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity");
        UnityEngine.Object.FindFirstObjectByType<PracticeRangeController>().persistRecords=false;
        SessionState.SetBool(Active,true);SessionState.SetBool(Active+"Failed",false);
        deadline=EditorApplication.timeSinceStartup+600;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        EditorApplication.EnterPlaymode();
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Active,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){startAfter=EditorApplication.timeSinceStartup+2;started=false;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Active,false);EditorApplication.Exit(SessionState.GetBool(Active+"Failed",false)?1:0);}
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false))return;
        if(EditorApplication.timeSinceStartup>deadline){Fail(new Exception("Recording timed out"));return;}
        if(!EditorApplication.isPlaying || started || EditorApplication.timeSinceStartup<startAfter)return;
        started=true;
        try { new GameObject("EditorOnlyRecordingDriver").AddComponent<PracticeRangeRecordingDriver>().Begin(); }
        catch(Exception e){Fail(e);}
    }
    public static void Fail(Exception e)
    {
        Debug.LogException(e);SessionState.SetBool(Active+"Failed",true);
        if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();else EditorApplication.Exit(1);
    }
}

[DefaultExecutionOrder(20000)]
public sealed class PracticeRangeRecordingDriver : MonoBehaviour
{
    const int Fps=30, Width=1280, Height=720, TotalFrames=53*Fps;
    const string Encoder="C:/Users/USER/AppData/Local/Temp/vector-video-tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    PracticeRangeController range;
    PracticeWeaponInput input;
    Camera camera;
    FPSCameraLook look;
    RenderTexture texture;
    Texture2D pixels;
    System.Diagnostics.Process encoder;
    FileStream audioFile;
    string output;
    int frame, channels, sampleRate, shots, errors;
    float nextShot, audioPeak;
    Quaternion aim;
    bool recording, audioRecording;
    public void Begin()
    {
        output=Path.Combine(Path.GetTempPath(),"VectorPracticeRecording-20261010");Directory.CreateDirectory(output);
        range=FindFirstObjectByType<PracticeRangeController>();range.persistRecords=false;
        input=range.player.GetComponent<PracticeWeaponInput>();camera=input.aimCamera;look=range.player.GetComponentInChildren<FPSCameraLook>();
        range.spawner.targetCharacterIndex=0;range.spawner.headOnly=false;range.spawner.irregularMovement=false;
        range.infiniteAmmo=false;range.EquipRifle();range.SetMode(1);range.SetDistance(0);range.FreePractice();range.spawner.Advance(1);range.OpenMenu();
        aim=camera.transform.rotation;
        var canvas=range.menu.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;
        texture=new RenderTexture(Width,Height,24);pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);
        var start=new System.Diagnostics.ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size {Width}x{Height} -framerate {Fps} -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 19 -pix_fmt yuv420p -movflags +faststart \"{Path.Combine(output,"video.mp4")}\"")
        {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
        encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(Path.Combine(output,"encoder.log"),e.Data+Environment.NewLine);};encoder.BeginErrorReadLine();
        Time.captureFramerate=Fps;
        var config=AudioSettings.GetConfiguration();sampleRate=config.sampleRate;
        channels=config.speakerMode==AudioSpeakerMode.Mono?1:config.speakerMode==AudioSpeakerMode.Quad?4:config.speakerMode==AudioSpeakerMode.Surround?5:config.speakerMode==AudioSpeakerMode.Mode5point1?6:config.speakerMode==AudioSpeakerMode.Mode7point1?8:2;
        audioFile=new FileStream(Path.Combine(output,"audio.f32le"),FileMode.Create,FileAccess.Write);
        audioRecording=AudioRenderer.Start();
        Application.logMessageReceived+=Log;
        recording=true;Debug.Log($"PRACTICE_RECORD_BEGIN {output} audio={audioRecording} sampleRate={sampleRate} channels={channels}");
    }
    void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception)errors++;}
    void LateUpdate()
    {
        if(!recording)return;
        try
        {
            if(frame>=TotalFrames){Finish();return;}
            float t=frame/(float)Fps;
            if(frame==2*Fps)range.Resume();
            if(frame==10*Fps){range.SetMode(2);range.spawner.irregularMovement=true;range.OpenMenu();}
            if(frame==12*Fps)range.FreePractice();
            if(frame==20*Fps){range.SetMode(3);range.OpenMenu();}
            if(frame==22*Fps)range.TimedChallenge();
            if(frame==27*Fps)range.inventory.SendMessage("TryStartReload");
            if(frame==30*Fps){range.SetMode(4);range.OpenMenu();}
            if(frame==32*Fps)range.FreePractice();
            if(frame==40*Fps){range.SetMode(1);range.EquipPistol();range.OpenMenu();}
            if(frame==42*Fps)range.FreePractice();
            if(frame==46*Fps)range.inventory.SendMessage("TryStartReload");
            if(frame==50*Fps)range.OpenMenu();
            if(!range.MenuOpen)
            {
                if(t>=32 && t<40)
                {
                    float x=40+Mathf.Sin((t-32)*Mathf.PI/2)*2.4f;
                    range.player.GetComponent<CharacterController>().Move(Vector3.right*(x-range.player.position.x));
                }
                var target=range.spawner.enemyObject;
                if(target.activeSelf)
                {
                    var zone=target.GetComponentsInChildren<PracticeTargetZone>().First(z=>!z.isHead);
                    var desired=Quaternion.LookRotation(zone.transform.position-camera.transform.position);
                    aim=Quaternion.Slerp(aim,desired,t>=22&&t<30?.17f:.12f);
                }
                look.transform.rotation=aim;camera.transform.localRotation=Quaternion.identity;
                bool shoot=(t>=2&&t<10)||(t>=12&&t<20)||(t>=32&&t<40)||(t>=42&&t<45.8f)||(t>=48.8f&&t<50);
                if(shoot && target.activeSelf && t>=nextShot && !range.inventory.IsReloading)
                {
                    if(input.TryFire())shots++;
                    nextShot=t+(t>=42?.65f:.28f);
                }
            }
            range.SendMessage("Refresh");Canvas.ForceUpdateCanvases();
            camera.targetTexture=texture;var old=RenderTexture.active;RenderTexture.active=texture;camera.Render();
            pixels.ReadPixels(new Rect(0,0,Width,Height),0,0,false);pixels.Apply(false,false);
            var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            camera.targetTexture=null;RenderTexture.active=old;
            if(audioRecording)
            {
                int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;
                using(var buffer=new NativeArray<float>(count,Allocator.Temp))
                {
                    AudioRenderer.Render(buffer);
                    var samples=buffer.ToArray();foreach(float value in samples)audioPeak=Mathf.Max(audioPeak,Mathf.Abs(value));
                    var soundBytes=new byte[samples.Length*sizeof(float)];Buffer.BlockCopy(samples,0,soundBytes,0,soundBytes.Length);audioFile.Write(soundBytes,0,soundBytes.Length);
                }
            }
            if(frame%(10*Fps)==0)Debug.Log($"PRACTICE_RECORD_PROGRESS {t:0}s shots={shots} score={range.spawner.Score}");
            frame++;
        }
        catch(Exception e){recording=false;Close();PracticeRangeVideoRecorder.Fail(e);}
    }
    void Finish()
    {
        recording=false;Close();
        File.WriteAllText(Path.Combine(output,"capture-info.txt"),$"Frames={frame}\nFPS={Fps}\nWidth={Width}\nHeight={Height}\nShots={shots}\nRuntimeErrors={errors}\nAudioPeak={audioPeak}\nSampleRate={sampleRate}\nChannels={channels}\nScripted demonstration of real Unity Play mode. No account or scene writes.\n");
        if(errors>0 || shots<5)throw new Exception($"Recording validation failed: errors={errors}, shots={shots}");
        Debug.Log($"PRACTICE_RECORD_PASS frames={frame} shots={shots} audioPeak={audioPeak} output={output}");EditorApplication.ExitPlaymode();
    }
    void Close()
    {
        Application.logMessageReceived-=Log;
        Time.captureFramerate=0;
        if(audioRecording){AudioRenderer.Stop();audioRecording=false;}
        audioFile?.Dispose();audioFile=null;
        if(encoder!=null)
        {
            encoder.StandardInput.Close();if(!encoder.WaitForExit(15000))throw new Exception("Video encoder did not finish");
            if(encoder.ExitCode!=0)throw new Exception("Video encoder failed");encoder.Dispose();encoder=null;
        }
        if(camera!=null)camera.targetTexture=null;
    }
    void OnDestroy(){if(recording){recording=false;Close();}}
}
#endif
