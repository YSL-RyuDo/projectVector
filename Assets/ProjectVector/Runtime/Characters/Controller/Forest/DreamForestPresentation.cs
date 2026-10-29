using System.Collections.Generic;
using UnityEngine;

// The visible dome, depth fog and outline policy share the same world-space volume.
[DefaultExecutionOrder(220)]
public sealed class DreamForestPresentation : MonoBehaviour
{
    public static readonly HashSet<DreamForestPresentation> Active = new();
    public const float HeightRatio = .5f;
    [SerializeField] private Renderer _shell;
    [SerializeField] private AudioSource _bloom, _bed;
    private float _duration, _age;
    private PlayerIdentity _ownerIdentity;
    private MaterialPropertyBlock _block;
    public float Radius => Mathf.Abs(transform.lossyScale.x) * .5f;
    public Vector3 Center => transform.position;
    public float Strength => _duration > 0 ? Mathf.Clamp01((_duration - _age) / .55f) : 0;
    public void Init(uint owner, float duration)
    {
        _duration = duration; _age = 0;
        foreach (var p in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
            if (p.PlayerId == owner) { _ownerIdentity = p; break; }
        if (_bloom) _bloom.Play();
        if (_bed) _bed.Play();
    }
    private void OnEnable() { Active.Add(this); if (Application.isPlaying) DreamForestAudio.Ensure(); }
    private void OnDisable() { Active.Remove(this); if (_bloom) _bloom.Stop(); if (_bed) _bed.Stop(); }
    private void OnDestroy() => Active.Remove(this);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();
    private void Update()
    {
        _age += Time.deltaTime;
        if (_shell)
        {
            _block ??= new MaterialPropertyBlock();
            _block.SetFloat("_CloudTime", _age);
            _block.SetFloat("_Opacity", Strength);
            _shell.SetPropertyBlock(_block);
        }
        if (_bed) _bed.volume = .14f * Mathf.SmoothStep(0,1,_age/2) * Strength;
    }
    public bool Contains(Vector3 point)
    {
        float r = Radius; if (r < .02f || Strength <= .01f) return false;
        Vector3 p = (point - Center) / r; p.y /= HeightRatio;
        return p.sqrMagnitude < 1;
    }
    public bool Crosses(Vector3 from, Vector3 to)
    {
        float r = Radius; if (r < .02f || Strength <= .01f) return false;
        from = (from - Center) / r; to = (to - Center) / r;
        from.y /= HeightRatio; to.y /= HeightRatio;
        return SmokeOutlineOccluder.IntersectsSegment(from, to, Vector3.zero, 1);
    }
    public static DreamForestPresentation Inside(Vector3 point)
    {
        DreamForestPresentation best = null;
        foreach (var area in Active) if (area && area.Contains(point) && (!best || area.Strength > best.Strength)) best = area;
        return best;
    }
    public static bool BlocksOutline(Camera camera, Vector3 target)
        => BlocksOutline(camera, new Bounds(target, Vector3.zero));
    public static bool BlocksOutline(Camera camera, Bounds body)
    {
        if (!camera) return false;
        var viewer = camera.GetComponentInParent<PlayerIdentity>();
        if (!viewer && NetworkManager.Instance) viewer = NetworkManager.Instance.MyPlayer;
        foreach (var area in Active)
        {
            if (!area) continue;
            bool crosses = area.Crosses(camera.transform.position,body.center), wholeBodyInside = area.Contains(body.center);
            for (int i=0;i<8;i++)
            {
                var e=body.extents;
                var point=body.center+new Vector3((i&1)==0?-e.x:e.x,(i&2)==0?-e.y:e.y,(i&4)==0?-e.z:e.z);
                crosses |= area.Crosses(camera.transform.position,point);
                wholeBodyInside &= area.Contains(point);
            }
            if (!crosses) continue;
            if (!area.Contains(camera.transform.position) || !wholeBodyInside) return true;
            // Close silhouettes remain visible but an enemy visitor loses friend/foe outline cues.
            if (!viewer || !area._ownerIdentity || viewer.Team != area._ownerIdentity.Team) return true;
            if (Vector3.Distance(camera.transform.position,body.center) > 6) return true;
        }
        return false;
    }
}
