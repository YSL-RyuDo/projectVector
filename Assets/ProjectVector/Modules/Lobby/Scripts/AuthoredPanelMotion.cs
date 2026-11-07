using System.Collections;
using UnityEngine;

// Presentation only. Never owns navigation, persisted state, or button bindings.
[RequireComponent(typeof(CanvasGroup))]
public sealed class AuthoredPanelMotion : MonoBehaviour
{
    public RectTransform content;
    public Vector2 entryOffset = new(0, -18);
    public float duration = .22f;
    public float delay;
    public float initialScale = .98f;
    private CanvasGroup group;
    private Vector2 position;
    private Vector3 scale;
    private Coroutine running;
    private bool initialized;
    private void Initialize()
    {
        if (initialized) return;
        group = GetComponent<CanvasGroup>();
        if (content == null) content = transform as RectTransform;
        position = content.anchoredPosition; scale = content.localScale; initialized = true;
    }
    private void OnEnable()
    {
        Initialize();
        if (group.alpha > .5f && group.interactable) Play();
    }
    private void OnDisable() { ResetMotion(); if (group != null) group.alpha = 1f; }
    public void ResetMotion()
    {
        if (!initialized) return;
        if (running != null) StopCoroutine(running);
        running = null; content.anchoredPosition = position; content.localScale = scale;
    }
    public void Play()
    {
        Initialize(); ResetMotion();
        if (isActiveAndEnabled) running = StartCoroutine(Reveal());
    }
    private IEnumerator Reveal()
    {
        group.alpha = 0f;
        if (delay > 0) yield return new WaitForSecondsRealtime(delay);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / Mathf.Max(.01f, duration)), 3f);
            group.alpha = t;
            content.anchoredPosition = position + entryOffset * (1f - t);
            content.localScale = scale * Mathf.Lerp(initialScale, 1f, t);
            yield return null;
        }
        group.alpha = 1f; content.anchoredPosition = position; content.localScale = scale; running = null;
    }
}
