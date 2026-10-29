using System;
using System.IO;
using System.Linq;
using Game.Pings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PingAuthoring
{
    const string Folder="Assets/Art/Ping";
    public const string PrefabPath=Folder+"/TeamPingHUD.prefab";
    static Font font;
    static Color ink=new(.035f,.052f,.07f,.92f);
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rt.gameObject.layer=5;
        rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;
        rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);return rt;
    }
    static Text Label(Transform p,string name,string value,float x,float y,float w,float h,int size)
    {
        var t=Rect(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;
        t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.color=Color.white;t.supportRichText=false;
        var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.65f);shadow.effectDistance=new Vector2(1,-1);
        return t;
    }
    static Image Box(Transform p,string name,float x,float y,float w,float h,Color color)
    {var i=Rect(p,name,x,y,w,h).gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
    static PingGraphics Graphic(Transform p,string name,float x,float y,float size,int glyph=0)
    {var g=Rect(p,name,x,y,size,size).gameObject.AddComponent<PingGraphics>();g.glyph=glyph;g.raycastTarget=false;return g;}
    static void Set(UnityEngine.Object target,string property,float value)
    {var s=new SerializedObject(target);s.FindProperty(property).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static AudioClip Tone(string name,float frequency)
    {
        string path=Folder+"/"+name+".wav";const int rate=24000,count=7200;
        using(var w=new BinaryWriter(File.Create(path)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            for(int i=0;i<count;i++)
            {
                double t=i/(double)rate,part=t<.12?t:t-.15;
                double e=part>=0&&part<.12?Math.Sin(Math.PI*part/.12)*Math.Exp(-part*15):0;
                double f=t<.12?frequency:frequency*1.5;
                w.Write((short)(Math.Sin(2*Math.PI*f*part)*e*.2*short.MaxValue));
            }
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    [MenuItem("Project Vector/UI/Author Team Ping HUD")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/Pretendard-Medium.otf");
        if(!font)throw new Exception("Missing project font");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("TeamPingHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        root.layer=5;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        var system=root.AddComponent<PingSystem>();var manager=root.GetComponent<PingManager>();root.AddComponent<PingBridge>();
        manager.canvasRoot=(RectTransform)root.transform;manager.presentation=root.GetComponent<CanvasGroup>();
        manager.presentation.interactable=false;manager.presentation.blocksRaycasts=false;
        manager.ConfigureNetwork(true,-1);
        Set(system,"wheelRadius",170);Set(system,"deadZone",52);
        var so=new SerializedObject(system);so.FindProperty("hitMask").intValue=~0;so.ApplyModifiedPropertiesWithoutUndo();
        manager.audioSource=root.AddComponent<AudioSource>();manager.audioSource.playOnAwake=false;manager.audioSource.spatialBlend=0;
        manager.confirmSound=Tone("TeamPing",670);manager.dangerSound=Tone("DangerPing",520);

        var w=Rect(root.transform,"PingWheel",0,0,360,360).gameObject.AddComponent<PingWheel>();manager.wheel=w;
        w.segments=new PingGraphics[4];w.rims=new PingGraphics[4];w.icons=new PingGraphics[4];w.labels=new Text[4];
        for(int i=0;i<4;i++)
        {
            var sector=Graphic(w.transform,"Sector_"+manager.Options[i].type,0,0,350);
            sector.startDegrees=i*90-43;sector.endDegrees=i*90+43;sector.innerRadius=65;sector.outerRadius=168;sector.color=ink;w.segments[i]=sector;
            var rim=Graphic(w.transform,"Rim_"+manager.Options[i].type,0,0,350);
            rim.startDegrees=sector.startDegrees;rim.endDegrees=sector.endDegrees;rim.innerRadius=168;rim.outerRadius=171;rim.color=new Color(.3f,.4f,.5f,.8f);w.rims[i]=rim;
            float angle=i*90*Mathf.Deg2Rad;var at=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*116;
            var icon=Graphic(w.transform,"Icon_"+manager.Options[i].type,at.x,at.y+13,30,i+1);icon.color=manager.Options[i].color;w.icons[i]=icon;
            w.labels[i]=Label(w.transform,"Label_"+manager.Options[i].type,manager.Options[i].label,at.x,at.y-23,90,30,22);
        }
        w.cancelCircle=Graphic(w.transform,"CancelDisc",0,0,110);w.cancelCircle.innerRadius=0;w.cancelCircle.outerRadius=53;w.cancelCircle.color=ink;
        w.centerLabel=Label(w.transform,"Selection","취소",0,0,100,35,22);
        Label(w.transform,"Heading","팀 핑",0,205,240,32,23);
        Label(w.transform,"Instructions","Z를 놓으면 전송, 중앙에서 놓으면 취소",0,-210,500,32,19).color=new Color(.77f,.84f,.9f);
        var pointer=Graphic(w.transform,"Pointer",0,0,9);pointer.diamond=true;pointer.color=Color.white;w.pointer=pointer.rectTransform;
        w.Initialize(manager.Options,170,52);

        manager.markerPool=new PingMarker[6];
        for(int i=0;i<manager.markerPool.Length;i++)
        {
            var p=Rect(root.transform,"PingSlot_"+i,0,0,0,0).gameObject.AddComponent<PingMarker>();manager.markerPool[i]=p;
            p.fade=p.gameObject.AddComponent<CanvasGroup>();
            p.worldRoot=Rect(p.transform,"WorldMarker",0,0,180,90);
            Box(p.worldRoot,"Plate",0,-37,166,48,ink);
            p.accent=Box(p.worldRoot,"Accent",0,-12,166,2,Color.cyan);
            p.icon=Graphic(p.worldRoot,"Kind",0,15,27,1);
            p.title=Label(p.worldRoot,"KindLabel","여기",0,-27,156,25,20);
            p.detail=Label(p.worldRoot,"SenderDistance","아군 / 12 m",0,-49,156,22,16);
            p.detail.color=new Color(.85f,.9f,.94f);
            p.hudIcon=Rect(p.transform,"HudMapMarker",0,0,26,26);
            var halo=Graphic(p.hudIcon,"Contrast",0,0,29);halo.innerRadius=0;halo.outerRadius=14;halo.color=ink;
            p.hudGlyph=Graphic(p.hudIcon,"Kind",0,0,20,1);
            p.fullIcon=Rect(p.transform,"FullMapMarker",0,0,30,30);
            var fullHalo=Graphic(p.fullIcon,"Contrast",0,0,34);fullHalo.innerRadius=0;fullHalo.outerRadius=17;fullHalo.color=ink;
            p.fullGlyph=Graphic(p.fullIcon,"Kind",0,0,24,1);
            p.Hide();
        }
        var hint=Rect(root.transform,"InputHint",0,0,260,34);hint.anchorMin=hint.anchorMax=new Vector2(0,1);hint.pivot=new Vector2(0,1);hint.anchoredPosition=new Vector2(38,-310);
        Box(hint,"Key",-108,0,30,28,ink);Label(hint,"KeyLabel","Z",-108,0,30,28,19);
        Label(hint,"Help","핑, 길게 눌러 선택",20,0,212,28,18).color=new Color(.80f,.87f,.92f);manager.inputHint=hint.gameObject;
        manager.statusText=Label(root.transform,"Status","",0,-260,600,32,20);manager.statusText.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);UnityEngine.Object.DestroyImmediate(root);

        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var systems=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PingSystem>(true)).ToArray();
        foreach(var old in systems)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
        var linked=instance.GetComponent<PingManager>();
        var mini=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MiniMapManager>(true)).First();
        var miniSO=new SerializedObject(mini);
        RectTransform Map(string key)
        {
            var rect=(RectTransform)miniSO.FindProperty(key).objectReferenceValue;
            var raw=rect.GetComponentInChildren<RawImage>(true);return raw?raw.rectTransform:rect;
        }
        linked.hudMap=Map("_miniMapHUD");linked.fullMap=Map("_miniMapOverlay");
        linked.mapCamera=(Camera)miniSO.FindProperty("_miniMapCamera").objectReferenceValue;
        PrefabUtility.RecordPrefabInstancePropertyModifications(linked);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("PING_AUTHOR_PASS: saved wheel, six marker slots, fonts, sound and map references; no runtime UI creation");
    }
}
