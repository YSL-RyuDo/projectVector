using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Existing references and difficulty-target entry point are preserved.
public class EnemySpawner : MonoBehaviour
{
    [Serializable] public struct DifficultySettings { public string name; public float activeTime, spawnDelay; }
    public DifficultySettings[] difficulties;
    public GameObject enemyObject;
    public PracticeTargetProfile[] characterTargets;
    [Range(0, 3)] public int targetCharacterIndex;
    public BoxCollider spawnArea;
    public TMP_Text scoreText;
    public int maxSpawnCount = 30;
    public Transform[] distanceTargets;
    public Transform[] authoredSpawnPoints;
    public float movementWidth = 2f;
    public int mode, distanceIndex;
    public enum SessionKind { Targets, Timed, Free }
    public SessionKind sessionKind;
    public float timeLimit = 60f;
    public float strafeSpeed = 2f;
    public bool irregularMovement, headOnly;
    public bool realisticHealth, randomCharacter;
    public int targetArmor;
    public int TargetHP { get; private set; }
    public int TargetBarrier { get; private set; }
    public int LastDamage { get; private set; }
    public int TotalDamage { get; private set; }
    public string LastRegion { get; private set; } = "—";
    public float LastKillTime { get; private set; }
    public int Generation { get; private set; }
    private readonly HashSet<int> hitShots = new HashSet<int>();
    private float movementOffset, movementDirection=1, directionTime, firstHitTime=-1;
    public Camera trackingCamera;
    public float TrackingTime { get; private set; }
    public float TrackingDuration { get; private set; }
    public float TrackingAccuracy => TrackingDuration <= 0 ? 0 : TrackingTime * 100f / TrackingDuration;
    public bool Paused { get; set; }
    public bool Running { get; private set; }
    public int Score { get; private set; }
    public int Shots { get; private set; }
    public int HeadHits { get; private set; }
    public int Hits { get; private set; }
    public int Spawned { get; private set; }
    public float Elapsed { get; private set; }
    public float MeanReaction => Score == 0 ? 0 : reactionTotal / Score;
    public float Accuracy => Shots == 0 ? 0 : Mathf.Min(100, Hits * 100f / Shots);
    public float Remaining => Mathf.Max(0, timeLimit - Elapsed);
    public string Difficulty { get; private set; } = "Normal";
    public event Action<bool> Ended;
    public event Action Began;
    private float activeTime, spawnDelay, phaseTime, reactionTotal;
    private bool targetVisible, credited;
    private Vector3 targetOrigin;
    private int previousSpawn = -1;
    public void StartTrainingByDifficulty(string difficultyName)
    {
        if (difficulties == null || enemyObject == null || spawnArea == null) return;
        foreach (var setting in difficulties)
        {
            if (!string.Equals(setting.name, difficultyName, StringComparison.OrdinalIgnoreCase)) continue;
            StopTraining(); Difficulty = setting.name;
            if (characterTargets != null && characterTargets.Length > 0)
            {
                targetCharacterIndex = Mathf.Clamp(targetCharacterIndex, 0, characterTargets.Length - 1);
                foreach(var target in characterTargets) if(target != null) target.gameObject.SetActive(false);
                enemyObject = characterTargets[targetCharacterIndex].gameObject;
            }
            activeTime = Mathf.Max(.1f, setting.activeTime); spawnDelay = Mathf.Max(.05f, setting.spawnDelay);
            Score = Shots = Spawned = HeadHits = Hits = 0; Elapsed = reactionTotal = phaseTime = TrackingTime = TrackingDuration = 0;
            TotalDamage=LastDamage=0; LastRegion="—"; LastKillTime=0; hitShots.Clear();
            Running = true; Paused = false; Began?.Invoke(); UpdateUI(); return;
        }
    }
    public void StopTraining()
    {
        ++Generation;
        bool wasRunning = Running; Running = targetVisible = false;
        if (enemyObject != null) enemyObject.SetActive(false);
        if (wasRunning) Ended?.Invoke(false);
    }
    private void OnDisable() => StopTraining();
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float delta)
    {
        if (!Running || Paused || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        Elapsed += delta; phaseTime += delta;
        if (sessionKind == SessionKind.Timed && Remaining <= 0) { Complete(); return; }
        if (!targetVisible)
        {
            if (phaseTime < spawnDelay) return;
            if(randomCharacter && characterTargets!=null && characterTargets.Length>0)
            {
                targetCharacterIndex=UnityEngine.Random.Range(0,characterTargets.Length);
                enemyObject=characterTargets[targetCharacterIndex].gameObject;
            }
            targetOrigin = distanceTargets == null || distanceTargets.Length == 0
                ? spawnArea.transform.TransformPoint(new Vector3(UnityEngine.Random.Range(-spawnArea.size.x / 2, spawnArea.size.x / 2), 1, UnityEngine.Random.Range(-spawnArea.size.z / 2, spawnArea.size.z / 2)))
                : distanceTargets[Mathf.Clamp(distanceIndex, 0, distanceTargets.Length - 1)].position;
            bool authored = mode != 1 && authoredSpawnPoints != null && authoredSpawnPoints.Length > 0;
            if (authored)
            {
                int index = UnityEngine.Random.Range(0, authoredSpawnPoints.Length);
                if (authoredSpawnPoints.Length > 1 && index == previousSpawn) index = (index + 1) % authoredSpawnPoints.Length;
                previousSpawn = index;
                targetOrigin = authoredSpawnPoints[index].position;
            }
            var profile = enemyObject.GetComponent<PracticeTargetProfile>();
            if (profile != null) targetOrigin.y += profile.groundedRootOffset;
            if (!authored && mode == 0) targetOrigin += Vector3.right * UnityEngine.Random.Range(-3f, 3f);
            if (!authored && mode == 4) targetOrigin += Vector3.right * (Spawned % 2 == 0 ? -2.5f : 2.5f);
            enemyObject.transform.position = targetOrigin; enemyObject.SetActive(true); ++Spawned;
            targetVisible = true; credited = false; phaseTime = 0;
            TargetHP=100; TargetBarrier=realisticHealth?Mathf.Clamp(targetArmor,0,100):0;
            firstHitTime=-1; movementOffset=0; directionTime=0; movementDirection=UnityEngine.Random.value<.5f?-1:1;
        }
        else
        {
            if ((mode == 2 || mode == 3) && enemyObject.activeSelf)
            {
                directionTime-=delta;
                if(irregularMovement && directionTime<=0){movementDirection=UnityEngine.Random.value<.25f?0:UnityEngine.Random.value<.5f?-1:1;directionTime=UnityEngine.Random.Range(.35f,1.2f);}
                movementOffset+=movementDirection*Mathf.Max(.1f,strafeSpeed)*delta;
                float edge=Mathf.Max(.1f,movementWidth);
                if(Mathf.Abs(movementOffset)>=edge){movementOffset=Mathf.Clamp(movementOffset,-edge,edge);movementDirection=-Mathf.Sign(movementOffset);}
                enemyObject.transform.position = targetOrigin + Vector3.right * movementOffset;
                enemyObject.GetComponent<PracticeTargetProfile>()?.SetMoving(Mathf.Abs(movementDirection)>.1f);
            }
            if (mode == 3 && enemyObject.activeSelf)
            {
                TrackingDuration += delta;
                if (trackingCamera != null && Physics.Raycast(trackingCamera.transform.position, trackingCamera.transform.forward, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var zone = hit.collider.GetComponent<PracticeTargetZone>();
                    if (zone != null && zone.spawner == this && (!headOnly || zone.isHead)) TrackingTime += delta;
                }
            }
            if (enemyObject.activeSelf && ((sessionKind == SessionKind.Free && mode!=0 && mode!=4) || phaseTime < (mode == 3 ? 5f : mode == 4 ? Mathf.Max(4f, activeTime) : activeTime))) return;
            enemyObject.SetActive(false); targetVisible = false; phaseTime = 0;
            if (sessionKind == SessionKind.Targets && Spawned >= Mathf.Max(1, maxSpawnCount)) Complete();
        }
        UpdateUI();
    }
    public void RecordShot() { if (Running && !Paused) ++Shots; }
    public void AddScore()
    {
        RegisterHit(false);
    }
    public bool RegisterHit(bool head)
        => RegisterDamage(1000,head?1:0,-1);
    public bool RegisterDamage(int damage,int region,int shotId)
    {
        if (!Running || Paused || !targetVisible || credited || !enemyObject.activeSelf) return false;
        if(shotId<0 || hitShots.Add(shotId)) ++Hits;
        if (region==1) ++HeadHits;
        LastRegion=region==1?"머리":region==2?"다리":"몸통"; LastDamage=0;
        if (headOnly && region!=1) return false;
        // Tracking is sustained aim, not a one-hit moving target. The timer owns its lifetime.
        if (mode == 3) return false;
        if(firstHitTime<0) firstHitTime=Elapsed;
        if(realisticHealth)
        {
            int applied=Mathf.Clamp(damage,0,TargetHP+TargetBarrier);
            int absorbed=Mathf.Min(applied,TargetBarrier); TargetBarrier-=absorbed; TargetHP-=applied-absorbed;
            LastDamage=applied; TotalDamage+=applied;
            if(TargetHP>0)return false;
        }
        else {LastDamage=Mathf.Max(0,damage);TotalDamage+=LastDamage;TargetHP=0;}
        LastKillTime=Mathf.Max(0,Elapsed-firstHitTime);
        credited = true; ++Score; reactionTotal += phaseTime; UpdateUI();
        return true;
    }
    private void Complete() { Running = targetVisible = false; enemyObject.SetActive(false); Ended?.Invoke(true); }
    private void UpdateUI() { if (scoreText != null) scoreText.text = $"{Score} / {maxSpawnCount}"; }
}
