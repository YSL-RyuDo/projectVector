using UnityEngine;

// A single first-person hand rig is shared by the four roles. Role-specific
// third-person weapon offsets must not determine its grasp point.
[DefaultExecutionOrder(1500)]
public sealed class FirstPersonKnifeGrip : MonoBehaviour
{
    [SerializeField] Vector3 handleCenter = new Vector3(0, -.035f, 0);
    Transform hand, knuckle, finger, upper, forearm, leftUpper, gripRoot;
    Camera viewCamera;
    Quaternion upperBase, foreBase, handBase, leftBase;
    Vector3 rigBase;
    bool posed;
    bool scaled;
    float swingStarted = -10;
    public bool Ready => hand != null && knuckle != null && finger != null;
    void OnEnable() { Resolve(); swingStarted = -10; }
    void Resolve()
    {
        // SpawnWeaponObject parents this prefab to the authored right-hand holder.
        var t=transform.parent;
        while(t != null && t.name != "mixamorig:RightHand") t=t.parent;
        hand=t;
        if(hand==null)return;
        bool firstPerson=false;
        for(var ancestor=hand;ancestor!=null;ancestor=ancestor.parent)if(ancestor.name=="CameraRoot"){firstPerson=true;break;}
        if(!firstPerson){hand=null;return;} // World drops and third-person weapons retain their authored transforms.
        if(!scaled){transform.localScale*=.65f;scaled=true;}
        foreach(var bone in hand.GetComponentsInChildren<Transform>(true)) {
            if(bone.name=="mixamorig:RightHandIndex1")knuckle=bone;
            if(bone.name=="mixamorig:RightHandIndex3")finger=bone;
        }
        forearm=hand.parent;upper=forearm != null ? forearm.parent : null;
        viewCamera=GetComponentInParent<PlayerCameraController>()?.PlayerCamera;
        gripRoot=upper != null ? upper.parent : null;
        if(upper && upper.parent && upper.parent.parent)
            foreach(var bone in upper.parent.parent.GetComponentsInChildren<Transform>(true))if(bone.name=="mixamorig:LeftArm")leftUpper=bone;
    }
    public void BeginSwing() { if(Ready && Time.time-swingStarted>=.3f)swingStarted=Time.time; }
    void Restore() { if(!posed)return;upper.localRotation=upperBase;forearm.localRotation=foreBase;hand.localRotation=handBase;if(leftUpper)leftUpper.localRotation=leftBase;if(gripRoot)gripRoot.localPosition=rigBase;posed=false; }
    void Update()=>Restore();
    void OnDisable()=>Restore();
    void LateUpdate()
    {
        if(!Ready)Resolve();
        if(!Ready)return;
        upperBase=upper.localRotation;foreBase=forearm.localRotation;handBase=hand.localRotation;
        // Turn the grasp with the blade so its side is readable in first person.
        // The edge still points forward; rotating only the mesh would break the grip.
        hand.rotation=Quaternion.AngleAxis(-22f,-hand.right)*hand.rotation;
        // The four rigs have different authored roots and the training camera
        // uses a narrower FOV. Offset the FP right-arm chain as a unit, never
        // the knife away from its fingers. Leave the arm model root and left
        // sleeve in place. Restore the authored shoulder on unequip.
        if(gripRoot)
        {
            rigBase=gripRoot.localPosition;
            if(viewCamera)
            {
                Vector3 palm=(knuckle.position+finger.position)*.5f;
                gripRoot.position+=viewCamera.transform.position+viewCamera.transform.rotation*new Vector3(.22f,-.20f,.55f)-palm;
            }
        }
        if(leftUpper){leftBase=leftUpper.localRotation;if(viewCamera)leftUpper.rotation=Quaternion.AngleAxis(90,viewCamera.transform.right)*leftUpper.rotation;}
        posed=true;
        float phase=(Time.time-swingStarted)/.3f;
        if(phase>=0 && phase<1 && upper && forearm) {
            // One arm-driven slash; the held mesh never translates away from the palm.
            float stroke=Mathf.Sin(phase*Mathf.PI);
            if(viewCamera)upper.rotation=Quaternion.AngleAxis(-30*stroke,viewCamera.transform.up)*upper.rotation;
            forearm.localRotation=foreBase*Quaternion.Euler(0,0,8*stroke);posed=true;
        }
        Align();
    }
    public void Align()
    {
        if(!Ready)Resolve();if(!Ready)return;
        // The mesh's +X cutting edge is turned forward by KnifeBody's saved
        // -90 Y rotation. Keep that correction: cancelling it presents the
        // broad side to the camera, with the cutting edge pointing right.
        transform.rotation=Quaternion.LookRotation(hand.up,-hand.right);
        Vector3 palm=(knuckle.position+finger.position)*.5f;
        transform.position=palm-transform.TransformVector(handleCenter);
    }
    public float GripError => Ready ? Vector3.Distance(transform.TransformPoint(handleCenter),(knuckle.position+finger.position)*.5f) : float.PositiveInfinity;
}
