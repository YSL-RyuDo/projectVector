using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerSpike : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;
    [SerializeField] private PlayerMovement playerMove;
    [SerializeField] private Transform dropPoint;

    [Header("Spike")]
    [SerializeField] private GameObject spikeItemPrefab;
    public bool hasSpike = true;

    private Spike spikeInRange;   // 설치된 스파이크 (해체용)

    [Header("Plant")]
    public float plantTime = 3f;

    private float currentPlantTime;
    private bool isPlanting;

    [Header("Zone")]
    private bool isInsidePlantZone;

    [Header("UI")]
    public GameObject spikeSlider;
    private Slider slider;

    private PlayerIdentity _playerIdentity;
    private bool spikeDropRequested;

    void Start()
    {
        playerItem = GetComponent<PlayerItem>();
        playerMove = GetComponent<PlayerMovement>();
        _playerIdentity = GetComponent<PlayerIdentity>();

        hasSpike = false;

        if (spikeSlider == null)
        {
            SetSpikeSlider(FindSpikeSliderInScene());
        }
        else
        {
            SetSpikeSlider(spikeSlider);
        }
    }

    void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;
        if (_playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId) return;

        // F 눌렀을 때
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (RoundManager.Instance.currentState == RoundManager.RoundState.Preparation) return;

            if (hasSpike)
                TryStartPlant();
            else if (spikeInRange != null)
                spikeInRange.TryStartDefuse();
        }

        // F 누르고 있는 동안
        if (Keyboard.current.fKey.isPressed)
        {
            if (RoundManager.Instance.currentState == RoundManager.RoundState.Preparation) return;

            if (isPlanting)
                Planting();

            if (spikeInRange != null)
                spikeInRange.Defusing();
        }

        // F 떼면
        if (Keyboard.current.fKey.wasReleasedThisFrame)
        {
            if (RoundManager.Instance.currentState == RoundManager.RoundState.Preparation) return;

            CancelPlant();

            if (spikeInRange != null)
                spikeInRange.CancelDefuse();
        }
    }

    private GameObject FindSpikeSliderInScene()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Canvas canvas in canvases)
        {
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.name == "SpikeSlider")
                    return child.gameObject;
            }
        }

        Debug.LogError("[PLAYER SPIKE] SpikeSlider object not found in scene");
        return null;
    }

    public void SetHasSpikeFromServer(bool value)
    {
        hasSpike = value;
        spikeDropRequested = false;

        if (!hasSpike)
        {
            ForceCancelPlant();
        }

        if (playerItem == null)
            playerItem = GetComponent<PlayerItem>();

        if (hasSpike)
        {
            if (spikeItemPrefab == null)
            {
                Debug.LogError("[PLAYER SPIKE] spikeItemPrefab is null. 손에 들 스파이크 프리팹을 넣어야 합니다.");
                return;
            }

            playerItem.GiveSpike(spikeItemPrefab);
        }
        else
        {
            if (playerItem != null)
                playerItem.RemoveSpike();
        }

        Debug.Log($"[PLAYER SPIKE] hasSpike={hasSpike}");
    }

    //  Spike 연결
    public void SetSpike(Spike spike)
    {
        spikeInRange = spike;
    }

    public void SetPlantZone(bool value)
    {
        isInsidePlantZone = value;
    }

    //  설치 시작
    public void TryStartPlant()
    {
        if (spikeDropRequested)
            return;

        if (!hasSpike)
        {
            ForceCancelPlant();
            return;
        }

        if (!isInsidePlantZone)
            return;

        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        if (NetworkManager.Instance == null ||
            _playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        if (spikeSlider == null)
        {
            Debug.LogError("[PLAYER SPIKE] spikeSlider is null on local player");
            return;
        }

        if (slider == null)
            slider = spikeSlider.GetComponent<Slider>();

        if (slider == null)
        {
            Debug.LogError("[PLAYER SPIKE] Slider component missing");
            return;
        }

        Debug.Log("Start Planting");

        isPlanting = true;
        currentPlantTime = 0f;

        SetPlayerControl(false);

        spikeSlider.SetActive(true);
        slider.value = 0f;
    }

    void Planting()
    {
        currentPlantTime += Time.deltaTime;

        if (slider != null)
            slider.value = currentPlantTime / plantTime;

        if (currentPlantTime >= plantTime)
        {
            CompletePlant();
        }
    }

    void CompletePlant()
    {
        isPlanting = false;
        currentPlantTime = 0f;

        SetPlayerControl(true);
        Vector3 plantPos =transform.position +transform.forward * 1.0f;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendSpikePlantComplete(plantPos);
        }

        Debug.Log("[SPIKE] Plant request sent to server");
    }

    void CancelPlant()
    {
        if (!isPlanting) return;

        Debug.Log("Cancel Planting");

        isPlanting = false;
        currentPlantTime = 0f;

        SetPlayerControl(true); // 제어권 돌려주기
    }

    public void RequestDrop()
    {
        if (!hasSpike)
            return;

        if (_playerIdentity == null)
            return;

        if (NetworkManager.Instance == null)
            return;

        if (_playerIdentity.PlayerId != NetworkManager.Instance.MyPlayerId)
            return;

        Vector3 dropPosition;
        Vector3 dropDirection;

        if (dropPoint != null)
        {
            dropPosition = dropPoint.position;
            dropDirection = dropPoint.forward;
        }
        else
        {
            dropPosition = transform.position + transform.forward * 1.0f + Vector3.up * 0.8f;
            dropDirection = transform.forward;
        }

        spikeDropRequested = true;
        ForceCancelPlant();

        NetworkManager.Instance.SendSpikeDropRequest(dropPosition, dropDirection);

        Debug.Log($"[SPIKE] Drop request sent from DropPoint. pos={dropPosition}, dir={dropDirection}");
    }

    public void SetPlayerControl(bool canControl)
    {
        if (playerItem != null)
            playerItem.SetWeaponVisibility(canControl);

        if (playerMove != null)
            playerMove.CanMove = canControl;

        if (canControl)
        {
            if (spikeSlider != null)
                spikeSlider.SetActive(false);

            if (slider != null)
                slider.value = 0f;
        }
    }

    public void SetSpikeSlider(GameObject sliderObj)
    {
        spikeSlider = sliderObj;

        if (spikeSlider != null)
        {
            slider = spikeSlider.GetComponent<Slider>();
            spikeSlider.SetActive(false);
        }
        else
        {
            Debug.LogError("[PLAYER SPIKE] Spike Slider UI is null");
        }
    }

    public void ForceCancelPlant()
    {
        bool wasPlanting = isPlanting;

        isPlanting = false;
        currentPlantTime = 0f;

        if (wasPlanting)
        {
            SetPlayerControl(true);
        }
        else
        {
            if (spikeSlider != null)
                spikeSlider.SetActive(false);

            if (slider != null)
                slider.value = 0f;
        }

        Debug.Log("[SPIKE PLANT CANCEL] force cancel");
    }
}