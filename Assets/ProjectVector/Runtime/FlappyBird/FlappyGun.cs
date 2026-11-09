using System.Collections;
using UnityEngine;

public class FlappyGun : MonoBehaviour
{
    public Transform firePoint;
    public LineRenderer bulletLine;

    public float fireRate = 0.2f;
    public float fireRange = 30f;
    public int damage = 10;

    private float nextFireTime;
    private Coroutine shotEffect;
    public LayerMask hitLayer;

    void Start()
    {
        bulletLine.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Fire()
    {
        Ray ray = new Ray(firePoint.position, Vector3.right);
        Vector3 endPoint = firePoint.position + Vector3.right * fireRange;

        RaycastHit[] hits = Physics.RaycastAll(ray, fireRange);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Pipe"))
                continue;

            endPoint = hit.point;

            FlappyEnemy enemy = hit.collider.GetComponentInParent<FlappyEnemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            Debug.Log("ИэСп: " + hit.collider.name);
            break;
        }

        if (shotEffect != null)
            StopCoroutine(shotEffect);

        shotEffect = StartCoroutine(ShowBulletLine(endPoint));
    }

    IEnumerator ShowBulletLine(Vector3 endPoint)
    {
        bulletLine.enabled = true;

        bulletLine.SetPosition(0, firePoint.position);
        bulletLine.SetPosition(1, endPoint);

        yield return new WaitForSeconds(0.1f);

        bulletLine.enabled = false;
        shotEffect = null;
    }
}