using UnityEngine;

public class DecoyProjectile : MonoBehaviour
{
    public System.Action<Vector3, Vector3> OnLand;
    private bool _hasLanded = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLanded) return;
        _hasLanded = true;

        ContactPoint contact = collision.contacts[0];

        OnLand?.Invoke(contact.point, contact.normal);

        Destroy(gameObject);
    }
}