#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ScannerFeedbackAuthoring
{
    const string Hud="Assets/Art/EMP/ElectricStatusHUD.prefab";
    static readonly Color Cyan=new Color(.33f,.86f,1), Amber=new Color(1,.72f,.33f);
    internal static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var child=parent.Find(name);
        var r=child ? (RectTransform)child : new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static T Component<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
    internal static Image Bar(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var image=Component<Image>(Rect(parent,name,x,y,w,h).gameObject);image.color=color;image.raycastTarget=false;return image;
    }
    internal static TMP_Text Label(Transform parent,string name,string value,float x,float y,float w,float h,float size,Color color)
    {
        var text=Component<TextMeshProUGUI>(Rect(parent,name,x,y,w,h).gameObject);
        text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/NanumGothic SDF.asset");
        text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;return text;
    }
    internal static CanvasGroup Group(Transform parent,string name,float y,float width,float height)
    {
        var group=Component<CanvasGroup>(Rect(parent,name,0,y,width,height).gameObject);group.blocksRaycasts=group.interactable=false;return group;
    }
    internal static void Ref(Object target,string property,Object value)
    {var data=new SerializedObject(target);data.FindProperty(property).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
    internal static AudioSource Sound(Transform parent,string name,string clip,float volume,float pitch)
    {
        var source=Component<AudioSource>(Rect(parent,name,0,0,0,0).gameObject);source.playOnAwake=false;source.spatialBlend=0;source.volume=volume;source.pitch=pitch;
        source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Drone/"+clip+".wav");return source;
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only; saved prefab update, no scene overwrite");
        var root=PrefabUtility.LoadPrefabContents(Hud);
        try
        {
            var ui=Component<ScannerFeedbackUI>(root);
            var owner=Group(root.transform,"ScannerOwnerNotice",-112,230,34);
            Bar(owner.transform,"Backing",0,0,230,34,new Color(.025f,.045f,.06f,.78f));
            Bar(owner.transform,"Accent",-114,0,2,34,Cyan);
            var icon=Bar(owner.transform,"ScannerIcon",-94,0,23,23,Cyan);
            icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character/Initiator/Initiator_C.png");icon.preserveAspect=true;
            var label=Label(owner.transform,"DetectedCount","스캐너 감지  1명",14,0,190,32,19,Cyan);
            var victim=Group(root.transform,"ScannerVictimNotice",-174,210,30);
            Bar(victim.transform,"Backing",0,0,210,30,new Color(.06f,.04f,.025f,.75f));
            Label(victim.transform,"HitLabel","스캐너에 감지됨",0,0,200,30,19,Amber);
            var edges=Group(root.transform,"ScannerVictimPulse",0,1920,1080);
            // Narrow symmetric marks, not a directional damage arrow or white flash.
            foreach(int side in new[]{-1,1})
            {
                var part=Rect(edges.transform,side<0?"Left":"Right",side*350,0,24,92);
                Bar(part,"Spine",0,0,3,92,new Color(.3f,.8f,1,.65f));
                Bar(part,"Top",-side*7,45,17,2,Cyan);Bar(part,"Bottom",-side*7,-45,17,2,Cyan);
                Bar(part,"Short",side*6,0,2,35,new Color(.9f,.97f,1,.8f));
            }
            Ref(ui,"_ownerNotice",owner);Ref(ui,"_ownerText",label);Ref(ui,"_victimNotice",victim);Ref(ui,"_victimEdges",edges);
            Ref(ui,"_ownerAudio",Sound(root.transform,"ScannerOwnerAudio","Confirmed",.42f,1.12f));
            Ref(ui,"_victimAudio",Sound(root.transform,"ScannerVictimAudio","Interrupted",.32f,.85f));
            ui.ResetFeedback();PrefabUtility.SaveAsPrefabAsset(root,Hud);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Verify();
        Debug.Log("SCANNER_FEEDBACK_AUTHORING_PASS");
    }
    static T Field<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("SCANNER_FEEDBACK_CHECK "+message);}
    public static void Verify()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Hud));
        var ui=root.GetComponent<ScannerFeedbackUI>();var existing=root.GetComponent<ElectricStatusUI>();
        var owner=Field<CanvasGroup>(ui,"_ownerNotice");var victim=Field<CanvasGroup>(ui,"_victimNotice");var edges=Field<CanvasGroup>(ui,"_victimEdges");
        Check(existing && root.transform.Find("LocalStatus") && root.transform.Find("CastConfirmation"),"existing EMP/status UI retained");
        Check(root.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"HUD never captures shooting or menu input");
        foreach(var sound in new[]{Field<AudioSource>(ui,"_ownerAudio"),Field<AudioSource>(ui,"_victimAudio")})Check(sound && sound.clip && sound.spatialBlend==0 && !sound.playOnAwake,"saved local audio independent of range");
        ui.ResetFeedback();ui.ConfirmZap(99,1,7,2);Check(!owner.gameObject.activeSelf&&!victim.gameObject.activeSelf,"uninvolved player gets no private feedback");
        ui.ConfirmZap(1,1,7,2);Check(owner.gameObject.activeSelf&&!victim.gameObject.activeSelf,"A gets owner-only confirmation");
        ui.ConfirmZap(1,1,7,2);Check(Field<int>(ui,"_count")==1,"repeated server event does not duplicate hit or audio");
        ui.ConfirmZap(1,1,7,3);Check(Field<int>(ui,"_count")==2,"two distinct victims aggregate");
        ui.Advance(2);Check(!owner.gameObject.activeSelf,"confirmation expires on unscaled time");
        ui.ConfirmZap(2,1,7,2);Check(victim.gameObject.activeSelf&&edges.gameObject.activeSelf&&!owner.gameObject.activeSelf,"B gets victim-only hit pulse");
        ui.Advance(.3f);Check(!edges.gameObject.activeSelf&&victim.gameObject.activeSelf,"brief pulse ends before readable label");
        ui.Advance(.5f);Check(!victim.gameObject.activeSelf,"victim label expires");
        ui.ConfirmZap(2,1,8,2);ui.ResetFeedback();Check(!edges.gameObject.activeSelf&&!victim.gameObject.activeSelf&&!owner.gameObject.activeSelf,"round/disconnect reset clears all cues");
        // Route a confirmed packet with missing world presentation/target objects.
        var manager=new GameObject("ScannerFeedbackFixture").AddComponent<ScannerNetworkManager>();
        Field<System.Collections.Generic.Dictionary<uint,uint>>(manager,"_scannerOwners")[55]=1;
        manager.OnScannerZap(55,2,1,1,new Vector3(1,2,3));manager.OnScannerZap(55,2,1,1,new Vector3(1,2,3));
        Check(Field<System.Collections.Generic.HashSet<ulong>>(manager,"_confirmedZaps").Count==1,"duplicate packet filtered before effects");
        manager.ClearAll();manager.OnScannerZap(55,2,1,1,Vector3.zero);
        Check(Field<System.Collections.Generic.HashSet<ulong>>(manager,"_confirmedZaps").Count==0,"late previous-round event ignored");
        Object.DestroyImmediate(manager.gameObject);
        RenderPreviews(root,ui,existing);
        Object.DestroyImmediate(root);
        Debug.Log("SCANNER_FEEDBACK_VERIFY_PASS");
    }
    static void RenderPreviews(GameObject root,ScannerFeedbackUI ui,ElectricStatusUI status)
    {
        string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"ScannerFeedbackPreview");Directory.CreateDirectory(folder);
        var camera=new GameObject("PreviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.18f);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(bool victim in new[]{false,true})
        {
            ui.ResetFeedback();status.Present(victim?1:0,0,0);ui.ConfirmZap(victim?2u:1u,1,7,2);
            var rt=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(folder,victim?"B-victim.png":"A-owner.png"),pixels.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        }
        Object.DestroyImmediate(camera.gameObject);
    }
}
#endif
