using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DreamForestPostProcessFeedback : MonoBehaviour
{
    public static DreamForestPostProcessFeedback Instance { get; private set; }

    [Header("Volume")]
    [SerializeField] private Volume volume;

    [Header("Dream Forest")]
    [SerializeField] private float dreamIntensity = 0.42f;
    [SerializeField] private Color dreamColor = new Color(0.25f, 0.1f, 0.85f, 1f);

    [Header("Hit")]
    [SerializeField] private float hitIntensity = 0.6f;
    [SerializeField] private float hitDuration = 0.18f;
    [SerializeField] private Color hitColor = Color.red;

    [Header("Normal")]
    [SerializeField] private float normalIntensity = 0f;
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private float colorFadeSpeed = 12f;
    [SerializeField] private float smoothness = 0.45f;

    private Vignette vignette;
    private bool isInsideDreamForest;
    private float hitTimer;

    private void Awake()
    {
        Instance = this;

        if (volume == null)
            volume = GetComponent<Volume>();

        if (volume == null || volume.profile == null)
        {
            Debug.LogError("[SCREEN FEEDBACK] Volume 또는 Profile 없음");
            return;
        }

        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogError("[SCREEN FEEDBACK] Vignette 없음");
            return;
        }

        vignette.intensity.Override(0f);
        vignette.color.Override(dreamColor);
        vignette.smoothness.Override(smoothness);
    }

    private void Update()
    {
        if (vignette == null)
            return;

        if (hitTimer > 0f)
            hitTimer -= Time.deltaTime;

        Color targetColor;
        float targetIntensity;

        if (hitTimer > 0f)
        {
            targetColor = hitColor;
            targetIntensity = hitIntensity;
        }
        else if (isInsideDreamForest)
        {
            targetColor = dreamColor;
            targetIntensity = dreamIntensity;
        }
        else
        {
            targetColor = dreamColor;
            targetIntensity = normalIntensity;
        }

        vignette.color.value = Color.Lerp(
            vignette.color.value,
            targetColor,
            colorFadeSpeed * Time.deltaTime
        );

        vignette.intensity.value = Mathf.MoveTowards(
            vignette.intensity.value,
            targetIntensity,
            fadeSpeed * Time.deltaTime
        );

        vignette.smoothness.value = smoothness;
    }

    public void SetInsideDreamForest(bool inside)
    {
        isInsideDreamForest = inside;
        Debug.Log($"[SCREEN FEEDBACK] DreamForest inside={inside}");
    }

    public void PlayHit()
    {
        hitTimer = hitDuration;
        Debug.Log("[SCREEN FEEDBACK] Hit");
    }

    public void ForceHide()
    {
        isInsideDreamForest = false;
        hitTimer = 0f;

        if (vignette != null)
            vignette.intensity.Override(0f);
    }
}