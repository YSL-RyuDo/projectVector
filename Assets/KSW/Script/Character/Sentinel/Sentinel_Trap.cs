using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sentinel_Trap : MonoBehaviour
{
    [SerializeField] private GameObject trapModel;
    [SerializeField] private GameObject scanSphereVisual;
    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] private float detectRadius = 10f;
    [SerializeField] private float scanDelay = 0.5f;
    [SerializeField] private float scanSpeed = 35f;

    private bool isPlaced = false;
    private bool isUsed = false;
    
    private Rigidbody rb;
    private Collider myCollider;

    void Awake()
    {
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
        isPlaced = false;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPlaced || other.CompareTag("Player")) return;

        isPlaced = true;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }

    void Update()
    {
        if (!isPlaced || isUsed) return;

        Collider[] enemies = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);
        if (enemies.Length > 0)
        {
            isUsed = true;
            StartCoroutine(SingleScanSequence());
        }
    }

    IEnumerator SingleScanSequence()
    {
        yield return new WaitForSeconds(scanDelay);
        yield return StartCoroutine(ExpandWaveOnce());
    }

    IEnumerator ExpandWaveOnce()
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

                    // 실루엣 효과 코드
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
            // StartCoroutine(DisableOutline(outline, 2.0f));
        }
    }
}