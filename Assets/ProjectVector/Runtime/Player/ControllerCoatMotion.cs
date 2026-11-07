using System;
using UnityEngine;

// Secondary motion for the authored coat bones only. No gameplay colliders or
// player movement are changed; the original body, arms and hit bones stay intact.
[DefaultExecutionOrder(2200)]
public sealed class ControllerCoatMotion : MonoBehaviour
{
    [Serializable] public sealed class Anchor
    {
        public Transform[] bones;
        public Vector3[] points;
        public float[] weights;
        public Vector3 Position()
        {
            var p=Vector3.zero;
            for(int i=0;i<bones.Length;i++)p+=bones[i].TransformPoint(points[i])*weights[i];
            return p;
        }
    }
    public Transform actor;
    public Transform[] nodes;
    public Vector3[] restPoints;
    public Anchor[] anchors;
    public Transform[] legJoints;
    [Serializable] public struct ShoeContact
    {
        public Transform bone;
        public Vector3 heel, toe;
        public float radius;
    }
    public ShoeContact[] shoes;
    [Serializable] public struct SurfaceContact
    {
        public int a,b,c,d;
        public Vector4 weights;
        public Vector3 oa,ob,oc,od;
    }
    public SurfaceContact[] surface;
    Quaternion[] surfaceRotations;
    public int rows=9,columns=7;
    Vector3[] current,previous;
    Vector3 previousActor;
    bool initialized;
    readonly RaycastHit[] hits=new RaycastHit[24];
    float ground;
    float crouchAmount;
    void OnEnable(){initialized=false;}
    void LateUpdate(){Step(Time.deltaTime);}
    public void ResetMotion(){initialized=false;}
    Vector3 Neutral(int i)=>anchors[i/rows].Position()+actor.rotation*(restPoints[i]-restPoints[i/rows*rows]);
    Vector3 Rest(int index)
    {
        int column=index/rows;
        float hem=(index%rows)/(float)(rows-1);
        var offset=restPoints[index]-restPoints[column*rows];
        var outward=new Vector3(restPoints[index].x,0,restPoints[index].z+.015f).normalized;
        offset+=outward*(.025f*Mathf.SmoothStep(0,1,hem));
        var anchor=anchors[column].Position();
        float crouch=Mathf.Clamp01((restPoints[0].y-.045f-(anchor.y-actor.position.y))/.35f);
        float back=Mathf.Clamp01(-outward.z);
        float arch=Mathf.Sin(hem*Mathf.PI)*crouch*back;
        // A structured back panel clears the bent calf before its hem reaches
        // the floor. Pulling it straight down would put the leg through its middle.
        offset.y+=arch*.16f;offset.z-=arch*.12f;
        var desired=anchor+actor.rotation*offset;
        // When the waist drops, the spare length spreads over the floor instead
        // of every row pulling downward into the same crumpled point.
        float below=Mathf.Max(0,ground+.015f-desired.y);
        desired+=actor.rotation*outward*(below*1.2f);
        desired.y=Mathf.Max(desired.y,ground+.015f);
        return desired;
    }
    public void Step(float deltaTime)
    {
        if(!actor||nodes==null||nodes.Length==0)return;
        float waist=0;foreach(var anchor in anchors)waist+=anchor.Position().y-actor.position.y;waist/=anchors.Length;
        crouchAmount=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.38f,restPoints[0].y-waist));
        if(crouchAmount<=.001f)
        {
            // Standing keeps the artist's original coat silhouette exactly.
            // Cloth deformation is reserved for low poses, not constant realism.
            for(int i=0;i<nodes.Length;i++)nodes[i].SetPositionAndRotation(Neutral(i),actor.rotation);
            initialized=false;return;
        }
        float dt=Mathf.Clamp(deltaTime,1f/240,1f/30);
        ground=actor.position.y+.025f;
        int count=Physics.RaycastNonAlloc(actor.position+Vector3.up*.5f,Vector3.down,hits,2,~0,QueryTriggerInteraction.Ignore);
        float nearest=float.MaxValue;
        for(int i=0;i<count;i++)
        {
            var h=hits[i];
            if(h.transform.IsChildOf(actor)||h.collider.GetComponentInParent<PlayerIdentity>()||h.normal.y<.5f||h.distance>=nearest)continue;
            nearest=h.distance;ground=h.point.y+.025f;
        }
        if(!initialized||(actor.position-previousActor).sqrMagnitude>4)
        {
            current=new Vector3[nodes.Length];previous=new Vector3[nodes.Length];
            for(int i=0;i<nodes.Length;i++)current[i]=previous[i]=Rest(i);
            initialized=true;
        }
        previousActor=actor.position;
        for(int i=0;i<current.Length;i++)
        {
            if(i%rows==0){current[i]=previous[i]=Rest(i);continue;}
            // Pose-driven folds keep a clean anime silhouette. Contact offsets
            // must not become velocity and fling the panels through the body.
            current[i]=Rest(i);previous[i]=current[i];
        }
        for(int iteration=0;iteration<10;iteration++)
        {
            for(int c=0;c<columns;c++)current[c*rows]=Rest(c*rows);
            for(int c=0;c<columns;c++)for(int r=1;r<rows;r++)
            {
                int i=c*rows+r;
                Constrain(i-1,i,Vector3.Distance(restPoints[i-1],restPoints[i]),r==1);
                // Bending resistance retains the broad folds without making the
                // lower hem rigid; adjacent rows still move independently.
                if(r>1)Constrain(i-2,i,Vector3.Distance(restPoints[i-2],restPoints[i]),r==2,.32f);
                if(c>0)Constrain(i-rows,i,Vector3.Distance(restPoints[i-rows],restPoints[i]),false,.40f);
                if(c>1)Constrain(i-rows*2,i,Vector3.Distance(restPoints[i-rows*2],restPoints[i]),false,.12f);
            }
            for(int i=0;i<current.Length;i++)if(i%rows!=0)
            {
                current[i]=Vector3.Lerp(current[i],Rest(i),.035f);
                // Cloth-body contacts use geometry calculations, never collision
                // components that could intercept weapon rays or player movement.
                for(int leg=0;leg<2;leg++)
                {
                    PushCapsule(i,legJoints[leg*3].position,legJoints[leg*3+1].position,.095f,true);
                    PushCapsule(i,legJoints[leg*3+1].position,legJoints[leg*3+2].position,.065f,true);
                }
                var p=current[i];
                // Keep the heavy hem draped downward. Ground contact should
                // spread it along the floor, not fold it back up through itself.
                p.y=Mathf.Max(ground,Mathf.Min(p.y,current[i-1].y+.005f));current[i]=p;
                // Shoe volumes must be applied after floor/drape constraints;
                // otherwise those constraints can push the cloth back into a boot.
                if(shoes!=null)foreach(var shoe in shoes)if(shoe.bone)
                    PushCapsule(i,shoe.bone.TransformPoint(shoe.heel),shoe.bone.TransformPoint(shoe.toe),shoe.radius);
            }
        }
        SolveSurfaceContacts();
        for(int i=0;i<nodes.Length;i++)
        {
            nodes[i].SetPositionAndRotation(Vector3.Lerp(Neutral(i),current[i],crouchAmount),Quaternion.Slerp(actor.rotation,surfaceRotations!=null?surfaceRotations[i]:CoatRotation(i),crouchAmount));
        }
    }
    Quaternion CoatRotation(int i)
    {
        int row=i%rows;int next=row==rows-1?i:i+1;int prev=row==0?i:i-1;
        var original=actor.rotation*(restPoints[next]-restPoints[prev]);
        var rotation=Quaternion.FromToRotation(original,current[next]-current[prev])*actor.rotation;
        return Quaternion.RotateTowards(actor.rotation,rotation,55);
    }
    void SolveSurfaceContacts()
    {
        if(surface==null||surface.Length==0)return;
        if(surfaceRotations==null||surfaceRotations.Length!=nodes.Length)surfaceRotations=new Quaternion[nodes.Length];
        for(int pass=0;pass<8;pass++)
        {
            for(int i=0;i<nodes.Length;i++)surfaceRotations[i]=CoatRotation(i);
            foreach(var s in surface)
            {
                var p=(current[s.a]+surfaceRotations[s.a]*s.oa)*s.weights.x+(current[s.b]+surfaceRotations[s.b]*s.ob)*s.weights.y+(current[s.c]+surfaceRotations[s.c]*s.oc)*s.weights.z+(current[s.d]+surfaceRotations[s.d]*s.od)*s.weights.w;
                var target=p;target.y=Mathf.Max(target.y,ground);
                var rest=restPoints[s.a]*s.weights.x+restPoints[s.b]*s.weights.y+restPoints[s.c]*s.weights.z+restPoints[s.d]*s.weights.w;
                // Keep each panel on its authored side of the body. Deriving
                // this from its already penetrated position flips back to front.
                var outward=actor.rotation*new Vector3(rest.x,0,rest.z+.015f).normalized;
                for(int leg=0;leg<2;leg++)
                {
                    target=OutsideCapsule(target,legJoints[leg*3].position,legJoints[leg*3+1].position,.095f,outward);
                    target=OutsideCapsule(target,legJoints[leg*3+1].position,legJoints[leg*3+2].position,.070f,outward);
                }
                if(shoes!=null)foreach(var shoe in shoes)if(shoe.bone)
                    target=OutsideCapsule(target,shoe.bone.TransformPoint(shoe.heel),shoe.bone.TransformPoint(shoe.toe),shoe.radius-.012f,Vector3.zero);
                var correction=Vector3.ClampMagnitude(target-p,.025f);
                float wa=s.a%rows==0?0:s.weights.x,wb=s.b%rows==0?0:s.weights.y,wc=s.c%rows==0?0:s.weights.z,wd=s.d%rows==0?0:s.weights.w;
                float sum=wa*wa+wb*wb+wc*wc+wd*wd;if(sum<.10f)continue;
                // Correct the skinned surface through its existing four weights.
                // Node-only contacts leave gaps between bones that a boot can cross.
                current[s.a]+=Vector3.ClampMagnitude(correction*(wa/sum),.015f);current[s.b]+=Vector3.ClampMagnitude(correction*(wb/sum),.015f);
                current[s.c]+=Vector3.ClampMagnitude(correction*(wc/sum),.015f);current[s.d]+=Vector3.ClampMagnitude(correction*(wd/sum),.015f);
            }
            if(pass<7)for(int c=0;c<columns;c++)for(int r=1;r<rows;r++)
            {int i=c*rows+r;Constrain(i-1,i,Vector3.Distance(restPoints[i-1],restPoints[i]),r==1,.65f);}
        }
    }
    static Vector3 OutsideCapsule(Vector3 p,Vector3 a,Vector3 b,float radius,Vector3 outward)
    {
        var ab=b-a;var nearest=a+ab*Mathf.Clamp01(Vector3.Dot(p-a,ab)/Mathf.Max(.00001f,ab.sqrMagnitude));
        var delta=p-nearest;if(delta.sqrMagnitude>=radius*radius)return p;
        var direction=delta.sqrMagnitude>.000001f?delta.normalized:Vector3.up;
        if(outward.sqrMagnitude>.5f&&Vector3.Dot(direction,outward)<.1f)direction=outward;
        return nearest+direction*radius;
    }
    void Constrain(int a,int b,float length,bool fixedA,float stiffness=1)
    {
        var d=current[b]-current[a];float distance=d.magnitude;if(distance<.00001f)return;
        var correction=d*((distance-length)/distance)*stiffness;
        if(fixedA)current[b]-=correction;
        else{current[a]+=correction*.5f;current[b]-=correction*.5f;}
    }
    void PushCapsule(int i,Vector3 a,Vector3 b,float radius,bool outside=false)
    {
        var ab=b-a;float t=Mathf.Clamp01(Vector3.Dot(current[i]-a,ab)/Mathf.Max(.00001f,ab.sqrMagnitude));
        var point=a+ab*t;var d=current[i]-point;
        if(d.sqrMagnitude<radius*radius)
        {
            var direction=d.sqrMagnitude>.000001f?d.normalized:-actor.forward;
            var outward=actor.rotation*new Vector3(restPoints[i].x,0,restPoints[i].z+.015f).normalized;
            if(outside&&Vector3.Dot(direction,outward)<.15f)direction=outward;
            current[i]=point+direction*radius;
        }
    }
}
