using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ServerColliderExporter
{
    [MenuItem("Tools/Export Server Colliders")]
    static void Export()
    {
        var colliders = GameObject.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
        List<string> lines = new List<string>();

        foreach (var col in colliders)
        {
            if (col.gameObject.layer != LayerMask.NameToLayer("ServerCollision"))
                continue;

            Vector3 center = col.transform.TransformPoint(col.center);
            Vector3 size = Vector3.Scale(col.size, col.transform.lossyScale);

            Vector3 min = center - size * 0.5f;
            Vector3 max = center + size * 0.5f;

            // y는 서버에서 거의 안 씀 (참고용)
            lines.Add($"{min.x},{min.z},{max.x},{max.z}");
        }

        File.WriteAllLines("Map_Colliders.txt", lines);
        Debug.Log("Server collider export complete");
    }
}
