#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class InGameUiVerification
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static T Field<T>(UnityEngine.Object o,string name) where T:UnityEngine.Object=>InGameUiAuthoring.Get<T>(o,name);
    static Rect BoundsIn(Transform target,Transform parent)
    {
        var corners=new Vector3[4];((RectTransform)target).GetWorldCorners(corners);
        var points=corners.Select(parent.InverseTransformPoint).ToArray();
        return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
    }
    static bool Contains(Rect outer,Rect inner)=>inner.xMin>=outer.xMin-.1f && inner.xMax<=outer.xMax+.1f && inner.yMin>=outer.yMin-.1f && inner.yMax<=outer.yMax+.1f;
    static void InvokeAuthoredButton(Button button)
    {
        Check(button.onClick.GetPersistentEventCount()==1,"single saved navigation callback");
        var state=button.onClick.GetPersistentListenerState(0);
        button.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        button.onClick.Invoke();button.onClick.SetPersistentListenerState(0,state);
    }
    static byte[] Packet(uint room,byte team,params (uint id,int primary,int pistol,int knife,byte armor,byte maxArmor,byte points,byte used,byte alive)[] entries)
    {using var ms=new MemoryStream();using var w=new BinaryWriter(ms);w.Write(room);w.Write(team);w.Write((byte)entries.Length);w.Write((byte)7);w.Write((ushort)100);foreach(var e in entries){w.Write(e.id);w.Write(e.primary);w.Write(e.pistol);w.Write(e.knife);w.Write(e.armor);w.Write(e.maxArmor);w.Write(e.points);w.Write(e.used);w.Write(e.alive);}return ms.ToArray();}
    public static void Run()
    {
        InGameUiAuthoring.Run(); VerifySaved();
    }
    public static void VerifySaved()
    {
        checks=0;
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");var roots=scene.GetRootGameObjects();
        var hud=roots.SelectMany(r=>r.GetComponentsInChildren<InGameUIManager>(true)).First();
        var store=roots.SelectMany(r=>r.GetComponentsInChildren<StorePanelUI>(true)).First();
        var board=roots.SelectMany(r=>r.GetComponentsInChildren<InGameScoreboardUI>(true)).First();
        var prompt=roots.SelectMany(r=>r.GetComponentsInChildren<InteractionPromptUI>(true)).First();
        var pick=roots.SelectMany(r=>r.GetComponentsInChildren<PickManager>(true)).First();
        int count=roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Length);
        var catalog=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);
        Check(catalog.armor.All(a=>a && AssetDatabase.Contains(a)),"shield sprite framing persists");
        Check(Field<LoadoutSummaryUI>(store,"loadoutSummary"),"authored store loadout binding");
        var skills=Field<GameObject>(store,"storePanelRoot").GetComponentsInChildren<StoreSkillButton>(true);
        Check(skills.Length==4 && new SerializedObject(store).FindProperty("skillButtons").arraySize==4,"four saved skill purchase buttons");
        SkillInventoryState.Reset();
        foreach(var b in skills){b.Refresh(9000,true);Check(Field<TMP_Text>(b,"keyLabel") && Field<TMP_Text>(b,"priceLabel") && Field<Image>(b,"skillIcon"),"authored skill labels");}
        var ultimate=skills.First(b=>b.Slot==SkillSlot.X);
        Check(!ultimate.GetComponent<Button>().interactable,"unknown points cannot buy");
        for(int character=0;character<4;character++)
        {
            var data=pick.GetCharacterData(character);Check(data!=null,"actual character data "+character);
            SkillInventoryState.SetCharacter(data);
            hud.InitializeLocalPlayerUI(character,data.characterImage,data);
            foreach(var b in skills){b.Refresh(9000,true);var expected=SkillInventoryState.Skill(b.Slot);Check(expected!=null && expected.skillIcon,"character-specific skill asset");Check(Field<Image>(b,"skillIcon").sprite==expected.skillIcon,"shop uses actual skill icon");Check(Field<TMP_Text>(b,"label").text==expected.skillName,"shop uses actual skill name");Check(!AssetDatabase.GetAssetPath(expected.skillIcon).Contains("Resources/Sprites/Skill"),"no legacy placeholder skill");}
            foreach(var b in skills)
            {
                var inner=((RectTransform)b.transform).rect;inner=Rect.MinMaxRect(inner.xMin+10,inner.yMin+10,inner.xMax-10,inner.yMax-10);
                foreach(var graphic in b.GetComponentsInChildren<Graphic>(true).Where(g=>g.transform!=b.transform))
                    Check(Contains(inner,BoundsIn(graphic.transform,b.transform)),"skill artwork/key/text remain inside padded card: "+b.Slot+"/"+graphic.name);
                foreach(string label in new[]{"label","stateLabel","priceLabel"})
                {
                    var text=Field<TMP_Text>(b,label);
                    Check(text.GetPreferredValues(text.text).x<=text.rectTransform.rect.width+.1f,"skill text fits without clipping: "+character+"/"+b.Slot+"/"+label);
                }
            }
        }
        SkillInventoryState.ApplyServerState(5,1);SkillInventoryState.ApplyUltimateState(4,7,100,false);hud.RefreshSkillAvailability();
        Check(SkillInventoryState.PurchasedMask==5 && SkillInventoryState.UsedMask==1,"ultimate keeps normal skill state");
        Check(SkillInventoryState.GetPrice(SkillSlot.X)==300,"actual missing points price");
        ultimate.Refresh(299,true);Check(!ultimate.GetComponent<Button>().interactable,"insufficient credit");ultimate.Refresh(300,true);Check(ultimate.GetComponent<Button>().interactable,"discounted purchase affordable");ultimate.Refresh(9000,false);Check(!ultimate.GetComponent<Button>().interactable,"buy phase closed");
        Check(Field<TMP_Text>(hud,"ultimatePointText").text=="4 / 7" && Mathf.Abs(Field<Image>(hud,"xSkillGauge").fillAmount-4f/7)<.001,"HUD charge amount");
        SkillInventoryState.ApplyUltimateState(7,7,100,false);hud.RefreshSkillAvailability();ultimate.Refresh(9000,true);Check(SkillInventoryState.CanUse(SkillSlot.X) && !ultimate.GetComponent<Button>().interactable,"full charge ready without purchase");Check(Field<TMP_Text>(hud,"ultimatePointText").text=="7 / 7" && hud.GetComponent<CombatHudReadout>().skillStates[3].text=="준비 완료","HUD ready state");
        SkillInventoryState.ApplyUltimateState(0,7,100,true);hud.RefreshSkillAvailability();ultimate.Refresh(9000,true);Check(!SkillInventoryState.CanUse(SkillSlot.X) && !ultimate.GetComponent<Button>().interactable,"used ultimate blocked this round");Check(Field<TMP_Text>(hud,"ultimatePointText").text=="0 / 7" && hud.GetComponent<CombatHudReadout>().skillStates[3].text=="사용 완료","HUD used state");
        SkillInventoryState.ApplyUltimateState(8,7,100,false);Check(SkillInventoryState.UltimatePoints==0 && SkillInventoryState.IsUsed(SkillSlot.X),"invalid state atomic");
        var dataBoard=new ScoreboardData{roomId=7,myTeam=0};for(uint i=1;i<=6;i++)dataBoard.members.Add(new ScoreboardMemberData{playerId=i,team=(byte)(i<=3?0:1),slotIndex=(byte)((i-1)%3)});
        var valid=Packet(7,0,(1,9,0,13,76,100,4,0,1),(2,12,1,13,0,100,7,0,1),(3,-1,0,13,0,0,0,1,0));
        Check(NetworkManager.ApplyTeamLoadout(valid,dataBoard),"server loadout accepted");
        Check(dataBoard.members[0].primaryWeaponId==9 && dataBoard.members[0].armor==76 && dataBoard.members[1].ultimatePoints==7 && dataBoard.members[2].ultimateUsed && !dataBoard.members[2].equipmentAlive,"all loadout fields decoded");
        var invalids=new[]{Packet(8,0,(1,8,0,13,0,0,0,0,1)),Packet(7,1,(1,8,0,13,0,0,0,0,1)),Packet(7,0,(4,8,0,13,0,0,0,0,1)),Packet(7,0,(1,8,0,13,0,0,0,0,1),(1,9,0,13,0,0,0,0,1)),Packet(7,0,(1,0,0,13,0,0,0,0,1)),Packet(7,0,(1,8,0,13,100,50,0,0,1)),Packet(7,0,(1,8,0,13,0,0,8,0,1)),Packet(7,0,(1,8,0,13,0,0,1,2,1)),valid.Take(valid.Length-1).ToArray()};
        foreach(var bad in invalids){Check(!NetworkManager.ApplyTeamLoadout(bad,dataBoard),"reject malformed/foreign snapshot");Check(dataBoard.members[0].primaryWeaponId==9 && dataBoard.members[0].armor==76,"reject without partial mutation");}
        var rows=Field<GameObject>(board,"rootPanel").GetComponentsInChildren<ScoreboardRowUI>(true);
        foreach(var row in rows){row.SetData(dataBoard.members[0],null,1,true);Check(Field<Image>(row,"pistolImage").enabled && Field<Image>(row,"knifeImage").enabled && Field<Image>(row,"armorImage").enabled,"own team full inventory");Check(Field<GameObject>(row,"ultTextObject").GetComponent<TMP_Text>().text=="4 / 7","board ultimate points");row.SetData(dataBoard.members[0],null,1,false);Check(!Field<GameObject>(row,"weaponImageObject").activeSelf && !Field<Image>(row,"pistolImage").enabled && Field<GameObject>(row,"ultTextObject").GetComponent<TMP_Text>().text=="—","enemy private information hidden");row.Clear();Check(!row.gameObject.activeSelf,"clear stale row");}
        Check(NetworkManager.ApplyTeamLoadout(Packet(7,0,(1,-1,0,13,0,0,0,0,1)),dataBoard) && !dataBoard.members[1].hasLoadout,"partial snapshot clears prior members");
        foreach(var action in new[]{("무기 줍기",false),("디바이스 설치",true),("디바이스 해체",true)}){prompt.Show(action.Item1,action.Item2);Check(Field<GameObject>(prompt,"root").activeSelf && Field<TMP_Text>(prompt,"keyText").text=="F","F shown");Check(Field<TMP_Text>(prompt,"actionHint").text==(action.Item2?"길게 누르기":"누르기"),"tap/hold hint");prompt.Hide();Check(!Field<GameObject>(prompt,"root").activeSelf,"hide prompt");}
        var history=Field<RoundHistoryUI>(board,"roundHistoryUI");history.Init(0);
        for(byte round=1;round<=28;round++)history.SetRoundResult(round,(byte)(round%2),RoundWinReason.ATTACK_ELIMINATION,(byte)(round/2),(byte)((round+1)/2));
        var cols=new SerializedObject(history).FindProperty("authoredColumns");Check(cols.arraySize==24,"fixed round history pool");Check(Field<TMP_Text>(history,"currentRoundText").text.Contains("29"),"overtime display advanced");
        var columns=Enumerable.Range(0,cols.arraySize).Select(i=>(RoundHistoryColumnUI)cols.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Check(columns.Count(c=>c.gameObject.activeSelf)==10,"ten round slots per page");
        Check(Field<TMP_Text>(columns[0],"roundNumber").text.StartsWith("21R") && Field<TMP_Text>(columns[8],"currentText").enabled,"latest overtime page and current state");
        Check(Field<TMP_Text>(history,"historyRangeText").text=="21 - 30R","visible round range is explicit");
        InvokeAuthoredButton(Field<Button>(history,"previousPageButton"));Check(history.PageIndex==1,"authored previous button works");
        history.PreviousPage();Check(history.PageIndex==0 && Field<TMP_Text>(columns[0],"winnerText").text=="패","first result is retained after long overtime");
        Check(!Field<Button>(history,"previousPageButton").interactable && Field<Button>(history,"nextPageButton").interactable,"first-page navigation limits");
        InvokeAuthoredButton(Field<Button>(history,"nextPageButton"));Check(history.PageIndex==1,"authored next button works");
        history.ShowLatestPage();Check(history.PageIndex==2 && !Field<Button>(history,"nextPageButton").interactable,"return to latest page");
        Check(history.GetComponent<Image>().color.a>=.95f,"round results protected from bright world background");
        Check(!history.transform.Find("ViewerTeam").gameObject.activeSelf,"unnecessary viewer-team explanation hidden");
        var content=history.transform.parent;
        var historyBounds=BoundsIn(history.transform,content);
        Check(rows.All(row=>!historyBounds.Overlaps(BoundsIn(row.transform,content))),"history does not overlap either team roster");
        foreach(var column in columns.Take(10))
        {
            Check(Contains(((RectTransform)history.transform).rect,BoundsIn(column.transform,history.transform)),"round card stays inside history band");
            foreach(string label in new[]{"roundNumber","winnerText","reasonText"})
            {
                var text=Field<TMP_Text>(column,label);
                Check(text && Contains(((RectTransform)column.transform).rect,BoundsIn(text.transform,column.transform)),"round label stays within its card");
            }
            foreach(string field in new[]{"ourTeamResultIcon","enemyTeamResultIcon"})
            {
                var icon=Field<Image>(column,field);
                Check(icon.transform.parent==column.transform && Contains(((RectTransform)column.transform).rect,BoundsIn(icon.transform,column.transform)),"original result artwork stays inside its round cell");
                Check(icon.rectTransform.rect.width>=56 && icon.material.shader.name=="UI/RoundResultInk","large high-contrast original artwork");
            }
        }
        for(byte viewer=0;viewer<=1;viewer++)
        {
            history.Init(viewer);
            for(byte round=1;round<=12;round++)
            {
                byte winner=(byte)(round%2);bool aAttack=round<=5 || round>=11 && (round-11)%2==0;
                bool attacking=winner==0?aAttack:!aAttack;
                history.SetRoundResult(round,winner,attacking?RoundWinReason.ATTACK_ELIMINATION:RoundWinReason.DEFENSE_ELIMINATION,2,2);
                if(history.PageIndex!=(round-1)/10)history.PreviousPage();
                var column=columns[(round-1)%10];
                Check(Field<TMP_Text>(column,"winnerText").text==(winner==viewer?"승":"패"),"short winner label follows viewer team");
                Check(Field<TMP_Text>(column,"roundNumber").text==round+"R  "+(attacking?"공격":"수비"),"historical role survives side swap and overtime");
                Check(Field<TMP_Text>(column,"reasonText").text=="전멸","win reason shown in text");
                history.ShowLatestPage();
            }
        }
        foreach(var reason in new[]{(RoundWinReason.ATTACK_SPIKE_EXPLODE,"제어권 확보"),(RoundWinReason.DEFENSE_SPIKE_DEFUSE,"디바이스 해체"),(RoundWinReason.DEFENSE_TIME_OUT,"시간 만료")})
        {
            history.SetRoundResult(1,0,reason.Item1,1,0);
            Check(Field<TMP_Text>(columns[0],"reasonText").text==reason.Item2,"device and timeout reasons are readable");
        }
        foreach(bool won in new[]{true,false})
        foreach(var entry in new[]{(RoundWinReason.ATTACK_ELIMINATION,"EliminationIcon"),(RoundWinReason.ATTACK_SPIKE_EXPLODE,"SpikeExplodeIcon"),(RoundWinReason.DEFENSE_SPIKE_DEFUSE,"DefuseIcon"),(RoundWinReason.DEFENSE_TIME_OUT,"TimeOutIcon")})
        {
            history.Init(0);history.SetRoundResult(1,(byte)(won?0:1),entry.Item1,(byte)(won?1:0),(byte)(won?0:1));
            var shown=Field<Image>(columns[0],won?"ourTeamResultIcon":"enemyTeamResultIcon");
            var hidden=Field<Image>(columns[0],won?"enemyTeamResultIcon":"ourTeamResultIcon");
            var expected=Field<Sprite>(history,(won?"ourTeam":"enemyTeam")+entry.Item2);
            Check(expected && shown.sprite==expected && shown.enabled && !hidden.enabled,"correct supplied icon for result and winner");
            Check(AssetDatabase.GetAssetPath(shown.sprite).StartsWith("Assets/ProjectVector/Modules/Match/UI/Sprites/GameBoardIcon/"),"original supplied icon asset retained");
            Check(!Field<TMP_Text>(columns[0],"reasonText").enabled,"icon replaces paragraph of reason text");
        }
        var rulesBody=new byte[8];BitConverter.GetBytes((uint)7).CopyTo(rulesBody,0);BitConverter.GetBytes((ushort)12).CopyTo(rulesBody,4);BitConverter.GetBytes((ushort)12).CopyTo(rulesBody,6);
        Check(NetworkManager.ApplyRoundRules(rulesBody,dataBoard) && dataBoard.firstHalfRounds==12 && dataBoard.secondHalfRounds==12,"server rules decoded without client constant changes");
        foreach(var invalid in new[]{rulesBody.Take(7).ToArray(),new byte[8]})Check(!NetworkManager.ApplyRoundRules(invalid,dataBoard) && dataBoard.firstHalfRounds==12,"bad/foreign rules cannot change current match");
        history.Init(0);history.ApplyRoundRules(dataBoard.firstHalfRounds,dataBoard.secondHalfRounds);
        for(byte round=1;round<=24;round++)history.SetRoundResult(round,(byte)(round%2),RoundWinReason.ATTACK_ELIMINATION,12,12);
        Check(Field<TMP_Text>(history,"currentRoundText").text.Contains("연장 1") && Field<TMP_Text>(history,"currentRoundText").text.Contains("25"),"24-round rules correctly start overtime at 25");
        history.PreviousPage();
        Check(Field<TMP_Text>(columns[1],"roundNumber").text=="12R  공격" && Field<TMP_Text>(columns[2],"roundNumber").text=="13R  공격","12-round side swap follows server rules for both winners");
        history.ShowLatestPage();
        for(byte round=25;round<=120;round++)history.SetRoundResult(round,(byte)(round%2),RoundWinReason.ATTACK_ELIMINATION,60,60);
        Check(history.PageIndex==12 && Field<TMP_Text>(history,"historyRangeText").text=="121 - 130R","long match advances beyond authored slot count");
        for(int page=0;page<12;page++)history.PreviousPage();
        Check(history.PageIndex==0 && Field<TMP_Text>(columns[0],"winnerText").text=="패","round 1 survives after 120 completed rounds");
        for(int page=0;page<12;page++)history.NextPage();
        history.PreviousPage();Check(Field<TMP_Text>(columns[9],"roundNumber").text.StartsWith("120R") && Field<TMP_Text>(columns[9],"winnerText").text=="승","last completed round remains accessible");
        history.ApplyRoundRules(5,5);history.Init(0);
        Check(history.PageIndex==0 && !Field<Image>(columns[0],"ourTeamResultIcon").enabled && !Field<Button>(history,"nextPageButton").interactable,"new match clears results and page selection");
        Check(roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Length)==count,"data updates never construct UI");
        history.Init(1);Check(Field<TMP_Text>(history,"ourTeamScoreText").text=="0" && Field<TMP_Text>(history,"ourTeamRoleText").text=="수비","round reset and team role");
        var storeItems=Field<GameObject>(store,"storePanelRoot").GetComponentsInChildren<StoreItemButton>(true);Check(storeItems.Length==15,"all weapon and shield cards retained");
        foreach(var item in storeItems){item.SetOwned(false);item.SetBlockedByBetterArmor(false);item.Refresh(9000,true);Check(Field<Image>(item,"equipmentImage").sprite==(item.IsArmorButton()?catalog.Armor(item.ItemId):catalog.Weapon(item.ItemId)),"source equipment icon mapping");item.Refresh(9000,false);Check(!item.GetComponent<Button>().interactable,"purchase closed");item.SetOwned(true);item.Refresh(9000,true);Check(!item.GetComponent<Button>().interactable,"owned blocked");}
        var details=Field<GameObject>(store,"storePanelRoot").GetComponentInChildren<StoreWeaponDetailsUI>(true);
        foreach(var item in storeItems){item.OnPointerEnter(null);Check(details.artwork.sprite==(item.IsArmorButton()?catalog.Armor(item.ItemId):catalog.Weapon(item.ItemId)),"hover selects real weapon/shield details");if(item.IsWeaponButton()){var stats=WeaponStoreStats.All[item.ItemId];Check(details.handling.text.Contains(stats.magazine+"발") && details.damageRows.text.Contains(WeaponDamage.Calculate(item.ItemId,stats.damage,0,1).ToString()),"authoritative table fields "+item.ItemId);}}
        details.ShowWeapon(9);Check(details.damageRows.text.Contains("160") && details.damageRows.text.Contains("152") && details.damageRows.text.Contains("144"),"AK head damage at 0/45/60 meters");
        details.ShowWeapon(5);Check(details.damageTitle.text.Contains("산탄 1발") && details.falloffNote.text.Contains("12발"),"shotgun per-pellet explanation");
        details.ShowWeapon(12);Check(details.handling.text.Contains("3.5초") && details.damageRows.text.Contains("300"),"AWM stats");
        var oldStore=StorePanelUI.Instance;var oldBoard=InGameScoreboardUI.Instance;StorePanelUI.Instance=store;InGameScoreboardUI.Instance=board;
        Field<GameObject>(store,"storePanelRoot").SetActive(true);board.Open();Check(!board.IsOpen(),"TAB cannot cover open store");
        Field<GameObject>(store,"storePanelRoot").SetActive(false);board.Open();Check(board.IsOpen(),"TAB works when store closes");store.Open();Check(!board.IsOpen(),"opening store closes TAB");
        Field<GameObject>(store,"storePanelRoot").SetActive(false);StorePanelUI.Instance=oldStore;InGameScoreboardUI.Instance=oldBoard;
        var boardRoot=Field<GameObject>(board,"rootPanel");Check(!boardRoot.transform.Find("ScoreboardContent/Footer").gameObject.activeSelf,"unnecessary privacy footer removed");Check(boardRoot.GetComponent<Image>().color.a==0 && boardRoot.transform.Find("ScoreboardContent").localScale.x<.85f,"compact board without full-screen shade");
        Check(Field<GameObject>(store,"storePanelRoot").GetComponent<Image>().color.a<.25f,"shop world remains visible");
        SkillInventoryState.Reset();Check(!SkillInventoryState.HasUltimateState && SkillInventoryState.Character==null,"new match clear");
        File.WriteAllText(InGameUiAuthoring.Output+"/UI-Verification.txt","INGAME_UI_PASS checks="+checks+"\nSaved bindings, four character skill sets, ultimate states/prices, ally privacy, malformed packets, F prompts, overtime, purchases and no runtime UI creation.");
        Debug.Log("INGAME_UI_PASS checks="+checks);
    }
}
#endif
