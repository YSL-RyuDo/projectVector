using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance;
    [SerializeField] private CombatHudReadout combatReadout;

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

    [Header("Skill State Colors")]
    [SerializeField] private Color lockedSkillColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color availableSkillColor = Color.white;
    [SerializeField] private Color usedSkillColor = new Color(0.14f, 0.14f, 0.14f, 1f);

    [Header("Skill Gauge Images")]
    [SerializeField] private Image cSkillGauge;
    [SerializeField] private Image qSkillGauge;
    [SerializeField] private Image eSkillGauge;
    [SerializeField] private Image xSkillGauge;
    [SerializeField] private TMP_Text ultimatePointText;
    [SerializeField] private Color availableGaugeColor = new Color(0.25f, 0.9f, 0.8f, 1f);

    [Header("Weapon Info")]
    [SerializeField] private EquipmentIconCatalog equipmentIcons;
    [SerializeField] private GameObject spikeSlotRoot;
    [SerializeField] private Image[] weaponSlotBackgrounds = new Image[4];
    [SerializeField] private Color slotColor = new Color(.22f,.28f,.36f,.9f);
    [SerializeField] private Color selectedSlotColor = new Color(.36f,.47f,.57f,.95f);
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
        SkillInventoryState.Changed += RefreshSkillAvailability;

        ResolveSkillGaugeReferences();
        ResolveCrosshairReference();

        if (spikeImage != null)
        {
            cachedSpikeSprite = equipmentIcons && equipmentIcons.device ? equipmentIcons.device : defaultSpikeSprite != null
                ? defaultSpikeSprite
                : spikeImage.sprite;

            (spikeSlotRoot ? spikeSlotRoot : spikeImage.gameObject).SetActive(false);
        }
    }


    private void OnDestroy()
    {
        SkillInventoryState.Changed -= RefreshSkillAvailability;

        if (Instance == this)
            Instance = null;
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

    public void SetSelectedWeaponSlot(int inventorySlot)
    {
        int row = inventorySlot == 4 ? 3 : inventorySlot;
        for (int i=0;i<weaponSlotBackgrounds.Length;i++)
            if (weaponSlotBackgrounds[i]) weaponSlotBackgrounds[i].color = i==row ? selectedSlotColor : slotColor;
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
        SkillInventoryState.SetCharacter(characterData);
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
        RefreshSkillAvailability();
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
    public void RefreshSkillAvailability()
    {
        SetSkillStateVisual(cSkillImage, cSkillGauge, SkillSlot.C);
        SetSkillStateVisual(qSkillImage, qSkillGauge, SkillSlot.Q);
        SetSkillStateVisual(eSkillImage, eSkillGauge, SkillSlot.E);
        SetSkillStateVisual(xSkillImage, xSkillGauge, SkillSlot.X);
        if (combatReadout) combatReadout.PresentSkills();
        else if (ultimatePointText) ultimatePointText.text = SkillInventoryState.HasUltimateState ?
            (SkillInventoryState.IsUsed(SkillSlot.X) ? "사용 완료" : SkillInventoryState.CanUse(SkillSlot.X) ? "준비 완료" : SkillInventoryState.UltimatePoints+" / "+SkillInventoryState.UltimateRequired) : "\u2014";
        if (xSkillGauge && SkillInventoryState.HasUltimateState) { xSkillGauge.fillAmount=(float)SkillInventoryState.UltimatePoints/SkillInventoryState.UltimateRequired; xSkillGauge.color=SkillInventoryState.IsUsed(SkillSlot.X)?usedSkillColor:availableGaugeColor; }
    }

    private void SetSkillStateVisual(
        Image targetImage,
        Image gaugeImage,
        SkillSlot slot)
    {
        bool canUse = SkillInventoryState.CanUse(slot);
        bool used = SkillInventoryState.IsUsed(slot);

        if (targetImage != null && targetImage.sprite != null)
        {
            targetImage.color = canUse
                ? availableSkillColor
                : used
                    ? usedSkillColor
                    : lockedSkillColor;
        }

        if (gaugeImage == null)
            return;

        gaugeImage.type = Image.Type.Filled;
        gaugeImage.fillMethod = Image.FillMethod.Horizontal;
        gaugeImage.fillOrigin = 0;
        gaugeImage.fillClockwise = true;
        gaugeImage.fillAmount = canUse || used ? 1f : 0f;
        gaugeImage.color = canUse
            ? availableGaugeColor
            : usedSkillColor;
    }

    private void ResolveSkillGaugeReferences()
    {
        cSkillGauge = ResolveSkillGauge(cSkillImage, cSkillGauge, "CSkillGauge");
        qSkillGauge = ResolveSkillGauge(qSkillImage, qSkillGauge, "QSkillGauge");
        eSkillGauge = ResolveSkillGauge(eSkillImage, eSkillGauge, "ESkillGauge");
        xSkillGauge = ResolveSkillGauge(xSkillImage, xSkillGauge, "XSkillGauge");
    }

    private Image ResolveSkillGauge(
        Image skillImage,
        Image currentGauge,
        string childName)
    {
        if (currentGauge != null)
            return currentGauge;

        if (skillImage == null)
            return null;

        Transform child = skillImage.transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }
    public void SetHpBarrier(int hp, int barrier, int maxHpValue, int maxBarrierValue)
    {
        combatReadout?.PresentHealth(hp,barrier,maxHpValue,maxBarrierValue);
        currentHp = Mathf.Clamp(hp, 0, Mathf.Max(1, maxHpValue));
        currentBarrier = Mathf.Clamp(barrier, 0, 100);

        maxHp = Mathf.Max(1, maxHpValue);
        maxBarrier = Mathf.Max(0, maxBarrierValue);

        int currentTotal = currentHp + currentBarrier;
        int maxTotal = maxHp + maxBarrier;

        if (hpText != null)
            hpText.text = currentHp.ToString();

        if (hpBar != null)
        {
            hpBar.fillAmount =
                Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));
        }

        if (barrierBar != null)
        {
            barrierBar.fillAmount =
                Mathf.Clamp01(maxBarrier > 0 ? currentBarrier / (float)maxBarrier : 0);
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
        SetSubWeapon(equipmentIcons ? equipmentIcons.Weapon(0) : defaultPistolSprite);
        SetKnife(equipmentIcons ? equipmentIcons.Weapon(13) : defaultKnifeSprite);
        SetThrowWeapon(defaultThrowWeaponSprite);
        SetSpike(equipmentIcons && equipmentIcons.device ? equipmentIcons.device : defaultSpikeSprite);
    }

    public Sprite ResolveWeaponIcon(WeaponData data)
    {
        if (!data) return null;
        var sprite = equipmentIcons ? equipmentIcons.Weapon(data.weaponId) : null;
        return sprite ? sprite : data.weaponIcon;
    }

    public void SetMainWeapon(WeaponData data) => SetWeaponImage(mainWeaponImage, ResolveWeaponIcon(data));
    public void SetSubWeaponData(WeaponData data) => SetSubWeapon(ResolveWeaponIcon(data));
    public void SetKnifeData(WeaponData data) => SetKnife(ResolveWeaponIcon(data));

    static void SetWeaponImage(Image target, Sprite sprite)
    {
        if (!target) return;
        target.sprite = sprite;
        target.preserveAspect = true;
        target.enabled = sprite != null;
    }

    public void SetSubWeapon(Sprite sprite)
    {
        if (subWeaponImage == null)
            return;

        SetWeaponImage(subWeaponImage, sprite);
    }

    public void SetKnife(Sprite sprite)
    {
        if (knifeImage == null)
            return;

        SetWeaponImage(knifeImage, sprite);
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

        bulletText.text = currentAmmo.ToString();
        combatReadout?.PresentAmmo(currentAmmo, reserveAmmo);
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

        SetMainWeapon(null);
        SetSubWeapon(null);
        SetKnife(null);

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
                        SetSubWeaponData(weaponData);
                        break;

                    case WeaponData.WeaponType.Knife:
                        SetKnifeData(weaponData);
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
            if (equipmentIcons && equipmentIcons.device) spikeImage.sprite = equipmentIcons.device;
            if (spikeImage.sprite == null)
            {
                spikeImage.sprite = defaultSpikeSprite != null
                    ? defaultSpikeSprite
                    : cachedSpikeSprite;
            }

            spikeImage.enabled = true;
            spikeImage.gameObject.SetActive(true);
            (spikeSlotRoot ? spikeSlotRoot : spikeImage.gameObject).SetActive(true);

            Debug.Log("[SPIKE UI] 스파이크 보유 아이콘 표시");
        }
        else
        {
            (spikeSlotRoot ? spikeSlotRoot : spikeImage.gameObject).SetActive(false);

            Debug.Log("[SPIKE UI] 스파이크 보유 아이콘 숨김");
        }
    }
}
