using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class VectorSetting
{
    public string key, title, help, section;
    public int tab;
    public float min, max, step, initial;
    public string[] choices;
    public bool Video => key == "DisplayMode" || key == "Resolution";
    public VectorSetting(int t, string s, string k, string n, string h, float lo, float hi, float d, float inc = 1, string[] options = null)
    { tab=t; section=s; key=k; title=n; help=h; min=lo; max=hi; initial=d; step=inc; choices=options; }
}

public static class VectorSettingsData
{
    public static event Action Changed;
    public static readonly string[] Tabs = { "일반", "조작", "감도", "조준선", "화면", "오디오" };
    public static readonly VectorSetting[] All = Build();
    static VectorSetting[] Build()
    {
        var a = new List<VectorSetting>();
        void Range(int t,string s,string k,string n,string h,float lo,float hi,float d,float step=1) => a.Add(new VectorSetting(t,s,k,n,h,lo,hi,d,step));
        void Choice(int t,string s,string k,string n,string h,int d,params string[] v) => a.Add(new VectorSetting(t,s,k,n,h,0,v.Length-1,d,1,v));
        void Toggle(int t,string s,string k,string n,string h,int d=0) => Choice(t,s,k,n,h,d,"끄기","켜기");
        Toggle(0,"성능 정보","ShowFPS","프레임 표시","화면 오른쪽 위에 초당 프레임 수를 표시합니다.");
        Toggle(0,"성능 정보","ShowFrameTime","프레임 시간 표시","한 프레임을 그리는 데 걸리는 시간을 밀리초(ms)로 표시합니다.");
        Toggle(0,"백그라운드","MuteUnfocused","다른 창 사용 중 음소거","게임 창이 선택되지 않았을 때 게임 소리를 끕니다. 음성 채팅 송신은 기존 음성 정책을 따릅니다.");
        Range(0,"백그라운드","BackgroundFPS","백그라운드 프레임 제한","다른 창을 사용 중일 때 적용할 최대 프레임입니다.",15,120,30,5);
        Range(2,"마우스","MouseSensitivity_General","전체 감도","마우스 이동에 대한 시점 회전 속도입니다. 로비에서 변경한 값도 다음 전투에 그대로 적용됩니다.",.01f,5,1,.01f);
        Range(2,"마우스","MouseSensitivity_Horizontal","수평 감도 배율","전체 감도에 곱해지는 좌우 회전 배율입니다.",.1f,3,1,.01f);
        Range(2,"마우스","MouseSensitivity_Vertical","수직 감도 배율","전체 감도에 곱해지는 위아래 회전 배율입니다.",.1f,3,1,.01f);
        Range(2,"정조준","ADSSensitivity","정조준 감도 배율","무기의 기본 정조준 감도에 추가로 곱해지는 배율입니다.",.1f,3,1,.01f);
        Toggle(2,"마우스","InvertY","수직 시점 반전","마우스를 위로 움직이면 아래를 바라보도록 반전합니다.");
        Toggle(3,"기본","CrossEnabled","조준선 표시","기본 전투 조준선을 표시합니다. 무기 정조준 시 숨김 동작은 유지됩니다.",1);
        Range(3,"색상","CrossR","빨강","조준선 색상의 빨강 성분입니다.",0,255,60);
        Range(3,"색상","CrossG","초록","조준선 색상의 초록 성분입니다.",0,255,255);
        Range(3,"색상","CrossB","파랑","조준선 색상의 파랑 성분입니다.",0,255,190);
        Range(3,"색상","CrossAlpha","불투명도","조준선 전체의 불투명도를 조절합니다.",10,100,100);
        Toggle(3,"중심점","CrossDot","중심점 표시","조준선 중앙의 점을 켜거나 끕니다.");
        Range(3,"중심점","CrossDotSize","중심점 크기","중심점의 한 변 길이입니다.",1,12,3);
        Range(3,"안쪽 선","CrossLength","선 길이","중심에서 바깥으로 뻗는 네 방향 선의 길이입니다.",0,30,7);
        Range(3,"안쪽 선","CrossThickness","선 두께","네 방향 선의 두께입니다.",1,10,2);
        Range(3,"안쪽 선","CrossGap","중앙 간격","중심과 각 선 사이의 빈 공간입니다.",0,30,4);
        Toggle(3,"안쪽 선","CrossTop","위쪽 선 표시","위쪽 선을 끄면 T자 조준선이 됩니다.",1);
        Toggle(3,"바깥쪽 선","CrossOuter","바깥쪽 선 표시","안쪽 선 외부에 한 쌍의 보조 선을 추가합니다.");
        Range(3,"바깥쪽 선","CrossOuterLength","바깥쪽 선 길이","바깥쪽 선의 길이입니다.",1,30,3);
        Range(3,"바깥쪽 선","CrossOuterGap","바깥쪽 선 간격","중심에서 바깥쪽 선까지의 거리입니다.",0,60,15);
        Toggle(3,"외곽선","CrossOutline","외곽선 표시","밝은 배경에서도 보이도록 검정 외곽선을 추가합니다.",1);
        Range(3,"외곽선","CrossOutlineSize","외곽선 두께","조준선 주변 검정 테두리의 두께입니다.",1,4,1);
        Choice(4,"디스플레이","DisplayMode","디스플레이 모드","적용 후 15초 안에 확인하지 않으면 이전 화면 설정으로 복원합니다.",1,"전체 화면","테두리 없는 전체 화면","창 모드");
        // Resolution options are supplied from the current display at runtime.
        a.Add(new VectorSetting(4,"디스플레이","Resolution","해상도","모니터가 지원하는 해상도입니다. 변경 후 적용을 눌러 확인합니다.",0,0,0,1,new[]{"현재 해상도"}));
        Range(4,"디스플레이","FrameLimit","최대 프레임","수직 동기화를 끄면 적용되는 최대 프레임 수입니다.",30,360,144,1);
        Toggle(4,"디스플레이","VSync","수직 동기화","화면 찢어짐을 줄입니다. 켜면 모니터 주사율에 맞춰 프레임이 제한됩니다.");
        Range(4,"그래픽 품질","RenderScale","3D 렌더링 비율","UI 해상도를 유지하면서 3D 화면의 렌더링 해상도를 조절합니다.",50,150,100,5);
        Choice(4,"그래픽 품질","TextureQuality","텍스처 품질","텍스처의 최대 해상도입니다. 낮추면 그래픽 메모리 사용량을 줄일 수 있습니다.",0,"높음","보통","낮음");
        Choice(4,"그래픽 품질","Shadows","그림자 거리","실시간 그림자가 표시되는 거리를 조절합니다.",2,"끄기","가까움","보통","멀리");
        Choice(4,"그래픽 품질","MSAA","안티앨리어싱","MSAA로 3D 물체의 가장자리 계단 현상을 줄입니다. 높을수록 렌더링 비용이 증가합니다.",0,"끄기","2x","4x","8x");
        Choice(4,"그래픽 품질","Anisotropic","텍스처 필터링","비스듬히 보이는 바닥 등 텍스처의 선명도를 조절합니다.",1,"끄기","텍스처 설정","강제 적용");
        Range(5,"게임 소리","MasterVolume","전체 볼륨","게임의 전체 소리 크기입니다. 음성 채팅 볼륨은 아래에서 따로 조절합니다.",0,100,100);
        Range(5,"음성 채팅","VoiceInput","마이크 볼륨","음성 채팅 마이크 입력 볼륨입니다. 50이 기본 크기입니다.",0,100,50);
        Range(5,"음성 채팅","VoiceOutput","음성 채팅 볼륨","다른 플레이어의 음성 출력 볼륨입니다. 50이 기본 크기입니다.",0,100,50);
        Toggle(5,"음성 채팅","VoiceMuted","마이크 음소거","음성 채팅에 내 목소리를 보내지 않습니다.");
        Choice(5,"음성 채팅","VoiceAutomatic","음성 채팅 모드","눌러서 말하기는 조작 탭에 지정한 음성 키를 누르는 동안 송신합니다.",0,"눌러서 말하기","항상 말하기");
        return a.ToArray();
    }
    public static VectorSetting Find(string key) => Array.Find(All, x => x.key == key);
    static string Pref(string key) => key.StartsWith("MouseSensitivity_") ? key : "Vector.Setting." + key;
    public static float Get(string key)
    {
        if(key=="VoiceInput") return PlayerPrefs.GetInt("VectorVoice.Input",0)+50;
        if(key=="VoiceOutput") return PlayerPrefs.GetInt("VectorVoice.Output",0)+50;
        if(key=="VoiceMuted") return PlayerPrefs.GetInt("VectorVoice.Muted",0);
        if(key=="VoiceAutomatic") return PlayerPrefs.GetInt("VectorVoice.Automatic",0);
        var d=Find(key); float v=PlayerPrefs.GetFloat(Pref(key), d?.initial ?? 0);
        return float.IsNaN(v)||float.IsInfinity(v) ? d?.initial??0 : d==null?v:Mathf.Clamp(v,d.min,d.max);
    }
    public static void Set(string key, float value)
    {
        var d=Find(key); if(d==null || float.IsNaN(value)||float.IsInfinity(value))return;
        value=Mathf.Clamp(Mathf.Round(value/d.step)*d.step,d.min,d.max);
        PlayerPrefs.SetFloat(Pref(key),value);
        var voice=VectorVoiceChat.Instance;
        switch(key) {
            case "VoiceInput": PlayerPrefs.SetInt("VectorVoice.Input",Mathf.RoundToInt(value)-50); voice?.SetInputVolume(value-50); break;
            case "VoiceOutput": PlayerPrefs.SetInt("VectorVoice.Output",Mathf.RoundToInt(value)-50); voice?.SetOutputVolume(value-50); break;
            case "VoiceMuted": PlayerPrefs.SetInt("VectorVoice.Muted",(int)value); if(voice!=null&&voice.MicrophoneMuted!=(value==1))voice.ToggleMicrophone(); break;
            case "VoiceAutomatic": PlayerPrefs.SetInt("VectorVoice.Automatic",(int)value); if(voice!=null&&voice.Automatic!=(value==1))voice.ToggleMode(); break;
        }
        if((d.tab==4&&!d.Video)||key=="MasterVolume"||key=="MuteUnfocused"||key=="BackgroundFPS")ApplyRuntime(); Changed?.Invoke();
    }
    static UniversalRenderPipelineAsset runtimePipeline, originalPipeline;
    static bool captured;
    static int oldLimit,oldVsync,oldTexture;
    static float oldVolume;
    static AnisotropicFiltering oldFiltering;
    public static void ApplyRuntime()
    {
        if(!Application.isPlaying)return;
        if(!captured){captured=true;oldLimit=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;oldTexture=QualitySettings.globalTextureMipmapLimit;oldVolume=AudioListener.volume;oldFiltering=QualitySettings.anisotropicFiltering;}
        AudioListener.volume=Get("MuteUnfocused")==1&&!Application.isFocused?0:Get("MasterVolume")/100f;
        Application.targetFrameRate=Mathf.RoundToInt(Get(Application.isFocused?"FrameLimit":"BackgroundFPS"));
        QualitySettings.vSyncCount=Application.isFocused?(int)Get("VSync"):0;
        QualitySettings.globalTextureMipmapLimit=(int)Get("TextureQuality");
        QualitySettings.anisotropicFiltering=(AnisotropicFiltering)(int)Get("Anisotropic");
        if(runtimePipeline==null && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset) {
            originalPipeline=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            runtimePipeline=UnityEngine.Object.Instantiate(asset); runtimePipeline.name="Vector runtime graphics";
            QualitySettings.renderPipeline=runtimePipeline;
        }
        if(runtimePipeline!=null) {
            runtimePipeline.renderScale=Get("RenderScale")/100f;
            runtimePipeline.shadowDistance=new[]{0f,25f,50f,100f}[(int)Get("Shadows")];
            runtimePipeline.msaaSampleCount=1<<(int)Get("MSAA");
        }
    }
    public static void Release() { if(runtimePipeline!=null){ QualitySettings.renderPipeline=originalPipeline; UnityEngine.Object.Destroy(runtimePipeline); runtimePipeline=null; }if(captured){Application.targetFrameRate=oldLimit;QualitySettings.vSyncCount=oldVsync;QualitySettings.globalTextureMipmapLimit=oldTexture;QualitySettings.anisotropicFiltering=oldFiltering;AudioListener.volume=oldVolume;captured=false;} }
    public static void ResetTab(int tab) { foreach(var d in All) if(d.tab==tab&&!d.Video)Set(d.key,d.initial); if(tab==1)VectorInput.Reset(); }
}
