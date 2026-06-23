using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HitEffect : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    public Volume postProcessVolume;

    [Header("Vignette 설정")]
    public float hitIntensity = 0.6f;
    public float normalIntensity = 0.2f;
    public float fadeSpeed = 3f;

    private Vignette _vignette;
    private Coroutine _fadeCoroutine;

    void Start()
    {
        if (postProcessVolume.profile.TryGet(out _vignette))
        {
            _vignette.intensity.value = normalIntensity;
            _vignette.color.value = Color.red;
        }
    }

    public void OnHit()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FlashVignette());
    }

    IEnumerator FlashVignette()
    {
        _vignette.intensity.value = hitIntensity;

        while (_vignette.intensity.value > normalIntensity + 0.01f)
        {
            _vignette.intensity.value = Mathf.Lerp(
                _vignette.intensity.value,
                normalIntensity,
                Time.deltaTime * fadeSpeed
            );
            yield return null;
        }

        _vignette.intensity.value = normalIntensity;
    }
}