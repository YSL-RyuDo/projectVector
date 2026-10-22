using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Saved HUD only. Called exclusively from the server-confirmed scanner packet.
public sealed class ScannerFeedbackUI : MonoBehaviour
{
    public static ScannerFeedbackUI Instance { get; private set; }
    [SerializeField] private CanvasGroup _ownerNotice, _victimNotice, _victimEdges;
    [SerializeField] private TMP_Text _ownerText;
    [SerializeField] private AudioSource _ownerAudio, _victimAudio;
    [SerializeField] private float _ownerDuration = 1.1f, _victimDuration = .65f, _edgeDuration = .28f;
    private readonly HashSet<ulong> _confirmed = new();
    private float _ownerLeft, _victimLeft, _edgeLeft, _ownerSoundAt = -10, _victimSoundAt = -10;
    private uint _localId, _countScanner;
    private int _count;

    private void Awake() { Instance = this; ResetFeedback(); }
    public void ConfirmZap(uint localId, uint ownerId, uint scannerId, uint targetId)
    {
        if (localId == 0 || (localId != ownerId && localId != targetId)) return;
        if (_localId != localId) { ResetFeedback(); _localId = localId; }
        if (!_confirmed.Add(((ulong)scannerId << 32) | targetId)) return;
        if (localId == ownerId)
        {
            if (_countScanner != scannerId || _ownerLeft <= 0) _count = 0;
            _countScanner = scannerId; _count++;
            if (_ownerText) _ownerText.text = $"스캐너 감지  {_count}명";
            _ownerLeft = _ownerDuration;
            Play(_ownerAudio, ref _ownerSoundAt);
        }
        if (localId == targetId)
        {
            _victimLeft = _victimDuration; _edgeLeft = _edgeDuration;
            Play(_victimAudio, ref _victimSoundAt);
        }
        Advance(0);
    }
    private static void Play(AudioSource source, ref float last)
    {
        if (!source || !source.clip || Time.unscaledTime - last < .08f) return;
        last = Time.unscaledTime;
        source.PlayOneShot(source.clip); // non-spatial, no dependence on camera direction
    }
    private void Update()
    {
        var net = NetworkManager.Instance;
        if (!net || !net.IsConnected || !net.IsAuthenticated || (_localId != 0 && net.MyPlayerId != _localId))
        { ResetFeedback(); return; }
        Advance(Time.unscaledDeltaTime);
    }
    public void Advance(float delta)
    {
        _ownerLeft = Mathf.Max(0, _ownerLeft - delta);
        _victimLeft = Mathf.Max(0, _victimLeft - delta);
        _edgeLeft = Mathf.Max(0, _edgeLeft - delta);
        Present(_ownerNotice, _ownerLeft, .22f);
        Present(_victimNotice, _victimLeft, .2f);
        Present(_victimEdges, _edgeLeft, _edgeDuration);
    }
    private static void Present(CanvasGroup group, float remaining, float fade)
    {
        if (!group) return;
        group.alpha = Mathf.Clamp01(remaining / Mathf.Max(.01f, fade));
        group.blocksRaycasts = group.interactable = false;
        group.gameObject.SetActive(remaining > 0);
    }
    public void ResetFeedback()
    {
        _ownerLeft = _victimLeft = _edgeLeft = 0;
        _count = 0; _localId = _countScanner = 0; _confirmed.Clear();
        _ownerSoundAt = _victimSoundAt = -10;
        if (_ownerAudio) _ownerAudio.Stop();
        if (_victimAudio) _victimAudio.Stop();
        Advance(0);
    }
    private void OnDisable() => ResetFeedback();
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
