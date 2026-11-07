using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardRowUI : MonoBehaviour
{
    [SerializeField] private Image charImage;
    [SerializeField] private TMP_Text nickNameText;
    [SerializeField] private TMP_Text kdaText;

    [SerializeField] private GameObject ultTextObject;
    [SerializeField] private GameObject weaponImageObject;
    [SerializeField] private GameObject creditTextObject;

    [SerializeField] private EquipmentIconCatalog equipmentIcons;
    [SerializeField] private Image pistolImage, knifeImage, armorImage, rowBackground, selfAccent;
    [SerializeField] private TMP_Text armorText, stateText;
    private TMP_Text creditText;

    private void Awake()
    {
        CacheCreditText();
    }

    private void CacheCreditText()
    {
        if (creditTextObject == null)
            return;

        creditText = creditTextObject.GetComponent<TMP_Text>();

        if (creditText == null)
            creditText = creditTextObject.GetComponentInChildren<TMP_Text>(true);
    }

    public void SetData(ScoreboardMemberData data, Sprite characterSprite, uint myPlayerId, bool isAlly = false)
    {
        gameObject.SetActive(true);

        if (charImage != null)
        {
            charImage.sprite = characterSprite;
            charImage.enabled = characterSprite != null;
        }

        if (nickNameText != null)
        {
            nickNameText.text = data.nickname;

            nickNameText.color = data.playerId == myPlayerId ? new Color(.62f,.9f,1f) : Color.white;
        }

        if (kdaText != null)
            kdaText.text = $"{data.kills} / {data.deaths} / {data.assists}";

        bool known = isAlly && data.hasLoadout;
        bool lifeKnown = data.hasLifeState || known;
        bool alive = !lifeKnown || data.equipmentAlive;
        if (rowBackground) rowBackground.color = isAlly ? new Color(.12f,.22f,.29f,.91f) : new Color(.25f,.15f,.20f,.91f);
        if (selfAccent) selfAccent.enabled = data.playerId == myPlayerId;
        if (stateText) { stateText.text = lifeKnown ? (alive ? "생존" : "사망") : ""; stateText.color = alive ? new Color(.63f,.83f,.85f) : new Color(.95f,.52f,.52f); }
        if (ultTextObject) {
            ultTextObject.SetActive(true);
            var text = ultTextObject.GetComponent<TMP_Text>();
            if(text) { text.text = !known ? "—" : data.ultimateUsed ? "사용 완료" : data.ultimatePoints == data.ultimateRequired ? "준비 완료" : data.ultimatePoints+" / "+data.ultimateRequired;
                text.color = known && !data.ultimateUsed && data.ultimatePoints == data.ultimateRequired ? new Color(.65f,.95f,.87f) : Color.white; }
        }

        if (weaponImageObject != null)
        {
            var image = weaponImageObject.GetComponent<Image>();
            var sprite = known && equipmentIcons ? equipmentIcons.Weapon(data.primaryWeaponId) : isAlly && data.hasEquipment && equipmentIcons ? equipmentIcons.Weapon(data.ownedWeaponId) : null;
            weaponImageObject.SetActive(sprite != null);
            if (image) { image.sprite = sprite; image.preserveAspect = true;
                image.color = data.equipmentAlive ? Color.white : new Color(.5f,.5f,.5f,1); }
        }
        SetIcon(pistolImage, known && equipmentIcons ? equipmentIcons.Weapon(data.pistolWeaponId) : null, alive);
        SetIcon(knifeImage, known && equipmentIcons ? equipmentIcons.Weapon(data.knifeWeaponId) : null, alive);
        SetIcon(armorImage, known && equipmentIcons && data.maxArmor>0 ? equipmentIcons.Armor(data.maxArmor>=100?1:0) : null, alive);
        if (armorText) armorText.text = known ? data.armor.ToString() : "—";

        if (creditTextObject != null)
            creditTextObject.SetActive(true);

        if (creditText == null)
            CacheCreditText();

        if (creditText != null)
            creditText.text = data.credits.ToString("N0");
    }

    static void SetIcon(Image image,Sprite sprite,bool alive) { if(!image)return; image.sprite=sprite; image.enabled=sprite; image.preserveAspect=true;image.color=alive?Color.white:new Color(.5f,.55f,.6f); }

    public void Clear()
    {
        if (charImage != null)
        {
            charImage.sprite = null;
            charImage.enabled = false;
        }

        if (nickNameText != null)
            nickNameText.text = "";

        if (kdaText != null)
            kdaText.text = "";

        if (creditText == null)
            CacheCreditText();

        if (creditText != null)
            creditText.text = "";

        if (creditTextObject != null)
            creditTextObject.SetActive(false);

        if (ultTextObject != null)
            ultTextObject.SetActive(false);

        if (weaponImageObject != null)
            weaponImageObject.SetActive(false);
        SetIcon(pistolImage,null,true);SetIcon(knifeImage,null,true);SetIcon(armorImage,null,true);
        if(armorText)armorText.text="";if(stateText)stateText.text="";if(selfAccent)selfAccent.enabled=false;

        gameObject.SetActive(false);
    }
}
