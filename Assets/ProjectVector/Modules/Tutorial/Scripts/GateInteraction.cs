
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GateInteraction : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform leftGate;
    [SerializeField] private Transform rightGate;

    [Header("설정")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float openDistance = 3f;
    [SerializeField] private float openSpeed = 2f;

    private bool isOpening;
    private bool isOpened;

    private Vector3 leftOpenPosition;
    private Vector3 rightOpenPosition;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (leftGate == null || rightGate == null)
        {
            Debug.LogError("좌우 게이트를 연결해야 합니다.");
            return;
        }

        leftOpenPosition =
            leftGate.position - transform.right * openDistance;

        rightOpenPosition =
            rightGate.position + transform.right * openDistance;
    }

    private void Update()
    {
        if (player == null || leftGate == null ||
            rightGate == null || isOpened || isOpening)
            return;

        float distance =
            Vector3.Distance(player.position, transform.position);

        if (Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("플레이어와 게이트 거리: " + distance);

            if (distance <= interactionDistance)
            {
                StartCoroutine(OpenGate());
            }
            else
            {
                Debug.Log("게이트에서 너무 멀리 있습니다.");
            }
        }
    }

    private IEnumerator OpenGate()
    {
        isOpening = true;

        while (
            Vector3.Distance(leftGate.position, leftOpenPosition) > 0.01f ||
            Vector3.Distance(rightGate.position, rightOpenPosition) > 0.01f)
        {
            leftGate.position = Vector3.MoveTowards(
                leftGate.position,
                leftOpenPosition,
                openSpeed * Time.deltaTime
            );

            rightGate.position = Vector3.MoveTowards(
                rightGate.position,
                rightOpenPosition,
                openSpeed * Time.deltaTime
            );

            yield return null;
        }

        leftGate.position = leftOpenPosition;
        rightGate.position = rightOpenPosition;

        isOpened = true;
        isOpening = false;
    }
}
