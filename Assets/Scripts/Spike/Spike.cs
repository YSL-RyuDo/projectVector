using UnityEngine;
using UnityEngine.UI;

public class Spike : Item
{
    private bool isPlayerInside;
    private PlayerSpike playerSpike;

    public bool isPlanted = false;

    [Header("Defuse")]
    public float defuseTime = 5f;

    private float currentDefuseTime;
    private bool isDefusing;
    private bool isHalfDefused;

    [Header("Explosion")]
    public float explosionTime = 40f;

    private bool isExploded;

    [Header("UI")]
    private Slider defuseSlider;
    private GameObject sliderObject;

    private bool halfSentToServer;
    private bool defuseStartRequested;

    public static Spike PlantedSpike { get; private set; }

    [Header("Spectator")]
    [SerializeField] private Transform spectatePoint;

    public Transform SpectatePoint => spectatePoint;

    private void Start()
    {
        playerSpike = GetComponentInParent<PlayerSpike>();
    }

    private void OnDestroy()
    {
        if (IsLocalDefuser())
            CombatFeedbackUI.Instance?.HideSpikeProgress();

        if (PlantedSpike == this)
            PlantedSpike = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerSpike ps = other.GetComponent<PlayerSpike>();

        if (ps == null)
            ps = other.GetComponentInParent<PlayerSpike>();

        if (ps == null)
            return;

        // 각 클라이언트에서는 자기 플레이어만 해체 입력 대상으로 등록한다.
        if (!ps.IsLocalPlayer())
            return;

        playerSpike = ps;
        isPlayerInside = true;

        playerSpike.SetSpike(this);

        sliderObject = playerSpike.spikeSlider;

        if (sliderObject != null)
            defuseSlider = sliderObject.GetComponent<Slider>();

        Debug.Log($"[SPIKE] Local player entered defuse range. player={other.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerSpike ps = other.GetComponent<PlayerSpike>();

        if (ps == null)
            ps = other.GetComponentInParent<PlayerSpike>();

        if (ps == null || ps != playerSpike)
            return;

        CancelDefuse();

        playerSpike.SetSpike(null);

        isPlayerInside = false;
        playerSpike = null;
        sliderObject = null;
        defuseSlider = null;

        CombatFeedbackUI.Instance?.HideSpikeProgress();

        Debug.Log($"[SPIKE] Local player left defuse range. player={other.name}");
    }

    private bool IsLocalDefuser()
    {
        return playerSpike != null && playerSpike.IsLocalPlayer();
    }

    public override void OnUse()
    {
    }

    public override void OnDrop()
    {
        if (playerSpike != null)
            playerSpike.RequestDrop();

        OnUnequip();
    }

    public void OnDeploy()
    {
        isPlanted = true;
        isExploded = false;

        isHalfDefused = false;
        halfSentToServer = false;
        defuseStartRequested = false;
        currentDefuseTime = 0f;

        PlantedSpike = this;

        if (spectatePoint == null)
        {
            Transform found = transform.Find("SpectatePoint");

            if (found != null)
                spectatePoint = found;
        }

        Debug.Log("[SPIKE] Planted and registered as PlantedSpike");

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnSpikePlanted();
    }

    public void TryStartDefuse()
    {
        if (!isPlayerInside)
            return;

        if (isExploded)
            return;

        if (playerSpike == null || !IsLocalDefuser())
            return;

        if (isDefusing || defuseStartRequested)
            return;

        defuseStartRequested = true;

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendSpikeDefuseStart();

        Debug.Log("[SPIKE] Defuse start request sent");
    }

    public void Defusing()
    {
        if (!isDefusing || !IsLocalDefuser())
            return;

        currentDefuseTime += Time.deltaTime;

        float progress = defuseTime > 0f
            ? Mathf.Clamp01(currentDefuseTime / defuseTime)
            : 0f;

        if (defuseSlider != null)
            defuseSlider.value = progress;

        CombatFeedbackUI.Instance?.ShowSpikeProgress(
            "스파이크를 해체하는 중입니다.",
            progress
        );

        if (!isHalfDefused && currentDefuseTime >= defuseTime * 0.5f)
        {
            isHalfDefused = true;
            currentDefuseTime = defuseTime * 0.5f;

            if (!halfSentToServer)
            {
                halfSentToServer = true;

                if (NetworkManager.Instance != null)
                    NetworkManager.Instance.SendSpikeDefuseHalf();
            }

            Debug.Log("[SPIKE] Half defuse request sent");
        }

        if (currentDefuseTime >= defuseTime)
            CompleteDefuse();
    }

    public void CancelDefuse()
    {
        if (!isDefusing && !defuseStartRequested)
            return;

        bool localDefuser = IsLocalDefuser();

        isDefusing = false;
        defuseStartRequested = false;

        if (NetworkManager.Instance != null && localDefuser)
            NetworkManager.Instance.SendSpikeDefuseCancel();

        if (playerSpike != null && localDefuser)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null && localDefuser)
            sliderObject.SetActive(false);

        if (localDefuser)
            CombatFeedbackUI.Instance?.HideSpikeProgress();

        Debug.Log("[SPIKE] Defuse cancelled");
    }

