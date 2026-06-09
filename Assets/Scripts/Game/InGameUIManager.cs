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
    }

    public void InitializeLocalPlayerUI(
    int characterId,
    Sprite characterSprite,
    PickCharacterData characterData)
    {
        SetCharacterImage(characterSprite);
        SetSkillIcons(characterData);

        InitializeWeaponUI();

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
            // HP바의 최대치는 항상 maxHp, 보통 100
            hpBar.fillAmount =
                Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));
        }

        if (barrierBar != null)
        {
            // 베리어바의 UI 최대치는 항상 100
            // Light 50이면 0.5, Heavy 100이면 1.0
            barrierBar.fillAmount =
                Mathf.Clamp01((float)currentBarrier / 100f);
        }
    }

    private void InitializeWeaponUI()
    {
        SetMainWeapon(null);
        SetSubWeapon(defaultPistolSprite);
        SetKnife(defaultKnifeSprite);
        SetThrowWeapon(defaultThrowWeaponSprite);
        SetSpike(defaultSpikeSprite);

        SetBulletText(0, 0);
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

    
}