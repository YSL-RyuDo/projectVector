using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HitEffect : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Vignette 설정")]
    [SerializeField] private float hitIntensity = 0.6f;
    [SerializeField] private float normalIntensity = 0.2f;
    [SerializeField] private float fadeSpeed = 3f;

    private Vignette vignette;
    private bool isHit;

    private void Awake()
    {
        if (postProcessVolume == null)
            postProcessVolume = GetComponent<Volume>();

        if (postProcessVolume == null || postProcessVolume.profile == null)
        {
            Debug.LogError("[HIT EFFECT] Volume 또는 Profile 없음");
            return;
        }

        if (!postProcessVolume.profile.TryGet(out vignette))
        {
            Debug.LogError("[HIT EFFECT] Vignette 없음");
            return;
        }

        vignette.intensity.Override(normalIntensity);
    }

    private void Update()
    {
        if (vignette == null)
            return;

        float target = isHit ? hitIntensity : normalIntensity;

        vignette.intensity.value = Mathf.MoveTowards(
            vignette.intensity.value,
            target,
            fadeSpeed * Time.deltaTime
        );

        if (isHit && Mathf.Abs(vignette.intensity.value - hitIntensity) < 0.01f)
            isHit = false;
    }

    public void OnHit()
    {
        if (vignette == null)
            return;

        vignette.color.Override(Color.red);
        isHit = true;
    }

    public void ForceNormal()
    {
        isHit = false;

        if (vignette != null)
            vignette.intensity.Override(normalIntensity);
    }
}