    private void CompleteDefuse()
    {
        if (isExploded || !IsLocalDefuser())
            return;

        isDefusing = false;
        defuseStartRequested = false;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null)
            sliderObject.SetActive(false);

        CombatFeedbackUI.Instance?.HideSpikeProgress();

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendSpikeDefuseComplete();

        Debug.Log("[SPIKE] Defuse complete request sent");
    }

    public void SetHalfDefusedFromServer(bool value)
    {
        isHalfDefused = value;

        currentDefuseTime = isHalfDefused
            ? defuseTime * 0.5f
            : 0f;

        float progress = defuseTime > 0f
            ? Mathf.Clamp01(currentDefuseTime / defuseTime)
            : 0f;

        if (defuseSlider != null)
            defuseSlider.value = progress;

        if (isDefusing && IsLocalDefuser())
        {
            CombatFeedbackUI.Instance?.ShowSpikeProgress(
                "스파이크를 해체하는 중입니다.",
                progress
            );
        }

        Debug.Log($"[SPIKE] half defused from server={isHalfDefused}");
    }

    public void StartDefuseFromServer(bool halfDefused)
    {
        defuseStartRequested = false;

        isDefusing = true;
        isHalfDefused = halfDefused;
        halfSentToServer = halfDefused;

        currentDefuseTime = isHalfDefused
            ? defuseTime * 0.5f
            : 0f;

        if (!IsLocalDefuser())
            return;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(false);

        if (sliderObject != null)
            sliderObject.SetActive(true);

        float progress = defuseTime > 0f
            ? Mathf.Clamp01(currentDefuseTime / defuseTime)
            : 0f;

        if (defuseSlider != null)
            defuseSlider.value = progress;

        CombatFeedbackUI.Instance?.ShowSpikeProgress(
            "스파이크를 해체하는 중입니다.",
            progress
        );
    }

    public void CancelDefuseFromServer(bool halfDefused)
    {
        defuseStartRequested = false;

        isDefusing = false;
        isHalfDefused = halfDefused;
        halfSentToServer = halfDefused;

        currentDefuseTime = isHalfDefused
            ? defuseTime * 0.5f
            : 0f;

        if (!IsLocalDefuser())
            return;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null)
            sliderObject.SetActive(false);

        if (defuseSlider != null)
            defuseSlider.value = defuseTime > 0f
                ? currentDefuseTime / defuseTime
                : 0f;

        CombatFeedbackUI.Instance?.HideSpikeProgress();
    }

    public void ForceCancelInteraction()
    {
        bool localDefuser = IsLocalDefuser();

        isDefusing = false;
        defuseStartRequested = false;
        currentDefuseTime = 0f;
        isHalfDefused = false;
        halfSentToServer = false;

        if (playerSpike != null && localDefuser)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null && localDefuser)
            sliderObject.SetActive(false);

        if (defuseSlider != null && localDefuser)
            defuseSlider.value = 0f;

        if (localDefuser)
            CombatFeedbackUI.Instance?.HideSpikeProgress();

        Debug.Log("[SPIKE] Force cancel interaction");
    }
}