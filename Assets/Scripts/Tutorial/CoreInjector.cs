using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoreInjector : MonoBehaviour
{
    [Header("플레이어 및 설치 범위")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float plantRadius = 3.0f;  

    [Header("설치 설정")]
    [SerializeField] private float plantDuration = 3.0f;
    [SerializeField] private KeyCode plantKey = KeyCode.G; 

    [Header("UI 연결")]
    [SerializeField] private GameObject plantCanvasGroup;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI breachText;

    [Header("튜토리얼 진행")]
    [SerializeField] private TutorialManager tutorialManager;

    private float currentPlantTime = 0f;
    private bool isInPlantZone = false;
    private bool isPlanting = false;
    private bool isPlantComplete = false;

    private void Start()
    {
        if (plantCanvasGroup != null)
            plantCanvasGroup.SetActive(false);

        if (progressSlider != null)
            progressSlider.value = 0f;

        if (breachText != null)
            breachText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isPlantComplete) return;

        CheckPlayerDistance();

        if (isInPlantZone)
        {
            HandlePlantingInput();
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInZone = isInPlantZone;
        isInPlantZone = distance <= plantRadius;

      
        if (wasInZone && !isInPlantZone && isPlanting)
        {
            CancelPlanting();
        }
    }

    private void HandlePlantingInput()
    {
        if (Input.GetKey(plantKey))
        {
            if (!isPlanting)
            {
                StartPlanting();
            }

            currentPlantTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentPlantTime / plantDuration);

            if (progressSlider != null)
                progressSlider.value = progress;

            if (statusText != null)
                statusText.text = $"CONNECTING CORE INJECTOR\n[ {Mathf.FloorToInt(progress * 100)}% ]";

            if (currentPlantTime >= plantDuration)
            {
                CompletePlanting();
            }
        }
        else if (Input.GetKeyUp(plantKey) && isPlanting)
        {
            CancelPlanting();
        }
    }

    private void StartPlanting()
    {
        isPlanting = true;
        if (plantCanvasGroup != null)
            plantCanvasGroup.SetActive(true);
    }

    private void CancelPlanting()
    {
        isPlanting = false;
        currentPlantTime = 0f;

        if (progressSlider != null)
            progressSlider.value = 0f;

        if (plantCanvasGroup != null)
            plantCanvasGroup.SetActive(false);
    }

    private void CompletePlanting()
    {
        isPlanting = false;
        isPlantComplete = true;

        if (statusText != null)
            statusText.text = "CORE INJECTOR INSTALLED\n관리자 권한 침투 시작";

        if (breachText != null)
        {
            breachText.gameObject.SetActive(true);
            breachText.text = "CORE BREACH 12%";
        }

        StartCoroutine(PostPlantSequence());

        tutorialManager.StartSection(TutorialManager.TutorialSection.EP5);
    }

    private IEnumerator PostPlantSequence()
    {
        yield return new WaitForSeconds(1.5f);

        if (plantCanvasGroup != null)
            plantCanvasGroup.SetActive(false);
    }

    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, plantRadius);
    }
}