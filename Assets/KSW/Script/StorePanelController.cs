using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        int resultRandomIndex = Random.Range(0, _weaponRenderTextures.Length);

        ShowGachaResultPopUp(resultRandomIndex);
    }

    private void OnGachaPopupCloseButton()
    {
        _gachaPopUpUI.GetComponent<UIAnimator>().Close();
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
        color.a = alpha;
        image.color = color;
    }

    #endregion

    #region Profile Gacha Panel

    [Header("---------Profile Gacha Panel---------")]
    [SerializeField] private Button _profileGachaButton;

    private void ProfileGachaPanelStart()
    {

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