using UnityEngine;

// Interpolates server time, never decides whether the spike has exploded.
public sealed class SpikePresentation : MonoBehaviour
{
    public SpikeDeviceVisual device;
    public Transform pulseRing;
    public Renderer ringRenderer;
    public AudioSource source;
    public AudioClip armSound, beepSound, defuseSound, halfSound, safeSound;
    public SpikeDetonation detonationPrefab;
    float deadline, deployedAt, lastPulse;
    float resolvedAt;
    bool released;
    bool armed, terminal, defusing, half;
    int lastBeat = -1;
    MaterialPropertyBlock block;
    public static Color Cyan => new Color(.25f, .78f, 1f);
    public static Color Amber => new Color(1f, .32f, .08f);
    public float Remaining => Mathf.Max(0, deadline - Time.unscaledTime);
    public static float BeatInterval(float remaining) => remaining > 20 ? 1f : remaining > 10 ? .65f : remaining > 5 ? .36f : .18f;
    public void Arm(float seconds)
    {
        armed = true; terminal = false; half = false; defusing = false;
        deployedAt = Time.unscaledTime; deadline = deployedAt + seconds; lastBeat = -1;
        if (source && armSound) source.PlayOneShot(armSound, .7f);
        // Only the art follows the floor. The authoritative interaction position
        // and the existing defuse trigger are deliberately left unchanged.
        RaycastHit? floor = null;
        foreach (var hit in Physics.RaycastAll(transform.position + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<PlayerIdentity>() || hit.normal.y < .65f) continue;
            if (!floor.HasValue || hit.distance < floor.Value.distance) floor = hit;
        }
        if (floor.HasValue && device)
        {
            device.transform.position = floor.Value.point + floor.Value.normal * .012f;
            device.transform.rotation = Quaternion.FromToRotation(Vector3.up, floor.Value.normal);
            if (pulseRing) { pulseRing.position = floor.Value.point + floor.Value.normal * .025f; pulseRing.rotation = device.transform.rotation; }
        }
    }
    public void Sync(float seconds)
    {
        if (!armed || terminal || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        deadline = Time.unscaledTime + Mathf.Clamp(seconds, 0, 45);
    }
    public void DefuseState(byte state, bool reachedHalf)
    {
        if (!armed || terminal) return;
        bool wasHalf = half; half = reachedHalf;
        if (state == 1 && !defusing && source && defuseSound) source.PlayOneShot(defuseSound, .7f);
        defusing = state == 1 || (state == 2 && defusing);
        if (!wasHalf && half && source && halfSound) source.PlayOneShot(halfSound, .7f);
    }
    public void Resolve(bool exploded)
    {
        if (!armed || terminal) return;
        terminal = true; armed = false;
        resolvedAt=Time.unscaledTime;released=exploded;
        if (source) source.Stop();
        if (pulseRing) pulseRing.gameObject.SetActive(false);
        if (exploded)
        {
            if (detonationPrefab) Instantiate(detonationPrefab, device ? device.transform.position : transform.position, Quaternion.identity).Begin();
            if (device) device.gameObject.SetActive(false);
        }
        else
        {
            if (device) device.RenderSafe(0);
            if (source && safeSound) source.PlayOneShot(safeSound, .8f);
        }
    }
    public void Silence()
    {
        if (terminal) return; // A round-result packet must not cut off the success cue.
        armed = false;
        if (source) source.Stop();
        if (pulseRing) pulseRing.gameObject.SetActive(false);
        if(device)device.RenderDormant(0);
    }
    void Update()
    {
        if(terminal){if(!released&&device)device.RenderSafe(Time.unscaledTime-resolvedAt);return;}
        if (!armed) return;
        float remaining = Remaining;
        int beat = Mathf.FloorToInt(Mathf.Max(0, 45 - remaining) / BeatInterval(remaining));
        if (beat > lastBeat && remaining > 0)
        {
            lastBeat = beat; lastPulse = Time.unscaledTime;
            if (source && beepSound) source.PlayOneShot(beepSound, remaining <= 10 ? .8f : .6f);
        }
        Render(remaining, Time.unscaledTime - deployedAt, Time.unscaledTime - lastPulse);
    }
    // Deterministic presentation sampling is also used by editor visual review.
    public void Render(float remaining, float age, float pulseAge)
    {
        float urgency = 1 - Mathf.Clamp01(remaining / 45);
        float pulse = Mathf.Exp(-pulseAge * 14);
        Color color = Color.Lerp(Cyan, Amber, Mathf.SmoothStep(0, 1, (urgency - .65f) / .35f));
        if (device)
        {
            device.Pose(Mathf.SmoothStep(0, 1, age / .85f), color, pulse);
            device.Signal(age, defusing);
        }
        if (pulseRing)
        {
            pulseRing.gameObject.SetActive(true);
            float radius = Mathf.Lerp(.28f, remaining <= 5 ? 1.15f : .62f, Mathf.Clamp01(pulseAge / .6f));
            pulseRing.localScale = Vector3.one * radius;
            block ??= new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(color.r, color.g, color.b, Mathf.Max(.02f, .55f * (1 - pulseAge / .6f))));
            if (ringRenderer) ringRenderer.SetPropertyBlock(block);
        }
    }
}
