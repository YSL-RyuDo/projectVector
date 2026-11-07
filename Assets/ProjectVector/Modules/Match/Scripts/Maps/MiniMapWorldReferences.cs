using System.Collections;
using UnityEngine;

public class MiniMapWorldReferences : MonoBehaviour
{
    [Header("¸Ê ¿ùµå À§Ä¡")]
    [SerializeField] private Transform zoneAPosition;
    [SerializeField] private Transform zoneBPosition;
    [SerializeField] private Transform fullMapCenter;

    [Header("Per-map camera framing")]
    [SerializeField, Min(1f)] private float hudSize = 30f;
    [SerializeField, Min(1f)] private float fullMapSize = 70f;
    [SerializeField, Min(1f)] private float hudHeight = 50f;
    [SerializeField, Min(1f)] private float fullMapHeight = 100f;

    private IEnumerator Start()
    {
        while (MiniMapManager.Instance == null ||
               MiniMapCamera.Instance == null)
        {
            yield return null;
        }

        MiniMapManager.Instance.SetMapWorldReferences(
            zoneAPosition,
            zoneBPosition
        );

        MiniMapCamera.Instance.SetOverlayMapCenter(
            fullMapCenter
        );
        MiniMapCamera.Instance.SetMapFraming(hudSize, fullMapSize, hudHeight, fullMapHeight);

        Debug.Log(
            $"[MINIMAP WORLD REGISTER] " +
            $"A={zoneAPosition?.name}, " +
            $"B={zoneBPosition?.name}, " +
            $"Center={fullMapCenter?.name}"
        );
    }
}
