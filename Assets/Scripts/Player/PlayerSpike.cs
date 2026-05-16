using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerSpike : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;
    [SerializeField] private PlayerMovement playerMove;
    [SerializeField] private Transform dropPoint;

    [Header("Spike")]
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

    void Start()
    {
        //slider = spikeSlider.GetComponent<Slider>();
        playerItem = GetComponent<PlayerItem>();
        playerMove = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (GameController.Instance != null && GameController.Instance.IsPaused) return;

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
        if (!hasSpike) return;
        if (!isInsidePlantZone) return;

        Debug.Log("Start Planting");

        isPlanting = true;
        currentPlantTime = 0f;

        SetPlayerControl(false); // 제어권 뺏기

        spikeSlider.SetActive(true);
    }

    void Planting()
    {
        currentPlantTime += Time.deltaTime;

        slider.value = currentPlantTime / plantTime;

        if (currentPlantTime >= plantTime)
        {
            CompletePlant();
        }
    }

    void CompletePlant()
    {
        isPlanting = false;
        hasSpike = false;

        //  Spike 생성 (현재는 로컬, 나중엔 서버에서 처리)
        SpikeSpawner.Instance.SpawnPlanted(transform.position);

        RoundManager.Instance.OnSpikePlanted();

        SetPlayerControl(true); // 제어권 돌려주기

        Debug.Log("Spike Planted");
    }

    void CancelPlant()
    {
        if (!isPlanting) return;

        Debug.Log("Cancel Planting");

        isPlanting = false;
        currentPlantTime = 0f;

        SetPlayerControl(true); // 제어권 돌려주기
    }

    //  Drop 요청
    public void RequestDrop()
    {
        if (!hasSpike) return;

        Debug.Log("Spike Dropped");

        hasSpike = false;

        SpikeSpawner.Instance.SpawnDropped(dropPoint);
    }

    public void SetPlayerControl(bool canControl)
    {
        // true면 무기 보이고 이동 가능, false면 무기 숨기고 이동 불가
        playerItem.SetWeaponVisibility(canControl);
        playerMove.CanMove = canControl;

        // 만약 컨트롤을 되돌려받을 때 슬라이더가 켜져있다면 꺼주는 안전장치
        if (canControl)
        {
            spikeSlider.SetActive(false);
            slider.value = 0f;
        }
    }
}