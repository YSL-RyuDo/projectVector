using UnityEngine;

public class TutorialMiniMapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialMiniMapCamera miniMapCamera;
    [SerializeField] private Transform player;

    [Header("HUD MiniMap")]
    [SerializeField] private GameObject miniMapUI;

    private void Start()
    {
        if (miniMapCamera != null && player != null)
        {
            miniMapCamera.SetTarget(player);
        }

        if (miniMapUI != null)
        {
            miniMapUI.SetActive(true);
        }
    }
}