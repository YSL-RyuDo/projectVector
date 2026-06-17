using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillLogEntryUI : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image killerImage;
    [SerializeField] private Image deathImage;

    [Header("Texts")]
    [SerializeField] private TMP_Text killerNicknameText;
    [SerializeField] private TMP_Text deathNicknameText;

    private void Awake()
    {
        // Inspector 연결이 빠졌을 때 이름으로 한 번 더 검색
        if (killerImage == null)
        {
            Transform target = transform.Find("KillerImage");

            if (target != null)
                killerImage = target.GetComponent<Image>();
        }

        if (deathImage == null)
        {
            Transform target = transform.Find("DeathImage");

            if (target != null)
                deathImage = target.GetComponent<Image>();
        }

        if (killerNicknameText == null)
        {
            Transform target =
                transform.Find("KillerNicknameText");

            if (target != null)
            {
                killerNicknameText =
                    target.GetComponent<TMP_Text>();
            }
        }

        if (deathNicknameText == null)
        {
            Transform target =
                transform.Find("DeathNicknameText");

            if (target != null)
            {
                deathNicknameText =
                    target.GetComponent<TMP_Text>();
            }
        }
    }

    public void Initialize(
        Sprite killerSprite,
        Sprite deadSprite,
        string killerNickname,
        string deadNickname)
    {
        if (killerImage != null)
        {
            killerImage.sprite = killerSprite;
            killerImage.enabled = killerSprite != null;
        }

        if (deathImage != null)
        {
            deathImage.sprite = deadSprite;
            deathImage.enabled = deadSprite != null;
        }

        if (killerNicknameText != null)
        {
            killerNicknameText.text =
                string.IsNullOrWhiteSpace(killerNickname)
                    ? "Unknown"
                    : killerNickname;
        }

        if (deathNicknameText != null)
        {
            deathNicknameText.text =
                string.IsNullOrWhiteSpace(deadNickname)
                    ? "Unknown"
                    : deadNickname;
        }
    }
}