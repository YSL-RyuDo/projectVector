using UnityEngine;

public class FlashGrenade : MonoBehaviour
{
    public float flashRadius = 5f;

    bool exploded = false;

    void OnCollisionEnter(Collision collision)
    {
        if (exploded) return;

        exploded = true;

        Explode();
    }

    void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            flashRadius
        );

        foreach (Collider hit in hits)
        {
            Flash flash =
                hit.GetComponentInChildren<Flash>();

            if (flash != null)
            {
                flash.flash();
            }
        }

        Destroy(gameObject);
    }
}