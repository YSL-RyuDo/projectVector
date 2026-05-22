using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ClawMachine : MonoBehaviour
{
    [Header("오브젝트 연결")]
    [SerializeField] private Transform claw;
    [SerializeField] private Transform grabPoint;
    [SerializeField] private Transform[] capsules;
    [SerializeField] private Transform dropZone;

    [Header("속도 및 시간 조절")]
    [SerializeField] private float moveSpeed = 300f;
    [SerializeField] private float dropDelay = 0.5f;

    [Header("등급별 확률 설정 (%)")]
    [SerializeField] private float percentS = 5f;
    [SerializeField] private float percentA = 25f;
    [SerializeField] private float percentB = 70f;

    private Vector3 clawOriginPos;
    private bool isPlaying = false;

    private Transform attachedCapsule = null;
    private bool isAttached = false;

    [Header("버튼")]
    [SerializeField] private Button _claw1;
    [SerializeField] private Button _claw2;

    void Start()
    {
        clawOriginPos = claw.position;

        _claw1.onClick.AddListener(StartGacha);
        _claw2.onClick.AddListener(StartGacha);
    }

    void LateUpdate()
    {
        if (isAttached && attachedCapsule != null)
        {
            attachedCapsule.position = grabPoint.position;
        }
    }

    public void StartGacha()
    {
        if (isPlaying) return;

        isPlaying = true;

        if (capsules == null || capsules.Length == 0)
        {
            isPlaying = false;
            return;
        }
        int randomIndex = Random.Range(0, capsules.Length);
        Transform targetCapsule = capsules[randomIndex];

        StartCoroutine(GachaSequence(targetCapsule));
    }

    IEnumerator GachaSequence(Transform targetCapsule)
    {
        attachedCapsule = null;
        isAttached = false;
        targetCapsule.DOKill();

        float targetX = targetCapsule.position.x;
        float durationX = Mathf.Abs(claw.position.x - targetX) / moveSpeed;

        claw.DOMoveX(targetX, durationX).SetEase(Ease.Linear);
        yield return new WaitForSeconds(durationX);

        float targetY = targetCapsule.position.y + (claw.position.y - grabPoint.position.y);
        float durationY = Mathf.Abs(claw.position.y - targetY) / moveSpeed;

        claw.DOMoveY(targetY, durationY).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(durationY);

        targetCapsule.DOMove(grabPoint.position, 0.1f);
        yield return new WaitForSeconds(0.1f);

        attachedCapsule = targetCapsule;
        isAttached = true;

        float returnDurationY = Mathf.Abs(claw.position.y - clawOriginPos.y) / moveSpeed;
        claw.DOMoveY(clawOriginPos.y, returnDurationY).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(returnDurationY);

        float dropX = dropZone.position.x;
        float durationToDrop = Mathf.Abs(claw.position.x - dropX) / moveSpeed;

        claw.DOMoveX(dropX, durationToDrop).SetEase(Ease.Linear);
        yield return new WaitForSeconds(durationToDrop);

        yield return new WaitForSeconds(dropDelay);

        isAttached = false;

        Vector3 dropFloorWorldPos = dropZone.position + Vector3.down * 300f;

        targetCapsule.DOMove(dropFloorWorldPos, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            targetCapsule.gameObject.SetActive(false);
            GetRandomRank();
        });
        yield return new WaitForSeconds(0.5f);

        float durationToOrigin = Vector3.Distance(claw.position, clawOriginPos) / moveSpeed;
        claw.DOMove(clawOriginPos, durationToOrigin).SetEase(Ease.Linear);
        yield return new WaitForSeconds(durationToOrigin);

        attachedCapsule = null;
        isPlaying = false;
    }

    private void GetRandomRank()
    {
        float randomValue = Random.Range(0f, 100f);

        if (randomValue < percentS)
        {
            Debug.Log($"<color=cyan>[가챠 결과] S 등급</color>");
        }
        else if (randomValue < percentS + percentA)
        {
            Debug.Log($"<color=green>[가챠 결과] A 등급</color>");
        }
        else
        {
            Debug.Log($"<color=white>[가챠 결과] B 등급</color>");
        }
    }
}