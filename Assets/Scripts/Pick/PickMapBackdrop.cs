using System;
using UnityEngine;

// Scene-authored presentation copies, never the live gameplay map.
// This component lives on the existing pick panel so its lifetime follows that UI.
public sealed class PickMapBackdrop : MonoBehaviour
{
    public GameObject stageRoot;
    public GameObject[] maps;
    public int SelectedMap { get; private set; } = -1;

    public void ApplyMap(string mapName)
    {
        SelectedMap = string.Equals(mapName, "TestMap", StringComparison.OrdinalIgnoreCase) ? 0
            : string.Equals(mapName, "Desert", StringComparison.OrdinalIgnoreCase) ? 1
            : string.Equals(mapName, "Construction", StringComparison.OrdinalIgnoreCase) ? 2 : -1;
        ApplyVisibility();
    }

    void OnEnable() => ApplyVisibility();

    void OnDisable()
    {
        if (stageRoot != null) stageRoot.SetActive(false);
    }

    void ApplyVisibility()
    {
        if (maps == null || stageRoot == null) return;
        for (int i = 0; i < maps.Length; i++)
            if (maps[i] != null) maps[i].SetActive(i == SelectedMap);
        stageRoot.SetActive(isActiveAndEnabled && SelectedMap >= 0 && SelectedMap < maps.Length);
    }
}
