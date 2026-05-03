using UnityEngine;
using UnityEngine.UI;

public class CharacterSkillUI : MonoBehaviour
{
    public static CharacterSkillUI Instance;

    [SerializeField] private Text _sentinel;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SentinelSkillText(bool active, string text)
    {
        if (active) _sentinel.GetComponent<UIAnimator>().Show();
        else _sentinel.GetComponent<UIAnimator>().Close();
        _sentinel.text = text;
    }
}
