using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GateInteraction : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform gate;

    [Header("설정")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float openHeight = 3f;
    [SerializeField] private float openSpeed = 2f;

    private bool isOpening;
    private bool isOpened;
    private Vector3 openPosition;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (gate == null)
        {
            Debug.LogError("Gate 칸에 실제 문 오브젝트를 연결해야 합니다.");
            return;
        }

        openPosition = gate.position + Vector3.up * openHeight;
    }

    private void Update()
    {
        if (player == null || gate == null || isOpened)
            return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (Keyboard.current.fKey.wasPressedThisFrame)
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
        if (isOpening)
            yield break;

        isOpening = true;

        while (Vector3.Distance(gate.position, openPosition) > 0.01f)
        {
            gate.position = Vector3.MoveTowards(
                gate.position,
                openPosition,
                openSpeed * Time.deltaTime
            );

            yield return null;
        }

        gate.position = openPosition;
        isOpened = true;
        isOpening = false;
    }
}