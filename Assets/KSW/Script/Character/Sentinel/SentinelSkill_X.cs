using System.Collections;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Input = SentinelCaptureInput;
#endif
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

        if (!GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) { if(isReady || isCharging){CancelCharging();SetReady(false);} return; }

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

        if (!isReady || isCharging || (GetComponent<SentinelCastMotion>() is { } motion && !motion.ReadyToCast))
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
        if (!isReady && !CanUsePurchasedSkill(SkillSlot.X))
            return;

        if(!isReady && (SentinelCNetworkManager.Instance==null ||
            !SentinelCNetworkManager.Instance.HasInstalledDevice(playerIdentity.PlayerId)))
        {SentinelFeedbackUI.Instance?.Notice("먼저 장치를 설치하세요");return;}

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
            if(!isCharging || !isReady || !GameplayActionPolicy.CanAct(this,GameplayAction.Skill)){chargingRoutine=null;CancelCharging();SetReady(false);yield break;}
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

        // The server projects the ultimate onto our installed devices, never an aim-point.
        NetworkManager.Instance.SendSentinelXCast(transform.position);
        GetComponent<SentinelCastMotion>()?.Release();
    }

    private void CancelCharging()
    {
        if(chargingRoutine!=null){StopCoroutine(chargingRoutine);chargingRoutine=null;}
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
                ready ? "X 과충전 · 좌클릭을 길게 눌러 장치 강화" : ""
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
        if(!isReady && !isCharging)return;
        if (chargingRoutine != null)
            StopCoroutine(chargingRoutine);

        CancelCharging();
        SetReady(false);
    }
}
