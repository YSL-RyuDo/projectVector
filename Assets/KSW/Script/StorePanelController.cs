using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ProfileItemType
{
    PlayerCard,
    PlayerIcon,
    ProfileBorder
}

[System.Serializable]
public class ProfileGachaItem
{
    public string itemId;
    public string itemName;
    public ProfileItemType itemType;
    public Sprite itemSprite;
    public int refundCurrency = 10;
}

public class StorePanelController : MonoBehaviour
{
    void Start()
    {
        WeaponSkinGachaPanelStart();
        ProfileGachaPanelStart();
        ExchangeStorePanelStart();
    }

    void Update()
    {

    }

    #region Weapon Skin Gacha Panel

    [Header("---------Weapon Skin Gacha Panel---------")]
    [SerializeField] private RawImage[] _weaponPreviewImage;
    [SerializeField] private TMP_Text _weaponSkinNameText;
    [SerializeField] private string[] _weaponSkinName;

    [SerializeField] private float _changeInterval = 3f;
    [SerializeField] private float _fadeDuration = 0.5f;

    private int _currentIndex = 0;
    private Coroutine _fadeCoroutine;

    [Header("---------Gacha PopUp UI---------")]
    [SerializeField] private GameObject _gachaPopUpUI;
    [SerializeField] private RawImage _popUpWeaponPreviewImage;
    [SerializeField] private TMP_Text _popUpWeaponSkinNameText;
    [SerializeField] private RenderTexture[] _weaponRenderTextures;

    [SerializeField] private Button _weaponSkinGachaButton;
    [SerializeField] private Button _gachaCloseButton;

    [SerializeField] private GameObject[] _weaponPrefabs;

    private void WeaponSkinGachaPanelStart()
    {
        if (_weaponPreviewImage == null || _weaponPreviewImage.Length == 0)
            return;

        if (_gachaPopUpUI != null)
        {
            _gachaPopUpUI.SetActive(false);
        }

        for (int i = 0; i < _weaponPreviewImage.Length; i++)
        {
            SetAlpha(_weaponPreviewImage[i], i == 0 ? 1f : 0f);
        }

        if (_weaponSkinNameText != null &&
            _weaponSkinName != null &&
            _weaponSkinName.Length > 0)
        {
            _weaponSkinNameText.text = _weaponSkinName[0];
        }

        if (_fadeCoroutine == null)
        {
            _fadeCoroutine = StartCoroutine(WeaponPreviewFade());
        }

        _weaponSkinGachaButton.onClick.RemoveAllListeners();
        _weaponSkinGachaButton.onClick.AddListener(OnWeaponSkinGachaButton);
        _gachaCloseButton.onClick.AddListener(OnGachaPopupCloseButton);
    }

    private void OnWeaponSkinGachaButton()
    {
        if (_weaponRenderTextures == null || _weaponRenderTextures.Length == 0)
            return;

        WeaponRotate.CallRotateInit();

        int resultRandomIndex = Random.Range(0, _weaponRenderTextures.Length);

        ShowGachaResultPopUp(resultRandomIndex);
    }

    private void OnGachaPopupCloseButton()
    {
        if (_gachaPopUpUI != null && _gachaPopUpUI.TryGetComponent<UIAnimator>(out var animator))
        {
            animator.Close();
        }
        else if (_gachaPopUpUI != null)
        {
            _gachaPopUpUI.SetActive(false);
        }
    }

    private IEnumerator WeaponPreviewFade()
    {
        while (true)
        {
            yield return new WaitForSeconds(_changeInterval);

            int nextIndex = (_currentIndex + 1) % _weaponPreviewImage.Length;

            RawImage currentImage = _weaponPreviewImage[_currentIndex];
            RawImage nextImage = _weaponPreviewImage[nextIndex];

            float time = 0f;

            while (time < _fadeDuration)
            {
                time += Time.deltaTime;

                float progress = time / _fadeDuration;

                SetAlpha(currentImage, 1f - progress);
                SetAlpha(nextImage, progress);

                yield return null;
            }

            SetAlpha(currentImage, 0f);
            SetAlpha(nextImage, 1f);

            _currentIndex = nextIndex;

            if (_weaponSkinNameText != null &&
                _weaponSkinName != null &&
                _currentIndex < _weaponSkinName.Length)
            {
                _weaponSkinNameText.text = _weaponSkinName[_currentIndex];
            }
        }
    }

    private void ShowGachaResultPopUp(int randomIndex)
    {
        if (_gachaPopUpUI == null) return;

        if (_popUpWeaponPreviewImage != null &&
            _weaponRenderTextures != null &&
            randomIndex < _weaponRenderTextures.Length)
        {
            _popUpWeaponPreviewImage.texture = _weaponRenderTextures[randomIndex];
        }

        if (_popUpWeaponSkinNameText != null &&
            _weaponSkinName != null &&
            randomIndex < _weaponSkinName.Length)
        {
            _popUpWeaponSkinNameText.text = _weaponSkinName[randomIndex];
        }

        if (_gachaPopUpUI.TryGetComponent<UIAnimator>(out var animator))
        {
            animator.Show();
        }
        else
        {
            _gachaPopUpUI.SetActive(true);
        }
    }

