using System.Collections.Generic;
using UnityEngine;

// Temporary 3D presentation geometry only. No Canvas/UI creation or gameplay asset mutation.
public sealed class VectorGachaChamber : MonoBehaviour
{
    readonly List<Mesh> meshes = new();
    Material metal, dark;
    public void Build()
    {
        var shader=Resources.Load<Shader>("Shop/VectorChamberMetal");
        metal=new Material(shader); metal.SetColor("_BaseColor",new Color(.67f,.73f,.83f));
        dark=new Material(shader); dark.SetColor("_BaseColor",new Color(.12f,.17f,.26f));
        // Six solid wall planes, segmented frames and narrow inset rails.
        for(int side=0;side<6;++side)
        {
            float angle=side*60*Mathf.Deg2Rad;
            var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            Box("ChamberWall",radial*7+Vector3.forward*56,new Vector3(.4f,8.2f,120),side*60,metal);
            Box("InsetRail",radial*6.76f+Vector3.forward*56,new Vector3(.08f,.18f,120),side*60,dark);
            for(int z=6;z<110;z+=12)
                Box("Frame",radial*6.72f+Vector3.forward*z,new Vector3(.25f,8.0f,.24f),side*60,dark);
        }
        Torus("OuterGate",5.55f,.40f,44,metal);
        Torus("Recess",5.05f,.32f,43.9f,dark);
        Torus("InnerGate",4.68f,.16f,43.75f,metal);
        Torus("OuterTrim",6.02f,.09f,44,metal);
        for(int i=0;i<12;++i)
        {
            float angle=i*30*Mathf.Deg2Rad;
            var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            Box("GateBrace",radial*5.64f+Vector3.forward*43.75f,new Vector3(.75f,.22f,.48f),i*30,metal);
        }
    }
    void Box(string name,Vector3 position,Vector3 scale,float angle,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.layer=31;
        go.transform.SetParent(transform,false); go.transform.localPosition=position;
        go.transform.localScale=scale; go.transform.localRotation=Quaternion.Euler(0,0,angle);
        Destroy(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial=material;
    }
    void Torus(string name,float radius,float thickness,float z,Material material)
    {
        const int segments=128,sides=8;
        var vertices=new Vector3[segments*sides]; var triangles=new int[segments*sides*6];
        for(int i=0;i<segments;++i) for(int j=0;j<sides;++j)
        {
            float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;
            vertices[i*sides+j]=new Vector3(Mathf.Cos(a)*(radius+thickness*Mathf.Cos(b)),Mathf.Sin(a)*(radius+thickness*Mathf.Cos(b)),z+thickness*Mathf.Sin(b));
            int k=(i*sides+j)*6,n=((i+1)%segments)*sides+j,m=i*sides+(j+1)%sides,p=((i+1)%segments)*sides+(j+1)%sides;
            triangles[k]=i*sides+j;triangles[k+1]=n;triangles[k+2]=m;triangles[k+3]=m;triangles[k+4]=n;triangles[k+5]=p;
        }
        var mesh=new Mesh {name=name,vertices=vertices,triangles=triangles}; mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=31;go.transform.SetParent(transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    void OnDestroy() {foreach(var mesh in meshes) Destroy(mesh);if(metal!=null)Destroy(metal);if(dark!=null)Destroy(dark);}
}
