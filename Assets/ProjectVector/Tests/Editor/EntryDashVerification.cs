#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Read-only checks. No scene/prefab/asset saves or live account access.
public static class EntryDashVerification
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string reason) { if (!ok) throw new Exception(reason); }
    public static void RunAndBuild() { Run(); EntryStealthVerification.Run(); RankedCaptureTestEditor.Build(); }
    public static void Run()
    {
        Require(Application.isBatchMode, "Explicit isolated batch required");
        var buffer = new RemoteMotionBuffer();
        // The unchanged 20 Hz network contract must cover a 28 m/s, .28 s dash
        // without tripping the teleport threshold or overshooting its endpoint.
        for (int i = 0; i <= 12; i++)
            Require(buffer.Push(new RemoteMotionBuffer.Pose {
                time = 10 + i * .05, position = Vector3.right * (28 * Mathf.Min(i * .05f, .28f))
            }, 20 + i * .05), "Snapshot rejected");
        float previous = 0; int steps = 0;
        for (int i = 0; i <= 40; i++)
        {
            Require(buffer.Sample(20.1 + i * .01, out var pose), "No pose");
            float delta = pose.position.x - previous;
            Require(delta >= -.0001f && delta <= .281f, "Remote discontinuity/backtracking");
            if (delta > .001f) steps++;
            previous = pose.position.x;
        }
        Require(steps > 20 && Mathf.Abs(previous - 7.84f) < .001f, "Dash endpoint or interpolation changed");
        Require(!buffer.Push(new RemoteMotionBuffer.Pose { time = 10.1 }, 21), "Stale packet accepted");
        buffer.Sample(22, out var held); Require(held.position.x == previous, "Extrapolated through a wall during stall");
        buffer.Push(new RemoteMotionBuffer.Pose { time = 12, position = Vector3.right * 30 }, 22);
        buffer.Sample(22, out var respawn); Require(respawn.position.x == 30, "Respawn incorrectly interpolated");

        var root = PrefabUtility.LoadPrefabContents("Assets/Prefab/Character/Entry/Entry.prefab");
        try {
            var movement = root.GetComponent<PlayerMovement>();
            typeof(PlayerHP).GetMethod("Awake", Flags).Invoke(root.GetComponent<PlayerHP>(), null);
            typeof(PlayerMovement).GetMethod("Awake", Flags).Invoke(movement, null);
            typeof(PlayerMovement).GetField("_currentAnimIndex", Flags).SetValue(movement, 0);
            movement.OnReceiveRemoteMove(root.transform.position,0,0,5,false,10);
            Require(movement.GetCurrentAnimIndex()==0, "Receive callback applied animation ahead of buffered pose");
            typeof(PlayerMovement).GetMethod("SyncRemotePlayerMovement", Flags).Invoke(movement, null);
            Require(movement.GetCurrentAnimIndex()==5, "Buffered pose did not apply animation");
            var motor = root.GetComponent<EntryDashMotor>();
            typeof(EntryDashMotor).GetMethod("Awake", Flags).Invoke(motor, null);
            movement.CanMove = false;
            // No local ownership: the event can animate but may not move, touch
            // the camera, or unlock movement. Remote snapshots own the trajectory.
            var position = root.transform.position;
            var routine = (IEnumerator)typeof(EntryDashMotor).GetMethod("DashRoutine", Flags)
                .Invoke(motor, new object[] {Vector3.forward, 28f, .28f});
            Require(routine.MoveNext() && motor.IsDashing, "Remote presentation not started");
            Require(root.transform.position == position && !movement.CanMove, "Remote dash wrote movement state");
            motor.ForceEndDash(); motor.ForceEndDash();
            Require(!motor.IsDashing && !movement.CanMove, "End event unlocked remote movement");
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("ENTRY_DASH_PASS: 20 Hz interpolation; no backtracking/overshoot; stale packet/stall/respawn; remote impulse isolation; idempotent cleanup");
    }
}
#endif
