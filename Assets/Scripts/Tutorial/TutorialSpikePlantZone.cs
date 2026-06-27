using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialSpikePlantZone : MonoBehaviour
{
    public GameObject plantedSpikePrefab;
    public GameObject spikeSliderObject;
    public float plantTime = 3f;

    bool playerInside;
    bool isPlanting;
    bool hasSpike = true;

    float currentPlantTime;
    Slider slider;
    Transform player;

    void Start()
    {
        if (spikeSliderObject != null)
        {
            slider = spikeSliderObject.GetComponent<Slider>();
            spikeSliderObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!playerInside) return;
        if (!hasSpike) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            isPlanting = true;
            currentPlantTime = 0f;

            if (spikeSliderObject != null)
                spikeSliderObject.SetActive(true);
        }

        if (Keyboard.current.fKey.isPressed && isPlanting)
        {
            currentPlantTime += Time.deltaTime;

            if (slider != null)
                slider.value = currentPlantTime / plantTime;

            if (currentPlantTime >= plantTime)
            {
                Plant();
            }
        }

        if (Keyboard.current.fKey.wasReleasedThisFrame)
        {
            CancelPlant();
        }
    }

    void Plant()
    {
        isPlanting = false;
        hasSpike = false;

        Instantiate(
            plantedSpikePrefab,
            player.position - new Vector3(0f, 0.5f, 0f),
            Quaternion.identity
        );

        if (spikeSliderObject != null)
            spikeSliderObject.SetActive(false);

        if (slider != null)
            slider.value = 0f;
    }

    void CancelPlant()
    {
        if (!isPlanting) return;

        isPlanting = false;
        currentPlantTime = 0f;

        if (spikeSliderObject != null)
            spikeSliderObject.SetActive(false);

        if (slider != null)
            slider.value = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        player = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        CancelPlant();
    }
}