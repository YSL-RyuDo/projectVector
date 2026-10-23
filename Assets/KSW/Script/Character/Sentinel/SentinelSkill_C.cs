using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Input = SentinelCaptureInput;
#endif

public class SentinelSkill_C : CharacterSkill
{
    public static bool IsSelectingSentinelC { get; private set; }
    private static int consumedThrowInputFrame = -1;

    public static bool ShouldBlockWeaponInput()
    {
        return
            IsSelectingSentinelC ||
            consumedThrowInputFrame == Time.frameCount;
    }

    [Header("C Trap")]
    [SerializeField] private Transform throwPoint;

    [Header("Throw")]
    [SerializeField] private float longThrowForce = 22f;
    [SerializeField] private float shortThrowForce = 12f;
    [SerializeField] private float upwardPower = 0.2f;

    private bool isReady;
    private PlayerIdentity playerIdentity;

    private void Awake()
    {
        playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (!GameplayActionPolicy.CanAct(this, GameplayAction.Skill)) { if(isReady)CancelReady(); return; }

        if (Input.GetKeyDown(KeyCode.C))
            UseSkill();

        if (!isReady)
            return;

        if (Input.GetMouseButtonDown(0))
            RequestThrow(longThrowForce);
        else if (Input.GetMouseButtonDown(1))
            RequestThrow(shortThrowForce);
        else if (Input.GetKeyDown(KeyCode.Escape))
            CancelReady();
    }

    protected override void UseSkill()
    {
        if (!isReady && !CanUsePurchasedSkill(SkillSlot.C))
            return;

        isReady = !isReady;
        IsSelectingSentinelC = isReady;

        if (CharacterSkillUI.Instance != null)
        {
            CharacterSkillUI.Instance.SentinelSkillText(
                isReady,
                isReady ? "C 투척 모드: 좌클릭(길게), 우클릭(짧게)" : ""
            );
        }
    }

    private void RequestThrow(float force)
    {
        if(GetComponent<SentinelCastMotion>() is { } motion && !motion.ReadyToCast)return;
        // 투척 후 선택 상태가 해제되어도 같은 프레임의 클릭을 무기가 다시 읽지 못하게 한다.
        consumedThrowInputFrame = Time.frameCount;

        Vector3 spawnPos =
            throwPoint != null
                ? throwPoint.position
                : transform.position + transform.forward + Vector3.up * 1.2f;

        Vector3 forward =
            Camera.main != null
                ? Camera.main.transform.forward
                : transform.forward;

        Vector3 throwDir = (forward + Vector3.up * upwardPower).normalized;
        Vector3 velocity = throwDir * force;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendSentinelCThrow(spawnPos, velocity);
            GetComponent<SentinelCastMotion>()?.Release();
        }

        CancelReady();
    }

    private void CancelReady()
    {
        isReady = false;
        IsSelectingSentinelC = false;

        if (CharacterSkillUI.Instance != null)
            CharacterSkillUI.Instance.SentinelSkillText(false, "");
    }


    private bool IsMine()
    {
        if (playerIdentity == null)
            playerIdentity = GetComponent<PlayerIdentity>();

        return playerIdentity != null &&
               NetworkManager.Instance != null &&
               playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }

    private void OnDisable()
    {
        if (isReady)
            CancelReady();
    }
}
