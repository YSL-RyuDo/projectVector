using System.Collections;
using UnityEngine;

public class MiniMapWorldReferences : MonoBehaviour
{
    [Header("¸Ê ¿ùµå À§Ä¡")]
    [SerializeField] private Transform zoneAPosition;
    [SerializeField] private Transform zoneBPosition;
    [SerializeField] private Transform fullMapCenter;

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

        Debug.Log(
            $"[MINIMAP WORLD REGISTER] " +
            $"A={zoneAPosition?.name}, " +
            $"B={zoneBPosition?.name}, " +
            $"Center={fullMapCenter?.name}"
        );
    }
}