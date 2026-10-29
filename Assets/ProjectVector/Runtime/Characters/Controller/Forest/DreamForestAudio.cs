using System.Collections.Generic;
using UnityEngine;

// Only spatial game effects: do not filter UI/music/non-positional team voice.
public sealed class DreamForestAudio : MonoBehaviour
{
    public static DreamForestAudio Instance { get; private set; }
    // Non-UI audio processing shared by every character, including spectators.
    public static void Ensure()
    {
        if (!Instance) new GameObject("DreamForestListenerAudio").AddComponent<DreamForestAudio>();
    }
    private void Awake() => Instance = this;
    private sealed class Saved { public AudioLowPassFilter filter; public bool added, enabled; public float cutoff; }
    private readonly Dictionary<AudioSource,Saved> _sources = new();
    private float _next;
    private bool _inside;
    public bool IsMuffled => _inside;
    private void Update()
    {
        if (Time.unscaledTime < _next) return; _next = Time.unscaledTime + .1f;
        if (DreamForestPresentation.Active.Count == 0) { Restore(); return; }
        // Use the rendered main view, as depth fog does. Some practice/spectator
        // rigs keep their AudioListener on a separate (non-camera) transform.
        Transform view = Camera.main ? Camera.main.transform : null;
        if (!view) foreach (var candidate in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (candidate.isActiveAndEnabled) { view = candidate.transform; break; }
        bool inside = view && DreamForestPresentation.Inside(view.position);
        if (!inside) { Restore(); return; }
        _inside = true;
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (!source || source.spatialBlend < .5f || _sources.ContainsKey(source)) continue;
            var filter = source.GetComponent<AudioLowPassFilter>();
            var saved = new Saved { added = !filter, filter = filter, enabled = filter && filter.enabled, cutoff = filter ? filter.cutoffFrequency : 22000 };
            if (!filter) saved.filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            saved.filter.enabled = true; saved.filter.cutoffFrequency = Mathf.Min(saved.enabled ? saved.cutoff : 22000, 1900);
            _sources[source] = saved;
        }
    }
    private void Restore()
    {
        foreach (var saved in _sources.Values) if (saved.filter)
        {
            if (saved.added) { saved.filter.enabled = false; Destroy(saved.filter); }
            else { saved.filter.cutoffFrequency = saved.cutoff; saved.filter.enabled = saved.enabled; }
        }
        _sources.Clear(); _inside = false;
    }
    private void OnDisable() => Restore();
    private void OnDestroy() { Restore(); if (Instance == this) Instance = null; }
}
