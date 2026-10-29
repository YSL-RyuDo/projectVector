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

public static class EquipmentUIVerification
{
    static int checks;
    static void Check(bool ok,string message) {if (!ok) throw new Exception(message);checks++;}
    static T Field<T>(UnityEngine.Object o,string name) where T:UnityEngine.Object => (T)new SerializedObject(o).FindProperty(name).objectReferenceValue;
    static byte[] Packet(uint room,byte team, params (uint id,int weapon,byte alive)[] entries)
    {
        using var ms=new MemoryStream();using var w=new BinaryWriter(ms);
        w.Write(room);w.Write(team);w.Write((byte)entries.Length);
        foreach(var e in entries){w.Write(e.id);w.Write(e.weapon);w.Write(e.alive);}return ms.ToArray();
    }
    public static void Run()
    {
        EquipmentIconsAuthoring.Run();
        var scene=EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var roots=scene.GetRootGameObjects();
        var catalog=AssetDatabase.LoadAssetAtPath<EquipmentIconCatalog>(EquipmentIconsAuthoring.CatalogPath);
        Check(catalog.weapons.Length==14 && catalog.weapons.Distinct().Count()==14,"14 distinct gun/knife sprites");
        for(int i=0;i<14;i++) Check(AssetDatabase.GetAssetPath(catalog.Weapon(i)).EndsWith("Weapon_"+i+".png"),"weapon mapping "+i);
        Check(!catalog.Weapon(-1) && !catalog.Weapon(14),"invalid icon ids");
        var items=roots.SelectMany(r=>r.GetComponentsInChildren<StoreItemButton>(true)).ToArray();
        Check(items.Length==15,"all existing store cards retained");
        foreach(var item in items)
        {
            Check(!item.transform.Find("AKMImage"),"no legacy overlay "+item.ItemId);
            var icon=Field<Image>(item,"equipmentImage");
            Check(Field<EquipmentIconCatalog>(item,"equipmentIcons")==catalog,"persistent store catalog binding");
            item.SetOwned(false);item.SetBlockedByBetterArmor(false);item.Refresh(9000,true);
            Check(icon.sprite==(item.IsArmorButton()?catalog.Armor(item.ItemId):catalog.Weapon(item.ItemId)),"store sprite "+item.ItemId);
            var button=Field<Button>(item,"button");Check(button && button.interactable==new SerializedObject(item).FindProperty("purchasable").boolValue,"buy eligibility "+item.ItemId);
            item.Refresh(0,false);Check(!button.interactable,"closed buy phase");
            item.SetOwned(true);item.Refresh(9000,true);Check(!button.interactable,"already owned");
            item.SetOwned(false);item.Refresh(9000,true);
            if(item.IsArmorButton()){item.SetBlockedByBetterArmor(true);Check(!button.interactable,"better armor blocks");item.SetBlockedByBetterArmor(false);}
            var name=Field<TMP_Text>(item,"itemNameText");var price=Field<TMP_Text>(item,"priceText");
            Check(name.rectTransform.anchoredPosition.y+15 <= icon.rectTransform.anchoredPosition.y-icon.rectTransform.rect.height*.5f,"name below icon");
            Check(price.rectTransform.anchoredPosition.y-price.rectTransform.rect.height*.5f>=-((RectTransform)item.transform).rect.height*.5f,"price within card");
        }
        var hud=roots.SelectMany(r=>r.GetComponentsInChildren<InGameUIManager>(true)).First();
        Check(Field<EquipmentIconCatalog>(hud,"equipmentIcons")==catalog,"persistent HUD catalog binding");
        Check(!Field<Image>(hud,"throwWeaponImage"),"retired throwable row removed");
        var bg=new SerializedObject(hud).FindProperty("weaponSlotBackgrounds");
        for(int i=0;i<4;i++)
        {
            var row=(Image)bg.GetArrayElementAtIndex(i).objectReferenceValue;
            Check(row.rectTransform.sizeDelta==new Vector2(256,66) && row.rectTransform.anchoredPosition==new Vector2(-22,54+i*78),"equal slot spacing "+i);
            Check(row.GetComponentInChildren<TMP_Text>(true).text==(i+1).ToString(),"visible slot number "+i);
        }
        hud.SetSpikePossession(true);Check(Field<GameObject>(hud,"spikeSlotRoot").activeSelf && Field<Image>(hud,"spikeImage").sprite==catalog.device,"device possession shows fourth slot");
        hud.SetSpikePossession(false);Check(!Field<GameObject>(hud,"spikeSlotRoot").activeSelf,"drop/plant hides device row");
        var weapons=AssetDatabase.FindAssets("t:WeaponData").Select(g=>AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).Where(x=>x && x.weaponId>=0 && x.weaponId<=13).GroupBy(w=>w.weaponId).ToDictionary(g=>g.Key,g=>g.First());
        int hierarchyCount=hud.transform.root.GetComponentsInChildren<Transform>(true).Length;
        for(int i=0;i<14;i++)
        {
            var data=weapons[i];Image icon;
            if(i==13){hud.SetKnifeData(data);icon=Field<Image>(hud,"knifeImage");}
            else if(i<=2){hud.SetSubWeaponData(data);icon=Field<Image>(hud,"subWeaponImage");}
            else{hud.SetMainWeapon(data);icon=Field<Image>(hud,"mainWeaponImage");}
            Check(icon.enabled && icon.sprite==catalog.Weapon(i) && icon.preserveAspect,"HUD server inventory "+i);
        }
        hud.SetMainWeapon(null);hud.SetSubWeapon(null);hud.SetKnife(null);
        Check(!Field<Image>(hud,"mainWeaponImage").enabled && !Field<Image>(hud,"subWeaponImage").enabled && !Field<Image>(hud,"knifeImage").enabled,"clear slots on loss/reset");
        var originalKnife=catalog.weapons[13];catalog.weapons[13]=catalog.weapons[0];hud.SetKnifeData(weapons[13]);
        Check(Field<Image>(hud,"knifeImage").sprite==catalog.weapons[0],"configurable knife icon");catalog.weapons[13]=originalKnife;
        Check(hierarchyCount==hud.transform.root.GetComponentsInChildren<Transform>(true).Length,"updates do not construct UI");
        VerifyInventory(hud,weapons,catalog);

        var board=new ScoreboardData{roomId=7,myTeam=0};
        for(uint i=1;i<=6;i++)board.members.Add(new ScoreboardMemberData{playerId=i,team=(byte)(i<=3?0:1)});
        Check(NetworkManager.ApplyTeamEquipment(Packet(7,0,(1,9,1),(2,12,1),(3,13,0)),board),"accept allied snapshot");
        Check(board.members[0].ownedWeaponId==9 && !board.members[2].equipmentAlive && !board.members[3].hasEquipment,"snapshot data");
        foreach(var bad in new[]{Packet(8,0,(1,0,1)),Packet(7,1,(1,0,1)),Packet(7,0,(4,0,1)),Packet(7,0,(1,0,1),(1,1,1)),Packet(7,0,(1,14,1)),Packet(7,0,(1,0,2)),new byte[5]})
        {Check(!NetworkManager.ApplyTeamEquipment(bad,board),"reject malformed/foreign equipment");Check(board.members[0].ownedWeaponId==9,"rejection leaves snapshot unchanged");}
        Check(NetworkManager.ApplyTeamEquipment(Packet(7,0,(1,-1,1)),board) && !board.members[1].hasEquipment,"empty and partial snapshot clears stale rows");
        var rows=roots.SelectMany(r=>r.GetComponentsInChildren<ScoreboardRowUI>(true)).ToArray();
        var sample=new ScoreboardMemberData{playerId=1,nickname="LYS",hasEquipment=true,ownedWeaponId=9,equipmentAlive=true};
        foreach(var row in rows)
        {
            Check(Field<EquipmentIconCatalog>(row,"equipmentIcons")==catalog,"persistent scoreboard catalog binding");
            row.SetData(sample,null,1,true);var icon=Field<GameObject>(row,"weaponImageObject");
            Check(icon.activeSelf && icon.GetComponent<Image>().sprite==catalog.Weapon(9),"ally visible");
            row.SetData(sample,null,1,false);Check(!icon.activeSelf,"enemy hidden even with data");
            row.Clear();Check(!icon.activeSelf,"clear row");
        }
        string output=Environment.GetEnvironmentVariable("VECTOR_EQUIPMENT_OUTPUT");
        var store=roots.SelectMany(r=>r.GetComponentsInChildren<StorePanelUI>(true)).First();
        Field<TMP_Text>(store,"playerNicknameText").text="LYS";Field<TMP_Text>(store,"playerCreditText").text="9,000";
        var storeSO=new SerializedObject(store);Field<Image>(store,"playerCharacterImage").sprite=(Sprite)storeSO.FindProperty("characterSprites").GetArrayElementAtIndex(0).objectReferenceValue;
        foreach(var item in items)item.SetOwned(item.IsWeaponButton() && item.ItemId==0);
        EquipmentIconsAuthoring.Render(Field<GameObject>(store,"storePanelRoot"),Path.Combine(output,"Store-Final.png"));
        hud.SetMainWeapon(weapons[9]);hud.SetSubWeaponData(weapons[2]);hud.SetKnifeData(weapons[13]);
        hud.SetSpikePossession(true);hud.SetSelectedWeaponSlot(0);
        var weaponRoot=Field<Image>(hud,"mainWeaponImage").transform.parent.parent.gameObject;
        EquipmentIconsAuthoring.Render(weaponRoot,Path.Combine(output,"HUD-AK.png"),null,new Rect(1600,10,320,335));
        hud.SetMainWeapon(weapons[12]);hud.SetSubWeaponData(weapons[0]);
        EquipmentIconsAuthoring.Render(weaponRoot,Path.Combine(output,"HUD-AWM.png"),null,new Rect(1600,10,320,335));
        hud.SetSelectedWeaponSlot(4);
        EquipmentIconsAuthoring.Render(weaponRoot,Path.Combine(output,"HUD-Device.png"),null,new Rect(1600,10,320,335));
        hud.SetSpikePossession(false);
        EquipmentIconsAuthoring.Render(weaponRoot,Path.Combine(output,"HUD-NoDevice.png"),null,new Rect(1600,10,320,335));
        var sb=roots.SelectMany(r=>r.GetComponentsInChildren<InGameScoreboardUI>(true)).First();
        EquipmentIconsAuthoring.Render(Field<GameObject>(sb,"rootPanel"),Path.Combine(output,"Scoreboard-Final.png"),clone=>{
            var cloneRows=clone.GetComponentsInChildren<ScoreboardRowUI>(true);
            var sprites=new SerializedObject(sb).FindProperty("characterSprites");
            int i=0;foreach(var row in cloneRows){sample.playerId=(uint)(i+1);sample.credits=2300;sample.kills=4+i;sample.deaths=2;sample.assists=1;sample.ownedWeaponId=new[]{9,12,13}[i%3];sample.nickname=new[]{"LYS","KSW","CJU"}[i%3];row.SetData(sample,(Sprite)sprites.GetArrayElementAtIndex(i%4).objectReferenceValue,1,i<3);i++;}
        });
        Check(!ShaderUtil.ShaderHasError(Shader.Find("UI/EquipmentInk")),"equipment shader compiled");
        Debug.Log("EQUIPMENT_UI_PASS checks="+checks);
    }

