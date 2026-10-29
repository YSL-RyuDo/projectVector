#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static class SpikeAssetAudit
{
    public static void CoatMesh()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefab/Character/Controller/Controller.prefab");
        var lines=new System.Collections.Generic.List<string>{"mesh,index,x,y,z,bone,weight"};
        foreach(var r in root.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var baked=new Mesh();r.BakeMesh(baked);var vs=baked.vertices;var weights=r.sharedMesh.boneWeights;
            Debug.Log("COAT_MESH "+r.name+" vertices="+vs.Length+" source="+AssetDatabase.GetAssetPath(r.sharedMesh));
            if(r.name=="HeadUV")File.WriteAllLines("C:/Users/GCT/Documents/Vector-Spike-20261027/V2/coat-triangles.csv",Enumerable.Range(0,r.sharedMesh.triangles.Length/3).Select(i=>string.Join(",",r.sharedMesh.triangles.Skip(i*3).Take(3))));
            for(int i=0;i<vs.Length;i++)
            {
                var p=root.transform.InverseTransformPoint(r.transform.TransformPoint(vs[i]));
                lines.Add(string.Join(",",r.name,i,p.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture),p.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture),p.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture),r.bones[weights[i].boneIndex0].name,weights[i].weight0.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
            }
            Object.DestroyImmediate(baked);
        }
        File.WriteAllLines("C:/Users/GCT/Documents/Vector-Spike-20261027/V2/coat-mesh.csv",lines);
        PrefabUtility.UnloadPrefabContents(root);
    }
    public static void Rigs()
    {
        foreach(string c in SpikePresentationAuthoring.Characters)
        {
            var root=PrefabUtility.LoadPrefabContents("Assets/Prefab/Character/"+c+"/"+c+".prefab");
            foreach(var b in root.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.bones).Where(b=>b).Distinct())
                if(b.name.Contains("Leg")||b.name.Contains("Thigh")||b.name.Contains("Foot")||b.name.Contains("Hips")||b.name.Contains("Spine")||b.name.Contains("Calf"))Debug.Log("LEG_AUDIT "+c+" "+b.name+" "+b.position.ToString("F3")+" parents="+b.parent.name+"/"+b.parent.parent.name+"/"+b.parent.parent.parent.name);
            var pose=root.GetComponent<SpikeInteractionPresentation>();
            foreach(float time in new[]{.1f,1.2f,2.8f})
            {
                SpikeInteractionPresentation.Sample(pose.thirdPerson,1,time);
                foreach(var b in pose.thirdPerson.bones.Where(b=>b.name.EndsWith("Foot")||b.name=="FootL"||b.name=="FootR"))Debug.Log("LEG_SAMPLE "+c+" "+time+" "+b.name+" "+b.position.ToString("F3"));
                SpikeInteractionPresentation.Sample(pose.thirdPerson,0,0);
            }
            foreach(var r in root.transform.Find("Model").GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(c=="Controller")Debug.Log("COAT_AUDIT "+r.name+" "+string.Join(" | ",r.bones.Where(b=>b).Select(b=>b.name+":"+b.position.ToString("F3"))));
                Debug.Log("RIG_RENDERER "+c+" active="+r.gameObject.activeSelf+" enabled="+r.enabled+" path="+AnimationUtility.CalculateTransformPath(r.transform,root.transform)+" root="+(r.rootBone?AnimationUtility.CalculateTransformPath(r.rootBone,root.transform):"null"));
                Debug.Log("RIG_BONES "+c+" "+r.name+" "+string.Join(" | ",r.bones.Where(b=>b&&(b.name.EndsWith("Spine")||b.name.EndsWith("LeftHand")||b.name.EndsWith("LeftArm"))).Select(b=>AnimationUtility.CalculateTransformPath(b,root.transform))));
            }
            foreach(var b in root.transform.Find("CameraRoot/Controller Model_Riging (1)").GetComponentsInChildren<Transform>(true).Where(b=>b.name.Contains("Hand")&&b.name.StartsWith("mixamorig")))Debug.Log("FP_HAND "+b.name+" parent="+b.parent.name+" local="+b.localPosition+" rotation="+b.localEulerAngles);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
    public const string Output="C:/Users/GCT/Documents/Vector-Spike-20261027/AssetAudit";
    public static Material Compatible(Material source)
    {
        if(source.shader.name.Contains("Universal") || source.shader.name.Contains("Toon"))return new Material(source);
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=source.name+" URP";
        m.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
        if(source.HasProperty("_MainTex")){m.SetTexture("_BaseMap",source.GetTexture("_MainTex"));m.SetTextureScale("_BaseMap",source.GetTextureScale("_MainTex"));}
        if(source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")){m.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));m.EnableKeyword("_NORMALMAP");}
        if(source.HasProperty("_MetallicGlossMap")&&source.GetTexture("_MetallicGlossMap")){m.SetTexture("_MetallicGlossMap",source.GetTexture("_MetallicGlossMap"));m.EnableKeyword("_METALLICSPECGLOSSMAP");}
        m.SetFloat("_Metallic",source.HasProperty("_Metallic")?source.GetFloat("_Metallic"):.2f);m.SetFloat("_Smoothness",.4f);
        if(source.HasProperty("_EmissionMap")&&source.GetTexture("_EmissionMap")){m.SetTexture("_EmissionMap",source.GetTexture("_EmissionMap"));m.SetColor("_EmissionColor",source.GetColor("_EmissionColor"));m.EnableKeyword("_EMISSION");}
        return m;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.6f,.68f);
        var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(40,-35,0);
        var cam=new GameObject("Camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.105f,.14f);cam.fieldOfView=35;cam.nearClipPlane=.01f;
        var rt=new RenderTexture(768,768,24);rt.antiAliasing=4;cam.targetTexture=rt;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);
        var paths=new[]{"Assets/FPS_Bundle/FPS_Weapon0/Prefabs/Grenade/C4.prefab","Assets/FPS_Bundle/FPS_Weapon0/Prefabs/Grenade/C4_2.prefab","Assets/FPS_Bundle/FPS_Weapon0/Prefabs/Grenade/C4_3.prefab","Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/shield_generator.prefab","Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Shield Core.prefab","Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab","Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/rotor.prefab"};
        foreach(var path in paths)
        {
            var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));foreach(var b in g.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
            foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m?Compatible(m):null).ToArray();
            var renderers=g.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Debug.Log("ASSET_AUDIT "+path+" bounds="+bounds+" meshes="+string.Join(",",g.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh.name+":"+f.sharedMesh.vertexCount)));
            for(int angle=0;angle<2;angle++)
            {
                cam.transform.position=bounds.center+(angle==0?new Vector3(1.3f,1.5f,-1.5f):new Vector3(-1.3f,1.1f,1.5f))*bounds.size.magnitude;cam.transform.LookAt(bounds.center);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+Path.GetFileNameWithoutExtension(path)+"-"+angle+".png",tex.EncodeToPNG());
            }
            Object.DestroyImmediate(g);
        }
        Debug.Log("ASSET_AUDIT_PASS");
    }
}
#endif
