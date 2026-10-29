#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ControllerCoatAuthoring
{
    const string Folder="Assets/Art/ControllerCoat";
    const float Top=1f,Bottom=.12f;
    public static void Run()
    {
        System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefab/Character/Controller/Controller.prefab");
        try
        {
            var model=root.transform.Find("Model");
            var renderer=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="HeadUV");
            var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Character(New)/Controller/Controller Model_Riging (1).fbx").OfType<Mesh>().First(m=>m.name=="HeadUV");
            // Start from the untouched import every time, never from yesterday's generated weights.
            var sourceBones=renderer.bones.Take(source.bindposes.Length).ToArray();
            renderer.sharedMesh=source;renderer.bones=sourceBones;
            var old=model.Find("Coat dynamics");if(old)Object.DestroyImmediate(old.gameObject);
            var rig=new GameObject("Coat dynamics");rig.transform.SetParent(model,false);
            var motion=root.GetComponent<ControllerCoatMotion>();if(!motion)motion=root.AddComponent<ControllerCoatMotion>();
            const int rows=13,columns=13;
            motion.actor=root.transform;motion.rows=rows;motion.columns=columns;
            // Skin directly into world space. BakeMesh includes an import-scale
            // factor here; transforming its output again shrank only the coat.
            var sourceVertices=source.vertices;var sourceWeights=source.boneWeights;
            var sourceNormals=source.normals;var sourceTangents=source.tangents;
            var matrices=sourceBones.Select((b,i)=>b.localToWorldMatrix*source.bindposes[i]).ToArray();
            var world=new Vector3[sourceVertices.Length];var bakedVertices=new Vector3[world.Length];var bakedNormals=new Vector3[world.Length];var bakedTangents=new Vector4[world.Length];
            for(int i=0;i<world.Length;i++)
            {
                var w=sourceWeights[i];var v=sourceVertices[i];var n=sourceNormals[i];var t=sourceTangents[i];
                Vector3 Point()=>matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                Vector3 Direction(Vector3 d)=>matrices[w.boneIndex0].MultiplyVector(d)*w.weight0+matrices[w.boneIndex1].MultiplyVector(d)*w.weight1+matrices[w.boneIndex2].MultiplyVector(d)*w.weight2+matrices[w.boneIndex3].MultiplyVector(d)*w.weight3;
                world[i]=Point();bakedVertices[i]=renderer.transform.InverseTransformPoint(world[i]);
                bakedNormals[i]=renderer.transform.InverseTransformDirection(Direction(n).normalized);
                var tangent=renderer.transform.InverseTransformDirection(Direction(t).normalized);bakedTangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,t.w);
            }
            var neutralBounds=new Bounds(bakedVertices[0],Vector3.zero);foreach(var p in bakedVertices)neutralBounds.Encapsulate(p);
            // Rest positions and solver lengths are metres, independent of the
            // character root's authored 0.5 scale.
            Vector3 ToWorld(Vector3 p)=>root.transform.position+root.transform.rotation*p;
            var local=world.Select(p=>Quaternion.Inverse(root.transform.rotation)*(p-root.transform.position)).ToArray();
            var mask=CoatIsland(local,source.triangles);
            if(mask.Count<1000||mask.Count>3500)throw new Exception("Unexpected controller coat topology: "+mask.Count);
            var nodes=new Transform[rows*columns];var points=new Vector3[rows*columns];var anchors=new ControllerCoatMotion.Anchor[columns];
            for(int c=0;c<columns;c++)
            {
                float angle=Mathf.Lerp(-126,126,c/(float)(columns-1))*Mathf.Deg2Rad;
                for(int row=0;row<rows;row++)
                {
                    float t=row/(float)(rows-1);float radius=Mathf.Lerp(.115f,.29f,t);
                    var p=new Vector3(Mathf.Sin(angle)*radius,Mathf.Lerp(Top,Bottom,t),-.015f-Mathf.Cos(angle)*radius);
                    int index=c*rows+row;points[index]=p;
                    var bone=new GameObject("Coat_"+c+"_"+row).transform;bone.SetParent(rig.transform,false);bone.SetPositionAndRotation(ToWorld(p),root.transform.rotation);nodes[index]=bone;
                }
                // Follow the actual seam's skinning, rather than attaching its
                // lower half to a different torso bone and tearing the waist.
                var waist=sourceBones.First(b=>b.name.EndsWith("Spine"));
                anchors[c]=new ControllerCoatMotion.Anchor{bones=new[]{waist},weights=new[]{1f},points=new[]{waist.InverseTransformPoint(ToWorld(points[c*rows]))}};
            }
            var mesh=Object.Instantiate(source);mesh.name="Controller coat, independent cloth bones";
            var weights=mesh.boneWeights;
            var vertices=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;
            foreach(int i in mask)
            {
                // New bind matrices refer to the current neutral skeleton, while
                // the FBX's original matrices refer to its import bind pose.
                vertices[i]=bakedVertices[i];normals[i]=bakedNormals[i];
                if(bakedTangents.Length==tangents.Length)tangents[i]=bakedTangents[i];
                var p=local[i];
                float column=Mathf.Clamp((Mathf.Atan2(p.x,-p.z-.015f)*Mathf.Rad2Deg+126)/252*(columns-1),0,columns-1);
                float row=Mathf.Clamp((Top-p.y)/(Top-Bottom)*(rows-1),0,rows-1);
                int c0=Mathf.Min(columns-2,(int)column),r0=Mathf.Min(rows-2,(int)row);float u=column-c0,v=row-r0;
                int offset=sourceBones.Length;
                weights[i]=new BoneWeight{boneIndex0=offset+c0*rows+r0,boneIndex1=offset+(c0+1)*rows+r0,boneIndex2=offset+c0*rows+r0+1,boneIndex3=offset+(c0+1)*rows+r0+1,weight0=(1-u)*(1-v),weight1=u*(1-v),weight2=(1-u)*v,weight3=u*v};
            }
            mesh.boneWeights=weights;
            mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;
            mesh.bindposes=source.bindposes.Concat(nodes.Select(n=>n.worldToLocalMatrix*renderer.transform.localToWorldMatrix)).ToArray();
            weights=mesh.boneWeights;
            string path=Folder+"/ControllerCoat.asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(previous){EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);mesh=previous;}else AssetDatabase.CreateAsset(mesh,path);
            renderer.sharedMesh=mesh;renderer.bones=sourceBones.Concat(nodes).ToArray();
            var scale=renderer.transform.lossyScale;
            neutralBounds.Expand(new Vector3(.7f/Mathf.Abs(scale.x),.5f/Mathf.Abs(scale.y),.7f/Mathf.Abs(scale.z)));renderer.localBounds=neutralBounds;
            motion.nodes=nodes;motion.restPoints=points;motion.anchors=anchors;
            var contacts=new List<ControllerCoatMotion.SurfaceContact>();var seen=new HashSet<Vector3Int>();
            var contactWorld=mesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
            foreach(int i in mask)
            {
                var p=root.transform.InverseTransformPoint(contactWorld[i])*10000;var key=new Vector3Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),Mathf.RoundToInt(p.z));
                if(!seen.Add(key))continue;
                var w=weights[i];int a=w.boneIndex0-sourceBones.Length,b=w.boneIndex1-sourceBones.Length,c=w.boneIndex2-sourceBones.Length,d=w.boneIndex3-sourceBones.Length;
                Vector3 Offset(int index)=>Quaternion.Inverse(nodes[index].rotation)*(contactWorld[i]-nodes[index].position);
                contacts.Add(new ControllerCoatMotion.SurfaceContact{a=a,b=b,c=c,d=d,weights=new Vector4(w.weight0,w.weight1,w.weight2,w.weight3),oa=Offset(a),ob=Offset(b),oc=Offset(c),od=Offset(d)});
            }
            motion.surface=contacts.ToArray();
            Transform Find(string n)=>sourceBones.First(b=>b.name.EndsWith(n));
            motion.legJoints=new[]{Find("LeftUpLeg"),Find("LeftLeg"),Find("LeftFoot"),Find("RightUpLeg"),Find("RightLeg"),Find("RightFoot")};
            motion.shoes=new[]{Find("LeftFoot"),Find("RightFoot")}.Select(foot=>
            {
                var shoeVertices=new List<Vector3>();var originalWeights=source.boneWeights;
                for(int i=0;i<local.Length;i++)
                {
                    var w=originalWeights[i];float weight=0;
                    if(sourceBones[w.boneIndex0].IsChildOf(foot))weight+=w.weight0;
                    if(sourceBones[w.boneIndex1].IsChildOf(foot))weight+=w.weight1;
                    if(sourceBones[w.boneIndex2].IsChildOf(foot))weight+=w.weight2;
                    if(sourceBones[w.boneIndex3].IsChildOf(foot))weight+=w.weight3;
                    if(weight>.6f)shoeVertices.Add(local[i]);
                }
                if(shoeVertices.Count<10)throw new Exception("Missing shoe geometry for coat contact");
                var bounds=new Bounds(shoeVertices[0],Vector3.zero);foreach(var p in shoeVertices)bounds.Encapsulate(p);
                float radius=Mathf.Max(bounds.extents.x,bounds.extents.y)+.035f;
                var a=bounds.center;var b=bounds.center;
                a.z=bounds.min.z+Mathf.Min(radius,bounds.extents.z);b.z=bounds.max.z-Mathf.Min(radius,bounds.extents.z);
                return new ControllerCoatMotion.ShoeContact{bone=foot,heel=foot.InverseTransformPoint(ToWorld(a)),toe=foot.InverseTransformPoint(ToWorld(b)),radius=radius};
            }).ToArray();
            motion.Step(1f/60);
            var allBones=renderer.bones;var generated=allBones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();float error=0;
            foreach(int i in mask){var w=weights[i];var v=vertices[i];var p=generated[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+generated[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+generated[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+generated[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;error=Mathf.Max(error,Vector3.Distance(p,world[i]));}
            if(error>.001f)throw new Exception("Standing coat differs from original by "+error+"m");
            Debug.Log("COAT_ORIGINAL_SILHOUETTE_PASS max error="+error.ToString("F6")+"m");
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefab/Character/Controller/Controller.prefab");
            AssetDatabase.SaveAssets();Debug.Log("COAT_AUTHOR_PASS: "+mask.Count+" cloth vertices, source body preserved, "+nodes.Length+" secondary bones, 2 fitted shoe contacts");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void AuditIslands(Vector3[] vertices,int[] triangles,float height)
    {
        var groups=Islands(vertices,triangles,height);
        Debug.Log("COAT_ISLANDS "+height+": "+string.Join(";",groups.Where(g=>g.Count()>100).Select(g=>g.Count()+" x="+g.Min(i=>vertices[i].x)+","+g.Max(i=>vertices[i].x)+" y="+g.Min(i=>vertices[i].y)+","+g.Max(i=>vertices[i].y)+" z="+g.Min(i=>vertices[i].z)+","+g.Max(i=>vertices[i].z))));
    }
    static HashSet<int> CoatIsland(Vector3[] vertices,int[] triangles)=>Islands(vertices,triangles,Top).Where(g=>g.Count()>1000&&g.Min(i=>vertices[i].z)<-.2f&&g.Max(i=>vertices[i].x)-g.Min(i=>vertices[i].x)>.45f).Single().ToHashSet();
    static IGrouping<int,int>[] Islands(Vector3[] vertices,int[] triangles,float height)
    {
        int[] parent=Enumerable.Range(0,vertices.Length).ToArray();
        int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
        void Union(int a,int b){parent[Find(a)]=Find(b);}
        var weld=new Dictionary<Vector3Int,int>();
        for(int i=0;i<vertices.Length;i++)if(vertices[i].y<=height)
        {
            var v=vertices[i]*10000;var key=new Vector3Int(Mathf.RoundToInt(v.x),Mathf.RoundToInt(v.y),Mathf.RoundToInt(v.z));
            if(weld.TryGetValue(key,out int j))Union(i,j);else weld[key]=i;
        }
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
            if(vertices[a].y<=height&&vertices[b].y<=height&&vertices[c].y<=height){Union(a,b);Union(b,c);}
        }
        return Enumerable.Range(0,vertices.Length).Where(i=>vertices[i].y<=height).GroupBy(Find).ToArray();
    }
}
#endif
