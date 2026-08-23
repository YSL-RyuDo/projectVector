using TMPro;
using UnityEngine;

public enum CurrencyType
{
    Money0,
    Money1,
    Money2
}

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("---------UI---------")]
    [SerializeField] private TMP_Text _money0Text;
    [SerializeField] private TMP_Text _money1Text;
    [SerializeField] private TMP_Text _money2Text;

    [Header("---------Currencies---------")]
    [SerializeField] private int _money0;
    [SerializeField] private int _money1;
    [SerializeField] private int _money2;

    public int Money0 => _money0;
    public int Money1 => _money1;
    public int Money2 => _money2;

    private void Start()
    {
        UpdateAllUI();
    }

    // UI 전체 갱신
    public void UpdateAllUI()
    {
        if (_money0Text != null) _money0Text.text = "재화 0"+_money0.ToString("#,##0");
        if (_money1Text != null) _money1Text.text = "재화 1"+_money1.ToString("#,##0");
        if (_money2Text != null) _money2Text.text = "재화 2"+_money2.ToString("#,##0");
    }

    #region 통합 제어 메서드 (Enum 지정 방식)

    public void IncreaseMoney(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Money0: _money0 += amount; break;
            case CurrencyType.Money1: _money1 += amount; break;
            case CurrencyType.Money2: _money2 += amount; break;
        }
        UpdateAllUI();
    }

    public void DecreaseMoney(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Money0: _money0 -= amount; break;
            case CurrencyType.Money1: _money1 -= amount; break;
            case CurrencyType.Money2: _money2 -= amount; break;
        }
        UpdateAllUI();
    }

    #endregion

    #region 각 재화별 개별 메서드

    // Money 0
    public void IncreaseMoney0(int amount) { _money0 += amount; UpdateAllUI(); }
    public void DecreaseMoney0(int amount) { _money0 -= amount; UpdateAllUI(); }

    // Money 1
    public void IncreaseMoney1(int amount) { _money1 += amount; UpdateAllUI(); }
    public void DecreaseMoney1(int amount) { _money1 -= amount; UpdateAllUI(); }

    // Money 2
    public void IncreaseMoney2(int amount) { _money2 += amount; UpdateAllUI(); }
    public void DecreaseMoney2(int amount) { _money2 -= amount; UpdateAllUI(); }

    #endregion
}