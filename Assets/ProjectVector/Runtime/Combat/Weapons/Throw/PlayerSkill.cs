using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    public GameObject grenadePrefab;
    public GameObject flashPrefab;

    public Transform throwPoint;

    public float throwForce = 10f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Throw(grenadePrefab);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Throw(flashPrefab);
        }
    }

    void Throw(GameObject prefab)
    {
        GameObject obj = Instantiate(
            prefab,
            throwPoint.position,
            throwPoint.rotation
        );

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                throwPoint.forward * throwForce,
                ForceMode.Impulse
            );
        }
    }
}