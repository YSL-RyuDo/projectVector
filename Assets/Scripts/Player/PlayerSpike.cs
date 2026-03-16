using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpike : MonoBehaviour
{
    [Header("Spike")]
    public bool hasSpike = true;
    public GameObject spikePrefab;

    [Header("Plant")]
    public float plantTime = 3f;

    private float currentPlantTime;
    private bool isPlanting;

    [Header("Zone")]
    private bool isInsidePlantZone;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            TryStartPlant();

        if (Input.GetKey(KeyCode.F) && isPlanting)
            Planting();

        if (Input.GetKeyUp(KeyCode.F))
            CancelPlant();
    }

    public void SetPlantZone(bool value)
    {
        isInsidePlantZone = value;
    }

    void TryStartPlant()
    {
        Debug.Log("Start Planting");

        if (!hasSpike) return;
        if (!isInsidePlantZone) return;

        isPlanting = true;
        currentPlantTime = 0f;
    }

    void Planting()
    {
        Debug.Log("Spike Planting");

        currentPlantTime += Time.deltaTime;

        if (currentPlantTime >= plantTime)
        {
            PlantSpike();
        }
    }

    void PlantSpike()
    {
        isPlanting = false;
        hasSpike = false;

        Instantiate(spikePrefab, transform.position, Quaternion.identity);

        Debug.Log("Spike Planted");
    }

    void CancelPlant()
    {
        Debug.Log("Cancel Planting");

        isPlanting = false;
        currentPlantTime = 0f;
    }
}
