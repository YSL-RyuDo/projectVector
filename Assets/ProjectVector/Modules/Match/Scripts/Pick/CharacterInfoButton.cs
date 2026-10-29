using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterInfoButton : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI infoText; // 화면에 정보를 표시할 TextMeshPro

    private Image buttonImage;
    private string skillName;       // name 대신 skillName으로 변경 (충돌 방지)
    private string skillDescription;

    public void Initialize(Sprite sprite, string name, string description)
    {
        buttonImage = GetComponent<Image>();
        buttonImage.sprite = sprite;

        // 데이터 할당
        this.skillName = name;
        this.skillDescription = description;
    }

    public void OnClick()
    {
        if (infoText != null)
        {
            // 스킬 이름이 없는 경우(역할 아이콘 등)를 고려한 조건부 출력
            if (string.IsNullOrEmpty(skillName))
            {
                infoText.text = skillDescription;
            }
            else
            {
                infoText.text = $"<b>{skillName}</b>\n\n{skillDescription}";
            }
        }
    }
}
