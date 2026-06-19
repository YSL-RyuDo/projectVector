using System.Collections;
using UnityEngine;

public class MiniMapEnemy : MonoBehaviour
{
    [SerializeField] private Collider _enemyCollider;
    public Collider EnemyCollider => _enemyCollider;

    private void Awake()
    {
        if (_enemyCollider == null)
        {
            _enemyCollider = GetComponent<Collider>();
        }
    }

    private void Start()
    {
        // 안전하게 매니저가 준비될 때까지 기다린 후 등록
        StartCoroutine(RegisterToManagerCo());
    }

    private IEnumerator RegisterToManagerCo()
    {
        // MiniMapManager 인스턴스가 씬에 생성될 때까지 대기
        while (MiniMapManager.Instance == null)
        {
            yield return null;
        }

        MiniMapManager.Instance.RegisterEnemy(this);
        Debug.Log($"[MiniMapEnemy] 매니저에 적 등록 성공: {gameObject.name}");
    }

    private void OnDisable()
    {
        if (MiniMapManager.Instance != null)
        {
            MiniMapManager.Instance.UnregisterEnemy(this);
        }
    }
}