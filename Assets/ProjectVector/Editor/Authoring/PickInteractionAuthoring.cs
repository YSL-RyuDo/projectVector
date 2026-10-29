#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PickInteractionAuthoring
{
    static readonly Color Ink = new Color32(22, 31, 42, 245), Cyan = new Color32(135, 222, 240, 255), Gold = new Color32(246, 209, 111, 255);
    static TMP_FontAsset font;
    static T Get<T>(Object o, string field) where T : Object => new SerializedObject(o).FindProperty(field).objectReferenceValue as T;
    static void Link(Object o, string f, Object v) { var s = new SerializedObject(o); s.FindProperty(f).objectReferenceValue = v; s.ApplyModifiedPropertiesWithoutUndo(); }
    static T Ensure<T>(GameObject g) where T : Component => g.GetComponent<T>() ?? g.AddComponent<T>();
    static void Pose(Transform t, Transform parent, float x, float y, float w, float h)
    {
        if(t.parent!=parent)t.SetParent(parent, false); var r = (RectTransform)t;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one;
    }
    static void InRow(Transform t, Transform row, float x, float y, float w, float h)
    {
        var p=(RectTransform)t.parent;var local=p.InverseTransformPoint(row.TransformPoint(new Vector3(x,-y,0)));
        Pose(t,p,local.x-p.rect.xMin,p.rect.yMax-local.y,w,h);
    }
    static GameObject Node(Transform p, string n, float x, float y, float w, float h)
    {
        var t = p.Find(n); var g = t ? t.gameObject : new GameObject(n, typeof(RectTransform)); Pose(g.transform, p, x, y, w, h); g.layer = 5; return g;
    }
    static Image Box(Transform p, string n, float x, float y, float w, float h, Color c)
    { var i = Ensure<Image>(Node(p, n, x, y, w, h)); i.color = c; i.raycastTarget = false; return i; }
    static TMP_Text Text(Transform p, string n, string value, float x, float y, float w, float h, float size, Color color)
    {
        var t = Ensure<TextMeshProUGUI>(Node(p, n, x, y, w, h)); t.font = font; t.fontSize = size; t.color = color; t.text = value;
        t.alignment = TextAlignmentOptions.MidlineLeft; t.enableAutoSizing = false; t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis; t.raycastTarget = false; return t;
    }
    static Button Button(Transform p, string n, string text, float x, float y, float w, Color color)
    {
        var i = Box(p, n, x, y, w, 36, color); i.raycastTarget = true; var b = Ensure<Button>(i.gameObject); b.targetGraphic = i;
        Text(i.transform, "Label", text, 0, 0, w, 36, 18, Color.white).alignment = TextAlignmentOptions.Center; return b;
    }
    static Sprite Icon(string name, bool exchange)
    {
        string dir = "Assets/ProjectVector/Modules/Match/UI/PickActions"; Directory.CreateDirectory(dir);
        const int n = 96; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false); var pixels = new Color[n*n];
        void Line(Vector2 a, Vector2 b, float width = 5)
        {
            for (int y=0;y<n;y++) for (int x=0;x<n;x++) {
                Vector2 p=new Vector2(x+.5f,y+.5f), d=b-a; float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
                float alpha=Mathf.Clamp01(width*.5f+.75f-Vector2.Distance(p,a+t*d));
                int k=y*n+x;pixels[k]=new Color(1,1,1,Mathf.Max(pixels[k].a,alpha));
            }
        }
        if(exchange) {
            Line(new Vector2(16,63),new Vector2(78,63));Line(new Vector2(63,78),new Vector2(78,63));Line(new Vector2(63,48),new Vector2(78,63));
            Line(new Vector2(80,31),new Vector2(18,31));Line(new Vector2(33,46),new Vector2(18,31));Line(new Vector2(33,16),new Vector2(18,31));
        } else {
            Vector2[] shield={new(22,79),new(74,79),new(74,42),new(65,25),new(48,14),new(31,25),new(22,42),new(22,79)};
            for(int i=1;i<shield.Length;i++)Line(shield[i-1],shield[i],4);
            Vector2 prev=default,first=default;
            for(int i=0;i<=10;i++){float a=(90+i*36)*Mathf.Deg2Rad;Vector2 v=new Vector2(48,52)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?18:8);if(i==0)first=v;else Line(prev,i==10?first:v,3.5f);prev=v;}
        }
        tex.SetPixels(pixels);tex.Apply();string path=dir+"/"+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static void BuildEvidence() { Run(); RankedCaptureTestEditor.Build(); }
    public static void Run()
    {
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
        var exchange=Icon("Exchange",true);var badge=Icon("RecordBadge",false);
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var pick=Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var parent=Get<GameObject>(pick,"characterInfoPanel").transform.parent;
        var view=Ensure<PickInteractionView>(parent.gameObject);Link(pick,"interactionView",view);
        var slots=new SerializedObject(pick).FindProperty("teamPickSlots");
        for(int i=0;i<slots.arraySize;i++) {
            var slot=(TeamPickSlotUI)slots.GetArrayElementAtIndex(i).objectReferenceValue;var row=slot.transform.parent;
            var name=Get<TMP_Text>(slot,"nicknameText");InRow(name.transform,row,96,10,232,34);name.fontSize=24;name.overflowMode=TextOverflowModes.Ellipsis;
            var status=row.Find("PickStatus");Pose(status,row,96,45,232,25);status.GetComponent<TMP_Text>().fontSize=17;
            foreach(string field in new[]{"changeButton","shareButton"}) {
                bool swap=field=="changeButton";var b=Get<Button>(slot,field);InRow(b.transform,row,224,76,104,28);
                var img=Ensure<Image>(b.gameObject);img.sprite=null;img.color=swap?new Color32(40,70,84,255):new Color32(81,67,35,255);img.raycastTarget=true;b.targetGraphic=img;b.transition=Selectable.Transition.ColorTint;b.spriteState=default;
                var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.25f,1.25f,1.25f);colors.disabledColor=new Color(.48f,.48f,.48f,1);b.colors=colors;
                var icon=Box(b.transform,"ActionIcon",7,3,22,22,swap?Cyan:Gold);icon.sprite=swap?exchange:badge;icon.preserveAspect=true;
                Text(b.transform,"ActionLabel",swap?"교환":"전적 자랑",35,0,65,28,15,Color.white);
                var navigation=b.navigation;navigation.mode=Navigation.Mode.None;b.navigation=navigation;b.gameObject.SetActive(false);
            }
            foreach(var component in row.GetComponentsInChildren<Component>(true))
                if(component && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        var panel=Box(parent,"PickExchange",48,620,340,222,Ink).transform;view.exchangePanel=panel.gameObject;
        Box(panel,"Accent",0,0,4,222,Cyan);view.exchangeTitle=Text(panel,"Title","캐릭터 교환 요청",18,12,250,32,23,Color.white);
        view.countdown=Text(panel,"Countdown","8초",282,12,42,32,19,Cyan);view.countdown.alignment=TextAlignmentOptions.MidlineRight;
        view.myPortrait=Box(panel,"MyCharacter",76,55,62,62,Color.white);view.myPortrait.preserveAspect=true;
        view.peerPortrait=Box(panel,"OtherCharacter",202,55,62,62,Color.white);view.peerPortrait.preserveAspect=true;
        var arrow=Box(panel,"Direction",153,69,34,34,Cyan);arrow.sprite=exchange;
        view.exchangeNames=Text(panel,"Names","",18,124,304,27,17,Color.white);view.exchangeNames.alignment=TextAlignmentOptions.Center;
        view.acceptButton=Button(panel,"Accept","수락",176,168,146,new Color32(45,104,119,255));
        view.declineButton=Button(panel,"Decline","거절",18,168,146,new Color32(56,65,77,255));
        view.cancelButton=Button(panel,"Cancel","요청 취소",18,168,304,new Color32(56,65,77,255));
        var notice=Box(parent,"PickNotice",430,178,620,98,Ink).transform;view.noticePanel=notice.gameObject;
        Box(notice,"Accent",0,0,4,98,Gold);view.noticePortrait=Box(notice,"Portrait",18,17,64,64,Color.white);view.noticePortrait.preserveAspect=true;
        view.noticeTitle=Text(notice,"Title","",98,14,504,30,23,Gold);view.noticeText=Text(notice,"Message","",98,49,504,34,19,Color.white);
        view.Hide();EditorUtility.SetDirty(view);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        VerifyAndCapture(pick,view,parent.gameObject);
        Debug.Log("PICK_ACTION_AUTHORING_PASS");
    }
    static void VerifyAndCapture(PickManager pick,PickInteractionView view,GameObject panel)
    {
        string output=Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT");if(string.IsNullOrEmpty(output))throw new Exception("Missing output");Directory.CreateDirectory(output);
        var state=new PickStateData{roomId=77,mapName="TestMap",remainingSeconds=15};
        string[] names={"LYS","KSW","CJU"};for(int i=0;i<3;i++)state.members.Add(new PickTeamMemberData{playerId=(uint)(i+1),slotIndex=(byte)i,nickname=names[i],isLocked=true,isMe=i==0,lockedCharacterId=i});
        pick.OnRecvPickState(state);
        var actions=new PickActionsState{roomId=77,revision=2,open=true};for(int i=0;i<3;i++)actions.members.Add(new PickActionMember{playerId=(uint)(i+1),characterId=i,locked=true});pick.OnRecvPickActions(actions);
        void Frame(string name) => EquipmentIconsAuthoring.Render(panel,Path.Combine(output,name+".png"),clone=>{
            foreach(var text in clone.GetComponentsInChildren<TMP_Text>()) {
                bool ours=text.transform.IsChildOf(clone.transform.Find("PickExchange")) || text.transform.IsChildOf(clone.transform.Find("PickNotice")) || text.name=="ActionLabel";
                text.ForceMeshUpdate();if(ours && text.isTextOverflowing)throw new Exception("Pick text overflow: "+text.name+" "+text.text);
            }
        });
        Frame("Pick-Actions");
        actions.revision++;actions.members[0].peerId=2;actions.members[1].peerId=1;actions.members[0].exchangeId=actions.members[1].exchangeId=99;
        actions.members[1].requester=true;actions.members[0].remainingMs=actions.members[1].remainingMs=8000;pick.OnRecvPickActions(actions);Frame("Pick-Exchange");
        view.SetExchange(false,false,null,null,"",0);pick.OnPickShowcase(77,1,0,20,12,1,14);Frame("Pick-Showcase");
        pick.OnPickActionResult(77,5);Frame("Pick-Private-Notice");
        // Preview fixtures are never saved to the scene or career data.
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var saved=Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        var savedSlots=new SerializedObject(saved).FindProperty("teamPickSlots");
        for(int i=0;i<savedSlots.arraySize;i++)foreach(string field in new[]{"changeButton","shareButton"}) {
            var button=Get<Button>(savedSlots.GetArrayElementAtIndex(i).objectReferenceValue,field);
            var surface=button.GetComponent<Image>();
            if(surface.sprite!=null || surface.color.maxColorComponent>.45f && surface.color.r>.45f || button.transition!=Selectable.Transition.ColorTint)
                throw new Exception("Pick action appearance was not saved: "+field);
        }
    }
}
#endif
