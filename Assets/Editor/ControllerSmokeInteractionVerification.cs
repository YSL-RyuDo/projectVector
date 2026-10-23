#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ControllerSmokeInteractionVerification
{
    const BindingFlags Fields=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Fields).SetValue(o,value);
    static void Check(bool value,string label){if(!value)throw new Exception("SMOKE_INPUT_FAIL "+label);Debug.Log("SMOKE_INPUT_CHECK "+label);}
    public static void Verify()
    {
        // Fixtures exercise selection and teardown without fabricating server acceptance.
        var go=new GameObject("SelectionFixture");var skill=go.AddComponent<ControllerSkill_E>();skill.enabled=false;
        var ui=new GameObject("SavedUIFixture",typeof(RectTransform),typeof(Canvas));var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var map=new GameObject("Map",typeof(RectTransform)).GetComponent<RectTransform>();map.SetParent(ui.transform,false);map.sizeDelta=new Vector2(400,400);
        var marker=new GameObject("Marker",typeof(RectTransform)).GetComponent<RectTransform>();marker.SetParent(map,false);var ring=marker.gameObject.AddComponent<SmokeTargetRing>();
        var status=new GameObject("Status",typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();status.transform.SetParent(ui.transform,false);
        var distance=new GameObject("Distance",typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();distance.transform.SetParent(ui.transform,false);
        ui.SetActive(false);var view=ui.AddComponent<SmokeTargetingView>();view.enabled=false;Set(view,"_map",map);Set(view,"_marker",marker);Set(view,"_ring",ring);Set(view,"_status",status);Set(view,"_distance",distance);ui.SetActive(true);
        var camera=new GameObject("MapCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=20;camera.transform.SetPositionAndRotation(new Vector3(2000,50,2000),Quaternion.Euler(90,0,0));
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(2000,-.5f,2000);floor.transform.localScale=new Vector3(100,1,100);Physics.SyncTransforms();
        Set(skill,"_selecting",true);Set(skill,"_mapRect",map);Set(skill,"_miniMapCamera",camera);Set(skill,"_view",view);Set(skill,"_miniMapOverlay",ui);
        Canvas.ForceUpdateCanvases();var point=RectTransformUtility.WorldToScreenPoint(null,map.position);
        int children=ui.GetComponentsInChildren<Transform>(true).Length;
        Check(skill.PreviewAt(point),"hover previews valid ground before selecting");
        var initial=marker.anchoredPosition;
        Check(skill.PreviewAt(point+Vector2.right*35)&&marker.anchoredPosition.x>initial.x,"hover footprint follows pointer");
        Check(!(bool)typeof(ControllerSkill_E).GetField("_selected",Fields).GetValue(skill),"hover does not arm confirmation");
        Check(!skill.PreviewAt(point+Vector2.one*1000)&&!marker.gameObject.activeSelf,"leaving map hides unselected preview");
        var actor=GameObject.CreatePrimitive(PrimitiveType.Cube);actor.name="HiddenEnemyOverGround";actor.transform.position=new Vector3(2000,3,2000);actor.transform.localScale=Vector3.one*3;actor.AddComponent<PlayerIdentity>();Physics.SyncTransforms();
        Check(skill.PreviewAt(point),"hidden actor does not block ground preview");
        Check(skill.SelectAt(point),"valid ground can be selected");Check(marker.gameObject.activeSelf&&marker.sizeDelta.y>0,"saved footprint becomes visible");
        var selected=(Vector3)typeof(ControllerSkill_E).GetField("_selectedWorld",Fields).GetValue(skill);
        Check(Mathf.Abs(selected.y)<.01f,"drop hits ground rather than hidden actor head");
        var locked=marker.anchoredPosition;
        Check(!skill.PreviewAt(point+Vector2.right*35)&&marker.anchoredPosition==locked,"left-click selection stays locked while pointer moves");
        Check(skill.SelectAt(point+Vector2.right*35)&&marker.anchoredPosition!=locked,"second left click reselects location");
        Object.DestroyImmediate(actor);
        Check(!skill.SelectAt(point+Vector2.one*1000),"outside-map click ignored");
        floor.SetActive(false);Physics.SyncTransforms();Check(!skill.SelectAt(point),"empty sky is not a valid drop");
        Check(!marker.gameObject.activeSelf,"invalid selection clears stale target");
        Check(children==ui.GetComponentsInChildren<Transform>(true).Length,"no runtime UI construction on repeated selection");
        VerifyMapHeading(skill,map,camera);
        Set(skill,"_ownsInput",true);
        foreach(var property in new[]{"IsSelectingSmoke","IsSmokeMapOpen","IsBlockingWeaponInput"})typeof(ControllerSkill_E).GetField("<"+property+">k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,true);
        var remote=new GameObject("RemoteSkill").AddComponent<ControllerSkill_E>();remote.enabled=false;
        Check(ControllerSkill_E.IsBlockingWeaponInput,"remote disable does not clear local state");
        skill.ForceCloseMiniMapImmediate();Check(!ControllerSkill_E.IsBlockingWeaponInput&&!ControllerSkill_E.IsSmokeMapOpen&&!ControllerSkill_E.IsSelectingSmoke,"death/round teardown clears input lock");
        Check(!ui.activeSelf,"teardown hides targeting UI");
        Object.DestroyImmediate(remote.gameObject);Object.DestroyImmediate(go);Object.DestroyImmediate(ui);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(floor);
        VerifyPlacementMapMarkers();
        var effect=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerSmokeAuthoring.Dir+"/ControllerSmoke.prefab"));
        effect.transform.position=new Vector3(3000,1,0);effect.transform.localScale=Vector3.one*4.5f;
        effect.GetComponent<ControllerSmokeVfx>().Begin(2);effect.GetComponent<ControllerSmokeVfx>().Advance(1.8f);
        Check(SmokeOutlineOccluder.BlocksOutline(new Vector3(3000,1,-10),new Bounds(new Vector3(3000,1,10),Vector3.one)),"new smoke stays occluding during expiry presentation");
        effect.GetComponent<ControllerSmokeVfx>().Dissolve();
        Check(!SmokeOutlineOccluder.BlocksOutline(new Vector3(3000,1,-10),new Bounds(new Vector3(3000,1,10),Vector3.one)),"server destroy removes concealment before visual residue");
        effect.SetActive(false);Check(!SmokeOutlineOccluder.BlocksOutline(new Vector3(3000,1,-10),new Bounds(new Vector3(3000,1,10),Vector3.one)),"new smoke disable removes occlusion");Object.DestroyImmediate(effect);
        Debug.Log("CONTROLLER_SMOKE_INPUT_PASS");
    }
    static void VerifyMapHeading(ControllerSkill_E skill,RectTransform map,Camera camera)
    {
        var texture=new RenderTexture(256,256,24);camera.targetTexture=texture;
        var controller=camera.gameObject.AddComponent<MiniMapCamera>();controller.enabled=false;
        controller.SetTarget(skill.transform);
        foreach(string mapName in new[]{"TestMap","Desert"})
        {
            var level=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/"+mapName+".prefab"));
            level.transform.position=new Vector3(6000,0,6000);
            var refs=level.GetComponentInChildren<MiniMapWorldReferences>(true);
            var center=(Transform)typeof(MiniMapWorldReferences).GetField("fullMapCenter",Fields).GetValue(refs);
            float size=(float)typeof(MiniMapWorldReferences).GetField("fullMapSize",Fields).GetValue(refs);
            controller.SetOverlayMapCenter(center);controller.SetMapFraming(20,size,50,150);
            var info=level.GetComponent<MapInfo>();info.SetBarriersActive(false);Physics.SyncTransforms();
            foreach(byte team in new byte[]{0,1})
            {
                controller.ClearSmokeHeading();controller.SetLocalRoundRole(team);controller.SetOverlayMode(true);
                var corners=new Vector3[4];int corner=0;
                foreach(float x in new[]{0f,1f})foreach(float y in new[]{0f,1f})corners[corner++]=camera.ViewportPointToRay(new Vector3(x,y,0)).origin;
                foreach(float angle in new[]{0f,15f,30f,45f,89.5f,90f,135f,180f,225f,270f,315f,359f})
                {
                    controller.SetOverlayMode(false);skill.transform.rotation=Quaternion.Euler(0,angle,0);
                    controller.SetSmokeHeading(angle);controller.SetOverlayMode(true);
                    var forward=Quaternion.Euler(0,angle,0)*Vector3.forward;
                    Check(Vector3.Dot(camera.transform.up,forward)>.99999f,mapName+" team="+team+" yaw="+angle+" facing-up without snapping");
                    foreach(var p in corners){var uv=camera.WorldToViewportPoint(p);Check(uv.x>=-.0001f&&uv.x<=1.0001f&&uv.y>=-.0001f&&uv.y<=1.0001f,"rotated map retains original corners");}
                    var spawn=team==0?info.attackSpawnPoints[0]:info.defendSpawnPoints[0];
                    var uvGround=camera.WorldToViewportPoint(spawn.position);
                    var ray=camera.ViewportPointToRay(uvGround);
                    Check(Physics.Raycast(ray,out var ground,1000,~0,QueryTriggerInteraction.Ignore),"map targeting ground exists");
                    var local=Rect.NormalizedToPoint(map.rect,new Vector2(uvGround.x,uvGround.y));
                    var screen=RectTransformUtility.WorldToScreenPoint(null,map.TransformPoint(local));
                    Check(skill.SelectAt(screen),"rotated map click accepted");
                    var selected=(Vector3)typeof(ControllerSkill_E).GetField("_selectedWorld",Fields).GetValue(skill);
                    Check(Vector3.Distance(selected,ground.point)<.03f,"rotated click preserves world drop position");
                    skill.transform.rotation=Quaternion.Euler(0,angle+31,0);
                    typeof(MiniMapCamera).GetMethod("ApplyCameraTransform",Fields).Invoke(controller,null);
                    Check(Vector3.Dot(camera.transform.up,forward)>.99999f,"opened heading remains fixed");
                }
                controller.ClearSmokeHeading();controller.SetOverlayMode(true);
                var ordinary=Quaternion.Euler(0,team==0?90:-90,0)*Vector3.forward;
                Check(Vector3.Dot(camera.transform.up,ordinary)>.99999f,"ordinary team map heading restored");
                controller.SetOverlayMode(false);
                Check(Vector3.Dot(camera.transform.up,skill.transform.forward)>.99999f,"HUD resumes player heading");
            }
            Object.DestroyImmediate(level);
        }
        Object.DestroyImmediate(controller);camera.targetTexture=null;Object.DestroyImmediate(texture);
        Debug.Log("SMOKE_HEADING_PASS maps=2 teams=2 angles=12");
    }
    static void VerifyPlacementMapMarkers()
    {
        var holder=new GameObject("MapPrivacyFixture");
        var manager=holder.AddComponent<MiniMapManager>();manager.enabled=false;
        var local=holder.AddComponent<PlayerIdentity>();local.PlayerId=1;local.Team=0;
        var camObject=new GameObject("PrivacyMapCamera");camObject.transform.SetParent(holder.transform);
        var cam=camObject.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=20;
        cam.transform.SetPositionAndRotation(new Vector3(4000,50,4000),Quaternion.Euler(90,0,0));
        Set(manager,"_miniMapCamera",cam);Set(manager,"_localPlayerCamera",cam);Set(manager,"_localPlayerIdentity",local);Set(manager,"_localPlayerTransform",holder.transform);
        var panel=new GameObject("Overlay",typeof(RectTransform)).GetComponent<RectTransform>();panel.SetParent(holder.transform);Set(manager,"_miniMapOverlay",panel);
        var list=(System.Collections.Generic.List<MiniMapEnemy>)typeof(MiniMapManager).GetField("_registeredPlayers",Fields).GetValue(manager);
        var hud=(System.Collections.Generic.Dictionary<MiniMapEnemy,RectTransform>)typeof(MiniMapManager).GetField("_hudPlayerMarkers",Fields).GetValue(manager);
        var overlay=(System.Collections.Generic.Dictionary<MiniMapEnemy,RectTransform>)typeof(MiniMapManager).GetField("_overlayPlayerMarkers",Fields).GetValue(manager);
        for(int i=0;i<2;i++)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.SetActive(false);obj.transform.SetParent(holder.transform);obj.transform.position=new Vector3(4000+i*4,1,4000);
            var identity=obj.AddComponent<PlayerIdentity>();identity.PlayerId=(uint)(i+2);identity.Team=(byte)i;
            var entry=obj.AddComponent<MiniMapEnemy>();entry.enabled=false;Set(entry,"_identity",identity);Set(entry,"_enemyCollider",obj.GetComponent<Collider>());obj.SetActive(true);list.Add(entry);
            foreach(var dict in new[]{hud,overlay}){var marker=new GameObject("ExistingMarker",typeof(RectTransform)).GetComponent<RectTransform>();marker.SetParent(panel);dict[entry]=marker;}
        }
        Physics.SyncTransforms();
        var flag=typeof(ControllerSkill_E).GetField("<IsSelectingSmoke>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static);
        var refresh=typeof(MiniMapManager).GetMethod("UpdateRegisteredPlayerMarkers",Fields);
        flag.SetValue(null,true);manager.ShowOverlayMiniMap();
        Check(!overlay[list[1]].gameObject.activeSelf,"opening placement map immediately hides existing enemy icon");
        refresh.Invoke(manager,null);
        Check(overlay[list[0]].gameObject.activeSelf&&!overlay[list[1]].gameObject.activeSelf,"placement map keeps ally and hides even visible enemy");
        flag.SetValue(null,false);refresh.Invoke(manager,null);
        Check(overlay[list[1]].gameObject.activeSelf,"ordinary minimap enemy discovery restored after skill closes");
        Object.DestroyImmediate(holder);
    }
}
#endif
