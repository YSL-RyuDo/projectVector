using System;
using System.Collections.Generic;
using UnityEngine;

// Timestamped interpolation only: never predicts a remote player through walls.
public sealed class RemoteMotionBuffer
{
    public struct Pose
    {
        public double time;
        public Vector3 position;
        public float yaw, pitch;
        public byte animation;
        public bool crouching;
    }
    readonly List<Pose> samples = new List<Pose>(32);
    double clockOffset;
    public void Clear() => samples.Clear();
    public bool Push(Pose pose, double arrival)
    {
        if (double.IsNaN(pose.time) || double.IsInfinity(pose.time) ||
            !float.IsFinite(pose.position.x) || !float.IsFinite(pose.position.y) || !float.IsFinite(pose.position.z) ||
            !float.IsFinite(pose.yaw) || !float.IsFinite(pose.pitch)) return false;
        if (samples.Count>0 && pose.time<=samples[samples.Count-1].time) return false;
        // Large discontinuities are respawns/teleports, not a path to glide along.
        if (samples.Count>0 && (Vector3.Distance(samples[samples.Count-1].position,pose.position)>5f ||
            pose.time-samples[samples.Count-1].time>1.0)) Clear();
        if (samples.Count==0) clockOffset=arrival-pose.time;
        else clockOffset=Math.Min(clockOffset,arrival-pose.time);
        samples.Add(pose);
        if(samples.Count>32) samples.RemoveAt(0);
        return true;
    }
    public bool Sample(double now, out Pose pose)
    {
        pose=default;
        if(samples.Count==0) return false;
        double time=now-clockOffset-0.10; // Two 20 Hz snapshots absorb normal jitter.
        while(samples.Count>2 && samples[1].time<=time) samples.RemoveAt(0);
        pose=samples[0];
        if(time<=pose.time) return true;
        Pose last=samples[samples.Count-1];
        if(time>=last.time) {pose=last;return true;} // Stall holds last known position.
        for(int i=1;i<samples.Count;i++)
        {
            if(samples[i].time<time) continue;
            var a=samples[i-1];var b=samples[i];
            float t=(float)((time-a.time)/(b.time-a.time));
            pose=a;
            pose.position=Vector3.Lerp(a.position,b.position,t);
            pose.yaw=Mathf.LerpAngle(a.yaw,b.yaw,t);
            pose.pitch=Mathf.LerpAngle(a.pitch,b.pitch,t);
            if(t>=1f) {pose.animation=b.animation;pose.crouching=b.crouching;}
            return true;
        }
        return true;
    }
}
