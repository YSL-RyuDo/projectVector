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

    private Spike spikeInRange;

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

    private void Start()
    {
        playerItem = GetComponent<PlayerItem>();
        playerMove = GetComponent<PlayerMovement>();
        _playerIdentity = GetComponent<PlayerIdentity>();

        hasSpike = false;

        if (spikeSlider == null)
            SetSpikeSlider(FindSpikeSliderInScene());
        else
            SetSpikeSlider(spikeSlider);
    }

    private void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused)
            return;

        if (!IsLocalPlayer())
            return;

        if (Keyboard.current == null)
            return;

        UpdateInteractionPrompt();

        if (RoundManager.Instance != null &&
            RoundManager.Instance.currentState == RoundManager.RoundState.Preparation)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame)
                playerItem?.TryPickupLookedItem();

            return;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (playerItem != null && playerItem.TryPickupLookedItem())
                return;

            if (hasSpike)
            {
                TryStartPlant();
                return;
            }

            if (spikeInRange != null)
            {
                spikeInRange.TryStartDefuse();
                return;
            }
        }

        if (Keyboard.current.fKey.isPressed)
        {
            if (playerItem != null && playerItem.IsLookingAtPickupItem())
                return;

            if (isPlanting)
                Planting();

            if (spikeInRange != null)
                spikeInRange.Defusing();
        }

        if (Keyboard.current.fKey.wasReleasedThisFrame)
        {
            CancelPlant();

            if (spikeInRange != null)
                spikeInRange.CancelDefuse();
        }
    }

    private void UpdateInteractionPrompt()
    {
        if (InteractionPromptUI.Instance == null)
            return;

        if (RoundManager.Instance != null &&
            RoundManager.Instance.currentState == RoundManager.RoundState.Preparation)
        {
            if (playerItem != null && playerItem.IsLookingAtPickupItem())
                InteractionPromptUI.Instance.Show("무기 변경");
            else
                InteractionPromptUI.Instance.Hide();

            return;
        }

        if (playerItem != null && playerItem.IsLookingAtPickupItem())
        {
            InteractionPromptUI.Instance.Show("무기 변경");
            return;
        }

        if (hasSpike && isInsidePlantZone && !isPlanting)
        {
            InteractionPromptUI.Instance.Show("스파이크 설치");
            return;
        }

        if (!hasSpike && spikeInRange != null)
        {
            InteractionPromptUI.Instance.Show("스파이크 해체");
            return;
        }

        InteractionPromptUI.Instance.Hide();
    }

    public bool IsLocalPlayer()
    {
        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
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
        bool previouslyHadSpike = hasSpike;

        hasSpike = value;
        spikeDropRequested = false;

        if (_playerIdentity == null)
            _playerIdentity = GetComponent<PlayerIdentity>();

        bool isLocalPlayer =
            _playerIdentity != null &&
            NetworkManager.Instance != null &&
            _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;

        if (isLocalPlayer && InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetSpikePossession(hasSpike);

            if (hasSpike && !previouslyHadSpike)
            {
                CombatFeedbackUI.Instance?.ShowObjectiveNotice(
                    "스파이크를 획득했습니다.",
                    false
                );
            }
        }

        if (!hasSpike)
            ForceCancelPlant();

        if (playerItem == null)
            playerItem = GetComponent<PlayerItem>();

        if (hasSpike)
        {
            if (spikeItemPrefab == null)
            {
                Debug.LogError(
                    "[PLAYER SPIKE] spikeItemPrefab is null. " +
                    "손에 들 스파이크 프리팹을 넣어야 합니다."
                );

                return;
            }

            if (playerItem != null)
                playerItem.GiveSpike(spikeItemPrefab);
        }
        else
        {
            if (playerItem != null)
                playerItem.RemoveSpike();
        }

        Debug.Log(
            $"[PLAYER SPIKE] playerId={_playerIdentity?.PlayerId}, " +
            $"hasSpike={hasSpike}, local={isLocalPlayer}"
        );
    }

    public void SetSpike(Spike spike)
    {
        spikeInRange = spike;
    }

    public void SetPlantZone(bool value)
    {
        isInsidePlantZone = value;

        if (!isInsidePlantZone)
            ForceCancelPlant();
    }

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

        if (!IsLocalPlayer())
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

        Debug.Log("[SPIKE] Start planting");

        isPlanting = true;
        currentPlantTime = 0f;

        SetPlayerControl(false);

        spikeSlider.SetActive(true);
        slider.value = 0f;

        CombatFeedbackUI.Instance?.ShowSpikeProgress(
            "스파이크를 설치 중입니다.",
            0f
        );
    }

    private void Planting()
    {
        if (!isPlanting)
            return;

        if (!hasSpike || !isInsidePlantZone)
        {
            CancelPlant();
            return;
        }

        currentPlantTime += Time.deltaTime;

        float progress = plantTime > 0f
            ? Mathf.Clamp01(currentPlantTime / plantTime)
            : 0f;

        if (slider != null)
            slider.value = progress;

        CombatFeedbackUI.Instance?.ShowSpikeProgress(
            "스파이크를 설치 중입니다.",
            progress
        );

        if (currentPlantTime >= plantTime)
            CompletePlant();
    }

    private void CompletePlant()
    {
        if (!isPlanting)
            return;

        isPlanting = false;
        currentPlantTime = 0f;

        CombatFeedbackUI.Instance?.HideSpikeProgress();

        SetPlayerControl(true);

        Vector3 plantPos = transform.position + transform.forward * 1f;

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendSpikePlantComplete(plantPos);

        Debug.Log("[SPIKE] Plant request sent to server");
    }

    private void CancelPlant()
    {
        if (!isPlanting)
            return;

        Debug.Log("[SPIKE] Cancel planting");

        isPlanting = false;
        currentPlantTime = 0f;

        CombatFeedbackUI.Instance?.HideSpikeProgress();

        SetPlayerControl(true);
    }

    public void RequestDrop()
    {
        if (!hasSpike)
            return;

        if (!IsLocalPlayer())
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
            dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.8f;
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

        if (!canControl)
            return;

        if (spikeSlider != null)
            spikeSlider.SetActive(false);

        if (slider != null)
            slider.value = 0f;

        CombatFeedbackUI.Instance?.HideSpikeProgress();
    }

    public void SetSpikeSlider(GameObject sliderObj)
    {
        spikeSlider = sliderObj;

        if (spikeSlider == null)
        {
            Debug.LogError("[PLAYER SPIKE] Spike Slider UI is null");
            return;
        }

        slider = spikeSlider.GetComponent<Slider>();
        spikeSlider.SetActive(false);

        if (slider != null)
            slider.value = 0f;
    }

    public void ForceCancelPlant()
    {
        bool wasPlanting = isPlanting;

        isPlanting = false;
        currentPlantTime = 0f;

        CombatFeedbackUI.Instance?.HideSpikeProgress();

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

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (IsLocalPlayer())
        {
            CombatFeedbackUI.Instance?.HideSpikeProgress();
            InteractionPromptUI.Instance?.Hide();
        }
    }
}
