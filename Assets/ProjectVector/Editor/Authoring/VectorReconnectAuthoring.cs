using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VectorReconnectAuthoring
{
    [MenuItem("Project Vector/UI/Author Match Reconnect")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        var panel=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MatchLoadingPanelUI>(true)).Single();
        var so=new SerializedObject(panel);
        var root=(GameObject)so.FindProperty("rootPanel").objectReferenceValue;
        var view=panel.GetComponent<VectorReconnectView>();
        if(view==null) view=panel.gameObject.AddComponent<VectorReconnectView>();
        view.root=root; view.loadingPanel=panel; view.presentation=panel.presentation;
        view.backdrop=root.GetComponent<Image>();
        if(view.backdrop==null) throw new Exception("Loading root needs its authored background Image");
        view.overlayCanvas=root.GetComponent<Canvas>();
        if(view.overlayCanvas==null) view.overlayCanvas=root.AddComponent<Canvas>();
        view.overlayCanvas.overrideSorting=false;
        if(root.GetComponent<GraphicRaycaster>()==null) root.AddComponent<GraphicRaycaster>();
        var prior=root.transform.Find("ReconnectActions");
        if(prior!=null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
        var actions=new GameObject("ReconnectActions",typeof(RectTransform)); actions.transform.SetParent(root.transform,false);
        var rect=actions.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);
        rect.pivot=new Vector2(.5f,0); rect.anchoredPosition=new Vector2(0,88); rect.sizeDelta=new Vector2(540,52);
        view.recoveryActions=actions;
        view.retry=MakeButton(rect,"Retry","다시 연결",-140,view.presentation.status.font);
        view.login=MakeButton(rect,"ReturnToLogin","로그인으로",140,view.presentation.status.font);
        actions.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("RECONNECT_HIERARCHY_AUTHORED");
    }
    static Button MakeButton(Transform parent,string name,string text,float x,TMP_FontAsset font)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=new Vector2(250,48);rt.anchoredPosition=new Vector2(x,0);
        go.GetComponent<Image>().color=new Color(.12f,.16f,.21f,1);
        var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)); label.transform.SetParent(rt,false);
        var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
        var tmp=label.GetComponent<TextMeshProUGUI>();tmp.font=font;tmp.text=text;tmp.fontSize=24;tmp.alignment=TextAlignmentOptions.Center;tmp.raycastTarget=false;
        return go.GetComponent<Button>();
    }
}