    private void SetAlpha(RawImage image, float alpha)
    {
        if (image == null) return;
        Color color = image.color;
        Color newColor = new Color(color.r, color.g, color.b, alpha);
        image.color = newColor;
    }

    #endregion

    #region Profile Gacha Panel

    [Header("---------Profile Gacha Panel---------")]
    [SerializeField] private Button _profileGachaButton;
    [SerializeField] private Button _profileGachaCloseButton;

    [Header("---------Profile PopUp UI---------")]
    [SerializeField] private GameObject _profileGachaPopUpUI;

    [SerializeField] private GameObject _cardHandler;
    [SerializeField] private GameObject _profileHandler;
    [SerializeField] private GameObject _borderHandler;

    [SerializeField] private Image _cardImage;
    [SerializeField] private Image _profileIconImage;
    [SerializeField] private Image _profileBorderImage;
    [SerializeField] private Sprite _defaultProfileIcon;

    [Header("---------Profile Result Text UI---------")]
    [SerializeField] private TMP_Text _statusStateText;
    [SerializeField] private TMP_Text _itemCategoryTitleText;

    [Header("---------Profile Gacha Data Pool---------")]
    [SerializeField] private List<ProfileGachaItem> _profileItemPool = new List<ProfileGachaItem>();

    [Header("---------Debug / Test Option---------")]
    [SerializeField] private bool _forceDuplicateTest = false;

    private HashSet<string> _ownedItemIds = new HashSet<string>();

    private void ProfileGachaPanelStart()
    {
        if (_profileGachaPopUpUI != null)
        {
            _profileGachaPopUpUI.SetActive(false);
        }

        if (_profileGachaButton != null)
        {
            _profileGachaButton.onClick.RemoveAllListeners();
            _profileGachaButton.onClick.AddListener(OnProfileGachaButton);
        }

        if (_profileGachaCloseButton != null)
        {
            _profileGachaCloseButton.onClick.RemoveAllListeners();
            _profileGachaCloseButton.onClick.AddListener(OnProfileGachaCloseButton);
        }
    }

    private void OnProfileGachaButton()
    {
        if (_profileItemPool == null || _profileItemPool.Count == 0) return;

        int randomIndex = Random.Range(0, _profileItemPool.Count);
        ProfileGachaItem resultItem = _profileItemPool[randomIndex];

        ShowProfileGachaResultPopUp(resultItem);
    }

    private void OnProfileGachaCloseButton()
    {
        if (_profileGachaPopUpUI != null && _profileGachaPopUpUI.TryGetComponent<UIAnimator>(out var animator))
        {
            animator.Close();
        }
        else if (_profileGachaPopUpUI != null)
        {
            _profileGachaPopUpUI.SetActive(false);
        }
    }

    private void ShowProfileGachaResultPopUp(ProfileGachaItem item)
    {
        if (_profileGachaPopUpUI == null || item == null) return;

        bool isDuplicate = _ownedItemIds.Contains(item.itemId) || _forceDuplicateTest;
        if (!isDuplicate)
        {
            _ownedItemIds.Add(item.itemId);
        }

        switch (item.itemType)
        {
            case ProfileItemType.PlayerCard:
                if (_cardHandler != null) _cardHandler.SetActive(true);
                if (_profileHandler != null) _profileHandler.SetActive(false);
                if (_borderHandler != null) _borderHandler.SetActive(false);

                if (_cardImage != null) _cardImage.sprite = item.itemSprite;
                break;

            case ProfileItemType.PlayerIcon:
                if (_cardHandler != null) _cardHandler.SetActive(false);
                if (_profileHandler != null) _profileHandler.SetActive(true);
                if (_borderHandler != null) _borderHandler.SetActive(false);

                if (_profileIconImage != null) _profileIconImage.sprite = item.itemSprite;
                break;

            case ProfileItemType.ProfileBorder:
                if (_cardHandler != null) _cardHandler.SetActive(false);
                if (_profileHandler != null) _profileHandler.SetActive(true);
                if (_borderHandler != null) _borderHandler.SetActive(true);

                if (_profileIconImage != null) _profileIconImage.sprite = _defaultProfileIcon;
                if (_profileBorderImage != null) _profileBorderImage.sprite = item.itemSprite;
                break;
        }

        if (isDuplicate)
        {
            if (_statusStateText != null) _statusStateText.text = "DUPLICATE";
            if (_itemCategoryTitleText != null)
            {
                _itemCategoryTitleText.text = $"¿Á»≠ +{item.refundCurrency}";
            }
        }
        else
        {
            if (_statusStateText != null) _statusStateText.text = "NEW";
            if (_itemCategoryTitleText != null)
            {
                _itemCategoryTitleText.text = item.itemName;
            }
        }

        if (_profileGachaPopUpUI.TryGetComponent<UIAnimator>(out var animator))
        {
            animator.Show();
        }
        else
        {
            _profileGachaPopUpUI.SetActive(true);
        }
    }

    #endregion

    #region Exchange Store Panel

    [Header("---------Exchange Store Panel---------")]
    [SerializeField] private GameObject _popup;

    private void ExchangeStorePanelStart()
    {

    }
    #endregion
}