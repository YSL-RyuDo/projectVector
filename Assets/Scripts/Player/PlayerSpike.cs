using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerSpike : MonoBehaviour
{
    [SerializeField] private PlayerWeapon playerWeapon;
    [SerializeField] private LocalPlayerController playerMove;

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
        slider = spikeSlider.GetComponent<Slider>();
        playerWeapon = GetComponent<PlayerWeapon>();
        playerMove = GetComponent<LocalPlayerController>();
    }

    void Update()
    {
        // F 눌렀을 때
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (hasSpike)
                TryStartPlant();
            else if (spikeInRange != null)
                spikeInRange.TryStartDefuse();
        }

        // F 누르고 있는 동안
        if (Keyboard.current.fKey.isPressed)
        {
            if (isPlanting)
                Planting();

            if (spikeInRange != null)
                spikeInRange.Defusing();
        }

        // F 떼면
        if (Keyboard.current.fKey.wasReleasedThisFrame)
        {
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

        playerWeapon.SetWeaponVisibility(false); // 무기 숨기기
        playerMove.canMove = false;             // 이동 불가

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

        spikeSlider.SetActive(false);
        slider.value = 0f;

        playerWeapon.SetWeaponVisibility(true);  // 무기 다시 꺼내기
        playerMove.canMove = true;               // 이동 가능

        Debug.Log("Spike Planted");
    }

    void CancelPlant()
    {
        if (!isPlanting) return;

        Debug.Log("Cancel Planting");

        isPlanting = false;
        currentPlantTime = 0f;

        slider.value = 0f;
        spikeSlider.SetActive(false);

        playerWeapon.SetWeaponVisibility(true);  // 무기 다시 꺼내기
        playerMove.canMove = true;               // 이동 가능
    }

    //  Drop 요청
    public void RequestDrop()
    {
        if (!hasSpike) return;

        Debug.Log("Spike Dropped");

        hasSpike = false;

        SpikeSpawner.Instance.SpawnDropped(transform.position);
    }
}