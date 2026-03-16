using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Header("UI")]
    public GameObject spikeSlider;
    private Slider slider;

    void Start()
    {
        slider = spikeSlider.GetComponent<Slider>();
    }

    void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
            TryStartPlant();

        if (Keyboard.current.fKey.isPressed && isPlanting)
            Planting();

        if (Keyboard.current.fKey.wasReleasedThisFrame)
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

        spikeSlider.SetActive(true);
    }

    void Planting()
    {
        Debug.Log("Spike Planting");

        currentPlantTime += Time.deltaTime;

        if (currentPlantTime >= plantTime)
        {
            PlantSpike();
        }

        slider.value = currentPlantTime / plantTime;
    }

    void PlantSpike()
    {
        isPlanting = false;
        hasSpike = false;

        Instantiate(spikePrefab, transform.position, Quaternion.identity);

        spikeSlider.GetComponent<Slider>().value = 0f;
        spikeSlider.SetActive(false);

        Debug.Log("Spike Planted");
    }

    void CancelPlant()
    {
        Debug.Log("Cancel Planting");

        isPlanting = false;
        currentPlantTime = 0f;

        spikeSlider.GetComponent<Slider>().value = 0f;
        spikeSlider.SetActive(false);
    }
}
