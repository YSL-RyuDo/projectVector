using UnityEngine;
using UnityEngine.InputSystem;

public class SwampTrapSkill : MonoBehaviour
{
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private float _throwForce = 15f;
    [SerializeField] private float _upwardForce = 2f;

    void Update()
    {
        // Q키를 눌러서 투척
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Throw();
        }
    }

    private void Throw()
    {
        Vector3 playerPos = transform.position;

        Vector3 forwardDir = transform.forward;
        Vector3 spawnPos = new Vector3(playerPos.x, playerPos.y + 1.5f, playerPos.z) + (forwardDir * 2.0f);

        GameObject trap = Instantiate(_trapPrefab, spawnPos, transform.rotation, null);

        Rigidbody rb = trap.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 force = forwardDir * _throwForce + Vector3.up * _upwardForce;
            rb.AddForce(force, ForceMode.Impulse);
        }

        Collider charCol = GetComponent<Collider>();
        Collider trapCol = trap.GetComponent<Collider>();
        if (charCol != null && trapCol != null)
        {
            Physics.IgnoreCollision(charCol, trapCol);
        }
    }
}