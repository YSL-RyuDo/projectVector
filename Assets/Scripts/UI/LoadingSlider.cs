using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSlider : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TMP_Text percentText;

    [Header("Fake Loading Settings")]
    [SerializeField] private float minTotalLoadTime = 6.0f;
    [SerializeField] private float maxTotalLoadTime = 11.0f;

    [SerializeField] private float minStepDuration = 0.15f;
    [SerializeField] private float maxStepDuration = 0.65f;

    [SerializeField] private float minStepAmount = 0.015f;
    [SerializeField] private float maxStepAmount = 0.12f;

    [SerializeField] private float stallChance = 0.22f;
    [SerializeField] private float minStallTime = 0.15f;
    [SerializeField] private float maxStallTime = 0.9f;

    [Header("Ready Send")]
    [SerializeField] private bool sendReadyToServer = true;

    private Coroutine loadingCoroutine;
    private bool readySent;
    [Header("Authored Match Briefing")]
    public bool useMatchBriefing;
    public TMP_Text briefingStatus;
    public float briefingSeconds = 3f;

    private void OnEnable()
    {
        readySent = false;

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        UpdatePercentText(0f);

        if (loadingCoroutine != null)
            StopCoroutine(loadingCoroutine);

        loadingCoroutine = StartCoroutine(useMatchBriefing ? MatchBriefing() : StartLoadingProcess());
    }

    private void OnDisable()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
    }

    private IEnumerator StartLoadingProcess()
    {
        if (GameController.Instance != null)
            GameController.Instance.isLoading = true;

        float targetTotalTime = Random.Range(minTotalLoadTime, maxTotalLoadTime);
        float elapsedTotalTime = 0f;

        float currentProgress = 0f;

        Debug.Log($"[LOADING] fake loading start. targetTime={targetTotalTime:F2}");

        while (currentProgress < 1f)
        {
            float remainingTimeRate = Mathf.Clamp01(elapsedTotalTime / targetTotalTime);

            float nextAmount = Random.Range(minStepAmount, maxStepAmount);

            if (currentProgress >= 0.75f)
                nextAmount *= 0.55f;

            if (currentProgress >= 0.9f)
                nextAmount *= 0.25f;

            float nextProgress = Mathf.Clamp01(currentProgress + nextAmount);

            if (elapsedTotalTime < targetTotalTime * 0.65f)
                nextProgress = Mathf.Min(nextProgress, 0.88f);

            if (elapsedTotalTime < targetTotalTime * 0.85f)
                nextProgress = Mathf.Min(nextProgress, 0.96f);

            if (elapsedTotalTime >= targetTotalTime)
                nextProgress = 1f;

            float stepDuration = Random.Range(minStepDuration, maxStepDuration);
            float startProgress = currentProgress;
            float stepElapsed = 0f;

            while (stepElapsed < stepDuration)
            {
                stepElapsed += Time.deltaTime;
                elapsedTotalTime += Time.deltaTime;

                float t = Mathf.Clamp01(stepElapsed / stepDuration);
                t = SmoothStep(t);

                currentProgress = Mathf.Lerp(startProgress, nextProgress, t);

                ApplyProgress(currentProgress);

                yield return null;
            }

            currentProgress = nextProgress;
            ApplyProgress(currentProgress);

            if (currentProgress < 0.98f && Random.value < stallChance)
            {
                float stallTime = Random.Range(minStallTime, maxStallTime);
                float stallElapsed = 0f;

                Debug.Log($"[LOADING] stall {stallTime:F2}s at {(currentProgress * 100f):F0}%");

                while (stallElapsed < stallTime)
                {
                    stallElapsed += Time.deltaTime;
                    elapsedTotalTime += Time.deltaTime;
                    yield return null;
                }
            }
        }

        ApplyProgress(1f);

        if (GameController.Instance != null)
            GameController.Instance.isLoading = false;

        SendReadyOnce();

        Debug.Log("[LOADING] 100% complete. Wait for server all ready.");
    }

    private IEnumerator MatchBriefing()
    {
        if (GameController.Instance != null) GameController.Instance.isLoading = true;
        if (briefingStatus != null) briefingStatus.text = "전투 정보 확인 중";
        yield return new WaitForSecondsRealtime(briefingSeconds);
        if (briefingStatus != null) briefingStatus.text = "다른 플레이어를 기다리는 중";
        SendReadyOnce();
        // The server's GAME_ALL_READY, not a fake percentage, starts gameplay.
        loadingCoroutine = null;
    }

    private void ApplyProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (loadingSlider != null)
            loadingSlider.value = progress;

        UpdatePercentText(progress);
    }

    private void UpdatePercentText(float progress)
    {
        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void SendReadyOnce()
    {
        if (readySent)
            return;

        readySent = true;

        if (!sendReadyToServer)
            return;

        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[LOADING] NetworkManager.Instance is null. Cannot send GAME_LOAD_READY.");
            return;
        }

        NetworkManager.Instance.SendGameLoadReady();
        Debug.Log("[LOADING] Send GAME_LOAD_READY to server.");
    }
}
