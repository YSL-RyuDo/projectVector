using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StoreStatBarUI : MonoBehaviour
{
    public TMP_Text label,value;
    public Image fill;
    public void Present(string name,string amount,float fraction)
    {
        label.text=name;value.text=amount;fill.fillAmount=Mathf.Clamp01(fraction);
    }
}
