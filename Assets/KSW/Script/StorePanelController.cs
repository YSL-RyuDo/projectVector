using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorePanelController : MonoBehaviour
{
    void Start()
    {
        WeaponSkinGachaPanelStart();
    }

    void Update()
    {

    }

    #region Weapon Skin Gacha Panel

    [SerializeField] private RawImage[] _weaponPreviewImage;
    [SerializeField] private TMP_Text _weaponSkinNameText;
    [SerializeField] private string[] _weaponSkinName;

    [SerializeField] private float _changeInterval = 3f;
    [SerializeField] private float _fadeDuration = 0.5f;

    private int _currentIndex = 0;
    private Coroutine _fadeCoroutine;

    private void WeaponSkinGachaPanelStart()
    {
        if (_weaponPreviewImage == null || _weaponPreviewImage.Length == 0)
            return;

        for (int i = 0; i < _weaponPreviewImage.Length; i++)
        {
            SetAlpha(_weaponPreviewImage[i], i == 0 ? 1f : 0f);
        }

        // 첫 번째 스킨 이름 출력
        if (_weaponSkinNameText != null &&
            _weaponSkinName != null &&
            _weaponSkinName.Length > 0)
        {
            _weaponSkinNameText.text = _weaponSkinName[0];
        }

        _fadeCoroutine = StartCoroutine(WeaponPreviewFade());
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

            // 이미지가 변경된 순서에 맞춰 이름 변경
            if (_weaponSkinNameText != null &&
                _weaponSkinName != null &&
                _currentIndex < _weaponSkinName.Length)
            {
                _weaponSkinNameText.text = _weaponSkinName[_currentIndex];
            }
        }
    }

    private void SetAlpha(RawImage image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    #endregion
}