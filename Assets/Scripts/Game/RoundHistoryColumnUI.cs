using UnityEngine;
using UnityEngine.UI;

public class RoundHistoryColumnUI : MonoBehaviour
{
    [SerializeField] private GameObject topMarker;
    [SerializeField] private GameObject bottomMarker;

    [SerializeField] private Image ourTeamResultIcon;
    [SerializeField] private Image enemyTeamResultIcon;

    public void SetEmpty()
    {
        if (topMarker != null)
            topMarker.SetActive(false);

        if (bottomMarker != null)
            bottomMarker.SetActive(false);

        if (ourTeamResultIcon != null)
            ourTeamResultIcon.enabled = false;

        if (enemyTeamResultIcon != null)
            enemyTeamResultIcon.enabled = false;
    }

    public void SetCurrent(bool isCurrent)
    {
        if (topMarker != null)
            topMarker.SetActive(isCurrent);

        if (bottomMarker != null)
            bottomMarker.SetActive(isCurrent);
    }

    public void SetResult(bool ourTeamWin, Sprite resultSprite)
    {
        Image target = ourTeamWin ? ourTeamResultIcon : enemyTeamResultIcon;
        Image other = ourTeamWin ? enemyTeamResultIcon : ourTeamResultIcon;

        if (target != null)
        {
            target.sprite = resultSprite;
            target.enabled = resultSprite != null;
        }

        if (other != null)
            other.enabled = false;
    }
}