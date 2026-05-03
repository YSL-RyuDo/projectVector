using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Sentinel_Trap : MonoBehaviour
{
    [Header("--- 프리팹 연결 ---")]
    [SerializeField] private GameObject trapModel;          // 회전할 머리 부분
    [SerializeField] private GameObject scanSphereVisual;   // 커지는 구체 이펙트
    [SerializeField] private Slider _hpUI;                  // 체력바 UI

    [Header("--- 설정 수치 ---")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float maxHealth = 40f;
    [SerializeField] private float currentHealth = 40f;
    [SerializeField] private float detectRadius = 10f;
    [SerializeField] private float scanDelay = 1.0f;        // 발동 전 대기 시간
    [SerializeField] private float scanSpeed = 35f;         // 구체 확장 속도

    private bool _isPlaced = false;
    private bool _isScanning = false;

    private Rigidbody rb;
    private Collider myCollider;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();
        if (scanSphereVisual) scanSphereVisual.SetActive(false);
        StartCoroutine(IgnoreCollisionRoutine());
    }

    IEnumerator IgnoreCollisionRoutine()
    {
        myCollider.enabled = false;
        yield return new WaitForSeconds(0.2f);
        myCollider.enabled = true;
    }

    public void Launch(Vector3 direction, float force)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isPlaced || other.CompareTag("Player")) return;

        _isPlaced = true;
        _hpUI.gameObject.SetActive(true);
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 바닥에 붙을 때 각도 정렬 (수직으로 세우기)
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }

    private void Start()
    {
        _hpUI.maxValue = maxHealth;
        _hpUI.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isPlaced) return;

        // 적 감지
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);

        // HP UI 실시간 업데이트
        _hpUI.value = currentHealth;

        if (currentHealth <= 0) Destroy(this.gameObject);

        if (enemies.Length > 0)
        {
            // 1. 적을 향해 모델 회전 (추적)
            RotateTowardsTarget(enemies[0].transform);

            // 2. 적 발견 시퀀스 시작 (알림 포함)
            if (!_isScanning)
            {
                StartCoroutine(RepeatScanSequence());
            }
        }
    }

    void RotateTowardsTarget(Transform target)
    {
        Vector3 targetDir = target.position - transform.position;
        targetDir.y = 0; // 수평 회전만

        if (targetDir != Vector3.zero && trapModel != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            trapModel.transform.rotation = Quaternion.Slerp(trapModel.transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    IEnumerator RepeatScanSequence()
    {
        _isScanning = true;

        CharacterSkillUI.Instance.SentinelSkillText(true, "Enemy 감지"); 

        // ★ [충전 단계] 1초 카운트다운
        yield return new WaitForSeconds(scanDelay);

        // 여전히 적이 있는지 재확인
        if (Physics.CheckSphere(transform.position, detectRadius, enemyLayer))
        {
            // ★ [발생 단계] 에너지 구체 확장 및 실루엣 노출
            yield return StartCoroutine(ExpandWave());
            CharacterSkillUI.Instance.SentinelSkillText(false, "");
        }

        // 스캔 후 1.5초 대기 (재충전 시간)
        yield return new WaitForSeconds(1.5f);
        _isScanning = false;
    }

    IEnumerator ExpandWave()
    {
        if (!scanSphereVisual) yield break;

        scanSphereVisual.SetActive(true);
        float currentScale = 0;
        HashSet<GameObject> detectedEnemies = new HashSet<GameObject>();

        while (currentScale < detectRadius * 2f)
        {
            currentScale += Time.deltaTime * scanSpeed;
            scanSphereVisual.transform.localScale = Vector3.one * currentScale;

            Collider[] hits = Physics.OverlapSphere(transform.position, currentScale / 2f, enemyLayer);
            foreach (var hit in hits)
            {
                if (!detectedEnemies.Contains(hit.gameObject))
                {
                    detectedEnemies.Add(hit.gameObject);
                    ShowSilhouette(hit.gameObject);
                }
            }
            yield return null;
        }

        scanSphereVisual.SetActive(false);
    }

    void ShowSilhouette(GameObject enemy)
    {
        var outline = enemy.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
            // 필요하다면 일정 시간 뒤에 끄는 로직 추가 가능
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Debug.Log("함정 파괴됨");
            Destroy(gameObject);
        }
    }
}