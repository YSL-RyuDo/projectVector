#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class FirstPersonSkillMotionAuthoring
{
    const string Path="Assets/Prefab/Character/Initiator/Initiator.prefab";
    const string Folder="Assets/Character Animation/Initiator/FirstPerson";
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Use an isolated batch editor");
        var root=PrefabUtility.LoadPrefabContents(Path);
        try { Author(root);PrefabUtility.SaveAsPrefabAsset(root,Path); }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("SKILL_MOTION_AUTHOR_PASS");
    }
    public static void Author(GameObject root)
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var model=root.transform.Find("CameraRoot/Controller Model_Riging (1)");
        var camera=root.GetComponentInChildren<Camera>(true).transform;
        var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t==model || t.name=="Armature" || t.name.StartsWith("mixamorig:")).ToArray();
        var neutral=bones.Select(t=>t.localRotation).ToArray();
        var positions=bones.Select(t=>t.localPosition).ToArray();
        Transform Bone(string name)=>bones.First(t=>t.name=="mixamorig:"+name);
        void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localRotation=neutral[i];bones[i].localPosition=positions[i];}}
        var upper=Bone("LeftArm");var fore=Bone("LeftForeArm");var hand=Bone("LeftHand");
        var right=Bone("RightArm");var rightFore=Bone("RightForeArm");
        Debug.Log("NEUTRAL_ARM shoulder="+camera.InverseTransformPoint(upper.position)+" elbow="+camera.InverseTransformPoint(fore.position)+" hand="+camera.InverseTransformPoint(hand.position));
        AnimationClip Build(string name,bool emp)
        {
            var samples=new Quaternion[3][];
            var samplePositions=new Vector3[3][];
            var times=new[]{0f,.16f,.32f};
            for(int stage=0;stage<3;stage++){
                Reset();
                // Whole viewmodel translation belongs to the skill clip, never
                // to individual shoulders/bone lengths. Frame the left arm from
                // lower-left as in the supplied drone reference, not gun-right.
                model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(-.34f,-.10f,.13f)));
                // Pose the bare neutral skeleton. No firearm animation is sampled here.
                Vector3 target=emp?Vector3.Lerp(new Vector3(-.05f,-.10f,.34f),new Vector3(-.015f,-.07f,.36f),stage*.5f):
                    Vector3.Lerp(new Vector3(-.025f,-.12f,.36f),new Vector3(.015f,-.07f,.39f),stage*.5f);
                // The elbow enters from lower-left; a straight downward pole
                // puts the wide sleeve at bottom-center instead of the reference framing.
                upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,camera.TransformDirection(new Vector3(-1,-.3f,.1f)))*upper.rotation;
                SkillArmPose.Reach(upper,fore,hand,camera,target,1);
                var fingerTip=hand.GetComponentsInChildren<Transform>(true).Last(t=>t.name.StartsWith("mixamorig:"));
                Vector3 fingers=emp?new Vector3(0,.8f,.45f):new Vector3(.9f,.1f,.65f);
                hand.rotation=Quaternion.FromToRotation(fingerTip.position-hand.position,camera.TransformDirection(fingers))*hand.rotation;
                if(emp)hand.rotation=Quaternion.AngleAxis(-65,camera.TransformDirection(fingers).normalized)*hand.rotation;
                // Neutral wrist follows the forearm; fingers remain in their original open pose.
                right.rotation=Quaternion.FromToRotation(rightFore.position-right.position,camera.TransformDirection(new Vector3(.15f,-1,-.25f)))*right.rotation;
                samples[stage]=bones.Select(t=>t.localRotation).ToArray();
                samplePositions[stage]=bones.Select(t=>t.localPosition).ToArray();
            }
            Reset();
            string path=Folder+"/"+name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool create=!clip;if(create)clip=new AnimationClip();else clip.ClearCurves();
            clip.name=name;clip.frameRate=60;clip.wrapMode=WrapMode.ClampForever;
            for(int i=0;i<bones.Length;i++){
                string bonePath=AnimationUtility.CalculateTransformPath(bones[i],model);
                for(int axis=0;axis<4;axis++){
                    var curve=new AnimationCurve();
                    for(int s=0;s<3;s++)curve.AddKey(new Keyframe(times[s],samples[s][i][axis]));
                    clip.SetCurve(bonePath,typeof(Transform),"localRotation."+"xyzw"[axis],curve);
                }
                for(int axis=0;axis<3;axis++){
                    var curve=new AnimationCurve();
                    for(int s=0;s<3;s++)curve.AddKey(new Keyframe(times[s],samplePositions[s][i][axis]));
                    clip.SetCurve(bonePath,typeof(Transform),"localPosition."+"xyz"[axis],curve);
                }
            }
            clip.EnsureQuaternionContinuity();
            if(create)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);
            return clip;
        }
        var drone=Build("Drone_Deploy_FP",false);var empClip=Build("EMP_Release_FP",true);
        var motion=root.GetComponent<FirstPersonSkillMotion>();if(!motion)motion=root.AddComponent<FirstPersonSkillMotion>();
        var data=new SerializedObject(motion);data.FindProperty("_model").objectReferenceValue=model;
        data.FindProperty("_drone").objectReferenceValue=drone;data.FindProperty("_emp").objectReferenceValue=empClip;
        var bound=data.FindProperty("_bones");bound.arraySize=bones.Length;
        for(int i=0;i<bones.Length;i++)bound.GetArrayElementAtIndex(i).objectReferenceValue=bones[i];
        data.ApplyModifiedPropertiesWithoutUndo();Reset();
    }
}
#endif
