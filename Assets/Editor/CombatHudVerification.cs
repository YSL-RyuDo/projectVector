#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class CombatHudVerification
{
    static int checks;
    static void Check(bool ok,string what){if(!ok)throw new Exception("COMBAT_HUD_FAIL "+what);checks++;}
    static T Get<T>(UnityEngine.Object o,string f) where T:UnityEngine.Object=>InGameUiAuthoring.Get<T>(o,f);
    public static void Run()
    {
        CombatHudAuthoring.Run();
        InGameUiVerification.VerifySaved();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game_TestVersion.unity");
        T Find<T>() where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).First();
        var hud=Find<InGameUIManager>();var read=Find<CombatHudReadout>();var top=Find<TopInfoUI>();var feed=Find<KillLogManager>();var pick=Find<PickManager>();
        int count=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
        hud.SetHpBarrier(28,76,100,100);Check(read.health.text=="28"&&read.armor.text=="76","HP and armor distinct");Check(read.healthState.text=="체력 위험","low health warning");
        hud.SetHpBarrier(100,25,100,50);Check(Mathf.Abs(Get<Image>(hud,"barrierBar").fillAmount-.5f)<.001f,"light armor uses own maximum");
        hud.SetHpBarrier(0,0,100,100);Check(read.healthState.text=="사망","death health state");
        hud.SetBulletText(0,90);read.PresentReload(true);Check(read.reloadRoot.activeSelf&&read.ammoState.text=="재장전","reload state");read.PresentReload(false);Check(!read.reloadRoot.activeSelf&&read.ammoState.text=="탄창 비어 있음","empty magazine state");
        SkillInventoryState.SetCharacter(pick.GetCharacterData(1));SkillInventoryState.ApplyServerState(15,2);
        foreach(int total in new[]{1,7,12,30}){SkillInventoryState.ApplyUltimateState(total,total,100,false);read.PresentSkills();Check(read.ultimateArc.segments==total&&read.ultimateArc.filled==total,"charge segments follow rules");Check(read.chargeText.text==$"{total} / {total}"&&read.skillStates[3].text=="준비 완료","one readiness label and numeric points");}
        read.PresentSkills(false,true);Check(read.skillStates[0].text=="사용 불가"&&read.skillStates[1].text=="사용 완료","blocked and used skills distinct");
        read.PresentSkills(true);Check(read.chargeText.text=="—"&&read.ultimateArc.filled==0,"spectator does not inherit own ultimate");
        top.ApplyGameStartInfo();top.SetMyTeam(0);top.ApplyRoundRules(12,12);top.SetCurrentRoundRole(12);top.SetCurrentRoundRole(13);Check(Get<TMP_Text>(top,"ourTeamRoleText").text=="수비"&&Get<TMP_Text>(top,"announcementText").text.Contains("공수 교대"),"actual rules side swap");
        top.SetGameDescription("구매 단계");top.SetGameDescription("전투 단계");Check(Get<TMP_Text>(top,"announcementText").text.Contains("구매 종료"),"buy-end announcement");
        top.SetGameDescription("디바이스 접속 중");top.SetTimer(35);Check(Get<TMP_Text>(top,"gameDescriptionText").text=="디바이스 설치됨"&&Get<TMP_Text>(top,"timerText").text=="접속 중","planted state no exact bomb countdown");
        top.SetRoundWinnerText(0);Check(Get<TMP_Text>(top,"gameDescriptionText").text=="라운드 승리","winner relative to viewer");
        top.SetGameDescription("구매 단계");top.SetTimer(25);Check(Get<TMP_Text>(top,"timerText").text=="00:25","new round restores normal timer");
        var pooled=new UnityEditor.SerializedObject(feed).FindProperty("authoredEntries");var rows=Enumerable.Range(0,pooled.arraySize).Select(i=>(KillLogEntryUI)pooled.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        feed.ClearAll();for(uint i=1;i<=7;i++)feed.ShowDeath(i,i+10,true,0,1,"Killer","Victim");Check(rows.Count(r=>r.gameObject.activeSelf)==5,"five saved killfeed slots");
        feed.ApplyDetails(7,17,9,true,"Teammate");Check(Get<Image>(rows[0],"weaponImage").sprite==read.icons.Weapon(9)&&Get<TMP_Text>(rows[0],"headshotText").text=="HS"&&Get<TMP_Text>(rows[0],"assistText").text.Contains("Teammate"),"server fatal weapon headshot assist");
        feed.ShowDeath(30,31,false,1,2,"Caster","Victim");feed.ApplyDetails(30,31,-1,false,"");Check(Get<TMP_Text>(rows[0],"sourceText").text=="스킬"&&!Get<Image>(rows[0],"weaponImage").enabled,"skill kill not held gun");feed.ClearAll();Check(rows.All(r=>!r.gameObject.activeSelf),"round reset clears killfeed");
        byte[] Body(uint k,uint d,int w,byte h,params uint[] assistants){using var m=new MemoryStream();using var b=new BinaryWriter(m);b.Write(k);b.Write(d);b.Write(w);b.Write(h);b.Write((byte)assistants.Length);foreach(uint id in assistants){b.Write(id);b.Write(1);b.Write(System.Text.Encoding.UTF8.GetBytes("Name"));b.Write(new byte[16]);}return m.ToArray();}
        bool Parse(byte[] body)=>NetworkManager.ParseKillFeed(body,out _,out _,out _,out _,out _);
        Check(Parse(Body(1,2,9,1,3))&&Parse(Body(1,2,-1,0)),"detail packets valid");
        foreach(var bad in new[]{Body(0,2,9,0),Body(1,0,9,0),Body(1,2,14,0),Body(1,2,9,2),Body(1,2,9,0,1),Body(1,2,9,0,3,3),Body(1,2,9,0).Take(13).ToArray()})Check(!Parse(bad),"malformed killfeed rejected");
        Check(!VectorVoicePanel.ShouldShowContext("Login",true,true,true,true)&&!VectorVoicePanel.ShouldShowContext("Game_TestVersion",true,false,false,false),"no voice button in login/pick/loading");
        Check(VectorVoicePanel.ShouldShowContext("Game_TestVersion",true,true,false,false)&&VectorVoicePanel.ShouldShowContext("Lobby",true,false,true,false)&&VectorVoicePanel.ShouldShowContext("Lobby",true,false,false,true),"voice in game/party/private room");
        Check(scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length)==count,"state changes create no UI");
        void FitsAtHeights(RectTransform rt,string label)
        {
            foreach(float height in new[]{1080f,1200f,1920f*9/21,1440f}) {
                var size=rt.rect.size;var center=new Vector2(rt.anchorMin.x*1920,rt.anchorMin.y*height)+rt.anchoredPosition;
                Check(center.x-size.x*rt.pivot.x>=0 && center.x+size.x*(1-rt.pivot.x)<=1920 && center.y-size.y*rt.pivot.y>=0 && center.y+size.y*(1-rt.pivot.y)<=height,label+" stays on 16:9 / 16:10 / 21:9 / 4:3 screen");
            }
        }
        FitsAtHeights((RectTransform)Get<Image>(hud,"charImage").transform.parent,"vitals");
        FitsAtHeights((RectTransform)Get<Image>(hud,"cSkillImage").transform.parent,"skills");
        FitsAtHeights((RectTransform)read.ammoRoot.transform,"ammo");
        FitsAtHeights((RectTransform)Get<ScrollRect>(feed,"scrollRect").transform,"feed");
        var mini=Find<MiniMapManager>();Check(Get<RectTransform>(mini,"_miniMapHUD").GetComponentInChildren<Mask>(true).GetComponent<Image>().sprite!=null,"smooth saved circle mask imported");FitsAtHeights(Get<RectTransform>(mini,"_miniMapHUD"),"minimap");
        SkillInventoryState.Reset();File.WriteAllText(CombatHudAuthoring.Output+"/CombatHud-checks.txt","COMBAT_HUD_PASS checks="+checks+"\nHealth, armor, skills, reload, actual round rules, device phase, saved kill feed, protocol validation, voice contexts, no UI generation.\n");
        MinimapHudVerification.Inspect();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);ControllerSmokeInteractionVerification.Verify();
        Debug.Log("COMBAT_HUD_VERIFY_PASS checks="+checks);
    }
}
#endif
