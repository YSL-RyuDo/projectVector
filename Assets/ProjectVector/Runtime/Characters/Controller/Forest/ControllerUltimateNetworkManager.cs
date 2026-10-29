using System.Collections.Generic;
using UnityEngine;

public class ControllerUltimateNetworkManager : MonoBehaviour
{
    public static ControllerUltimateNetworkManager Instance { get; private set; }

    [SerializeField] private GameObject dreamForestPrefab;

    private readonly Dictionary<uint, DreamForestArea> ultimates = new();

    private void Awake()
    {
        Instance = this;
    }

    public void OnUltimateSpawn(uint ultimateId, uint ownerPlayerId, Vector3 position, float radius, float duration)
    {
        if (dreamForestPrefab == null)
        {
            Debug.LogError("[CONTROLLER ULTIMATE] DreamForest Prefab 없음");
            return;
        }

        if (ultimates.TryGetValue(ultimateId, out DreamForestArea oldArea) && oldArea != null)
            Destroy(oldArea.gameObject);

        GameObject obj = Instantiate(dreamForestPrefab, position, Quaternion.identity);

        DreamForestArea area = obj.GetComponent<DreamForestArea>();

        if (area == null)
            area = obj.GetComponentInChildren<DreamForestArea>(true);

        if (area == null)
        {
            Debug.LogError("[CONTROLLER ULTIMATE] DreamForestArea 컴포넌트 없음");
            Destroy(obj);
            return;
        }

        area.InitFromServer(ultimateId, ownerPlayerId, position, radius, duration);
        obj.GetComponent<DreamForestPresentation>()?.Init(ownerPlayerId,duration);
        ultimates[ultimateId] = area;

        Debug.Log($"[CONTROLLER ULTIMATE SPAWN APPLY] id={ultimateId}, owner={ownerPlayerId}, pos={position}");
    }

    public void OnUltimateDestroy(uint ultimateId)
    {
        if (!ultimates.TryGetValue(ultimateId, out DreamForestArea area))
            return;

        if (area != null)
            Destroy(area.gameObject);

        ultimates.Remove(ultimateId);

        if (DreamForestPostProcessFeedback.Instance != null)
            DreamForestPostProcessFeedback.Instance.ForceHide();

        Debug.Log($"[CONTROLLER ULTIMATE DESTROY APPLY] id={ultimateId}");
    }

    public void ClearAll()
    {
        foreach (var pair in ultimates)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        ultimates.Clear();

        if (DreamForestPostProcessFeedback.Instance != null)
            DreamForestPostProcessFeedback.Instance.ForceHide();
    }
}
