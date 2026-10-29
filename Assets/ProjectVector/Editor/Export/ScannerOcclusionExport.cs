#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Export only authored map physics, never renderer bounds or client hit claims.
public static class ScannerOcclusionExport
{
    static string F(float value)
    {
        string s=value.ToString("G9",CultureInfo.InvariantCulture);
        return s+(s.Contains(".") || s.Contains("E") ? "f" : ".0f");
    }
    static string V(Vector3 v)=>"{"+F(v.x)+","+F(v.y)+","+F(v.z)+"}";
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only; no saved scene changes");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var triangles=new List<string>();var boxes=new List<string>();var meshes=new List<string>();
        var checks=new List<string>();
        foreach(string mapName in new[]{"TestMap","Desert","Construction"})
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{mapName}.prefab"));
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var info=root.GetComponent<MapInfo>();info.SetBarriersActive(true);Physics.SyncTransforms();
            int count=0;
            foreach(var col in root.GetComponentsInChildren<Collider>())
            {
                if(!col.enabled || col.isTrigger || !col.gameObject.activeInHierarchy)continue;
                bool barrier=info.barriers && col.transform.IsChildOf(info.barriers.transform);
                string prefix="{\""+mapName+"\","+(barrier?"true":"false")+",";
                if(col is BoxCollider box)
                {
                    // PhysX boxes use world rotation and absolute lossy scale,
                    // not the sheared inverse hierarchy matrix in Desert props.
                    var m=Matrix4x4.TRS(box.transform.TransformPoint(box.center),box.transform.rotation,Vector3.one).inverse;
                    var scale=box.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
                    var matrix=new List<string>();
                    for(int r=0;r<3;r++)for(int c=0;c<4;c++)matrix.Add(F(m[r,c]));
                    boxes.Add(prefix+"{"+string.Join(",",matrix)+"},"+V(Vector3.zero)+","+V(Vector3.Scale(box.size*.5f,scale))+"},");
                }
                else if(col is MeshCollider mesh && mesh.sharedMesh && !mesh.convex)
                {
                    var vertices=mesh.sharedMesh.vertices;var indices=mesh.sharedMesh.triangles;int start=triangles.Count;
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        Vector3 a=mesh.transform.TransformPoint(vertices[indices[i]]),b=mesh.transform.TransformPoint(vertices[indices[i+1]]),c=mesh.transform.TransformPoint(vertices[indices[i+2]]);
                        if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-16f)continue;
                        triangles.Add("{"+V(a)+","+V(b)+","+V(c)+"},");
                    }
                    meshes.Add(prefix+V(col.bounds.min)+","+V(col.bounds.max)+","+start+","+(triangles.Count-start)+"}, // "+col.name.Replace("\n"," "));
                }
                else throw new Exception($"Unsupported map collider: {mapName}/{col.name} {col.GetType().Name}; export must not silently omit walls");
                count++;
            }
            Debug.Log($"SCANNER_OCCLUSION_MAP {mapName} colliders={count} cumulativeTriangles={triangles.Count}");
            // Compare the exported server geometry against actual Unity physics.
            // Origins inside solids are excluded: Unity ignores that collider,
            // whereas the server deliberately fails closed for embedded devices.
            var random=new System.Random(1093);
            var bounds=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).Select(c=>c.bounds).ToArray();
            bool previousBackfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
            try
            {
                foreach(bool barriers in new[]{false,true})
                {
                    info.SetBarriersActive(barriers);Physics.SyncTransforms();int blocked=0,clear=0;
                    for(int attempt=0;attempt<30000 && blocked+clear<2000;attempt++)
                    {
                        var bound=bounds[attempt%bounds.Length];
                        float R()=> (float)random.NextDouble();
                        var a=bound.center+Vector3.Scale(bound.extents+Vector3.one*3,new Vector3(R()*2-1,R()*2-1,R()*2-1));
                        var b=a+new Vector3(R()*2-1,R()*2-1,R()*2-1).normalized*(.2f+R()*6.2f);
                        if(Physics.CheckSphere(a,.015f,~0,QueryTriggerInteraction.Ignore) || Physics.CheckSphere(b,.015f,~0,QueryTriggerInteraction.Ignore))continue;
                        bool visible=!Physics.Linecast(a,b,out var hit,~0,QueryTriggerInteraction.Ignore);
                        if(visible)clear++;else blocked++;
                        string N(float v)=>v.ToString("G9",CultureInfo.InvariantCulture);
                        checks.Add(string.Join(",",mapName,N(a.x),N(a.y),N(a.z),N(b.x),N(b.y),N(b.z),barriers?"1":"0",visible?"1":"0")+
                            (visible?"":","+hit.collider.name+","+hit.collider.GetType().Name+","+hit.triangleIndex));
                    }
                    if(blocked<10 || clear<10)throw new Exception("Insufficient clear/blocked physics samples");
                    Debug.Log($"SCANNER_PHYSICS_SAMPLES {mapName} barriers={barriers} clear={clear} blocked={blocked}");
                }
            }
            finally{Physics.queriesHitBackfaces=previousBackfaces;}
            Object.DestroyImmediate(root);
        }
        var output=new StringBuilder("// Generated by ScannerOcclusionExport from map collision geometry. Re-export after map edits.\n#pragma once\nnamespace ScannerOcclusion {\n");
        output.Append("inline constexpr Triangle triangles[]={\n").AppendJoin("\n",triangles).Append("\n};\n");
        output.Append("inline constexpr Box boxes[]={\n").AppendJoin("\n",boxes).Append("\n};\n");
        output.Append("inline constexpr Mesh meshes[]={\n").AppendJoin("\n",meshes).Append("\n};\n}\n");
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"ScannerOcclusion.generated.h"),output.ToString());
        File.WriteAllLines(Path.Combine(Path.GetTempPath(),"ScannerOcclusionCases.csv"),checks);
        Debug.Log("SCANNER_OCCLUSION_EXPORT_PASS");
    }
}
#endif
