#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug=UnityEngine.Debug;
using Object=UnityEngine.Object;

// Offline footage of the saved gameplay prefab. Does not author or save scenes,
// spawn replacement UI, change gameplay, or drive the user's desktop.
public static class ConstructionMapShowcase
{
    const int Fps=30;
    static bool aerial;
    static int Width=>aerial?2560:1920;
    static int Height=>aerial?1440:1080;
    const string Encoder="C:/Users/GCT/AppData/Local/Programs/Python/Python310/lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe";
    public static string Output=>Path.Combine(ConstructionMapSurvey.Output,aerial?"Aerial-Tour":"Map-Film");
    sealed class Shot
    {
        public string name,title,description;
        public float seconds,fov;
        public Vector3[] path;
        public Vector3? focus;
        public float orthoSize,endOrthoSize;
        public bool topDown,minimapCutaway;
        public Shot(string name,string title,string description,float seconds,float fov,Vector3? focus,params Vector3[] path)
        {this.name=name;this.title=title;this.description=description;this.seconds=seconds;this.fov=fov;this.focus=focus;this.path=path;}
    }
    static Vector3 P(float x,float z,float y=1.35f)=>new Vector3(x,y,z);
    static Shot Top(string name,string title,string description,float seconds,float size,float endSize,params Vector3[] centers)
    {
        return new Shot(name,title,description,seconds,60,null,centers.Select(c=>new Vector3(c.x,80,c.z)).ToArray())
        {topDown=true,orthoSize=size,endOrthoSize=endSize};
    }
    static Shot[] AerialShots()
    {
        var cutaway=Top("service_cutaway","작업실 바닥 확인","미니맵 표시 방식: 이 장면만 지붕을 제외 / 양쪽 경사로, 높이 0.9m",6,11,10,P(-20,-3),P(-20,-2));
        cutaway.minimapCutaway=true;
        return new[]{
            new Shot("whole_oblique","공사장 전체 전경","A 연결로와 실내 작업실 수정본",9,58,Vector3.zero,new Vector3(76,100,-86),new Vector3(0,110,-112)),
            Top("whole_top","전체 배치","위쪽 수비, 아래쪽 공격 / 회청색은 건물, 밝은 바닥과 검은 아스팔트는 통로",8,55,51,P(0,0),P(0,0)),
            Top("attack_branch","공격 스폰과 분기","A 외곽, 중앙, B 작업장 / A 연결로는 남쪽 미드에서 분기",10,27,23,P(0,-35),P(-9,-22)),
            Top("a_new_route","새 미드 → A 진입로","공격측 분기 → 높은 작업실 → 굴곡 진입",12,20,18,P(-9,-12),P(-20,-9),P(-20,-1),P(-25,9)),
            cutaway,
            Top("a_top","A 설치 구역","기존 엄폐와 추가 진입 엄폐 / 외곽, 작업실, 후방 입구",10,19,16,P(-29,16),P(-29,21)),
            new Shot("a_oblique","A 입체 구조","크레인 기초와 엄폐물 높이",8,57,new Vector3(-29,1,18),new Vector3(-57,38,-4),new Vector3(-54,37,31)),
            Top("mid","중앙 변압기","엄폐물을 돌아 나뉘는 시야 / 북쪽은 수비 합류",9,21,19,P(0,-2),P(4,10)),
            Top("mid_b","중앙 → B 연결로","꺾인 연결로에서 하역장 측면으로",9,19,17,P(8,10),P(16,17),P(23,24)),
            Top("b_top","B 설치 구역","두 엄폐물과 동쪽 하역 데크",8,18,15,P(28,25),P(30,27)),
            new Shot("b_oblique","B 하역장 고저차","높이 1.2m 데크와 진입 경사로",8,57,new Vector3(30,0,26),new Vector3(55,31,8),new Vector3(50,32,45)),
            Top("defense_rear","수비 스폰과 후방 연결로","검은 아스팔트는 A ↔ B로 이동하는 실제 통로",12,25,24,P(0,37),P(-10,34),P(10,34)),
            Top("b_workshop","B 작업장과 공격 공급로","연속 코너를 지나 공격 스폰으로",10,25,27,P(28,-12),P(23,-27),P(5,-36)),
            Top("whole_end","전체 동선 다시 보기","최신 Construction 프리팹 / 위쪽 수비, 아래쪽 공격",8,51,55,P(0,0),P(0,0))
        };
    }
    static Shot[] Shots()=>new[]{
        new Shot("overview","공사장","CONSTRUCTION / PROJECT VECTOR",6,58,new Vector3(0,1,0),new Vector3(68,58,-73),new Vector3(56,49,-64)),
        new Shot("attack_spawn","공격 시작 지점","양쪽 출구에서 A, 중앙, B로 분기",5,73,P(3,-37,1.6f),P(-7,-43),P(-5,-40)),
        new Shot("attack_supply_a","A 진입 준비","자재 공급로",5,72,null,P(-10,-38),P(-28,-38),P(-28,-34)),
        new Shot("a_elbow","A 외부 통로","좌측 외곽 진입",5,72,null,P(-28,-34),P(-28,-30),P(-40,-30),P(-40,-24)),
        new Shot("a_long","A 긴 사선","36m 교전 구간",8,72,P(-40,19,1.35f),P(-40,-23),P(-40,6)),
        new Shot("a_entry","A 크레인 마당","외부 진입에서 설치 구역으로",6,70,P(-28,22,1.6f),P(-40,6),P(-35,11),P(-34,16)),
        new Shot("a_site","A 설치 구역","기본 엄폐와 후방 진입",6,70,P(-27,22,1.55f),P(-35,19),P(-32,27)),
        new Shot("crane","크레인 기초","A 구역의 기준점",5,59,new Vector3(-39,2.7f,24),new Vector3(-34,2.4f,19),new Vector3(-33,4.2f,26)),
        new Shot("a_retake","A 후방 시야","주 진입과 분리된 재진입 각도",5,72,P(-30,17),P(-33,28),P(-26,28)),
        new Shot("a_connector","A 연결로","공격측 연결로, 높은 작업실, A 진입",8,75,null,P(-4,-12),P(-20,-12),P(-20,-11),P(-20,-6,2.25f),P(-20,0,2.25f),P(-20,5),P(-20,6),P(-26,6),P(-26,11)),
        new Shot("mid_left","중앙 변압기, 서쪽","엄폐를 돌아 확인하는 반대편",7,73,null,P(-5,10),P(-5,-2),P(0,-3)),
        new Shot("mid_entry","중앙 진입","공격 대기선에서 중앙으로",8,75,null,P(-4,-22),P(-4,-12),P(4,-12),P(6,-3)),
        new Shot("mid_right","중앙 변압기, 동쪽","B 연결로와 수비 중앙 합류",6,73,null,P(7,-3),P(9,7),P(8,11)),
        new Shot("b_connector","B 연결로","중앙에서 하역장 측면으로",8,75,null,P(9,10),P(16,10),P(16,24),P(23,24)),
        new Shot("b_site","B 설치 구역","진입 엄폐와 하역장 기본 엄폐",6,70,P(28,30,1.6f),P(24,25),P(32,25),P(34,29)),
        new Shot("b_ramp","하역 데크","램프로 접근하는 1.2m 고저차",6,73,P(27,26,1.5f),P(36.5f,20),P(36.5f,24,1.95f),P(36.5f,28,2.55f),P(36.5f,31,2.55f)),
        new Shot("b_rear","B 후방","하역장 뒤편과 재진입 시야",5,73,P(28,26,1.5f),P(20,34.5f),P(24,34),P(32,34)),
        new Shot("b_main","B 주 진입로","연속된 코너에서 나뉘는 교전",9,75,null,P(35,17),P(35,2),P(21,2),P(21,-4)),
        new Shot("workshop","작업장","B 구매 대기선과 준비 동선",8,75,null,P(29,-28),P(29,-10),P(21,-10),P(21,-4)),
        new Shot("attack_supply_b","공격 공급로","작업장에서 공격 시작 지점으로",7,75,null,P(29,-28),P(29,-38),P(10,-38)),
        new Shot("defense_spawn","수비 시작 지점","후방 회전로와 중앙으로 분기",5,73,P(0,31,1.5f),P(-5,43),P(-5,38),P(-2,35)),
        new Shot("rear_rotation","수비 후방 회전로","B에서 A로 이어지는 보호된 연결",11,75,null,P(20,35),P(-20,35),P(-20,23)),
        new Shot("defense_mid","수비 중앙 합류","후방에서 중앙으로 내려오는 동선",8,75,null,P(0,35),P(0,27),P(7,27),P(7,12)),
        new Shot("b_aerial","B와 중앙 연결","지상 동선을 위에서 다시 확인",6,61,new Vector3(25,0,23),new Vector3(47,25,45),new Vector3(27,20,40)),
        new Shot("top","전체 배치","위쪽 수비 시작 지점, 아래쪽 공격 시작 지점",7,49,Vector3.zero,new Vector3(0,122,0),new Vector3(0,116,0))
    };
    static float Distance(Vector3[] path){float d=0;for(int i=1;i<path.Length;i++)d+=Vector3.Distance(path[i-1],path[i]);return d;}
    static Vector3 Along(Vector3[] path,float distance)
    {
        for(int i=1;i<path.Length;i++){
            float d=Vector3.Distance(path[i-1],path[i]);
            if(distance<=d)return Vector3.Lerp(path[i-1],path[i],d>.0001f?distance/d:0);
            distance-=d;
        }
        return path[path.Length-1];
    }
    static void Pose(Shot shot,float u,out Vector3 position,out Quaternion rotation)
    {
        // Ease starts/stops without Catmull-Rom overshoot through inside corners.
        float t=Mathf.SmoothStep(0,1,u),length=Distance(shot.path),d=t*length;
        position=Along(shot.path,d);
        if(shot.topDown){rotation=Quaternion.Euler(90,0,0);return;}
        Vector3 target;
        if(shot.focus.HasValue)target=shot.focus.Value;
        else{
            var ahead=Along(shot.path,Mathf.Min(length,d+3));
            if((ahead-position).sqrMagnitude<.01f)ahead=position+(shot.path[^1]-shot.path[^2]).normalized*3;
            target=ahead;
        }
        rotation=Quaternion.LookRotation(target-position,Vector3.up);
    }
    static void Validate(Shot[] shots)
    {
        var evidence=new List<string>{"shot,samples,camera_radius_m,result"};int failures=0;
        foreach(var shot in shots){
            Vector3 previous=default;int count=Mathf.RoundToInt(shot.seconds*Fps);
            for(int f=0;f<count;f++){
                Pose(shot,f/(float)(count-1),out var p,out _);
                var overlaps=Physics.OverlapSphere(p,.14f,~0,QueryTriggerInteraction.Ignore);
                if(overlaps.Length>0){evidence.Add($"{shot.name},{f},0.14,INSIDE {overlaps[0].name} at {p}");failures++;break;}
                if(f>0&&Physics.SphereCast(previous,.14f,(p-previous).normalized,out var hit,Vector3.Distance(p,previous),~0,QueryTriggerInteraction.Ignore)){
                    evidence.Add($"{shot.name},{f},0.14,CROSS {hit.collider.name} at {p}");failures++;break;
                }
                previous=p;
                if(f==count-1)evidence.Add($"{shot.name},{count},0.14,PASS");
            }
        }
        File.WriteAllLines(Path.Combine(Output,"camera-validation.csv"),evidence);
        if(failures>0)throw new Exception("Camera route intersects saved map: "+failures+" shots; see camera-validation.csv");
    }
    static string Stamp(float t)
    {
        int cs=Mathf.RoundToInt(t*100);return $"{cs/360000}:{(cs/6000)%60:D2}:{(cs/100)%60:D2}.{cs%100:D2}";
    }
    static string Csv(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
    static void Titles(Shot[] shots)
    {
        var ass=new StringBuilder("[Script Info]\nScriptType: v4.00+\nPlayResX: 1920\nPlayResY: 1080\nWrapStyle: 2\n\n[V4+ Styles]\nFormat: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n");
        ass.AppendLine("Style: Label,Malgun Gothic,40,&H00FFFFFF,&H00FFFFFF,&H70000000,&HA0000000,-1,0,0,0,100,100,0,0,1,1.8,0,1,82,80,86,1");
        ass.AppendLine("Style: Sub,Malgun Gothic,24,&H00E4E4E4,&H00FFFFFF,&H70000000,&HA0000000,0,0,0,0,100,100,0,0,1,1.6,0,1,84,80,48,1");
        ass.AppendLine("Style: Brand,Arial,19,&H00FFFFFF,&H00FFFFFF,&H90000000,&HA0000000,-1,0,0,0,100,100,3,0,1,1,0,7,84,80,55,1");
        ass.AppendLine("\n[Events]\nFormat: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
        var manifest=new List<string>{"index,start_seconds,duration_seconds,shot,title,description"};var chapters=new StringBuilder();float start=0;
        for(int i=0;i<shots.Length;i++){
            var shot=shots[i];float end=start+shot.seconds;
            ass.AppendLine($"Dialogue: 0,{Stamp(start+.15f)},{Stamp(end-.1f)},Label,,0,0,0,,{{\\fad(180,180)}}{i+1:D2}  {shot.title}");
            ass.AppendLine($"Dialogue: 0,{Stamp(start+.15f)},{Stamp(end-.1f)},Sub,,0,0,0,,{{\\fad(180,180)}}{shot.description}");
            manifest.Add($"{i},{start.ToString(CultureInfo.InvariantCulture)},{shot.seconds.ToString(CultureInfo.InvariantCulture)},{Csv(shot.name)},{Csv(shot.title)},{Csv(shot.description)}");
            chapters.AppendLine($"{(int)start/60:D2}:{(int)start%60:D2}  {shot.title}");start=end;
        }
        ass.AppendLine($"Dialogue: 1,0:00:00.00,{Stamp(start)},Brand,,0,0,0,,PROJECT VECTOR   /   CONSTRUCTION");
        File.WriteAllText(Path.Combine(Output,"titles.ass"),ass.ToString(),new UTF8Encoding(false));
        File.WriteAllLines(Path.Combine(Output,"shots.csv"),manifest,new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(Output,"chapters.txt"),chapters.ToString(),new UTF8Encoding(false));
    }
    public static void Preview(){aerial=false;Capture(false);}
    public static void Run(){aerial=false;Capture(true);}
    public static void PreviewAerial(){aerial=true;Capture(false);}
    public static void RunAerial(){aerial=true;Capture(true);}
    static void Capture(bool video)
    {
        if(!Application.isBatchMode)throw new Exception("Use a closed-project batch editor only");
        Process encoder=null;RenderTexture rt=null;Texture2D pixels=null;ConstructionMapEnvironment environment=null;
        int result=1;
        try{
            Directory.CreateDirectory(Output);Directory.CreateDirectory(Path.Combine(Output,"frames"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructionMapAuthoring.MapPath));
            map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);map.GetComponent<MapInfo>().SetBarriersActive(false);
            ConstructionMapSurvey.Lighting();environment=map.GetComponent<ConstructionMapEnvironment>();environment.Apply();Physics.SyncTransforms();
            var shots=aerial?AerialShots():Shots();Validate(shots);Titles(shots);
            var camera=new GameObject("Offline Film Camera").AddComponent<Camera>();camera.enabled=false;
            camera.nearClipPlane=.05f;camera.farClipPlane=400;camera.backgroundColor=new Color(.62f,.73f,.83f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.GetUniversalAdditionalCameraData().renderShadows=true;
            rt=new RenderTexture(Width,Height,24){antiAliasing=4};pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);camera.targetTexture=rt;
            if(video){
                var info=new ProcessStartInfo(Encoder,$"-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size {Width}x{Height} -framerate {Fps} -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 17 -pix_fmt yuv420p -movflags +faststart \"{Output}/Construction-clean.mp4\"")
                {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
                encoder=new Process{StartInfo=info};encoder.ErrorDataReceived+=(s,e)=>{if(!string.IsNullOrEmpty(e.Data))Debug.LogWarning(e.Data);};encoder.Start();encoder.BeginErrorReadLine();
            }
            int total=0;
            for(int index=0;index<shots.Length;index++){
                var shot=shots[index];camera.fieldOfView=shot.fov;camera.orthographic=shot.topDown;
                camera.cullingMask=shot.minimapCutaway?LayerMask.GetMask("MiniMap"):~0;
                int count=Mathf.RoundToInt(shot.seconds*Fps);
                foreach(int frame in video?Enumerable.Range(0,count):new[]{0,count/2,count-1}){
                    if(shot.topDown)camera.orthographicSize=Mathf.Lerp(shot.orthoSize,shot.endOrthoSize,Mathf.SmoothStep(0,1,frame/(float)(count-1)));
                    Pose(shot,frame/(float)(count-1),out var position,out var rotation);camera.transform.SetPositionAndRotation(position,rotation);
                    camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply(false);RenderTexture.active=null;
                    if(video){var bytes=pixels.GetRawTextureData<byte>();encoder.StandardInput.BaseStream.Write(bytes.ToArray(),0,bytes.Length);total++;}
                    if(frame==0||frame==count/2||frame==count-1){int key=frame==0?0:frame==count/2?1:2;File.WriteAllBytes(Path.Combine(Output,"frames",$"shot-{index:D2}-{key}.png"),pixels.EncodeToPNG());}
                }
                File.WriteAllText(Path.Combine(Output,"progress.txt"),$"shot={index+1}/{shots.Length}\nname={shot.name}\nframes={total}\n");Debug.Log("MAP_FILM_SHOT "+(index+1)+"/"+shots.Length+" "+shot.name);
            }
            if(video){encoder.StandardInput.Close();if(!encoder.WaitForExit(30000)||encoder.ExitCode!=0)throw new Exception("Film encoder failed");}
            File.WriteAllText(Path.Combine(Output,video?"capture-info.txt":"preview-info.txt"),$"Saved gameplay prefab: {ConstructionMapAuthoring.MapPath}\nShots={shots.Length}\nSeconds={shots.Sum(s=>s.seconds)}\nFrames={total}\nResolution={Width}x{Height}\nFPS={Fps}\nActual map rendered with an inspection camera, NOT recorded multiplayer gameplay. Camera paths are editorial, not movement-speed evidence.\nBuy barriers disabled. Aerial service_cutaway uses the existing minimap layer mask, explicitly labelled in captions; every other shot retains the real roof. No production scenes or map assets changed. No game audio recorded.\n");
            if(aerial&&video)ConstructionMapSurvey.Capture("Construction-top-latest-4K",new Vector3(0,125,0),Vector3.zero,3840,3840,52);
            Debug.Log("MAP_FILM_PASS video="+video+" frames="+total);result=0;
        }
        catch(Exception error){Debug.LogException(error);}
        finally{
            environment?.Restore();RenderTexture.active=null;
            if(encoder!=null){if(!encoder.HasExited)encoder.StandardInput.Close();encoder.Dispose();}
            if(rt)Object.DestroyImmediate(rt);if(pixels)Object.DestroyImmediate(pixels);EditorApplication.Exit(result);
        }
    }
}
#endif
