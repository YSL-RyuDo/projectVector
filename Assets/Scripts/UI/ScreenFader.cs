using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    public Image fadeImage; // 화면을 덮는 검은색 UI Image 연결

    private void Awake()
    {
        // 싱글톤 중복 생성 방지 및 인스턴스 할당
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 페이드아웃 (화면이 점차 어두워짐)
    public IEnumerator FadeOut(float duration)
    {
        fadeImage.gameObject.SetActive(true);

        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Clamp01(timer / duration);
            fadeImage.color = color;
            yield return null;
        }
    }

    // 페이드인 (화면이 점차 밝아짐)
    public IEnumerator FadeIn(float duration)
    {
        fadeImage.gameObject.SetActive(true);

        float timer = duration;
        Color color = fadeImage.color;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            color.a = Mathf.Clamp01(timer / duration);
            fadeImage.color = color;
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);
    }
}