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
    private bool isHalfDefused = false; // 50% 달성 여부

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

    void Update()
    {
        if (isPlanted)
        {
            ExplosionTimer();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerSpike = other.GetComponent<PlayerSpike>();

        isPlayerInside = true;
        playerSpike.SetSpike(this);

        sliderObject = playerSpike.spikeSlider;
        defuseSlider = sliderObject.GetComponent<Slider>();

        Debug.Log("Player in");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerSpike != null)
        {
            CancelDefuse(); // 범위 나가면 해체 취소
            playerSpike.SetSpike(null);
            isPlayerInside = false;
            playerSpike = null;
            sliderObject = null;
            defuseSlider = null;
        }

        Debug.Log("Player out");
    }

    public override void OnUse() { }
    public override void OnDrop()
    {
        playerSpike.RequestDrop();
        this.OnUnequip();
    }

    public void OnDeploy()
    {
        isPlanted = true;
        // 필요하다면 여기서 설치 시 사운드나 이펙트 실행
    }

    //  Defuse 시작
    public void TryStartDefuse()
    {
        if (!isPlayerInside || isExploded) return;

        isDefusing = true;

        // 플레이어 제어권 뺏기 (무기 숨기기, 이동 불가)
        playerSpike?.SetPlayerControl(false);

        sliderObject?.SetActive(true); // UI 켜기
        Debug.Log("Start Defuse");
    }

    public void Defusing()
    {
        if (!isDefusing) return;

        currentDefuseTime += Time.deltaTime;

        if (defuseSlider != null)
        {
            defuseSlider.value = currentDefuseTime / defuseTime;
        }

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
        if (!isDefusing) return;

        isDefusing = false;

        // 플레이어 제어권 돌려주기 (무기 다시 꺼내기, 이동 가능)
        playerSpike?.SetPlayerControl(true);

        if (isHalfDefused)
        {
            currentDefuseTime = defuseTime * 0.5f; // 50%로 고정
            Debug.Log("Defuse Canceled - Progress saved at 50%");
        }

        else
        {
            currentDefuseTime = 0f; // 처음부터 다시
            Debug.Log("Defuse Canceled - Progress reset to 0%");
        }
    }

    void CompleteDefuse()
    {
        isExploded = true;

        // 해체 성공 시에도 제어권 돌려주기
        playerSpike?.SetPlayerControl(true);

        Debug.Log("Defuse Complete - Defenders Win");
        Destroy(gameObject);
    }

    //  폭발 타이머
    void ExplosionTimer()
    {
        if (isExploded) return;
        currentExplosionTime += Time.deltaTime;
        if (currentExplosionTime >= explosionTime) Explode();
    }

    void Explode()
    {
        isExploded = true;
        sliderObject?.SetActive(false);
        Debug.Log("Spike Exploded - Attackers Win");
        Destroy(gameObject);
    }
}