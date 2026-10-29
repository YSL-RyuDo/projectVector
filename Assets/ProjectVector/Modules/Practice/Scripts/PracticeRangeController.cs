using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PracticeRangeController : MonoBehaviour
{
    public EnemySpawner spawner;
    public PlayerItem inventory;
    public Transform player, originalStation, distanceStation;
    public GameObject menu;
    public TMP_Text title, stats, result, selection;
    public TMP_Text options, consoleStatus;
    public Transform console;
    public GameObject peekCover;
    public Button[] modeButtons, distanceButtons;
    public PracticeTrainingZone[] trainingZones;
    public int activeZone;
    public bool HasCampus => trainingZones != null && trainingZones.Length > 0;
    public bool infiniteAmmo = true;
    public WeaponData rifle, pistol;
    public bool persistRecords = true;
    public PracticeLoadout loadout;
    public bool MenuOpen => menu != null && menu.activeSelf;
    private string accountKey;
    private string lastDifficulty = "Normal";
    private string runKey;
    private WeaponData runWeapon;
    private bool recordEligible;
    private static readonly string[] Modes = { "반응 사격", "고정 사격", "이동 사격", "지속 추적", "엄폐 사격" };
    private System.Collections.IEnumerator Start()
    {
        accountKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(NetworkManager.Instance?.MyUserId ?? "local"));
        spawner.Ended += OnEnded; spawner.Began += OnBegan;
        yield return null;
        if (rifle != null) inventory.EquipPrimaryWeapon(rifle);
        if (HasCampus) ApplyZoneForMode(spawner.mode);
        if(loadout!=null) loadout.BeginSelection(); else FreePractice();
        Refresh();
    }
    private void OnDestroy() { if (spawner != null) { spawner.Ended -= OnEnded; spawner.Began -= OnBegan; } }
    private void Update()
    {
        if (HasCampus && !MenuOpen) CheckWalkInZone();
        if(spawner.Running && inventory.GetCurrentWeapon()?.Data != runWeapon) recordEligible = false;
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { if (MenuOpen) Resume(); else OpenMenu(); }
        if (Keyboard.current?.f3Key.wasPressedThisFrame == true) { if (MenuOpen) Resume(); else OpenMenu(); }
        if (!MenuOpen && Keyboard.current?.fKey.wasPressedThisFrame == true && console != null && Vector3.Distance(player.position, console.position) < 3f) OpenMenu();
        Refresh();
    }
    public void OpenMenu() { menu.SetActive(true); spawner.Paused = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    public void Resume() { if(loadout!=null && (!loadout.Admitted || loadout.Switching)) return; if(!spawner.Running){StartRun(lastDifficulty);return;} menu.SetActive(false); spawner.Paused = false; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    public void SetMode(int value) { spawner.StopTraining(); spawner.mode = Mathf.Clamp(value, 0, 4); if(HasCampus) ApplyZoneForMode(spawner.mode); else if(peekCover != null) peekCover.SetActive(spawner.mode == 4); MoveToStation(); Refresh(); }
    private void ApplyZoneForMode(int mode)
    {
        for(int i=0;i<trainingZones.Length;i++) if(trainingZones[i].Supports(mode))
        {
            activeZone=i;var zone=trainingZones[i];
            distanceStation=zone.station;console=zone.console;
            spawner.distanceTargets=zone.distanceTargets;
            spawner.authoredSpawnPoints=zone.spawnPoints;
            spawner.movementWidth=zone.movementWidth;
            return;
        }
    }
    // Walk-in switches only upon entering another authored bay, never moves the player.
    private void CheckWalkInZone()
    {
        for(int i=0;i<trainingZones.Length;i++)
        {
            var zone=trainingZones[i];
            if(i==activeZone || zone.walkInVolume==null || !zone.walkInVolume.bounds.Contains(player.position)) continue;
            spawner.StopTraining();spawner.mode=zone.defaultMode;ApplyZoneForMode(spawner.mode);
            spawner.sessionKind=EnemySpawner.SessionKind.Free;
            spawner.StartTrainingByDifficulty(lastDifficulty);
            return;
        }
    }
    public void SetDistance(int value) { spawner.StopTraining(); spawner.distanceIndex = Mathf.Clamp(value, 0, 2); Refresh(); }
    public void Easy() => StartRun("Easy");
    public void Normal() => StartRun("Normal");
    public void Hard() => StartRun("Hard");
    public void Retry() => StartRun(lastDifficulty);
    public void FreePractice() { spawner.sessionKind = EnemySpawner.SessionKind.Free; StartRun(lastDifficulty); }
    public void TimedChallenge() { spawner.sessionKind = EnemySpawner.SessionKind.Timed; StartRun(lastDifficulty); }
    public void TargetChallenge() { spawner.sessionKind = EnemySpawner.SessionKind.Targets; StartRun(lastDifficulty); }
    public void ToggleAmmo() { spawner.StopTraining(); infiniteAmmo = !infiniteAmmo; Refresh(); }
    public void ToggleHeadOnly() { spawner.StopTraining(); spawner.headOnly = !spawner.headOnly; Refresh(); }
    public void ToggleMovement() { spawner.StopTraining(); spawner.irregularMovement = !spawner.irregularMovement; Refresh(); }
    private void StartRun(string difficulty) { lastDifficulty = difficulty; if(loadout!=null && (!loadout.Admitted || loadout.Switching)){OpenMenu();return;} MoveToStation(); spawner.StartTrainingByDifficulty(difficulty); }
    private void OnBegan() { runKey = BestKey; runWeapon = inventory.GetCurrentWeapon()?.Data; recordEligible = true; result.text = ""; Resume(); }
    private void MoveToStation()
    {
        var station = distanceStation;
        var capsule = player.GetComponent<CharacterController>();
        capsule.enabled = false; player.position = station.position; capsule.enabled = true;
        player.GetComponentInChildren<FPSCameraLook>()?.ResetLookRotation(station.eulerAngles.y);
    }
    public void ReturnToStation() => MoveToStation();
    public void EquipRifle() { spawner.StopTraining(); inventory.CancelReload(false); inventory.EquipPrimaryWeapon(rifle); }
    public void EquipPistol() { spawner.StopTraining(); inventory.CancelReload(false); inventory.ApplyPickedWeaponFromServer(pistol, pistol.magSize, pistol.maxReserveAmmo); }
    private string BestKey => loadout==null
        ? $"VectorPractice.{(HasCampus ? "campus1" : "v4")}.{accountKey}.{spawner.targetCharacterIndex}.{spawner.mode}.{spawner.distanceIndex}.{spawner.Difficulty}.{spawner.sessionKind}.{spawner.headOnly}.{infiniteAmmo}.{spawner.irregularMovement}.{inventory.GetCurrentWeapon()?.Data.weaponId}"
        : $"VectorPractice.campus2.{accountKey}.{loadout.selectedCharacter}.{(spawner.randomCharacter?-1:spawner.targetCharacterIndex)}.{spawner.mode}.{spawner.distanceIndex}.{spawner.Difficulty}.{spawner.sessionKind}.{spawner.headOnly}.{infiniteAmmo}.{spawner.irregularMovement}.{inventory.GetCurrentWeapon()?.Data.weaponId}.{spawner.realisticHealth}.{spawner.targetArmor}.{spawner.randomCharacter}.{spawner.strafeSpeed}.{spawner.timeLimit}.{loadout.autoReload}";
    public void InvalidateRecord() => recordEligible=false;
    private void OnEnded(bool completed)
    {
        if (!completed || spawner.sessionKind == EnemySpawner.SessionKind.Free) return;
        int value = spawner.mode == 3 ? Mathf.RoundToInt(spawner.TrackingAccuracy * 100) : spawner.Score;
        int best = Mathf.Max(PlayerPrefs.GetInt(runKey, 0), value);
        if (persistRecords && recordEligible) { PlayerPrefs.SetInt(runKey, best); PlayerPrefs.Save(); }
        result.text = $"훈련 완료   {spawner.Score}개 성공\n명중률 {spawner.Accuracy:0.0}% · 헤드샷 {spawner.HeadHits}회\n평균 처리 {spawner.MeanReaction:0.00}초\n동일 조건 최고 기록  {best}개 (이 기기)";
        if (spawner.mode == 3) result.text = $"추적 완료  ·  조준 유지율 {spawner.TrackingAccuracy:0.0}%\n유지 {spawner.TrackingTime:0.0}초 / 노출 {spawner.TrackingDuration:0.0}초\n동일 조건 최고  {best / 100f:0.0}%";
        if (!recordEligible) result.text = $"훈련 완료 · 성공 {spawner.Score} · 추적 {spawner.TrackingAccuracy:0.0}%\n무기 변경 또는 탄약 보충으로 최고 기록에는 반영하지 않습니다.";
        OpenMenu();
    }
    private void Refresh()
    {
        if (spawner == null) return;
        string mode = Modes[Mathf.Clamp(spawner.mode, 0, 4)];
        string difficulty = spawner.Difficulty == "Easy" ? "쉬움" : spawner.Difficulty == "Hard" ? "어려움" : "보통";
        string instruction = spawner.mode == 3 ? "움직이는 상대에게 조준을 유지하세요." : spawner.mode == 4 ? "엄폐물 좌우로 나와 상대를 맞히세요." : spawner.mode == 0 ? "무작위로 나타나는 상대를 빠르게 맞히세요." : spawner.mode == 2 ? "좌우로 움직이는 상대를 맞히세요." : "거리별 조준과 반동을 연습하세요.";
        string location = HasCampus ? trainingZones[activeZone].displayName : "사격 훈련";
        string distance = !HasCampus || spawner.mode == 1 ? $"{(spawner.distanceIndex + 1) * 10}m" : "구역별 출현 위치";
        selection.text = $"{mode} · {distance} · {difficulty}\n{instruction}";
        title.text = $"{location} · {mode}";
        string session = spawner.sessionKind == EnemySpawner.SessionKind.Free ? "자유 사격" : spawner.sessionKind == EnemySpawner.SessionKind.Timed ? $"남은 시간 {spawner.Remaining:0.0}초" : $"{spawner.Spawned} / {spawner.maxSpawnCount} 표적";
        stats.text = $"{session}\n성공 {spawner.Score:00}    명중률 {spawner.Accuracy:0}%\n헤드샷 {spawner.HeadHits}    평균 처리 {spawner.MeanReaction:0.00}초\nF3 / ESC  훈련 설정";
        if(spawner.mode == 3) stats.text = $"{session}\n조준 유지율 {spawner.TrackingAccuracy:0.0}%\n유지 시간 {spawner.TrackingTime:0.0}초\nF3 / ESC  훈련 설정";
        if (options != null) options.text = $"탄약  {(infiniteAmmo ? "무제한" : "탄창 / 재장전")}\n판정  {(spawner.headOnly ? "헤드샷만 성공" : "전신 명중")}\n이동  {(spawner.irregularMovement ? "불규칙 좌우 이동" : "일정한 좌우 이동")}";
        if (consoleStatus != null) consoleStatus.text = $"VECTOR RANGE\n{session}\n성공 {spawner.Score:00} · 명중률 {spawner.Accuracy:0}%\n[F] 훈련 설정";
        Highlight(modeButtons, spawner.mode, true); Highlight(distanceButtons, spawner.distanceIndex, !HasCampus || spawner.mode == 1);
    }
    private static void Highlight(Button[] buttons, int selected, bool interactable)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; ++i) if (buttons[i] != null)
        {
            buttons[i].interactable = interactable;
            buttons[i].image.color = i == selected && interactable ? new Color32(174, 35, 61, 255) : new Color32(55, 58, 54, 255);
        }
    }
    public void ReturnToLobby()
    {
        spawner.StopTraining(); Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        SceneNavigation.Load(NetworkManager.Instance != null && NetworkManager.Instance.IsAuthenticated ? ProjectVector.Core.ProjectScene.Lobby : ProjectVector.Core.ProjectScene.Login);
    }
}
