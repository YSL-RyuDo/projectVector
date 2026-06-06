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
    private bool isHalfDefused = false;

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

        playerSpike = ps;
        isPlayerInside = true;

        playerSpike.SetSpike(this);

        sliderObject = playerSpike.spikeSlider;

        if (sliderObject != null)
            defuseSlider = sliderObject.GetComponent<Slider>();

        Debug.Log($"[SPIKE] Player in defuse range. player={other.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerSpike ps = other.GetComponent<PlayerSpike>();

        if (ps == null)
            ps = other.GetComponentInParent<PlayerSpike>();

        if (ps == null)
            return;

        if (playerSpike != null && ps == playerSpike)
        {
            CancelDefuse();

            playerSpike.SetSpike(null);

            isPlayerInside = false;
            playerSpike = null;
            sliderObject = null;
            defuseSlider = null;

            Debug.Log($"[SPIKE] Player out defuse range. player={other.name}");
        }
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
        if (!isPlayerInside) return;
        if (isExploded) return;
        if (playerSpike == null) return;

        if (isDefusing)
            return;

        if (defuseStartRequested)
            return;

        defuseStartRequested = true;

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendSpikeDefuseStart();

        Debug.Log("[SPIKE] Defuse start request sent");
    }

    public void Defusing()
    {
        if (!isDefusing)
            return;

        currentDefuseTime += Time.deltaTime;

        if (defuseSlider != null)
            defuseSlider.value = currentDefuseTime / defuseTime;

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

        isDefusing = false;
        defuseStartRequested = false;

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendSpikeDefuseCancel();

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null)
            sliderObject.SetActive(false);
    }

    private void CompleteDefuse()
    {
        if (isExploded)
            return;

        isDefusing = false;
        defuseStartRequested = false;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null)
            sliderObject.SetActive(false);

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

        if (defuseSlider != null)
            defuseSlider.value = currentDefuseTime / defuseTime;

        Debug.Log($"[SPIKE] half defused from server={isHalfDefused}");
    }

    public void StartDefuseFromServer(bool halfDefused)
    {
        defuseStartRequested = false;

        isDefusing = true;
        isHalfDefused = halfDefused;

        currentDefuseTime = isHalfDefused
            ? defuseTime * 0.5f
            : 0f;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(false);

        if (sliderObject != null)
            sliderObject.SetActive(true);

        if (defuseSlider != null)
            defuseSlider.value = currentDefuseTime / defuseTime;
    }

    public void CancelDefuseFromServer(bool halfDefused)
    {
        defuseStartRequested = false;

        isDefusing = false;
        isHalfDefused = halfDefused;

        currentDefuseTime = isHalfDefused
            ? defuseTime * 0.5f
            : 0f;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null)
            sliderObject.SetActive(false);

        if (defuseSlider != null)
            defuseSlider.value = currentDefuseTime / defuseTime;
    }

    public void ForceCancelInteraction()
    {
        isDefusing = false;
        currentDefuseTime = 0f;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null)
            sliderObject.SetActive(false);

        if (defuseSlider != null)
            defuseSlider.value = 0f;

        Debug.Log("[SPIKE] Force cancel interaction");
    }
}