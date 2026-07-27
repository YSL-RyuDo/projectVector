using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SentinelSkill_X : CharacterSkill
{
    public static bool IsSelectingSentinelX { get; private set; }
    public static bool IsChargingSentinelX { get; private set; }

    private static int consumedCastInputFrame = -1;

    public static bool ShouldBlockWeaponInput()
    {
        return
            IsSelectingSentinelX ||
            IsChargingSentinelX ||
            consumedCastInputFrame == Time.frameCount;
    }

    [SerializeField] private float chargingTime = 1.5f;
    [SerializeField] private float skillDistance = 20f;

    private Slider chargingSlider;
    private bool isReady;
    private bool isCharging;
    private PlayerIdentity playerIdentity;
    private Coroutine chargingRoutine;

    private void Awake()
    {
        playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Start()
    {
        if (IsMine())
            TryResolveChargingSlider();
    }

    private void TryResolveChargingSlider()
    {
        if (chargingSlider != null)
            return;

        Slider[] sliders = FindObjectsByType<Slider>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Slider slider in sliders)
        {
            if (slider != null &&
                slider.gameObject.name ==
                "SentinelXChargingSlider")
            {
                chargingSlider = slider;
                break;
            }
        }

        if (chargingSlider == null)
            return;

        chargingSlider.minValue = 0f;
        chargingSlider.maxValue = 1f;
        chargingSlider.value = 0f;
        chargingSlider.interactable = false;
        chargingSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (chargingSlider == null)
            TryResolveChargingSlider();

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (isCharging)
            {
                CancelCharging();
                SetReady(false);
                return;
            }

            UseSkill();
        }

        if (!isReady || isCharging)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            consumedCastInputFrame = Time.frameCount;
            chargingRoutine = StartCoroutine(XSkillRoutine());
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetReady(false);
        }
    }

    protected override void UseSkill()
    {
        if (RoundManager.Instance != null &&
            !RoundManager.Instance.CanAttack)
        {
            return;
        }

        SetReady(!isReady);
    }

    private IEnumerator XSkillRoutine()
    {
        isCharging = true;
        IsChargingSentinelX = true;
        float timer = 0f;

        if (chargingSlider != null)
        {
            chargingSlider.gameObject.SetActive(true);
            chargingSlider.value = 0f;
        }

        while (timer < chargingTime)
        {
            if (!Input.GetMouseButton(0))
            {
                CancelCharging();
                yield break;
            }

            timer += Time.deltaTime;

            if (chargingSlider != null)
                chargingSlider.value = timer / chargingTime;

            yield return null;
        }

        consumedCastInputFrame = Time.frameCount;
        SendCastRequest();
        CancelCharging();
        SetReady(false);
    }

    private void SendCastRequest()
    {
        if (Camera.main == null ||
            NetworkManager.Instance == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            )
        );

        Vector3 spawnPosition;

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                skillDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            spawnPosition = hit.point;
        }
        else
        {
            spawnPosition =
                ray.origin + ray.direction * skillDistance;
        }

        NetworkManager.Instance.SendSentinelXCast(spawnPosition);
    }

    private void CancelCharging()
    {
        isCharging = false;
        IsChargingSentinelX = false;
        chargingRoutine = null;

        if (chargingSlider != null)
        {
            chargingSlider.value = 0f;
            chargingSlider.gameObject.SetActive(isReady);
        }
    }

    private void SetReady(bool ready)
    {
        isReady = ready;
        IsSelectingSentinelX = ready;

        if (chargingSlider != null)
        {
            chargingSlider.value = 0f;
            chargingSlider.gameObject.SetActive(ready);
        }

        if (CharacterSkillUI.Instance != null)
        {
            CharacterSkillUI.Instance.SentinelSkillText(
                ready,
                ready ? "X 궁극기: 좌클릭을 길게 눌러 차징" : ""
            );
        }
    }

    private bool IsMine()
    {
        if (playerIdentity == null)
            playerIdentity = GetComponent<PlayerIdentity>();

        return
            playerIdentity != null &&
            NetworkManager.Instance != null &&
            playerIdentity.PlayerId ==
            NetworkManager.Instance.MyPlayerId;
    }

    private void OnDisable()
    {
        if (chargingRoutine != null)
            StopCoroutine(chargingRoutine);

        CancelCharging();
        SetReady(false);
    }
}
