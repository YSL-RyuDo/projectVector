#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WeaponAimAlignmentEditor
{
    public static void Author()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene/Play: refused");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var folder=Path.Combine(Path.GetTempPath(),"VectorAim-before-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"));Directory.CreateDirectory(folder);
        var weapons=AssetDatabase.FindAssets("t:WeaponData",new[]{"Assets/Prefab/Weapon"}).Select(g=>AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).Where(w=>w!=null && w.weaponPrefab!=null).ToArray();
        int count=0;
        foreach(var weapon in weapons)
        {
            if(weapon.weaponType==WeaponData.WeaponType.Pistol || weapon.weaponType==WeaponData.WeaponType.Shotgun || weapon.weaponType==WeaponData.WeaponType.Knife)continue;
            string path=AssetDatabase.GetAssetPath(weapon.weaponPrefab);File.Copy(path,Path.Combine(folder,weapon.name+".prefab"),true);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var points=root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>
                {
                    var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                    return Enumerable.Range(0,mesh.subMeshCount).Where(i=>i<r.sharedMaterials.Length && r.sharedMaterials[i].name.Contains("Light2"))
                        .SelectMany(i=>mesh.GetIndices(i).Distinct().Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(mesh.vertices[v]))));
                }).ToArray();
                if(points.Length==0)throw new Exception("No optical reticle geometry: "+weapon.name);
                var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
                if(bounds.size.x>.05f || bounds.size.y>.05f)throw new Exception("Reticle geometry too large: "+weapon.name+" "+bounds);
                var sight=root.transform.Find("AimSight");if(sight==null){sight=new GameObject("AimSight").transform;sight.SetParent(root.transform,false);}
                sight.localPosition=bounds.center;sight.localRotation=Quaternion.identity;sight.localScale=Vector3.one;
                var so=new SerializedObject(root.GetComponent<WeaponADS>());so.FindProperty("sightReference").objectReferenceValue=sight;so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
                Debug.Log($"ADS_SIGHT_AUTHORED {weapon.name} position={bounds.center:F6} size={bounds.size:F6}");count++;
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(string role in new[]{"Initiator","Controller","Sentinel","Entry"})
        {
            string path="Assets/Prefab/Character/"+role+"/"+role+".prefab";File.Copy(path,Path.Combine(folder,"Character-"+role+".prefab"),true);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var arm=root.GetComponentInChildren<ArmADS>(true);
                var alignment=arm.GetComponent<FirstPersonAimAlignment>() ?? arm.gameObject.AddComponent<FirstPersonAimAlignment>();
                var so=new SerializedObject(alignment);so.FindProperty("aimCamera").objectReferenceValue=root.GetComponentInChildren<Camera>(true);
                so.FindProperty("inventory").objectReferenceValue=root.GetComponent<PlayerItem>();so.FindProperty("movement").objectReferenceValue=root.GetComponent<PlayerMovement>();so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);Debug.Log("ADS_RIG_AUTHORED "+role);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        Debug.Log("ADS_AUTHOR_PASS: "+count+" optics / 4 saved gameplay rigs; no UI, shot or packet changes");
    }
}
#endif
