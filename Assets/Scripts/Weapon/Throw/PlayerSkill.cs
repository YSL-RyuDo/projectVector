using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    public GameObject grenadePrefab;
    public GameObject flashPrefab;

    public Transform throwPoint;

    public float throwForce = 15f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            Throw(grenadePrefab);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Throw(flashPrefab);
        }
    }

    void Throw(GameObject prefab)
    {
        GameObject obj = Instantiate(
            prefab,
            throwPoint.position,
            Quaternion.identity
        );

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                Camera.main.transform.forward * throwForce,
                ForceMode.Impulse
            );
        }
    }
}