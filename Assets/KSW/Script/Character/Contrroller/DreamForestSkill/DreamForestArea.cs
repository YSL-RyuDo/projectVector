using UnityEngine;
using System.Collections.Generic;

public class DreamForestArea : MonoBehaviour
{
    [SerializeField] private float _maxRadius = 15f; // 궁극기 범위
    [SerializeField] private float _expandSpeed = 5f; // 퍼지는 속도

    private List<GameObject> _playersInArea = new List<GameObject>();

    void Start()
    {
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        // 서서히 커지는 연출
        if (transform.localScale.x < _maxRadius * 2)
        {
            transform.localScale += Vector3.one * _expandSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 영역 진입 시 처리 (식별 차단, 사운드 변조 등)
            ApplyDreamEffect(other.gameObject, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 영역 이탈 시 복구
            ApplyDreamEffect(other.gameObject, false);
        }
    }

    private void ApplyDreamEffect(GameObject player, bool isInside)
    {
        // 여기에 상대 식별 차단 로직(UI 끄기 등)이랑 사운드 효과 넣을 거야
        Debug.Log($"{player.name}이 영역 {(isInside ? "진입" : "이탈")}");
    }
}