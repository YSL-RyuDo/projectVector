using UnityEngine;
using TMPro;

public class HPTextUI : MonoBehaviour
{
    [SerializeField] TMP_Text hpText;
    PlayerHP target;

    public void Bind(PlayerHP hp)
    {
        target = hp;
        UpdateText();
    }

    void Update()
    {
        if (target == null) return;
        UpdateText();
    }

    void UpdateText()
    {
        hpText.text = $"HP : {target.CurrentHP}";
    }
}
