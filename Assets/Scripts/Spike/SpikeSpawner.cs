using UnityEngine;

public class SpikeSpawner : MonoBehaviour
{
    public static SpikeSpawner Instance;

    public GameObject spikePrefab;
    public GameObject DroppedSpikePrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnDropped(Transform pos)
    {
        GameObject obj = Instantiate(DroppedSpikePrefab, pos.position, Quaternion.identity);
        obj.GetComponent<Rigidbody>()?.AddForce(pos.forward * 3f, ForceMode.Impulse);
    }

    public void SpawnDroppedAt(Vector3 position, Vector3 direction, uint dropperPlayerId = 0)
    {
        GameObject obj = Instantiate(
            DroppedSpikePrefab,
            position + Vector3.up * 0.6f,
            Quaternion.identity
        );

        DroppedSpikePickup pickup = obj.GetComponentInChildren<DroppedSpikePickup>(true);

        if (pickup != null)
            pickup.Init(dropperPlayerId, 1.0f);

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 throwDir = direction.normalized;
            throwDir.y = 0.45f;

            rb.AddForce(throwDir.normalized * 6f, ForceMode.Impulse);
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
        }
    }

    public Spike SpawnPlanted(Vector3 pos)
    {
        if (spikePrefab == null)
        {
            Debug.LogError("[SPIKE SPAWNER] spikePrefab is null");
            return null;
        }

        GameObject obj = Instantiate(
            spikePrefab,
            pos,
            Quaternion.identity
        );

        Spike spike = obj.GetComponent<Spike>();

        if (spike != null)
            spike.OnDeploy();

        Debug.Log($"[SPIKE SPAWNER] SpawnPlanted serverPos={pos}");

        return spike;
    }

    public void ResetSpike(Transform pos)
    {
        Instantiate(DroppedSpikePrefab, pos.position, Quaternion.identity);
    }
}