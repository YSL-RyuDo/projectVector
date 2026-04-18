using UnityEngine;
using UnityEngine.InputSystem;

public class DecoyTrapSkill : MonoBehaviour
{
    [SerializeField] private GameObject _decoyProjectilePrefab;
    [SerializeField] private GameObject _decoyAreaPrefab;
    private DecoyTrapArea _spawnedArea;

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            throwDecoy();
        }
    }

    private void throwDecoy()
    {
        Vector3 spawnPos = transform.position + (transform.forward * 2.0f) + (Vector3.up * 1.5f);
        GameObject projectile = Instantiate(_decoyProjectilePrefab, spawnPos, transform.rotation);

        var projectileScript = projectile.GetComponent<DecoyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.OnLand = (pos, normal) => {
                GameObject areaObj = Instantiate(_decoyAreaPrefab, pos, Quaternion.LookRotation(normal));
                areaObj.GetComponent<DecoyTrapArea>().PlayDecoy();
            };
        }

        projectile.GetComponent<Rigidbody>().AddForce(transform.forward * 15f, ForceMode.Impulse);
    }
}