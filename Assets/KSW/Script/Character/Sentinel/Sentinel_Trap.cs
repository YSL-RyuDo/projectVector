using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Sentinel_Trap : MonoBehaviour
{
    [Header("--- 프리팹 연결 ---")]
    [SerializeField] private GameObject trapModel;
    [SerializeField] private GameObject scanSphereVisual;
    [SerializeField] private Slider _hpUI;

    [Header("--- 설정 수치 ---")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float maxHealth = 40f;
    [SerializeField] private float currentHealth = 40f;
    [SerializeField] private float detectRadius = 10f;
    [SerializeField] private float scanDelay = 1.0f;
    [SerializeField] private float scanSpeed = 35f;

    private bool _isPlaced = false;
    private bool _isScanning = false;

    private Rigidbody rb;
    private Collider myCollider;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();
        if (scanSphereVisual) scanSphereVisual.SetActive(false);
        StartCoroutine(ignoreCollisionRoutine());
    }

    private void Start()
    {
        _hpUI.maxValue = maxHealth;
        _hpUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isPlaced) return;

        _hpUI.value = currentHealth;
        if (currentHealth <= 0) { Destroy(this.gameObject); return; }

        Collider[] enemies = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);
        if (enemies.Length > 0)
        {
            rotateTowardsTarget(enemies[0].transform);
            if (!_isScanning) StartCoroutine(repeatScanSequence());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isPlaced || other.CompareTag("Player")) return;
        _isPlaced = true;
        _hpUI.gameObject.SetActive(true);
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }

    private IEnumerator ignoreCollisionRoutine()
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

    private void rotateTowardsTarget(Transform target)
    {
        Vector3 targetDir = target.position - transform.position;
        targetDir.y = 0;
        if (targetDir != Vector3.zero && trapModel != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            trapModel.transform.rotation = Quaternion.Slerp(trapModel.transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    private IEnumerator repeatScanSequence()
    {
        _isScanning = true;
        CharacterSkillUI.Instance.SentinelSkillText(true, "Enemy 감지");
        yield return new WaitForSeconds(scanDelay);

        if (Physics.CheckSphere(transform.position, detectRadius, enemyLayer))
        {
            yield return StartCoroutine(ExpandWave());
        }

        CharacterSkillUI.Instance.SentinelSkillText(false, "");
        yield return new WaitForSeconds(1.5f);
        _isScanning = false;
    }

    IEnumerator ExpandWave()
    {
        if (!scanSphereVisual) yield break;

        scanSphereVisual.SetActive(true);
        float currentRadius = 0;
        float edgeThickness = 2.0f;

        while (currentRadius < detectRadius)
        {
            currentRadius += (Time.deltaTime * scanSpeed) / 2f;
            scanSphereVisual.transform.localScale = Vector3.one * (currentRadius * 2f);

            Collider[] enemies = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);

            foreach (var enemy in enemies)
            {
                Outline outline = enemy.GetComponent<Outline>();
                if (outline == null) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);

                if (dist <= currentRadius && dist > currentRadius - edgeThickness)
                {
                    outline.enabled = true;
                }
                else
                {
                    outline.enabled = false;
                }
            }
            yield return null;
        }

        Collider[] finalClean = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);
        foreach (var e in finalClean)
        {
            var o = e.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }

        scanSphereVisual.SetActive(false);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0) Destroy(this.gameObject);
    }
}