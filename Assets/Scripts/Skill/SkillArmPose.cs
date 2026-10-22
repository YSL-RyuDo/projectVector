using UnityEngine;

// Two-bone presentation solve: rotate existing joints only. Never translate the
// model/shoulder, change bone lengths, or move the gameplay camera.
public static class SkillArmPose
{
    public static void Reach(Transform upper, Transform fore, Transform hand,
        Transform frame, Vector3 localTarget, float weight)
    {
        if(!upper || !fore || !hand || !frame || weight<=0)return;
        Vector3 a=upper.position,b=fore.position,c=hand.position;
        float first=Vector3.Distance(a,b),second=Vector3.Distance(b,c);
        if(first<.001f || second<.001f)return;
        Quaternion wrist=hand.localRotation;
        Vector3 target=Vector3.Lerp(c,frame.TransformPoint(localTarget),Mathf.Clamp01(weight));
        Vector3 direction=(target-a).normalized;
        float distance=Mathf.Clamp(Vector3.Distance(a,target),Mathf.Abs(first-second)+.002f,(first+second)*.97f);
        target=a+direction*distance;
        // Preserve the existing elbow side, biased down rather than flipping across the arm.
        Vector3 pole=Vector3.ProjectOnPlane(b-a,direction);
        if(pole.sqrMagnitude<.0001f)pole=Vector3.ProjectOnPlane(-frame.up-frame.right*.35f,direction);
        pole.Normalize();
        float along=(first*first+distance*distance-second*second)/(2*distance);
        float height=Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
        Vector3 elbow=a+direction*along+pole*height;
        upper.rotation=Quaternion.FromToRotation(b-a,elbow-a)*upper.rotation;
        fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,target-fore.position)*fore.rotation;
        // Keep the authored wrist-to-forearm angle. Keeping its world rotation
        // instead would counter-rotate the wrist as the elbow extends.
        hand.localRotation=wrist;
    }
}
