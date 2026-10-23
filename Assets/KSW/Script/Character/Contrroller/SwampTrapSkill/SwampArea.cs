using System.Collections.Generic;
using UnityEngine;

public class SwampArea : MonoBehaviour
{
    [SerializeField] private float _slowMultiplier = .2f;
    [SerializeField] private GameObject _affectedEffect;
    [SerializeField] private AudioClip _entrySound;
    private readonly Dictionary<PlayerMovement, HashSet<Collider>> _affected = new();
    private readonly List<PlayerMovement> _expired = new();
    public int AffectedCount => _affected.Count;
    public void InitFromServer(uint ownerPlayerId,float slowMultiplier) => _slowMultiplier=Mathf.Clamp(slowMultiplier,.05f,1);
    public void Init(float radius,float duration,float slowMultiplier)
    {
        InitFromServer(0,slowMultiplier);
        var sphere=GetComponentInChildren<SphereCollider>(true);
        if(sphere){sphere.enabled=true;sphere.isTrigger=true;sphere.radius=radius;}
        Destroy(gameObject,duration);
    }
    private bool Alive(PlayerMovement player) => player && player.isActiveAndEnabled && player.GetComponent<PlayerHP>()?.IsDead != true && player.GetComponent<PlayerDeathController>()?.IsDead != true;
    private void OnTriggerEnter(Collider other) => Track(other);
    private void OnTriggerStay(Collider other) => Track(other);
    private void Track(Collider other)
    {
        var player=other.GetComponentInParent<PlayerMovement>();
        if(!Alive(player))return;
        if(SwampSurfaceQuery.Blocked(transform.position,SwampSurfaceQuery.Foot(player),out _)) { Remove(player); return; }
        if(!_affected.TryGetValue(player,out var colliders)) {
            _affected[player]=colliders=new HashSet<Collider>();
            // Balance one application per area, not per body collider; preserve other buffs.
            player.ApplySwampSlow(_slowMultiplier);
            var feedback=player.GetComponent<SwampAffectedFeedback>();
            if(!feedback)feedback=player.gameObject.AddComponent<SwampAffectedFeedback>();
            feedback.Enter(this,_affectedEffect,_entrySound);
            GetComponent<ControllerSwampVfx>()?.Pulse();
        }
        colliders.Add(other);
    }
    private void OnTriggerExit(Collider other)
    {
        var player=other.GetComponentInParent<PlayerMovement>();
        if(!player || !_affected.TryGetValue(player,out var colliders))return;
        colliders.Remove(other);if(colliders.Count==0)Remove(player);
    }
    private void Update()
    {
        _expired.Clear();
        foreach(var pair in _affected){pair.Value.RemoveWhere(c=>!c||!c.enabled||!c.gameObject.activeInHierarchy);if(!Alive(pair.Key)||pair.Value.Count==0)_expired.Add(pair.Key);}
        foreach(var player in _expired)Remove(player);
    }
    private void Remove(PlayerMovement player)
    {
        if(!_affected.Remove(player))return;
        if(player){player.RemoveSwampSlow();player.GetComponent<SwampAffectedFeedback>()?.Exit(this);}
    }
    private void OnDisable(){_expired.Clear();_expired.AddRange(_affected.Keys);foreach(var player in _expired)Remove(player);}
}
