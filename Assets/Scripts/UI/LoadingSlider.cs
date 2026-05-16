using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSlider : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider loadingSlider; // 연결할 UI 슬라이더

    [Header("Settings")]
    [SerializeField] private float fillDuration = 2.0f; // 슬라이더가 차오르는 시간

    private Coroutine loadingCoroutine;

    private void OnEnable()
    {
        // 오브젝트가 활성화될 때마다 슬라이더를 0으로 초기화
        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
        }

        // 혹시 이미 실행 중인 코루틴이 있다면 안전하게 정지
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        // 로딩 프로세스 시작
        loadingCoroutine = StartCoroutine(StartLoadingProcess());
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화되면 코루틴 정지
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
    }

    private IEnumerator StartLoadingProcess()
    {
        GameController.Instance.isLoading = true; 

        // 5초 ~ 7초 사이의 랜덤한 시간 동안 대기
        float randomWaitTime = Random.Range(5f, 7f);
        Debug.Log($"로딩 시작 전 {randomWaitTime:F2}초 대기합니다.");

        yield return new WaitForSeconds(randomWaitTime);

        // 슬라이더 값 채우기 시작
        Debug.Log("로딩 바를 채우기 시작합니다.");
        float elapsedTime = 0f;

        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;

            loadingSlider.value = Mathf.Lerp(0f, 1f, elapsedTime / fillDuration);

            yield return null; // 다음 프레임까지 대기
        }

        // 확실하게 최종 값을 1(100%)로 설정
        loadingSlider.value = 1f;
        Debug.Log("로딩 완료!");

        GameController.Instance.isLoading = false;
    }
}