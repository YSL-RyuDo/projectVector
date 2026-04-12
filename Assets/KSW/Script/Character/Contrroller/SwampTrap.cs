using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SwampTrap : MonoBehaviour
{
    [SerializeField] private GameObject _areaPrefab;
    [SerializeField] private float _duration = 5f;
    [SerializeField] private float _decalSize = 3f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.7f)
        {
            ContactPoint contact = collision.contacts[0];

            Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, contact.normal);

            spawnRot *= Quaternion.Euler(90, 0, 0);

            Vector3 spawnPos = contact.point + contact.normal * 0.1f;

            GameObject area = Instantiate(_areaPrefab, spawnPos, spawnRot);

            var projector = area.GetComponentInChildren<DecalProjector>();
            if (projector != null)
            {
                projector.size = new Vector3(_decalSize, _decalSize, 0.3f);
            }

            var col = area.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.size = new Vector3(_decalSize, _decalSize, 0.5f);
                col.center = new Vector3(0, 0, 0);
            }

            Destroy(area, _duration);
            Destroy(gameObject);
        }
    }
}