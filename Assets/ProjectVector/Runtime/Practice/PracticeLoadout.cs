using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// All actors, buttons and layout are authored in WeaponTest before Play.
public sealed class PracticeLoadout : MonoBehaviour
{
    public PracticeRangeController range;
    public PracticePlayerContext[] players;
    public string[] characterNames;
    public WeaponData[] weapons;
    public int[] serverDamage;
    public Button[] characterButtons, weaponButtons;
    public TMP_Text state, targetState, feedback;
    public TMP_Text timedLabel;
    public GameObject panel;
    public int selectedCharacter;
    public bool Admitted { get; private set; }
    public bool Switching { get; private set; }
    public bool autoReload;
    public bool randomTarget;
    private int equippedIndex;

    public int DamageFor(WeaponData data)
    {
        if (data == null) return 0;
        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i] == data && i < serverDamage.Length) return serverDamage[i];
        return data.damage;
    }
    public void BeginSelection()
    {
        Admitted = false;
        for (int i=0;i<weapons.Length;i++) if (weapons[i]==range.rifle) equippedIndex=i;
        range.spawner.StopTraining(); range.OpenMenu(); panel.SetActive(true); Refresh();
    }
    public void SelectCharacter(int index)
    {
        if (Switching || index < 0 || index >= players.Length) return;
        StartCoroutine(ChangeCharacter(index));
    }
    private IEnumerator ChangeCharacter(int index)
    {
        Switching = true;
        range.spawner.StopTraining();
        range.inventory.CancelReload(false);
        var previous = range.inventory.GetCurrentWeapon()?.Data;
        if (previous != null) for (int i=0;i<weapons.Length;i++) if (weapons[i]==previous) equippedIndex=i;
        foreach (var rig in players) rig.gameObject.SetActive(false);
        var context=players[index]; selectedCharacter=index;
        range.player=context.transform; range.inventory=context.GetComponent<PlayerItem>();
        context.gameObject.SetActive(true);
        yield return null; // Existing prefab Awake/Start initialize holders and camera first.
        range.inventory.InitializeForMatch();
        context.GetComponent<PlayerMovement>().SetLocalPlayer(true);
        context.GetComponent<PlayerCameraController>().SetLocalPlayer(true);
        range.spawner.trackingCamera=context.GetComponent<PracticeWeaponInput>().aimCamera;
        range.ReturnToStation();
        EquipWeapon(equippedIndex);
        Switching=false; range.OpenMenu(); Refresh();
    }
    public void EquipWeapon(int index)
    {
        if (index<0 || index>=weapons.Length || weapons[index]==null) return;
        equippedIndex=index; range.spawner.StopTraining(); range.inventory.CancelReload(false);
        var data=weapons[index];
        range.inventory.ApplyPickedWeaponFromServer(data,data.magSize,data.maxReserveAmmo);
        Refresh();
    }
    public void Confirm()
    {
        if (Switching) return;
        Admitted=true; range.Retry();
    }
    public void Refill()
    {
        if(range.spawner.Running && !range.infiniteAmmo) range.InvalidateRecord();
        range.inventory.CancelReload(false);
        foreach (var item in range.inventory.Items) if(item!=null) item.GetComponent<Weapon>()?.ReloadFull();
        Refresh();
    }
    public void ResetTargets() { range.spawner.StopTraining(); if(Admitted) range.Retry(); }
    public void ReturnToLine() { range.spawner.StopTraining(); range.ReturnToStation(); range.OpenMenu(); }
    public void ToggleAutoReload() { range.spawner.StopTraining(); autoReload=!autoReload; Refresh(); }
    public void ToggleHealth() { range.spawner.StopTraining(); range.spawner.realisticHealth=!range.spawner.realisticHealth; Refresh(); }
    public void CycleArmor() { range.spawner.StopTraining(); range.spawner.targetArmor=(range.spawner.targetArmor+50)%150; Refresh(); }
    public void CycleTarget()
    {
        range.spawner.StopTraining();
        if(randomTarget){randomTarget=false; range.spawner.targetCharacterIndex=0;}
        else if(++range.spawner.targetCharacterIndex>=players.Length){range.spawner.targetCharacterIndex=0;randomTarget=true;}
        range.spawner.randomCharacter=randomTarget; Refresh();
    }
    public void CycleSpeed() { range.spawner.StopTraining(); range.spawner.strafeSpeed=range.spawner.strafeSpeed>=4?1.5f:range.spawner.strafeSpeed+1.25f; Refresh(); }
    public void CycleDuration() { range.spawner.StopTraining(); range.spawner.timeLimit=range.spawner.timeLimit==30?60:range.spawner.timeLimit==60?120:30; Refresh(); }
    private void Update()
    {
        if (!Admitted || Switching) return;
        if (!range.MenuOpen)
        {
            if(Keyboard.current?.f4Key.wasPressedThisFrame==true) ResetTargets();
            if(Keyboard.current?.f5Key.wasPressedThisFrame==true) Refill();
            var weapon=range.inventory.GetCurrentWeapon();
            if(autoReload && !range.infiniteAmmo && !range.inventory.IsReloading && weapon!=null && weapon.CurrentAmmo==0 && weapon.CanReloadNow())
                range.inventory.SendMessage("TryStartReload",SendMessageOptions.DontRequireReceiver);
        }
        Refresh();
    }
    public void Refresh()
    {
        if(state==null || range.inventory==null) return;
        var weapon=range.inventory.GetCurrentWeapon();
        if(timedLabel!=null)timedLabel.text=$"{range.spawner.timeLimit:0}초 도전";
        state.text=$"{characterNames[selectedCharacter]}  /  {weapon?.Data?.name ?? "무기 선택"}\n{(Switching?"캐릭터 교체 중":Admitted?"설정 후 훈련 시작을 눌러주세요":"캐릭터와 무기를 선택하고 훈련을 시작하세요")}";
        targetState.text=$"표적  {(randomTarget?"무작위":characterNames[range.spawner.targetCharacterIndex])}  ·  {(range.spawner.realisticHealth?$"체력 100 / 보호막 {range.spawner.targetArmor}":"한 번 명중")}\n이동 {range.spawner.strafeSpeed:0.0} m/s  ·  도전 {range.spawner.timeLimit:0}초  ·  자동 재장전 {(autoReload?"켜짐":"꺼짐")}";
        if(feedback!=null)
        {
            var sp=range.spawner;
            feedback.text=sp.Running && sp.mode!=3 ? $"{(sp.realisticHealth?$"표적  HP {sp.TargetHP}  /  보호막 {sp.TargetBarrier}":"한 번 명중 훈련")}\n최근 {sp.LastRegion} · 피해 {sp.LastDamage}  /  누적 {sp.TotalDamage}\n처치 시간 {sp.LastKillTime:0.00}초  ·  F4 초기화 / F5 보충" : "";
        }
        for(int i=0;i<characterButtons.Length;i++) characterButtons[i].image.color=i==selectedCharacter?new Color32(176,123,44,255):new Color32(58,63,56,255);
        for(int i=0;i<weaponButtons.Length;i++) weaponButtons[i].image.color=weapon?.Data==weapons[i]?new Color32(176,123,44,255):new Color32(58,63,56,255);
    }
}
