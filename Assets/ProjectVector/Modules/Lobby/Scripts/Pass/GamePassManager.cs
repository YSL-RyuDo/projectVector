using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public enum ItemType
{
    Icon,
    Weapon,
    Card
}

[System.Serializable]
public class ItemData
{
    public string itemName;
    public Sprite itemSprite;
    public ItemType itemType;
    public bool isAcquired = false;
}

public class GamePassManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Slider _passPointSlider;
    [SerializeField] private TextMeshProUGUI _passPointText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _acquireButton;
    [SerializeField] private TextMeshProUGUI _acquireButtonText;

    [Header("Item Spawning Settings")]
    [SerializeField] private List<ItemData> _itemDataList;
    [SerializeField] private GameObject _itemBoxPrefab;
    [SerializeField] private Transform _contentTransform;

    [Header("Outline Color Settings")]
    [SerializeField] private Color _lockedColor = Color.black;
    [SerializeField] private Color _unlockedColor = Color.green;

    [Header("Display Settings")]
    [SerializeField] private Image _iconHandleImage;
    [SerializeField] private Image _weaponHandleImage;
    [SerializeField] private Image _cardHandleImage;
    [SerializeField] private TextMeshProUGUI _handleText;

    [SerializeField] private GameObject _acquirePopup;

    private float _maxPassPoint = 0;
    private List<UnityEngine.UI.Outline> _itemOutlines = new List<UnityEngine.UI.Outline>();
    private int _currentSelectedIndex = 0;

    void Start()
    {
        _maxPassPoint = _itemDataList.Count * 10;
        if (_passPointSlider != null)
        {
            _passPointSlider.maxValue = _maxPassPoint;
        }
        InitializeItemBoxes();
    }

    void Update()
    {
        _passPointText.text = _passPointSlider.value.ToString("F0") + " Point";
        UpdateOutlines();
        UpdateAcquireButtonState();
    }

    public void InitializeItemBoxes()
    {
        _itemOutlines.Clear();

        foreach (Transform child in _contentTransform)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < _itemDataList.Count; i++)
        {
            AddNewItemBox(_itemDataList[i], i);
        }

        if (_itemDataList.Count > 0)
        {
            SelectItem(_itemDataList[0], 0);
        }
    }

    public void AddNewItemBox(ItemData itemData, int index)
    {
        GameObject newItemBox = Instantiate(_itemBoxPrefab, _contentTransform);

        Transform iconTransform = newItemBox.transform.Find("Item Image");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null && itemData.itemSprite != null)
            {
                iconImage.sprite = itemData.itemSprite;
            }
        }

        TextMeshProUGUI percentText = newItemBox.GetComponentInChildren<TextMeshProUGUI>();
        if (percentText != null)
        {
            int requiredPercent = (index + 1) * 10;
            percentText.text = requiredPercent.ToString() + " Point";
        }

        UnityEngine.UI.Outline outline = newItemBox.GetComponent<UnityEngine.UI.Outline>();
        if (outline != null)
        {
            _itemOutlines.Add(outline);
        }
        else
        {
            Debug.LogWarning($"{newItemBox.name}에 Outline 컴포넌트가 없습니다!");
        }

        Button itemButton = newItemBox.GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(() =>
            {
                SelectItem(itemData, index);
            });
        }
    }

    private void SelectItem(ItemData itemData, int index)
    {
        _currentSelectedIndex = index;

        if (_iconHandleImage != null) _iconHandleImage.gameObject.SetActive(false);
        if (_weaponHandleImage != null) _weaponHandleImage.gameObject.SetActive(false);
        if (_cardHandleImage != null) _cardHandleImage.gameObject.SetActive(false);

        switch (itemData.itemType)
        {
            case ItemType.Icon:
                if (_iconHandleImage != null)
                {
                    _iconHandleImage.gameObject.SetActive(true);
                    _iconHandleImage.sprite = itemData.itemSprite;
                }
                break;

            case ItemType.Weapon:
                if (_weaponHandleImage != null)
                {
                    _weaponHandleImage.gameObject.SetActive(true);
                    _weaponHandleImage.sprite = itemData.itemSprite;
                }
                break;

            case ItemType.Card:
                if (_cardHandleImage != null)
                {
                    _cardHandleImage.gameObject.SetActive(true);
                    _cardHandleImage.sprite = itemData.itemSprite;
                }
                break;
        }

        if (_handleText != null)
        {
            _handleText.text = itemData.itemName;
        }

        UpdateAcquireButtonState();
    }

    private void UpdateOutlines()
    {
        for (int i = 0; i < _itemOutlines.Count; i++)
        {
            if (_itemOutlines[i] == null) continue;

            float requiredPoint = (i + 1) * 10f;

            if (_passPointSlider.value >= requiredPoint)
            {
                _itemOutlines[i].effectColor = _unlockedColor;
            }
            else
            {
                _itemOutlines[i].effectColor = _lockedColor;
            }
        }
    }

    private void UpdateAcquireButtonState()
    {
        if (_acquireButton == null) return;

        ItemData currentItem = _itemDataList[_currentSelectedIndex];
        float requiredPoint = (_currentSelectedIndex + 1) * 10f;

        if (currentItem.isAcquired)
        {
            _acquireButton.interactable = false;
            if (_acquireButtonText != null)
            {
                _acquireButtonText.text = "받기 완료";
            }
        }

        else if (_passPointSlider.value < requiredPoint)
        {
            _acquireButton.interactable = false;
            if (_acquireButtonText != null)
            {
                _acquireButtonText.text = "보상받기";
            }
        }

        else
        {
            _acquireButton.interactable = true;
            if (_acquireButtonText != null)
            {
                _acquireButtonText.text = "보상받기";
            }
        }
    }

    public void AcquireButton()
    {
        if (_currentSelectedIndex >= 0 && _currentSelectedIndex < _itemDataList.Count)
        {
            _itemDataList[_currentSelectedIndex].isAcquired = true;
        }

        StartCoroutine(Popup());
        UpdateAcquireButtonState();
    }

    IEnumerator Popup()
    {
        if (_acquirePopup != null)
        {
            _acquirePopup.GetComponent<UIAnimator>().Show();
            yield return new WaitForSeconds(1.5f);
            _acquirePopup.GetComponent<UIAnimator>().Close();
        }
    }
}