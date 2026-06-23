using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitMarkerUI : MonoBehaviour
{
    public Image hitmarkerImage;
    private Coroutine fadeCoroutine;

    private void OnEnable()
    {
        // HitManager의 hit 이벤트가 켜지면 내 OnHitEnemy 함수도 실행되도록 등록
        HitManager.OnEnemyHit += OnHitEnemy;
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때는 링크를 해제 (메모리 누수 방지)
        HitManager.OnEnemyHit -= OnHitEnemy;
    }

    public void OnHitEnemy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeOutHitmarker(0.1f));
    }

    IEnumerator FadeOutHitmarker(float duration)
    {
        float currentTime = 0f;
        SetAlpha(1f);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, currentTime / duration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0f);
    }

    void SetAlpha(float alpha)
    {
        if (hitmarkerImage != null)
        {
            Color color = hitmarkerImage.color;
            color.a = alpha;
            hitmarkerImage.color = color;
        }
    }
}