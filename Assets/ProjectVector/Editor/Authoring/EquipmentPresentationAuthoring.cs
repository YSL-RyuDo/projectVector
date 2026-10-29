#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class EquipmentPresentationAuthoring
{
    public static void VerifyAndBuild()
    {
        SpikeRuntimeVerification.Run();
        RankedCaptureTestEditor.Build();
    }
    public static void Inspect()
    {
        var report=new StringBuilder();
        foreach(var role in SpikePresentationAuthoring.Characters)
        {
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{role}/{role}.prefab"));
            foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
            var rig=actor.GetComponent<SpikeInteractionPresentation>().firstPerson;
            SpikeInteractionPresentation.Sample(rig,1,.65f);
            foreach(string side in new[]{"Left","Right"})
            {
                var hand=rig.bones.First(b=>b.name=="mixamorig:"+side+"Hand");
                var target=rig.device.transform.TransformPoint(new Vector3(side=="Left"?-.17f:.17f,.065f,-.02f));
                report.AppendLine($"{role} {side} hand={hand.position:F4} target={target:F4} distance={Vector3.Distance(hand.position,target):F4} upper={hand.parent.parent.position:F4} length={Vector3.Distance(hand.position,hand.parent.position)+Vector3.Distance(hand.parent.position,hand.parent.parent.position):F4}");
                report.AppendLine($"model={rig.model.position:F4} scale={rig.model.lossyScale} hand parent={hand.parent.name} shoulder={hand.parent.parent.parent.name} included={rig.bones.Contains(hand.parent.parent.parent)}");
            }
            UnityEngine.Object.DestroyImmediate(actor);
        }
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ROOT"),"EquipmentRig.txt"),report.ToString());
    }
}
#endif
