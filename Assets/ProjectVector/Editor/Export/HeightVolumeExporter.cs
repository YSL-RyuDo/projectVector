using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

public static class HeightVolumeExporter
{
    [MenuItem("Tools/Export Height Volumes (Layer Only)")]
    static void Export()
    {
        int layer = LayerMask.NameToLayer("HeightVolume");
        if (layer < 0)
        {
            Debug.LogError("Layer 'HeightVolume' not found");
            return;
        }

        var colliders = Object.FindObjectsByType<Collider>(
            FindObjectsSortMode.None);

        StringBuilder sb = new StringBuilder();
        int count = 0;

        foreach (var col in colliders)
        {
            if (col.gameObject.layer != layer)
                continue;

            Bounds b = col.bounds;

            float groundY = b.max.y;

            sb.AppendLine(
                $"{b.min.x},{b.max.x}," +
                $"{b.min.z},{b.max.z}," +
                $"{groundY}"
            );

            count++;
        }

        File.WriteAllText("HeightVolumes.csv", sb.ToString());
        Debug.Log($"[HeightVolumeExporter] Exported {count} volumes");
    }
}
