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
    private float currentExplosionTime;
    private bool isExploded;

    [Header("UI")]
    private Slider defuseSlider;
    private GameObject sliderObject;

    private void Start()
    {
        playerSpike = GetComponentInParent<PlayerSpike>();
    }

    private void Update()
    {
        if (isPlanted)
        {
            ExplosionTimer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerSpike = other.GetComponent<PlayerSpike>();

        if (playerSpike == null)
            return;

        isPlayerInside = true;
        playerSpike.SetSpike(this);

        sliderObject = playerSpike.spikeSlider;

        if (sliderObject != null)
            defuseSlider = sliderObject.GetComponent<Slider>();

        Debug.Log("Player in");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playerSpike != null)
        {
            CancelDefuse();

            playerSpike.SetSpike(null);

            isPlayerInside = false;
            playerSpike = null;
            sliderObject = null;
            defuseSlider = null;
        }

        Debug.Log("Player out");
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
        currentExplosionTime = 0f;

        Debug.Log("Spike Planted");

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnSpikePlanted();
    }

    public void TryStartDefuse()
    {
        if (!isPlayerInside)
            return;

        if (isExploded)
            return;

        if (playerSpike == null)
            return;

        isDefusing = true;

        playerSpike.SetPlayerControl(false);

        if (sliderObject != null)
            sliderObject.SetActive(true);

        Debug.Log("Start Defuse");
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
            Debug.Log("Half Defused! (50%)");
        }

        if (currentDefuseTime >= defuseTime)
        {
            CompleteDefuse();
        }
    }

    public void CancelDefuse()
    {
        if (!isDefusing)
            return;

        isDefusing = false;

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (isHalfDefused)
        {
            currentDefuseTime = defuseTime * 0.5f;
            Debug.Log("Defuse Canceled - Progress saved at 50%");
        }
        else
        {
            currentDefuseTime = 0f;
            Debug.Log("Defuse Canceled - Progress reset to 0%");
        }

        if (defuseSlider != null)
            defuseSlider.value = currentDefuseTime / defuseTime;
    }

    private void CompleteDefuse()
    {
        if (isExploded)
            return;

        isExploded = true;
        isDefusing = false;

        if (RoundManager.Instance != null)
            RoundManager.Instance.RequestRoundEnd(false);

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (sliderObject != null)
            sliderObject.SetActive(false);

        Debug.Log("Defuse Complete - Defenders Win");

        Destroy(gameObject);
    }

    private void ExplosionTimer()
    {
        if (isExploded)
            return;

        currentExplosionTime += Time.deltaTime;

        if (currentExplosionTime >= explosionTime)
            Explode();
    }

    private void Explode()
    {
        if (isExploded)
            return;

        isExploded = true;
        isDefusing = false;

        if (sliderObject != null)
            sliderObject.SetActive(false);

        if (playerSpike != null)
            playerSpike.SetPlayerControl(true);

        if (RoundManager.Instance != null)
            RoundManager.Instance.RequestRoundEnd(true);

        Debug.Log("Spike Exploded - Attackers Win");

        Destroy(gameObject);
    }
}