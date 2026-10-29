#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MinimapHudVerification
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,Fields).GetValue(o);
    static void Call(object o,string m)=>o.GetType().GetMethod(m,Fields).Invoke(o,null);
    static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
    public static void Inspect()
    {
        var log=new StringBuilder();
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var saved=UnityEngine.Object.FindFirstObjectByType<MiniMapCamera>(FindObjectsInactive.Include);
        var prototype=UnityEngine.Object.Instantiate(saved.gameObject);
        foreach(string name in new[]{"TestMap","Desert"})
        {
            var map=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/"+name+".prefab"));
            map.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var refs=map.GetComponentInChildren<MiniMapWorldReferences>(true);var info=map.GetComponent<MapInfo>();
            var cam=prototype.GetComponent<Camera>();var controller=prototype.GetComponent<MiniMapCamera>();
            MiniMapCamera.Instance=null;Call(controller,"Awake");
            controller.SetOverlayMapCenter(Get<Transform>(refs,"fullMapCenter"));
            controller.SetMapFraming(Get<float>(refs,"hudSize"),Get<float>(refs,"fullMapSize"),Get<float>(refs,"hudHeight"),Get<float>(refs,"fullMapHeight"));
            var geometry=map.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
            log.AppendLine(name+" map "+map.transform.position+" meshes "+geometry.Length+" layer counts "+string.Join(",",geometry.GroupBy(r=>r.gameObject.layer).Select(g=>g.Key+":"+g.Count())));
            foreach(var r in geometry.OrderByDescending(r=>r.bounds.size.sqrMagnitude).Take(12))log.AppendLine(r.name+" layer="+r.gameObject.layer+" center="+r.bounds.center+" size="+r.bounds.size);
            var light=new GameObject("Minimap QA Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(55,-25,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
            controller.SetTarget(info.attackSpawnPoints[0]);controller.SetOverlayMode(false);
            var view=new GameObject("Independent view").AddComponent<Camera>();view.enabled=false;controller.SetViewCamera(view);
            foreach(var spawn in info.attackSpawnPoints.Concat(info.defendSpawnPoints))
            foreach(float yaw in new[]{0f,45f,90f,135f,180f,225f,270f,315f})
            {
                view.transform.SetPositionAndRotation(spawn.position+Vector3.up*1.5f,Quaternion.Euler(35,yaw,0));controller.SetTarget(spawn);Call(controller,"LateUpdate");
                Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                var centerUV=cam.WorldToViewportPoint(spawn.position);var aheadUV=cam.WorldToViewportPoint(spawn.position+forward*10);
                Check(Mathf.Abs(centerUV.x-.5f)<.001f&&Mathf.Abs(centerUV.y-.5f)<.001f,name+" player centered");
                Check(Mathf.Abs(aheadUV.x-.5f)<.001f&&aheadUV.y>.6f,name+" view forward must be map up at "+yaw);
                Check(aheadUV.z>cam.nearClipPlane&&aheadUV.z<cam.farClipPlane,name+" geometry inside clipping range");
            }
            log.AppendLine("PASS "+name+": 6 spawn positions x 8 independent camera headings; forward projects UP, centered, inside clipping range.");
            controller.SetTarget(info.attackSpawnPoints[0]);view.transform.rotation=info.attackSpawnPoints[0].rotation;Call(controller,"LateUpdate");
            Capture(cam,CombatHudAuthoring.Output+"/"+name+"-actual-minimap.png");
            log.AppendLine("camera "+cam.transform.position+" yaw "+cam.transform.eulerAngles+" size "+cam.orthographicSize+" clips "+cam.nearClipPlane+","+cam.farClipPlane+" mask "+cam.cullingMask);
            controller.SetOverlayMode(true);Capture(cam,CombatHudAuthoring.Output+"/"+name+"-actual-fullmap.png");
            controller.SetSmokeHeading(37);float before=cam.transform.eulerAngles.y;view.transform.rotation=Quaternion.Euler(0,190,0);Call(controller,"LateUpdate");
            Check(Mathf.Abs(Mathf.DeltaAngle(before,cam.transform.eulerAngles.y))<.01f,"smoke cast heading remains locked");
            controller.SetOverlayMode(false);Check(Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.y,190))<.01f,"HUD resumes current view heading");
            UnityEngine.Object.DestroyImmediate(view.gameObject);UnityEngine.Object.DestroyImmediate(map);UnityEngine.Object.DestroyImmediate(light.gameObject);
        }
        UnityEngine.Object.DestroyImmediate(prototype);File.WriteAllText(CombatHudAuthoring.Output+"/Minimap-inspect.txt",log.ToString());
    }
    public static void Capture(Camera cam,string path)
    {
        var previous=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(768,768,24);var tex=new Texture2D(768,768,TextureFormat.RGB24,false);
        cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
        cam.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
    }
}
#endif
