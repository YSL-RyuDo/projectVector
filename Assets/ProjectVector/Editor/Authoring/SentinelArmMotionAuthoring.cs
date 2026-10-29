#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Targeted authoring: preserve the existing Sentinel prefab, HUD, hitboxes and animator.
public static class SentinelArmMotionAuthoring
{
    const string Prefab="Assets/ProjectVector/Shared/Characters/Prefabs/Sentinel/Sentinel.prefab";
    const string Clip="Assets/ProjectVector/Shared/Characters/Effects/Sentinel/Sentinel/Sentinel_Deploy_FP.anim";
    public static void AuthorVerifyAndBuild()
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch only");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try{
            var model=root.transform.Find("CameraRoot/Controller Model_Riging (1)");
            if(!model)throw new Exception("Missing existing first-person skeleton");
            var camera=root.GetComponent<PlayerCameraController>().PlayerCamera.transform;
            var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t==model||t.name=="Armature"||t.name.StartsWith("mixamorig:")).ToArray();
            var rotations=bones.Select(t=>t.localRotation).ToArray();
            var positions=bones.Select(t=>t.localPosition).ToArray();
            Transform Bone(string name)=>bones.First(t=>t.name=="mixamorig:"+name);
            void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localRotation=rotations[i];bones[i].localPosition=positions[i];}}
            var samples=new Quaternion[3][];var ps=new Vector3[3][];
            for(int stage=0;stage<3;stage++){
                Reset();
                // Same neutral-skeleton clip method as Initiator and Controller. Do not
                // add a left-hand correction on top of a still-active right-hand gun pose.
                model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(-.34f,-.10f,.13f)));
                var left=Bone("LeftArm");var fore=Bone("LeftForeArm");var hand=Bone("LeftHand");
                left.rotation=Quaternion.FromToRotation(fore.position-left.position,camera.TransformDirection(new Vector3(-1,-.3f,.1f)))*left.rotation;
                SkillArmPose.Reach(left,fore,hand,camera,Vector3.Lerp(new Vector3(-.26f,-.24f,.43f),new Vector3(-.18f,-.13f,.66f),stage*.5f),1);
                var right=Bone("RightArm");var rightFore=Bone("RightForeArm");var rightHand=Bone("RightHand");
                right.rotation=Quaternion.FromToRotation(rightFore.position-right.position,camera.TransformDirection(new Vector3(.15f,-1,-.25f)))*right.rotation;
                rightFore.rotation=Quaternion.FromToRotation(rightHand.position-rightFore.position,camera.TransformDirection(new Vector3(.1f,-1,-.35f)))*rightFore.rotation;
                samples[stage]=bones.Select(t=>t.localRotation).ToArray();ps[stage]=bones.Select(t=>t.localPosition).ToArray();
                var elbow=camera.InverseTransformPoint(rightFore.position);var wrist=camera.InverseTransformPoint(rightHand.position);
                if(wrist.y>-.3f||wrist.y>=elbow.y)throw new Exception("Right arm not lowered below view: "+wrist);
                Debug.Log("SENTINEL_ARM_POSE stage="+stage+" elbow="+elbow+" wrist="+wrist);
            }
            Reset();
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Clip);bool create=!clip;
            if(create)clip=new AnimationClip();else clip.ClearCurves();
            clip.name="Sentinel_Deploy_FP";clip.frameRate=60;clip.wrapMode=WrapMode.ClampForever;
            for(int i=0;i<bones.Length;i++)for(int axis=0;axis<4;axis++){
                string path=AnimationUtility.CalculateTransformPath(bones[i],model);
                clip.SetCurve(path,typeof(Transform),"localRotation."+"xyzw"[axis],new AnimationCurve(Enumerable.Range(0,3).Select(s=>new Keyframe(s*.16f,samples[s][i][axis])).ToArray()));
                if(axis<3)clip.SetCurve(path,typeof(Transform),"localPosition."+"xyz"[axis],new AnimationCurve(Enumerable.Range(0,3).Select(s=>new Keyframe(s*.16f,ps[s][i][axis])).ToArray()));
            }
            clip.EnsureQuaternionContinuity();if(create)AssetDatabase.CreateAsset(clip,Clip);else EditorUtility.SetDirty(clip);
            var motion=root.GetComponent<FirstPersonSkillMotion>();if(!motion)motion=root.AddComponent<FirstPersonSkillMotion>();
            var so=new SerializedObject(motion);so.FindProperty("_model").objectReferenceValue=model;so.FindProperty("_drone").objectReferenceValue=clip;
            var array=so.FindProperty("_bones");array.arraySize=bones.Length;for(int i=0;i<bones.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=bones[i];so.ApplyModifiedPropertiesWithoutUndo();
            var rig=new SerializedObject(root.GetComponent<SentinelCastMotion>());rig.FindProperty("skillMotion").objectReferenceValue=motion;rig.ApplyModifiedPropertiesWithoutUndo();
            // Apply/restore must leave every original bone and camera exactly where it was.
            var cameraPosition=camera.position;var cameraRotation=camera.rotation;
            foreach(float sample in new[]{0f,.16f,.32f}){
                motion.Apply(FirstPersonSkillMotion.Motion.Drone,sample,1);
                if(camera.InverseTransformPoint(Bone("RightHand").position).y>-.3f)throw new Exception("Clip did not lower right hand");
                motion.Restore(FirstPersonSkillMotion.Motion.Drone);
                for(int i=0;i<bones.Length;i++)if(Quaternion.Angle(bones[i].localRotation,rotations[i])>.02f||Vector3.Distance(bones[i].localPosition,positions[i])>.00001f)throw new Exception("Arm restore changed base pose: "+bones[i].name);
                if(camera.position!=cameraPosition||Quaternion.Angle(camera.rotation,cameraRotation)>.01f)throw new Exception("Skill changed gameplay camera");
            }
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            Debug.Log("SENTINEL_TWO_ARM_RESTORE_PASS: authored neutral clip; right arm lowered; all bone/camera state restored");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        SentinelPlacementVerification.VerifyAndBuild();
    }
}
#endif
