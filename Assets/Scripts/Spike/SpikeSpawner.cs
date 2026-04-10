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

    public void SpawnDropped(Vector3 pos)
    {
        GameObject obj = Instantiate(DroppedSpikePrefab, pos, Quaternion.identity);
        obj.GetComponent<Rigidbody>()?.AddForce(transform.forward * 5f, ForceMode.Impulse);
    }

    public Spike SpawnPlanted(Vector3 pos)
    {
        GameObject obj = Instantiate(spikePrefab, pos - new Vector3(0f, 0.5f, 0f), Quaternion.identity);
        Spike spike = obj.GetComponent<Spike>();
        return spike;
    }
}