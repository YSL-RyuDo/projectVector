using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerSkill_C : CharacterSkill
{
    [SerializeField] private GameObject _decoyProjectilePrefab;
    [SerializeField] private GameObject _decoyAreaPrefab;

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    protected override void UseSkill()
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