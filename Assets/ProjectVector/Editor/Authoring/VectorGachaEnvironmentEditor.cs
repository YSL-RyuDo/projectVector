using System;
using UnityEditor;
using UnityEngine;

public static class VectorGachaEnvironmentEditor
{
    [MenuItem("Project Vector/UI/Build Gacha Asset Corridor")]
    public static void Build()
    {
        var root = new GameObject("GachaCorridor");
        try
        {
            for (int i=0;i<10;++i)
            {
                var module=new GameObject($"CorridorModule_{i:00}");module.transform.SetParent(root.transform,false);
                var instance=Spawn("Assets/ThirdParty/Sci-Fi Styled Modular Pack/Prefabs/Corridors/Corridor_I.prefab",module.transform);
                var bounds=BoundsOf(instance);
                instance.transform.position-=bounds.center;
                module.transform.localScale=new Vector3(15/bounds.size.x,15/bounds.size.y,12/bounds.size.z);
                module.transform.localPosition=new Vector3(0,0,i*12+6);
            }
            var enclosure=new GameObject("Sealed Walls and Service Bays");enclosure.transform.SetParent(root.transform,false);
            const string sci="Assets/ThirdParty/Sci-Fi Styled Modular Pack/Prefabs/";
            for(int bay=0;bay<10;++bay)
            {
                float z=6+bay*12;
                foreach(int side in new[]{-1,1})
                {
                    // Continuous opaque backing closes the gaps between the purchased arch modules.
                    Fit(enclosure.transform,sci+"Walls/Simple/blank_wall_A.prefab",
                        new Vector3(side*7.25f,0,z),new Vector3(.45f,15,12.15f),new Vector3(0,side*90,0));
                    Fit(enclosure.transform,sci+"Walls/decorative_wall_4_computer.prefab",
                        new Vector3(side*6.93f,-1.1f,z),new Vector3(.5f,5.2f,7.2f),new Vector3(0,-side*90,0));
                    Fit(enclosure.transform,sci+"Lights/light_wall_1.prefab",
                        new Vector3(side*6.5f,3.7f,z),new Vector3(.3f,.55f,3.2f),new Vector3(0,-side*90,0));
                }
                Fit(enclosure.transform,sci+"Floors/floor_2_blank.prefab",
                    new Vector3(0,-7.55f,z),new Vector3(15,.3f,12.15f),Vector3.zero);
                Fit(enclosure.transform,sci+"Floors/floor_2_blank.prefab",
                    new Vector3(0,7.55f,z),new Vector3(15,.3f,12.15f),new Vector3(180,0,0));
            }
            var staging=new GameObject("FPS Bundle - Supply Staging");staging.transform.SetParent(root.transform,false);
            // Supply bays frame the gate; the central flight path remains clear.
            for(int bay=0;bay<6;++bay)
            {
                float z=12+bay*18;
                Prop(staging.transform,"Assets/ThirdParty/FPS_Bundle/FPS_Map3/Prefabs/Props/Rack_01.prefab",new Vector3(-5.0f,-5.8f,z),4.5f,20);
                Prop(staging.transform,"Assets/ThirdParty/FPS_Bundle/FPS_Map4/Prefabs/Props/Generator_01.prefab",new Vector3(5.1f,-5.8f,z+1),3.3f,-25);
                Prop(staging.transform,"Assets/ThirdParty/FPS_Bundle/FPS_Map3/Prefabs/Props/Pallet_01.prefab",new Vector3(-3.5f,-5.8f,z-1),.28f,8);
                Prop(staging.transform,"Assets/ThirdParty/FPS_Bundle/FPS_Weapon0/Prefabs/Loot/BigAmoBox_01.prefab",new Vector3(-3.5f,-5.5f,z-1),1.15f,8);
                Prop(staging.transform,"Assets/ThirdParty/FPS_Bundle/FPS_Weapon0/Prefabs/Loot/BigAmoBox_02.prefab",new Vector3(-3.5f,-4.35f,z-1),.85f,-6);
                Prop(staging.transform,"Assets/ThirdParty/FPS_Bundle/FPS_Map3/Prefabs/Props/Barrel_03.prefab",new Vector3(3.6f,-5.8f,z),1.85f,0);
                Prop(staging.transform,"Assets/ThirdParty/FPS_Bundle/FPS_Map3/Prefabs/Props/Box_01.prefab",new Vector3(4.3f,-5.8f,z+3),1.4f,-12);
            }
            foreach(Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=31;
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/ProjectVector/Modules/Lobby/Resources/Shop/GachaCorridor.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("[GACHA ASSET] Saved sealed corridor, 20 service panels, 20 wall lamps and 42 FPS Bundle props; source materials preserved.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static GameObject Spawn(string path,Transform parent)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(source==null) throw new InvalidOperationException(path);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.transform.SetParent(parent,false);
        foreach(var collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach(var script in instance.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled=false;
        foreach(var light in instance.GetComponentsInChildren<Light>(true)) light.enabled=false;
        foreach(var lod in instance.GetComponentsInChildren<LODGroup>(true)) lod.ForceLOD(0);
        return instance;
    }
    private static Bounds BoundsOf(GameObject instance)
    {
        var renderers=instance.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0) throw new InvalidOperationException(instance.name);
        var bounds=renderers[0].bounds;
        foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    private static void Prop(Transform parent,string path,Vector3 basePosition,float height,float yaw)
    {
        var instance=Spawn(path,parent);
        instance.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var bounds=BoundsOf(instance);
        instance.transform.localScale*=height/Mathf.Max(bounds.size.y,.01f);
        bounds=BoundsOf(instance);
        instance.transform.position+=basePosition-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
    }
    private static void Fit(Transform parent,string path,Vector3 center,Vector3 size,Vector3 rotation)
    {
        var mount=new GameObject(System.IO.Path.GetFileNameWithoutExtension(path)+" Mount");
        mount.transform.SetParent(parent,false);
        var instance=Spawn(path,mount.transform);
        instance.transform.localRotation=Quaternion.Euler(rotation);
        var bounds=BoundsOf(instance);
        instance.transform.position-=bounds.center;
        mount.transform.localScale=new Vector3(size.x/Mathf.Max(bounds.size.x,.01f),
            size.y/Mathf.Max(bounds.size.y,.01f),size.z/Mathf.Max(bounds.size.z,.01f));
        mount.transform.localPosition=center;
    }
}