    static void VerifyInventory(InGameUIManager hud,System.Collections.Generic.Dictionary<int,WeaponData> data,EquipmentIconCatalog catalog)
    {
        var actor=new GameObject("Equipment inventory verification");var remote=new GameObject("Remote inventory verification");
        var previous=InGameUIManager.Instance;InGameUIManager.Instance=hud;
        try
        {
            actor.AddComponent<PlayerIdentity>();actor.AddComponent<PracticePlayerContext>();
            var ps=actor.AddComponent<PlayerSpike>();var inventory=actor.AddComponent<PlayerItem>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(PlayerItem).GetField("playerSpike",flags).SetValue(inventory,ps);
            for(int slot=0;slot<3;slot++)
            {
                var go=new GameObject("Owned slot "+slot);go.transform.SetParent(actor.transform);
                var w=go.AddComponent<Weapon>();var so=new SerializedObject(w);so.FindProperty("weaponData").objectReferenceValue=data[new[]{9,2,13}[slot]];so.ApplyModifiedPropertiesWithoutUndo();
                inventory.Items[slot]=w;
            }
            var device=new GameObject("Owned device");device.transform.SetParent(actor.transform);inventory.Items[4]=device.AddComponent<Spike>();
            inventory.RefreshLocalInventoryHUD();
            Check(Field<Image>(hud,"mainWeaponImage").sprite==catalog.Weapon(9) && Field<Image>(hud,"subWeaponImage").sprite==catalog.Weapon(2),"real inventory refresh fills owned slots");
            remote.AddComponent<PlayerItem>().RefreshLocalInventoryHUD();
            Check(Field<Image>(hud,"mainWeaponImage").sprite==catalog.Weapon(9),"remote inventory cannot overwrite local HUD");
            var change=typeof(PlayerItem).GetMethod("Switch",flags);
            change.Invoke(inventory,new object[]{0});ps.hasSpike=false;change.Invoke(inventory,new object[]{4});
            Check(inventory.CurrentIndex==0,"unowned device selection rejected");
            ps.hasSpike=true;change.Invoke(inventory,new object[]{4});
            Check(inventory.CurrentIndex==4 && inventory.Items[4].gameObject.activeSelf,"device selection equips device instead of throwable");
            Check(Field<Image>(hud,"mainWeaponImage").sprite==catalog.Weapon(9),"holding device preserves owned primary icon");
            inventory.Items[0]=null;inventory.RefreshLocalInventoryHUD();
            Check(!Field<Image>(hud,"mainWeaponImage").enabled,"lost primary clears inventory HUD");
        }
        finally {UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(remote);InGameUIManager.Instance=previous;}
    }
}
#endif
