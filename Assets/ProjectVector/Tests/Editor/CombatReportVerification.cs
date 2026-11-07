#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CombatReportVerification
{
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static byte[] Page(uint request=9,uint room=42,uint life=7,ushort page=0,ushort total=2)
    {
        var b=new byte[508];void U(int at,uint value)=>BitConverter.GetBytes(value).CopyTo(b,at);void H(int at,ushort value)=>BitConverter.GetBytes(value).CopyTo(b,at);
        U(0,request);b[4]=1;b[6]=1;b[7]=(byte)Math.Min(8,total-page*8);U(8,room);U(12,life);U(16,101);H(20,page);H(22,(ushort)Math.Max(1,(total+7)/8));H(24,total);
        for(int i=0;i<b[7];i++){int at=28+i*60;U(at,101);U(at+4,0);Packet.WriteFixedString(b,at+8,"KSW",20);b[at+29]=(byte)(i%2);U(at+32,(uint)(i+page*8));U(at+36,20);U(at+44,1);}
        return b;
    }
    static CombatReportAssembly Assembly(){var a=new CombatReportAssembly();a.Begin(42);a.Expect(9);return a;}
    public static void Run()
    {
        checks=0;Directory.CreateDirectory(CombatReportAuthoring.Output);
        EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var report=UnityEngine.Object.FindFirstObjectByType<ReportUI>(FindObjectsInactive.Include);
        Check(report!=null,"existing author report retained");
        var root=ShopReadabilityAuthoring.Get<GameObject>(report,"rootPanel");Check(!root.activeSelf,"report hidden in saved scene");
        Check(report.detailRows.Length==4 && report.detailRows.All(r=>r),"four saved detail rows");
        Check(report.presentation && report.presentation.gameObject==root,"saved report visibility binding");
        var spectator=UnityEngine.Object.FindFirstObjectByType<SpectatorHUD>(FindObjectsInactive.Include);
        Check(spectator && spectator.presentation && spectator.presentation.gameObject==spectator.root,"saved spectator visibility binding");
        var a=Assembly();Check(a.Accept(Page())==CombatReportAssembly.Result.Complete && a.Entries.Count==2,"wire layout and complete");
        Check(a.Accept(Page())==CombatReportAssembly.Result.Ignored && a.Entries.Count==2,"duplicate cannot double count");
        for(int n=0;n<508;n++)Check(Assembly().Accept(Page().Take(n).ToArray())==CombatReportAssembly.Result.Ignored,"truncated packet "+n);
        Check(Assembly().Accept(Page().Concat(new byte[]{0}).ToArray())==CombatReportAssembly.Result.Ignored,"oversized packet");
        Check(Assembly().Accept(Page(request:8))==CombatReportAssembly.Result.Ignored,"stale request");
        Check(Assembly().Accept(Page(room:43))==CombatReportAssembly.Result.Ignored,"wrong room");
        Check(Assembly().Accept(Page(life:0))==CombatReportAssembly.Result.Ignored,"invalid life");
        a=Assembly();Check(a.Accept(Page(total:10))==CombatReportAssembly.Result.NextPage,"multi page starts");a.Expect(10);
        Check(a.Accept(Page(request:10,life:8,page:1,total:10))==CombatReportAssembly.Result.Ignored,"wrong life on second page");
        Check(a.Accept(Page(request:10,page:0,total:10))==CombatReportAssembly.Result.Ignored,"wrong sequence");
        Check(a.Accept(Page(request:10,page:1,total:11))==CombatReportAssembly.Result.Ignored,"inconsistent total");
        Check(a.Accept(Page(request:10,page:1,total:10))==CombatReportAssembly.Result.Complete && a.Entries.Count==10,"all pages exactly once");
        a.Reset();Check(a.Accept(Page(request:10,page:1,total:10))==CombatReportAssembly.Result.Ignored && a.Entries.Count==0,"round/respawn cancels pending response");
        var bad=Page();bad[28+28]=2;Check(Assembly().Accept(bad)==CombatReportAssembly.Result.Ignored,"unknown damage source");
        bad=Page();bad[28+29]=2;Check(Assembly().Accept(bad)==CombatReportAssembly.Result.Ignored,"unknown damage direction");
        bad=Page();bad[7]=9;Check(Assembly().Accept(bad)==CombatReportAssembly.Result.Ignored,"oversized entry count");
        bad=Page();bad[5]=1;Check(Assembly().Accept(bad)==CombatReportAssembly.Result.Failed,"unavailable response");
        a=Assembly();Check(a.Accept(Page(total:0))==CombatReportAssembly.Result.Complete && a.Entries.Count==0,"empty death snapshot");
        var fixture=Path.Combine(CombatReportAuthoring.Output,"ServerData/combat-report.bin");
        Check(File.Exists(fixture),"real server wire fixture exists");
        var wire=File.ReadAllBytes(fixture);var body=wire.Skip(4).ToArray();a=new CombatReportAssembly();a.Begin(BitConverter.ToUInt32(body,8));a.Expect(BitConverter.ToUInt32(body,0));
        Check(a.Accept(body)==CombatReportAssembly.Result.Complete,"real dispatcher response parsed in Unity");
        report.Open();report.ApplyData(a.Entries,a.Killer,a.Limited);
        Check(report.Summaries.Count==1 && report.Summaries[0].damageDealt==20 && report.Summaries[0].damageTaken==130,"actual HP/shield applied damage, no overkill");
        Check(report.Summaries[0].hitsTakenHead==2 && report.Summaries[0].hitsTakenBody==3 && report.Summaries[0].hitsTakenLeg==1,"actual server region counts");
        Check(!report.IsExpanded,"opens in summary");report.ToggleExpand();Check(report.IsExpanded,"expand detail");
        Check(report.detailRows.Count(r=>r.gameObject.activeSelf)==2,"one given and one received weapon line");
        Check(report.detailRows[0].directionText.text=="준 피해" && report.detailRows[1].directionText.text=="받은 피해","directions visible on detail rows");
        var synthetic=a.Entries.Select(e=>e).ToList();
        for(int i=0;i<7;i++)synthetic.Add(new CombatReportRawEntry{opponentId=444,nickname="다른 상대",sourceKind=1,sourceId=i+1,direction=1,hpDamage=3,unlocalizedHits=1});
        report.ApplyData(synthetic,a.Killer,false);report.SelectOpponent(1);
        Check(report.Summaries[report.OpponentIndex].damageTaken==21 && report.Summaries[report.OpponentIndex].hitsTakenBody==0,"skill events are not body hits");
        report.ChangePage(1);Check(report.DetailPage==1 && report.detailRows.Count(r=>r.gameObject.activeSelf)==3,"saved row paging exposes all details");
        int children=root.GetComponentsInChildren<Transform>(true).Length;
        for(int i=0;i<10;i++){report.SelectOpponent(1);report.ToggleExpand();}
        Check(children==root.GetComponentsInChildren<Transform>(true).Length,"no runtime row creation or deletion");
        int before=report.OpponentIndex;report.SetPresentationSuppressed(true);
        Check(report.presentation.alpha==0 && !report.presentation.blocksRaycasts,"report is actually hidden under overlay");
        report.SetPresentationSuppressed(false);
        Check(report.presentation.alpha==1,"report returns after overlay");
        Check(report.OpponentIndex==before && report.Summaries.Count==2,"TAB/settings preserves report");
        report.CloseAndReset();report.ApplyData(a.Entries,a.Killer,false);report.ShowFailed();
        Check(!report.IsVisible && report.Summaries.Count==0,"late data or error cannot reopen after respawn");
        report.Open();report.ShowFailed();Check(report.retryButton.gameObject.activeSelf,"failure has retry");
        report.Open();report.ApplyData(new List<CombatReportRawEntry>(),0,false);
        Check(ShopReadabilityAuthoring.Get<GameObject>(report,"emptyNotice").activeSelf,"empty record notice");
        Capture(report,a);
        File.WriteAllText(CombatReportAuthoring.Output+"/Verification.txt","REPORT_INTEGRATION_PASS checks="+checks+"\nReal server response, privacy contract, region/skill separation, pagination, stale/reset, saved layout, display bounds, observer states.\n");
        Debug.Log("REPORT_INTEGRATION_PASS checks="+checks);
    }
    static void Capture(ReportUI report,CombatReportAssembly data)
    {
        var spectate=UnityEngine.Object.FindFirstObjectByType<SpectatorHUD>(FindObjectsInactive.Include);
        var target=new ScoreboardMemberData{playerId=2,nickname="KSW",characterId=1,hasLoadout=true,ultimatePoints=4,ultimateRequired=7};
        var snapshot=new PlayerCombatSnapshot{hp=74,barrier=25,maxHp=100,maxBarrier=50,weaponId=9,magAmmo=17,reserveAmmo=75};
        report.Open();report.ApplyData(data.Entries,data.Killer,false);
        spectate.Present(target,snapshot,2,false);Check(spectate.playerName.text=="KSW" && spectate.controls.text.Contains("다음"),"target and controls");
        spectate.Present(null,null,0,false);Check(spectate.playerName.text=="생존한 아군 없음" && !spectate.weapon.enabled,"no living targets");
        spectate.Present(target,snapshot,1,true);Check(spectate.state.text=="드론 시점" && spectate.controls.text=="","one target drone state");
        spectate.Present(target,snapshot,2,false);
        var map=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectVector/Shared/Maps/Prefabs/TestMap.prefab"));
        foreach(var t in map.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;
        var spawn=map.GetComponent<MapInfo>().attackSpawnPoints[0];
        var light=new GameObject("CaptureLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-25,0);RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
        Action<Camera> world=c=>{c.cullingMask|=1<<28;c.transform.SetPositionAndRotation(spawn.position+Vector3.up*1.5f,spawn.rotation);c.fieldOfView=65;c.backgroundColor=new Color(.45f,.56f,.68f);};
        // Capture actual saved objects with the server fixture. No UI image mockup.
        var wrapper=new GameObject("Report capture",typeof(RectTransform));var root=ShopReadabilityAuthoring.Get<GameObject>(report,"rootPanel");
        var hud=UnityEngine.Object.FindFirstObjectByType<InGameUIManager>(FindObjectsInactive.Include);
        var pick=UnityEngine.Object.FindFirstObjectByType<PickManager>(FindObjectsInactive.Include);
        hud.InitializeLocalPlayerUI(1,pick.GetCharacterData(1).characterImage,pick.GetCharacterData(1));
        hud.SetHpBarrier(74,25,100,50);hud.SetBulletText(17,75);hud.SetSpikePossession(false);hud.SetSelectedWeaponSlot(0);
        var icons=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);
        foreach(var pair in new[]{("mainWeaponImage",9),("subWeaponImage",0),("knifeImage",13)}) {
            var image=ShopReadabilityAuthoring.Get<Image>(hud,pair.Item1);image.sprite=icons.Weapon(pair.Item2);image.enabled=true;
        }
        var read=hud.GetComponent<CombatHudReadout>();read.PresentSkills(true);read.PresentReload(false);
        var top=UnityEngine.Object.FindFirstObjectByType<TopInfoUI>(FindObjectsInactive.Include);
        top.ApplyGameStartInfo();top.SetScoreByTeam(3,2);top.SetGameDescription("전투 단계");top.SetCurrentRoundRole(4);top.SetTimer(48);
        ShopReadabilityAuthoring.Get<TMP_Text>(top,"announcementText").gameObject.SetActive(false);
        var canvas=top.GetComponentInParent<Canvas>();
        UnityEngine.Object.FindFirstObjectByType<CombatFeedbackUI>(FindObjectsInactive.Include)?.ResetAll();
        UnityEngine.Object.FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include)?.Hide();
        var match=UnityEngine.Object.FindFirstObjectByType<MatchManager>(FindObjectsInactive.Include);
        foreach(string field in new[]{"ourTeamImages","enemyTeamImages"}) {
            var property=new SerializedObject(match).FindProperty(field);
            for(int i=0;i<property.arraySize;i++) {
                var image=(Image)property.GetArrayElementAtIndex(i).objectReferenceValue;
                image.sprite=pick.GetCharacterData(i%4).characterImage;image.enabled=true;image.color=Color.white;
                image.GetComponent<TeamLifeBadge>()?.Present(i==2);
            }
        }
        ShopReadabilityAuthoring.Get<GameObject>(match,"ourTeamListRoot").SetActive(true);
        ShopReadabilityAuthoring.Get<GameObject>(match,"enemyTeamListRoot").SetActive(true);
        var gameCopy=UnityEngine.Object.Instantiate(canvas.transform.Find("GameUIs").gameObject,wrapper.transform,false);gameCopy.SetActive(true);
        var topCopy=UnityEngine.Object.Instantiate(top.gameObject,wrapper.transform,false);topCopy.SetActive(true);
        foreach(var board in gameCopy.GetComponentsInChildren<InGameScoreboardUI>(true))ShopReadabilityAuthoring.Get<GameObject>(board,"rootPanel").SetActive(false);
        foreach(var store in gameCopy.GetComponentsInChildren<StorePanelUI>(true))ShopReadabilityAuthoring.Get<GameObject>(store,"storePanelRoot").SetActive(false);
        foreach(var chat in gameCopy.GetComponentsInChildren<InGameChatUI>(true)) {
            ShopReadabilityAuthoring.Get<GameObject>(chat,"inputRoot").SetActive(false);
            ShopReadabilityAuthoring.Get<CanvasGroup>(chat,"messageCanvasGroup").alpha=0;
        }
        void Frame(string name,bool expanded) {
            if(report.IsExpanded!=expanded)report.ToggleExpand();
            var reportCopy=UnityEngine.Object.Instantiate(root,wrapper.transform,false);reportCopy.SetActive(true);
            var spectateCopy=UnityEngine.Object.Instantiate(spectate.root,wrapper.transform,false);spectateCopy.SetActive(true);
            EquipmentIconsAuthoring.Render(wrapper,CombatReportAuthoring.Output+"/"+name+".png",clone=>{
                Canvas.ForceUpdateCanvases();
                Rect Bounds(Transform t){var corners=new Vector3[4];((RectTransform)t).GetWorldCorners(corners);var points=corners.Select(clone.transform.InverseTransformPoint).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));}
                var card=clone.transform.Find(spectate.root.name+"(Clone)");var cardBounds=Bounds(card);
                var gameRoot=clone.transform.Find(gameCopy.name);Check(gameRoot,"full HUD present in spectator layout verification");
                Transform Copied(Transform original)=>gameRoot.Find(AnimationUtility.CalculateTransformPath(original,canvas.transform.Find("GameUIs")));
                Check(!cardBounds.Overlaps(Bounds(Copied(ShopReadabilityAuthoring.Get<Image>(hud,"cSkillImage").transform.parent))),"spectator card clear of entire skill row");
                Check(!cardBounds.Overlaps(Bounds(Copied(read.reloadRoot.transform))),"spectator card clear of reload notice");
                foreach(var child in card.GetComponentsInChildren<RectTransform>(true)){
                    var bounds=Bounds(child);
                    Check(bounds.xMin>=cardBounds.xMin-.01f && bounds.yMin>=cardBounds.yMin-.01f && bounds.xMax<=cardBounds.xMax+.01f && bounds.yMax<=cardBounds.yMax+.01f,"spectator child contained: "+child.name);
                }
                var glyphs=new List<(TMP_Text text,Rect rect)>();
                foreach(var t in clone.GetComponentsInChildren<TMP_Text>(true)) {
                    if(!t.gameObject.activeInHierarchy)continue;
                    t.ForceMeshUpdate(true);
                    if(t.transform.IsChildOf(clone.transform.Find(root.name+"(Clone)")) || t.transform.IsChildOf(clone.transform.Find(spectate.root.name+"(Clone)"))) {
                        Check(!t.isTextOverflowing,"text fits: "+t.name);
                        if(!string.IsNullOrWhiteSpace(t.text)){
                            Vector3 min=clone.transform.InverseTransformPoint(t.transform.TransformPoint(t.textBounds.min)),max=clone.transform.InverseTransformPoint(t.transform.TransformPoint(t.textBounds.max));
                            glyphs.Add((t,new Rect(min.x,min.y,max.x-min.x,max.y-min.y)));
                        }
                    }
                }
                for(int i=0;i<glyphs.Count;i++)for(int j=i+1;j<glyphs.Count;j++)
                    Check(!glyphs[i].rect.Overlaps(glyphs[j].rect),"overlapping glyphs: "+glyphs[i].text.name+" "+glyphs[i].rect+" / "+glyphs[j].text.name+" "+glyphs[j].rect);
            },null,world);
            UnityEngine.Object.DestroyImmediate(reportCopy);UnityEngine.Object.DestroyImmediate(spectateCopy);
        }
        Frame("Report-Summary",false);Frame("Report-Detail",true);
        spectate.Present(null,null,0,false);Frame("Report-NoAllies",true);
        UnityEngine.Object.DestroyImmediate(wrapper);UnityEngine.Object.DestroyImmediate(map);UnityEngine.Object.DestroyImmediate(light.gameObject);
    }
}
#endif
