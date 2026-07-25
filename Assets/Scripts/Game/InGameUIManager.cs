using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance;

    [Header("Player Info")]
    [SerializeField] private Image charImage;
    [SerializeField] private Image hpBar;
    [SerializeField] private Image barrierBar;
    [SerializeField] private TMP_Text hpText;

    [Header("Skill Info")]
    [SerializeField] private Image cSkillImage;
    [SerializeField] private Image qSkillImage;
    [SerializeField] private Image eSkillImage;
    [SerializeField] private Image xSkillImage;

    [Header("Weapon Info")]
    [SerializeField] private Image mainWeaponImage;
    [SerializeField] private Image subWeaponImage;
    [SerializeField] private Image knifeImage;
    [SerializeField] private Image throwWeaponImage;
    [SerializeField] private Image spikeImage;
    [SerializeField] private TMP_Text bulletText;

    [Header("Default Weapon Sprites")]
    [SerializeField] private Sprite defaultPistolSprite;
    [SerializeField] private Sprite defaultKnifeSprite;
    [SerializeField] private Sprite defaultThrowWeaponSprite;
    [SerializeField] private Sprite defaultSpikeSprite;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshairObject;

    private Sprite cachedSpikeSprite;
    private int currentHp;
    private int currentBarrier;
    private int maxHp;
    private int maxBarrier;

    [Header("Spike Plant / Defuse UI")]
    [SerializeField] private GameObject spikeSliderObject;

    public GameObject SpikeSliderObject => spikeSliderObject;

    private void Awake()
    {
        Instance = this;

        ResolveCrosshairReference();

        if (spikeImage != null)
        {
            cachedSpikeSprite = defaultSpikeSprite != null
                ? defaultSpikeSprite
                : spikeImage.sprite;

            spikeImage.gameObject.SetActive(false);
        }
    }

    private void ResolveCrosshairReference()
    {
        if (crosshairObject != null)
            return;

        crosshairObject = GameObject.Find("CrossHair");

        if (crosshairObject == null)
        {
            Debug.LogWarning(
                "[INGAME UI] CrossHair 오브젝트를 찾지 못했습니다. " +
                "Inspector의 Crosshair Object에 직접 연결하세요."
            );
        }
    }

    public void SetCrosshairVisible(bool visible)
    {
        ResolveCrosshairReference();

        if (crosshairObject == null)
            return;

        if (crosshairObject.activeSelf != visible)
            crosshairObject.SetActive(visible);
    }

    public void SetDisplayedPlayerADS(bool isADS)
    {
        SetCrosshairVisible(!isADS);
    }

    public void RefreshSpectatorCrosshair()
    {
        bool isSpectating =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating;

        if (!isSpectating)
        {
            SetCrosshairVisible(true);
            return;
        }

        bool targetIsADS = false;
        uint targetPlayerId =
            SpectatorManager.Instance.CurrentTargetPlayerId;

        if (NetworkManager.Instance != null &&
            NetworkManager.Instance.TryGetCombatSnapshot(
                targetPlayerId,
                out PlayerCombatSnapshot snapshot))
        {
            targetIsADS = snapshot.isADS;
        }

        SetDisplayedPlayerADS(targetIsADS);
    }

    public void InitializeLocalPlayerUI(
        int characterId,
        Sprite characterSprite,
        PickCharacterData characterData)
    {
        SetCharacterImage(characterSprite);
        SetSkillIcons(characterData);

        InitializeWeaponUI();
        SetCrosshairVisible(true);

        Debug.Log($"[INGAME UI INIT] characterId={characterId}");
    }

    private void SetCharacterImage(Sprite sprite)
    {
        if (charImage == null)
            return;

        charImage.sprite = sprite;
        charImage.enabled = sprite != null;
    }

    private void SetSkillIcons(PickCharacterData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[INGAME UI] PickCharacterData is null");
            return;
        }

        SetSkillIcon(cSkillImage, data.cSkill);
        SetSkillIcon(qSkillImage, data.qSkill);
        SetSkillIcon(eSkillImage, data.eSkill);
        SetSkillIcon(xSkillImage, data.xSkill);
    }

    private void SetSkillIcon(Image targetImage, PickSkillData skillData)
    {
        if (targetImage == null)
            return;

        if (skillData == null || skillData.skillIcon == null)
        {
            targetImage.enabled = false;
            targetImage.sprite = null;
            return;
        }

        targetImage.enabled = true;
        targetImage.sprite = skillData.skillIcon;
    }

    public void SetHpBarrier(int hp, int barrier, int maxHpValue, int maxBarrierValue)
    {
        currentHp = Mathf.Clamp(hp, 0, Mathf.Max(1, maxHpValue));
        currentBarrier = Mathf.Clamp(barrier, 0, 100);

        maxHp = Mathf.Max(1, maxHpValue);
        maxBarrier = Mathf.Max(0, maxBarrierValue);

        int currentTotal = currentHp + currentBarrier;
        int maxTotal = maxHp + maxBarrier;

        if (hpText != null)
            hpText.text = $"{currentTotal} / {maxTotal}";

        if (hpBar != null)
        {
            hpBar.fillAmount =
                Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));
        }

        if (barrierBar != null)
        {
            barrierBar.fillAmount =
                Mathf.Clamp01((float)currentBarrier / 100f);
        }
    }

    public void PlayDamageFeedback()
    {
        if (DreamForestPostProcessFeedback.Instance != null)
        {
            DreamForestPostProcessFeedback.Instance.PlayHit();
            return;
        }

        Debug.LogWarning("[INGAME UI] DreamForestPostProcessFeedback.Instance is null");
    }
    private void InitializeWeaponUI()
    {
        SetMainWeapon(null);
        SetSubWeapon(defaultPistolSprite);
        SetKnife(defaultKnifeSprite);
        SetThrowWeapon(defaultThrowWeaponSprite);
        SetSpike(defaultSpikeSprite);
    }

    public void SetMainWeapon(WeaponData weaponData)
    {
        if (mainWeaponImage == null)
            return;

        if (weaponData == null || weaponData.weaponIcon == null)
        {
            mainWeaponImage.sprite = null;
            mainWeaponImage.enabled = false;
            return;
        }

        mainWeaponImage.sprite = weaponData.weaponIcon;
        mainWeaponImage.enabled = true;
    }

    public void SetSubWeapon(Sprite sprite)
    {
        if (subWeaponImage == null)
            return;

        subWeaponImage.sprite = sprite;
        subWeaponImage.enabled = sprite != null;
    }

    public void SetKnife(Sprite sprite)
    {
        if (knifeImage == null)
            return;

        knifeImage.sprite = sprite;
        knifeImage.enabled = sprite != null;
    }

    public void SetThrowWeapon(Sprite sprite)
    {
        if (throwWeaponImage == null)
            return;

        throwWeaponImage.sprite = sprite;
        throwWeaponImage.enabled = sprite != null;
    }

    public void SetSpike(Sprite sprite)
    {
        if (spikeImage == null)
            return;

        spikeImage.sprite = sprite;
        spikeImage.enabled = sprite != null;
    }

    public void SetBulletText(int currentAmmo, int reserveAmmo)
    {
        if (bulletText == null)
            return;

        bulletText.text = $"{currentAmmo} / {reserveAmmo}";
    }

    public void SetSpectatorPlayerUI(
        int characterId,
        Sprite characterSprite,
        int hp,
        int barrier,
        int maxHpValue,
        int maxBarrierValue,
        int weaponId,
        int magAmmo,
        int reserveAmmo)
    {
        SetCharacterImage(characterSprite);

        if (PickManager.Instance != null)
        {
            PickCharacterData characterData =
                PickManager.Instance.GetCharacterData(
                    characterId
                );

            if (characterData != null)
            {
                SetSkillIcons(
                    characterData
                );
            }
        }

        SetHpBarrier(
            hp,
            barrier,
            maxHpValue,
            maxBarrierValue
        );

        SetBulletText(
            magAmmo,
            reserveAmmo
        );

        if (WeaponDropSpawner.Instance != null &&
            weaponId >= 0)
        {
            WeaponData weaponData =
                WeaponDropSpawner.Instance.GetWeaponData(
                    weaponId
                );

            if (weaponData != null)
            {
                switch (weaponData.weaponType)
                {
                    case WeaponData.WeaponType.Pistol:
                        SetSubWeapon(
                            weaponData.weaponIcon
                        );
                        break;

                    case WeaponData.WeaponType.Knife:
                        SetKnife(
                            weaponData.weaponIcon
                        );
                        break;

                    default:
                        SetMainWeapon(
                            weaponData
                        );
                        break;
                }
            }
        }

        // 관전 대상을 변경했을 때 이미 ADS 중인 대상이면 즉시 크로스헤어를 숨긴다.
        RefreshSpectatorCrosshair();
    }

    public void SetSpikePossession(bool hasSpike)
    {
        if (spikeImage == null)
        {
            Debug.LogWarning("[SPIKE UI] Spike Image가 연결되지 않았습니다.");
            return;
        }

        if (hasSpike)
        {
            if (spikeImage.sprite == null)
            {
                spikeImage.sprite = defaultSpikeSprite != null
                    ? defaultSpikeSprite
                    : cachedSpikeSprite;
            }

            spikeImage.enabled = true;
            spikeImage.gameObject.SetActive(true);

            Debug.Log("[SPIKE UI] 스파이크 보유 아이콘 표시");
        }
        else
        {
            spikeImage.gameObject.SetActive(false);

            Debug.Log("[SPIKE UI] 스파이크 보유 아이콘 숨김");
        }
    }
}
