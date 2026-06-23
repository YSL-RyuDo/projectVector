using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HitMarkerUI : MonoBehaviour
{
    [SerializeField] private Image hitmarkerImage;
    [SerializeField] private float visibleDuration = 0.08f;
    [SerializeField] private float fadeDuration = 0.15f;

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (hitmarkerImage == null)
            hitmarkerImage = GetComponent<Image>();

        if (hitmarkerImage == null)
            hitmarkerImage = GetComponentInChildren<Image>(true);

        if (hitmarkerImage == null)
        {
            Debug.LogError("[HIT MARKER UI] Image가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        Color imageColor = hitmarkerImage.color;
        imageColor.a = 1f;
        hitmarkerImage.color = imageColor;

        hitmarkerImage.enabled = true;
        hitmarkerImage.raycastTarget = false;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        transform.SetAsLastSibling();
    }

    private void OnEnable()
    {
        HitManager.OnEnemyHit -= OnHitEnemy;
        HitManager.OnEnemyHit += OnHitEnemy;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        Debug.Log("[HIT MARKER UI] 이벤트 등록 완료");
    }

    private void OnDisable()
    {
        HitManager.OnEnemyHit -= OnHitEnemy;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private void OnHitEnemy()
    {
        Debug.Log("[HIT MARKER UI] 적 명중 이벤트 수신");

        if (canvasGroup == null || hitmarkerImage == null)
            return;

        transform.SetAsLastSibling();

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(ShowHitmarker());
    }

    private IEnumerator ShowHitmarker()
    {
        canvasGroup.alpha = 1f;

        if (visibleDuration > 0f)
            yield return new WaitForSecondsRealtime(visibleDuration);

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = 0f;
            fadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        fadeCoroutine = null;
    }
}