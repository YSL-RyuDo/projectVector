using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ServerGroundExporter
{
    [MenuItem("Tools/Export Server Ground HeightMap")]
    static void Export()
    {
        const float grid = 0.5f;
        const float rayHeight = 200f;
        LayerMask groundMask = LayerMask.GetMask("Ground");

        GameObject ground = GameObject.Find("PrototypeMap_Ground");
        if (ground == null)
        {
            Debug.LogError("PrototypeMap_Ground not found");
            return;
        }

        MeshCollider mc = ground.GetComponent<MeshCollider>();
        if (mc == null)
        {
            Debug.LogError("MeshCollider not found on PrototypeMap_Ground");
            return;
        }

        Bounds b = mc.bounds;
        StringBuilder sb = new StringBuilder();

        int hitCount = 0;

        for (float x = b.min.x; x <= b.max.x; x += grid)
        {
            for (float z = b.min.z; z <= b.max.z; z += grid)
            {
                Vector3 origin = new Vector3(x, rayHeight, z);

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 500f, groundMask))
                {
                    sb.AppendLine($"{x:F2},{z:F2},{hit.point.y:F4}");
                    hitCount++;
                }
            }
        }

        string path = Application.dataPath + "/../ServerGround.csv";
        File.WriteAllText(path, sb.ToString());

        Debug.Log($"ServerGround exported: {hitCount} points");
        Debug.Log($"Path: {path}");
    }
}
