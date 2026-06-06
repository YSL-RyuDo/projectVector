using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class ClawMachine : MonoBehaviour
{
    [Header("오브젝트 연결")]
    [SerializeField] private Transform claw;
    [SerializeField] private Transform grabPoint;
    [SerializeField] private Transform[] capsules;
    [SerializeField] private Transform dropZone;

    [Header("속도 및 시간 조절")]
    [SerializeField] private float moveSpeed = 300f;

    [Tooltip("5연뽑 진행 시 집게가 이동하는 속도 배속 (예: 2면 2배속)")]
    [SerializeField] private float fiveMultiplier = 5f;
    [SerializeField] private float dropDelay = 0.5f;

    [Header("등급별 확률 설정 (%)")]
    [SerializeField] private float percentS = 5f;
    [SerializeField] private float percentA = 25f;
    [SerializeField] private float percentB = 70f;

    private Vector3 clawOriginPos;
    private bool isPlaying = false;
    private float currentMoveSpeed;

    private Transform attachedCapsule = null;
    private bool isAttached = false;

    // 현재 뽑혀서 이동 중인 캡슐들을 일시적으로 체크하기 위한 셋
    private HashSet<Transform> pickedCapsules = new HashSet<Transform>();

    [Header("버튼")]
    [SerializeField] private Button _claw1; // 1회 뽑기 버튼
    [SerializeField] private Button _claw2; // 5회 뽑기 버튼
    [SerializeField] private Button _close;
    [SerializeField] private Button _closeFive;

    [Header("아이템 출력 (1회용 결과창)")]
    [SerializeField] private GameObject _itemBackground;
    [SerializeField] private GameObject _capculeImage;
    [SerializeField] private TextMeshProUGUI _text;

    [Header("아이템 출력 (5회용 패널)")]
    [SerializeField] private GameObject _fiveResultPanel;
    [SerializeField] private TextMeshProUGUI[] _fiveTexts;

    void Start()
    {
        clawOriginPos = claw.position;
        currentMoveSpeed = moveSpeed;

        _claw1.onClick.AddListener(() => StartGacha(1));
        _claw2.onClick.AddListener(() => StartGacha(5));
        _close.onClick.AddListener(CloseButton);

        if (_closeFive != null)
        {
            _closeFive.onClick.AddListener(CloseFiveButton);
        }
    }

    void LateUpdate()
    {
        if (isAttached && attachedCapsule != null)
        {
            attachedCapsule.position = grabPoint.position;
        }
    }

    private int GetRandomAvailableIndex()
    {
        if (capsules == null || capsules.Length == 0) return -1;

        List<int> availableIndices = new List<int>();

        for (int i = 0; i < capsules.Length; i++)
        {
            if (capsules[i] != null && !pickedCapsules.Contains(capsules[i]))
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0) return -1;

        int randomIndex = Random.Range(0, availableIndices.Count);
        return availableIndices[randomIndex];
    }

    public void StartGacha(int count)
    {
        if (isPlaying) return;

        isPlaying = true;

        if (count > 1)
        {
            currentMoveSpeed = moveSpeed * fiveMultiplier;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
        }

        StartCoroutine(GachaLoopSequence(count));
    }

    IEnumerator GachaLoopSequence(int totalCount)
    {
        List<string> results = new List<string>();

        for (int i = 0; i < totalCount; i++)
        {
            int targetIndex = GetRandomAvailableIndex();

            if (targetIndex == -1)
            {
                break;
            }

            Transform targetCapsule = capsules[targetIndex];

            yield return StartCoroutine(GachaSequence(targetCapsule, results));

            float nextDelay = (totalCount > 1) ? 0.1f : 0.2f;
            yield return new WaitForSeconds(nextDelay);
        }

        DisplayFinalResults(results);

        isPlaying = false;
    }

    IEnumerator GachaSequence(Transform targetCapsule, List<string> resultsList)
    {
        attachedCapsule = null;
        isAttached = false;
        targetCapsule.DOKill();

        Vector3 capsuleOriginalPos = targetCapsule.position;

        // 1. X축 이동
        float targetX = targetCapsule.position.x;
        float durationX = Mathf.Abs(claw.position.x - targetX) / currentMoveSpeed;
        claw.DOMoveX(targetX, durationX).SetEase(Ease.Linear);
        yield return new WaitForSeconds(durationX);

        // 2. Y축 하강
        float targetY = targetCapsule.position.y + (claw.position.y - grabPoint.position.y);
        float durationY = Mathf.Abs(claw.position.y - targetY) / currentMoveSpeed;
        claw.DOMoveY(targetY, durationY).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(durationY);

        // 3. 캡슐 집기
        targetCapsule.DOMove(grabPoint.position, 0.1f);
        yield return new WaitForSeconds(0.1f);

        attachedCapsule = targetCapsule;
        isAttached = true;

        // 4. 집게 상승
        float returnDurationY = Mathf.Abs(claw.position.y - clawOriginPos.y) / currentMoveSpeed;
        claw.DOMoveY(clawOriginPos.y, returnDurationY).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(returnDurationY);

        // 5. DropZone으로 이동
        float dropX = dropZone.position.x;
        float durationToDrop = Mathf.Abs(claw.position.x - dropX) / currentMoveSpeed;
        claw.DOMoveX(dropX, durationToDrop).SetEase(Ease.Linear);
        yield return new WaitForSeconds(durationToDrop);

        // 6. 투하 대기 후 놓기
        float currentDropDelay = (currentMoveSpeed > moveSpeed) ? dropDelay * 0.5f : dropDelay;
        yield return new WaitForSeconds(currentDropDelay);
        isAttached = false;

        pickedCapsules.Add(targetCapsule);

        Vector3 dropFloorWorldPos = dropZone.position + Vector3.down * 300f;

        bool isCapsuleAnimationComplete = false;
        Sequence capsuleSeq = DOTween.Sequence();

        float capDuration = (currentMoveSpeed > moveSpeed) ? 0.2f : 0.4f;
        float capReturnDuration = (currentMoveSpeed > moveSpeed) ? 0.15f : 0.3f;

        capsuleSeq.Append(targetCapsule.DOMove(dropFloorWorldPos, capDuration).SetEase(Ease.InQuad))
                  .AppendInterval(0.05f)
                  .Append(targetCapsule.DOMoveX(capsuleOriginalPos.x, capReturnDuration).SetEase(Ease.InOutQuad))
                  .Append(targetCapsule.DOMoveY(capsuleOriginalPos.y, capReturnDuration).SetEase(Ease.OutQuad))
                  .OnComplete(() =>
                  {
                      string grade = CalculateRandomRank();
                      resultsList.Add(grade);

                      pickedCapsules.Remove(targetCapsule);
                      isCapsuleAnimationComplete = true;
                  });

        yield return new WaitUntil(() => isCapsuleAnimationComplete);

        // 7. 집게 원위치 복귀
        float durationToOrigin = Vector3.Distance(claw.position, clawOriginPos) / currentMoveSpeed;
        claw.DOMove(clawOriginPos, durationToOrigin).SetEase(Ease.Linear);
        yield return new WaitForSeconds(durationToOrigin);

        attachedCapsule = null;
    }

    private string CalculateRandomRank()
    {
        float randomValue = Random.Range(0f, 100f);

        if (randomValue < percentS) return "S등급";
        else if (randomValue < percentS + percentA) return "A등급";
        else return "B등급";
    }

    private void DisplayFinalResults(List<string> finalResults)
    {
        if (finalResults.Count == 0) return;

        if (finalResults.Count == 1)
        {
            _text.text = finalResults[0];
            _itemBackground.GetComponent<UIAnimator>().Show();
            _capculeImage.GetComponent<UIAnimator>().Show();
            _text.gameObject.GetComponent<UIAnimator>().Show();
        }
        else
        {
            for (int i = 0; i < _fiveTexts.Length; i++)
            {
                if (i < finalResults.Count && _fiveTexts[i] != null)
                {
                    _fiveTexts[i].text = finalResults[i];
                }
            }

            if (_fiveResultPanel.GetComponent<UIAnimator>() != null)
            {
                _fiveResultPanel.GetComponent<UIAnimator>().Show();
            }
            else
            {
                _fiveResultPanel.SetActive(true);
            }
        }
    }

    private void CloseButton()
    {
        _itemBackground.GetComponent<UIAnimator>().Close();
    }

    private void CloseFiveButton()
    {
        if (_fiveResultPanel.GetComponent<UIAnimator>() != null)
        {
            _fiveResultPanel.GetComponent<UIAnimator>().Close();
        }
        else
        {
            _fiveResultPanel.SetActive(false);
        }
    }
}