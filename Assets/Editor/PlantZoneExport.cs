using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlantZoneExport
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Batch only; temporary scene");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var b = new StringBuilder("// Generated from authored map BoxColliders by PlantZoneExport. Re-export after map edits.\n#pragma once\n#include <cmath>\n#include <string>\nnamespace PlantZones {\nstruct Zone { const char* map; float inverse[12]; float center[3]; float half[3]; };\ninline constexpr Zone zones[] = {\n");
        string F(float v) { string s=v.ToString("G9", CultureInfo.InvariantCulture); return s+(s.Contains(".") || s.Contains("E") ? "f" : ".0f"); }
        foreach (string name in new[] { "TestMap", "Desert" })
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{name}.prefab"));
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var zones = root.GetComponentsInChildren<SpikePlantZone>(true);
            if (zones.Length == 0) throw new Exception(name + ": missing plant zones");
            foreach (var zone in zones)
            {
                var box = zone.GetComponent<BoxCollider>();
                if (box == null || !box.enabled || !box.isTrigger || !zone.gameObject.activeInHierarchy) throw new Exception(name + ": unsupported/inactive plant zone");
                var m = box.transform.worldToLocalMatrix;
                b.Append("{\"").Append(name).Append("\", {");
                for (int r=0;r<3;r++) for(int c=0;c<4;c++) b.Append(F(m[r,c])).Append(r==2 && c==3 ? "}, {" : ",");
                b.Append(F(box.center.x)).Append(',').Append(F(box.center.y)).Append(',').Append(F(box.center.z)).Append("}, {");
                b.Append(F(box.size.x/2)).Append(',').Append(F(box.size.y/2)).Append(',').Append(F(box.size.z/2)).Append("}},\n");
                Debug.Log($"PLANT_ZONE {name}/{zone.name} center={box.transform.TransformPoint(box.center):F3} bounds={box.bounds} root={root.transform.position}");
            }
            UnityEngine.Object.DestroyImmediate(root);
        }
        b.Append("};\ninline bool Contains(const std::string& map, float x, float y, float z) {\nif (!std::isfinite(x)||!std::isfinite(y)||!std::isfinite(z)) return false;\nfor (const auto& zone : zones) { if (map != zone.map) continue; bool inside=true;\nfor(int r=0;r<3;r++){ const float* m=zone.inverse+r*4; float local=m[0]*x+m[1]*y+m[2]*z+m[3]; if(std::abs(local-zone.center[r])>zone.half[r]+std::abs(m[1])*0.75f+0.001f) inside=false; }\nif(inside) return true; } return false; }\n}\n");
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "VectorPlantZones.generated.h"), b.ToString());
        Debug.Log("PLANT_ZONE_EXPORT_PASS");
    }
}
