#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Explicit batch authoring only. All UI and hand attachments are saved before Play.
public static class EntryPolishAuthoring
{
    const string Art="Assets/Art/Entry", Prefab="Assets/Prefab/Character/Entry/Entry.prefab";
    static Material white,cyan,graphite;
    static Mesh bladeMesh;
    static T Ensure<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
    static Transform Node(Transform parent,string name){var t=parent.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(parent,false);}return t;}
    static void Ref(Object target,string name,Object value){var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Refs(Object target,string name,Object[] values){var so=new SerializedObject(target);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static Material Mat(string name,Color color,bool glow=false){var path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.55f);m.SetFloat("_Smoothness",.65f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.7f);}EditorUtility.SetDirty(m);return m;}
    static Renderer Shape(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material){var t=parent.Find(name);if(!t){var g=GameObject.CreatePrimitive(type);t=g.transform;t.name=name;t.SetParent(parent,false);Object.DestroyImmediate(g.GetComponent<Collider>());}t.localPosition=pos;t.localScale=scale;var r=t.GetComponent<Renderer>();r.sharedMaterial=material;return r;}
    static void CreateMesh()
    {
        var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
        for(int i=0;i<=8;i++){float t=i/8f,width=Mathf.Lerp(.008f,.0002f,t),y=-t*t*t*.045f,z=t*.19f;
            vertices.Add(new Vector3(-width,y,z));vertices.Add(new Vector3(0,y+.008f*(1-t),z));vertices.Add(new Vector3(width,y,z));vertices.Add(new Vector3(0,y-.004f*(1-t),z));}
        for(int i=0;i<8;i++)for(int side=0;side<4;side++){int a=i*4+side,b=i*4+(side+1)%4;triangles.AddRange(new[]{a,b,a+4,b,b+4,a+4});}
        triangles.AddRange(new[]{0,2,1,0,3,2,32,33,34,32,34,35});
        var path=Art+"/CurvedClaw.asset";bladeMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=!bladeMesh;if(create)bladeMesh=new Mesh();else bladeMesh.Clear();
        bladeMesh.name="EntryCurvedClaw";bladeMesh.SetVertices(vertices);bladeMesh.SetTriangles(triangles,0);bladeMesh.RecalculateNormals();bladeMesh.RecalculateBounds();
        if(create)AssetDatabase.CreateAsset(bladeMesh,path);else EditorUtility.SetDirty(bladeMesh);
    }
    static Transform Claw(Transform parent,string name)
    {
        var t=Node(parent,name);Ensure<MeshFilter>(t.gameObject).sharedMesh=bladeMesh;Ensure<MeshRenderer>(t.gameObject).sharedMaterial=white;
        var edge=Node(t,"CyanCuttingEdge");edge.localPosition=new Vector3(0,.003f,0);edge.localScale=new Vector3(.08f,1,.98f);Ensure<MeshFilter>(edge.gameObject).sharedMesh=bladeMesh;Ensure<MeshRenderer>(edge.gameObject).sharedMaterial=cyan;
        return t;
    }
    static Transform HandFrame(Transform hand,Transform camera,string name)
    {
        var finger=hand.GetComponentsInChildren<Transform>(true).FirstOrDefault(b=>b!=hand&&b.name.IndexOf("Index",StringComparison.OrdinalIgnoreCase)>=0);
        if(!finger)throw new Exception("No index finger on "+hand.name);
        var forward=(finger.position-hand.position).normalized;
        var up=Vector3.ProjectOnPlane(camera.up,forward).normalized;
        if(up.sqrMagnitude<.1f)up=Vector3.ProjectOnPlane(camera.forward,forward).normalized;
        var t=Node(hand,name);t.position=hand.position+up*.012f+forward*.065f;
        t.rotation=Quaternion.LookRotation(forward,up);var scale=hand.lossyScale;t.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));return t;
    }
    static void Gauntlet(Transform mount)
    {
        // Replace only this authoring pass's prototype parts, not the original hand or sleeves.
        foreach(Transform child in mount.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        var outline=new[]{new Vector2(-.036f,-.04f),new Vector2(.036f,-.04f),new Vector2(.044f,-.022f),new Vector2(.038f,.028f),new Vector2(.024f,.04f),new Vector2(-.024f,.04f),new Vector2(-.038f,.028f),new Vector2(-.044f,-.022f)};
        var v=new System.Collections.Generic.List<Vector3>();var tr=new System.Collections.Generic.List<int>();
        for(int ring=0;ring<3;ring++)for(int i=0;i<outline.Length;i++){var pt=outline[i]*(ring==1?1:.84f);v.Add(new Vector3(pt.x,ring*.004f+(.003f-Mathf.Abs(pt.x)*.08f),pt.y));}
        for(int ring=0;ring<2;ring++)for(int i=0;i<8;i++){int a=ring*8+i,b=ring*8+(i+1)%8;tr.AddRange(new[]{a,b,a+8,b,b+8,a+8});}
        for(int i=1;i<7;i++)tr.AddRange(new[]{16,16+i,17+i,0,i+1,i});
        string path=Art+"/HandGuard.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=!mesh;if(create)mesh=new Mesh();else mesh.Clear();
        mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
        var guard=Node(mount,"CurvedHandGuard");Ensure<MeshFilter>(guard.gameObject).sharedMesh=mesh;Ensure<MeshRenderer>(guard.gameObject).sharedMaterial=graphite;
        for(int i=0;i<3;i++){
            var socket=Shape(mount,"ClawSocket_"+i,PrimitiveType.Cylinder,new Vector3((i-1)*.028f,.012f,.026f),new Vector3(.022f,.022f,.022f),white);socket.transform.localRotation=Quaternion.Euler(90,0,0);
            var liner=Shape(mount,"SocketLiner_"+i,PrimitiveType.Cylinder,new Vector3((i-1)*.028f,.012f,.049f),new Vector3(.016f,.002f,.016f),graphite);liner.transform.localRotation=Quaternion.Euler(90,0,0);
            var led=Shape(mount,"ChargeLight_"+i,PrimitiveType.Sphere,new Vector3((i-1)*.028f,.019f,.006f),new Vector3(.006f,.004f,.018f),cyan);
        }
    }
    static void SmokeDevice(Transform parent)
    {
        const string name="BundleSmokeCanister";
        foreach(Transform child in parent.Cast<Transform>().ToArray())if(child.name!=name)Object.DestroyImmediate(child.gameObject);
        var t=parent.Find(name);
        if(!t){
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FPS_Bundle/FPS_Weapon1/Prefabs/Accessories/Grenade_02.prefab");
            if(!source)throw new Exception("Bundled grenade mesh missing");
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(source);t=obj.transform;t.SetParent(parent,false);t.name=name;
            PrefabUtility.UnpackPrefabInstance(obj,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var c in obj.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var c in obj.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(c);
        }
        t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;t.localScale=Vector3.one;
        var renderers=t.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        float height=Mathf.Max(.001f,bounds.size.y);float factor=.14f/height;t.localScale=Vector3.one*factor;
        bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        t.position+=parent.position-bounds.center;
        foreach(var renderer in renderers){
            var mats=renderer.sharedMaterials;
            for(int i=0;i<mats.Length;i++){
                if(!mats[i])continue;string matPath=Art+"/Canister_"+i+"_"+mats[i].name.Replace(" ","_")+".mat";
                // Keep the authored texture, normal map and surface detail from the bundled model.
                if(!AssetDatabase.GetAssetPath(mats[i]).StartsWith(Art+"/")){
                    var copy=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if(!copy){copy=new Material(mats[i]);copy.name="EntryCanister_"+i;AssetDatabase.CreateAsset(copy,matPath);}
                    if(copy.HasProperty("_BaseColor"))copy.SetColor("_BaseColor",new Color(.65f,.92f,.95f));
                    else if(copy.HasProperty("_Color"))copy.SetColor("_Color",new Color(.65f,.92f,.95f));
                    mats[i]=copy;
                }
            }
            renderer.sharedMaterials=mats;
        }
    }
    static ParticleSystem Particles(Transform parent,string name,bool wake)
    {
        var t=Node(parent,name);t.localPosition=new Vector3(0,wake?.9f:0,wake?0:.45f);var ps=Ensure<ParticleSystem>(t.gameObject);ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=wake?.28f:.35f;main.startLifetime=wake?.28f:.3f;main.startSpeed=wake?.1f:.8f;main.startSize=wake?.055f:.025f;main.startColor=new Color(.3f,.9f,1,.7f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=80;
        var emission=ps.emission;emission.rateOverTime=wake?110:0;if(!wake)emission.SetBursts(new[]{new ParticleSystem.Burst(0,24)});
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=wake?.25f:.2f;
        var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=cyan;return ps;
    }
    static AudioSource Audio(Transform parent){var source=Ensure<AudioSource>(Node(parent,"EntrySkillAudio").gameObject);source.playOnAwake=false;source.spatialBlend=1;source.minDistance=1;source.maxDistance=22;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;return source;}
    static void Hud(Transform root,EntrySkillPresentation rig)
    {
        var t=root.Find("EntrySkillStatus");if(!t){t=new GameObject("EntrySkillStatus",typeof(RectTransform)).transform;t.SetParent(root,false);}
        var canvas=Ensure<Canvas>(t.gameObject);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=12;canvas.enabled=false;
        var scaler=Ensure<CanvasScaler>(t.gameObject);scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        RectTransform Rect(string name,Vector2 pos,Vector2 size){var child=t.Find(name) as RectTransform;if(!child){child=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();child.SetParent(t,false);}child.anchorMin=child.anchorMax=new Vector2(.5f,0);child.pivot=new Vector2(.5f,0);child.anchoredPosition=pos;child.sizeDelta=size;return child;}
        var label=Ensure<Text>(Rect("SkillName",new Vector2(0,165),new Vector2(420,30)).gameObject);label.text="야수 여제  6/6";label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/Pretendard-SemiBold.otf");label.fontSize=20;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.raycastTarget=false;
        var images=new Image[6];for(int i=0;i<6;i++){images[i]=Ensure<Image>(Rect("ClawCharge_"+(i+1),new Vector2((i-2.5f)*20,154),new Vector2(14,5)).gameObject);images[i].color=new Color(.2f,.9f,1);images[i].raycastTarget=false;}
        Ref(rig,"statusCanvas",canvas);Ref(rig,"status",label);Refs(rig,"charges",images);
    }
    static AnimationClip PoseClip(string name,Transform model,Transform[] bones,Action<int> pose,int stages,float interval)
    {
        var q=bones.Select(b=>b.localRotation).ToArray();var p=bones.Select(b=>b.localPosition).ToArray();
        var rotations=new Quaternion[stages][];var positions=new Vector3[stages][];
        void Reset(){for(int i=0;i<bones.Length;i++){bones[i].localRotation=q[i];bones[i].localPosition=p[i];}}
        for(int s=0;s<stages;s++){Reset();pose(s);rotations[s]=bones.Select(b=>b.localRotation).ToArray();positions[s]=bones.Select(b=>b.localPosition).ToArray();}Reset();
        string path=Art+"/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);bool create=!clip;if(create)clip=new AnimationClip();else clip.ClearCurves();clip.name=name;clip.frameRate=60;clip.wrapMode=WrapMode.ClampForever;
        for(int i=0;i<bones.Length;i++)for(int axis=0;axis<4;axis++){string binding=AnimationUtility.CalculateTransformPath(bones[i],model);
            clip.SetCurve(binding,typeof(Transform),"localRotation."+"xyzw"[axis],new AnimationCurve(Enumerable.Range(0,stages).Select(s=>new Keyframe(s*interval,rotations[s][i][axis])).ToArray()));
            if(axis<3)clip.SetCurve(binding,typeof(Transform),"localPosition."+"xyz"[axis],new AnimationCurve(Enumerable.Range(0,stages).Select(s=>new Keyframe(s*interval,positions[s][i][axis])).ToArray()));}
        clip.EnsureQuaternionContinuity();if(create)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);return clip;
    }
    static void Player()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try{
            var camera=root.GetComponent<PlayerCameraController>().PlayerCamera.transform;
            var armReference=new SerializedObject(root.GetComponent<PlayerMovement>()).FindProperty("firstPersonArmModel").objectReferenceValue;
            var model=armReference is GameObject go?go.transform:armReference as Transform;
            if(!model)throw new Exception("Entry first-person arm model missing");
            var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t==model||t.name=="Armature"||t.name.StartsWith("mixamorig:")).ToArray();
            Transform Bone(string name)=>bones.FirstOrDefault(t=>t.name=="mixamorig:"+name) ?? throw new Exception("Missing "+name+"; bones="+string.Join(",",bones.Select(t=>t.name)));
            var rig=Ensure<EntrySkillPresentation>(root);var motion=Ensure<FirstPersonSkillMotion>(root);Ref(rig,"motion",motion);Ref(motion,"_model",model);Refs(motion,"_bones",bones);
            void Pose(EntrySkillPresentation.Cast cast,Vector3 left,Vector3 right){
                model.localPosition+=model.parent.InverseTransformVector(camera.TransformVector(new Vector3(-.24f,-.08f,.08f)));
                foreach(string side in new[]{"Left","Right"}){var a=Bone(side+"Arm");var f=Bone(side+"ForeArm");var h=Bone(side+"Hand");
                    if(!EntrySkillPresentation.UsesHand(cast,side=="Left")){
                        a.rotation=Quaternion.FromToRotation(f.position-a.position,camera.TransformDirection(new Vector3(side=="Left"?-.15f:.15f,-1,-.25f)))*a.rotation;
                        f.rotation=Quaternion.FromToRotation(h.position-f.position,camera.TransformDirection(new Vector3(0,-1,-.3f)))*f.rotation;continue;
                    }
                    a.rotation=Quaternion.FromToRotation(f.position-a.position,camera.TransformDirection(new Vector3(side=="Left"?-1:1,-.45f,.15f)))*a.rotation;
                    var target=side=="Left"?left:right;target.y+=.115f;
                    SkillArmPose.Reach(a,f,h,camera,target,1);
                    // This project's FPS rig intentionally has one articulated finger chain.
                    var index=Bone(side+"HandIndex1");
                    var aim=camera.TransformDirection(new Vector3(side=="Left"?-.12f:.12f,.12f,1)).normalized;
                    h.rotation=Quaternion.FromToRotation(index.position-h.position,aim)*h.rotation;
                    var normal=Vector3.ProjectOnPlane(h.up,aim).normalized;
                    if(Vector3.Dot(normal,camera.up)<0)normal=-normal;
                    h.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(normal,Vector3.ProjectOnPlane(camera.up,aim),aim),aim)*h.rotation;
                }
            }
            Ref(motion,"_drone",PoseClip("Entry_C_Pounce",model,bones,s=>Pose(EntrySkillPresentation.Cast.Dash,new Vector3(-.22f,-.3f,.4f),new Vector3(s==1?.35f:.22f,s==1?-.4f:-.3f,s==1?.2f:.45f)),3,.28f));
            Ref(motion,"_emp",PoseClip("Entry_Q_Throw",model,bones,s=>Pose(EntrySkillPresentation.Cast.Smoke,Vector3.zero,s==1?new Vector3(.12f,-.09f,.75f):new Vector3(.28f,-.19f,.44f)),3,.28f));
            Ref(motion,"_scanner",PoseClip("Entry_E_Phase",model,bones,s=>Pose(EntrySkillPresentation.Cast.Stealth,new Vector3(s==1?.06f:-.3f,s==1?-.15f:-.22f,.48f),Vector3.zero),3,.275f));
            Ref(motion,"_ultimate",PoseClip("Entry_X_DualClaws",model,bones,s=>{
                bool l=s==5||s==11,r=s==8||s==11;
                Pose(EntrySkillPresentation.Cast.Blades,new Vector3(l?-.10f:-.27f,s==0?-.68f:l?-.16f:-.36f,l?.78f:.54f),new Vector3(r?.09f:.27f,s==0?-.65f:r?-.10f:-.25f,r?.80f:.58f));
                foreach(string side in new[]{"Left","Right"}){
                    var h=Bone(side+"Hand");var finger=Bone(side+"HandIndex1");bool firing=side=="Left"?l:r;
                    var direction=camera.TransformDirection(firing?new Vector3(0,-.06f,1):side=="Left"?new Vector3(-.18f,-.20f,1):new Vector3(.12f,-.10f,1));
                    h.rotation=Quaternion.FromToRotation(finger.position-h.position,direction)*h.rotation;
                    h.rotation=Quaternion.AngleAxis(firing?0:side=="Left"?-18:12,direction)*h.rotation;
                }
            },14,.1f));
            var local=new Transform[6];var world=new Transform[6];var mounts=new Transform[4];motion.Apply(FirstPersonSkillMotion.Motion.Ultimate,.4f,1);
            for(int side=0;side<2;side++){
                string handName=side==0?"LeftHand":"RightHand";
                mounts[side]=HandFrame(Bone(handName),camera,"EntryClawMount");Gauntlet(mounts[side]);
                var worldHand=root.transform.Find("Model").GetComponentsInChildren<Transform>(true).First(t=>t.name.EndsWith(handName));
                mounts[side+2]=HandFrame(worldHand,root.transform,"EntryClawMount");Gauntlet(mounts[side+2]);
            }
            for(int i=0;i<6;i++){
                bool left=i%2==0;int lane=i/2-1;var hand=Bone(left?"LeftHand":"RightHand");
                var mount=mounts[i%2];var claw=Claw(hand,"EntryClaw_"+(i+1));claw.position=mount.position+mount.right*(lane*.028f)+mount.up*.012f+mount.forward*.05f;
                claw.rotation=mount.rotation*Quaternion.Euler(0,lane*4,0);var scale=hand.lossyScale;claw.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));local[i]=claw;
                var worldHand=root.transform.Find("Model").GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.EndsWith(left?"LeftHand":"RightHand"));
                if(!worldHand)throw new Exception("Entry third-person hand missing");
                var wm=mounts[i%2+2];var third=Claw(worldHand,"EntryClaw_"+(i+1));third.position=wm.position+wm.right*(lane*.028f)+wm.up*.012f+wm.forward*.05f;third.rotation=wm.rotation*Quaternion.Euler(0,lane*4,0);
                scale=worldHand.lossyScale;third.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));world[i]=third;
                claw.gameObject.SetActive(false);third.gameObject.SetActive(false);
            }
            motion.Restore(FirstPersonSkillMotion.Motion.Ultimate);Refs(rig,"localClaws",local);Refs(rig,"worldClaws",world);
            foreach(var mount in mounts)mount.gameObject.SetActive(false);Refs(rig,"clawMounts",mounts);
            var worldBones=root.transform.Find("Model").GetComponentsInChildren<Transform>(true);
            Refs(rig,"worldArmBones",new[]{"LeftArm","LeftForeArm","LeftHand","RightArm","RightForeArm","RightHand"}.Select(name=>worldBones.First(t=>t.name.EndsWith(name))).ToArray());
            motion.Apply(FirstPersonSkillMotion.Motion.Emp,0,1);
            var held=Node(Bone("RightHand"),"EntryHeldSmoke");held.position=Bone("RightHand").position+camera.forward*.055f+camera.up*.035f;held.rotation=camera.rotation;var hs=held.parent.lossyScale;held.localScale=new Vector3(1/Mathf.Abs(hs.x),1/Mathf.Abs(hs.y),1/Mathf.Abs(hs.z));
            foreach(string old in new[]{"WhiteCapsule","CyanSeal"}){var child=held.Find(old);if(child)Object.DestroyImmediate(child.gameObject);}SmokeDevice(held);
            held.gameObject.SetActive(false);motion.Restore(FirstPersonSkillMotion.Motion.Emp);Ref(rig,"heldSmoke",held.gameObject);
            Ref(rig,"dashWake",Particles(root.transform,"EntryDashWake",true));Ref(rig,"phaseFlash",Particles(camera,"EntryPhaseFlash",false));Ref(rig,"audioSource",Audio(root.transform));
            Ref(rig,"dashSound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Controller/Decoy/DecoyThrow.wav"));Ref(rig,"smokeSound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Controller/Swamp/SwampThrow.wav"));
            Ref(rig,"phaseSound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/EMP/MagneticPulse.wav"));Ref(rig,"bladeSound",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Controller/Decoy/DecoyLatch.wav"));
            Ref(root.GetComponent<EntrySkill_Q>(),"_playerCamera",camera.GetComponent<Camera>());Ref(root.GetComponent<EntrySkill_E>(),"_localFirstPersonRoot",model.parent.gameObject);
            var stealth=new SerializedObject(root.GetComponent<EntrySkill_E>());stealth.FindProperty("_debugStealthTeam").boolValue=false;stealth.ApplyModifiedPropertiesWithoutUndo();Hud(root.transform,rig);
            var projectile=new GameObject("EntryCyanClawProjectile");Claw(projectile.transform,"Claw");projectile.AddComponent<Entry_Toenail>();
            var trailMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/ClawTrail.mat");if(!trailMaterial){trailMaterial=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(trailMaterial,Art+"/ClawTrail.mat");}
            var trail=projectile.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMaterial;trail.time=.035f;trail.startWidth=.012f;trail.endWidth=0;trail.minVertexDistance=.02f;trail.startColor=new Color(.8f,.94f,1,.5f);trail.endColor=new Color(.8f,.94f,1,0);trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var asset=PrefabUtility.SaveAsPrefabAsset(projectile,Art+"/ClawProjectile.prefab");Object.DestroyImmediate(projectile);Ref(root.GetComponent<EntrySkill_X>(),"_toenailPrefab",asset);Ref(root.GetComponent<EntrySkill_X>(),"_bladeLineMaterial",cyan);
            var impactRoot=new GameObject("ClawImpact");var impact=Particles(impactRoot.transform,"SilverSparks",false);impact.transform.localPosition=Vector3.zero;var main=impact.main;main.playOnAwake=true;main.startSize=.018f;main.startLifetime=.18f;main.startSpeed=2.5f;
            main.startColor=new Color(.85f,.92f,1,.7f);var impactEmission=impact.emission;impactEmission.SetBursts(new[]{new ParticleSystem.Burst(0,8)});var impactRenderer=impact.GetComponent<ParticleSystemRenderer>();impactRenderer.sharedMaterial=trailMaterial;impactRenderer.renderMode=ParticleSystemRenderMode.Stretch;impactRenderer.lengthScale=2;impactRenderer.velocityScale=.04f;
            var impactAsset=PrefabUtility.SaveAsPrefabAsset(impactRoot,Art+"/ClawImpact.prefab");Object.DestroyImmediate(impactRoot);Ref(root.GetComponent<EntrySkill_X>(),"_impactPrefab",impactAsset);
            var xVisual=new SerializedObject(root.GetComponent<EntrySkill_X>());xVisual.FindProperty("_lineStartWidth").floatValue=.018f;xVisual.FindProperty("_lineEndWidth").floatValue=.002f;xVisual.FindProperty("_lineDuration").floatValue=.06f;xVisual.FindProperty("_toenailLifeTime").floatValue=.09f;xVisual.ApplyModifiedPropertiesWithoutUndo();
            var originalPositions=bones.Select(t=>t.localPosition).ToArray();var originalRotations=bones.Select(t=>t.localRotation).ToArray();var cp=camera.position;
            foreach(FirstPersonSkillMotion.Motion kind in Enum.GetValues(typeof(FirstPersonSkillMotion.Motion))){motion.Apply(kind,.28f,1);motion.Restore(kind);
                for(int i=0;i<bones.Length;i++)if(Vector3.Distance(originalPositions[i],bones[i].localPosition)>.00001f||Quaternion.Angle(originalRotations[i],bones[i].localRotation)>.02f)throw new Exception("Entry pose restoration failed");if(camera.position!=cp)throw new Exception("Entry pose moved gameplay camera");}
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);Debug.Log("ENTRY_PREFAB_PASS: four clips, both hands, 6 local/world claws, saved HUD, camera/bones restored");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Smoke()
    {
        const string path="Assets/KSW/Prefabs/Effect/Smoke Effect.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Controller/ControllerSmoke.prefab");var so=new SerializedObject(source.GetComponent<ControllerSmokeVfx>());var template=so.FindProperty("_volume").objectReferenceValue as Renderer;
            if(!template)throw new Exception("Controller smoke volume missing");
            string matPath=Art+"/EntrySmoke.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!m){m=new Material(template.sharedMaterial);AssetDatabase.CreateAsset(m,matPath);}
            m.SetColor("_Deep",new Color(.13f,.14f,.15f));m.SetColor("_Light",new Color(.54f,.55f,.56f));m.SetColor("_Accent",new Color(.28f,.48f,.50f));m.SetColor("_CloudShadow",new Color(.30f,.31f,.32f));m.SetColor("_CloudBody",new Color(.70f,.71f,.72f));m.SetColor("_CloudHighlight",new Color(.92f,.93f,.94f));m.SetFloat("_PatternStrength",.88f);m.SetFloat("_InteriorMist",.045f);m.SetFloat("_EdgeMist",.2f);EditorUtility.SetDirty(m);
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var e=ps.emission;e.enabled=false;var r=ps.GetComponent<Renderer>();if(r)r.enabled=false;}
            var volume=Shape(root.transform,"EntryRollingSmoke",PrimitiveType.Sphere,Vector3.zero,Vector3.one*2,m);volume.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;volume.receiveShadows=false;
            var fx=Ensure<ControllerSmokeVfx>(root);Ref(fx,"_volume",volume);var sound=Audio(root.transform);sound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Controller/SmokeBloom.wav");sound.volume=.5f;Ref(fx,"_audio",sound);
            Ensure<SmokeOutlineOccluder>(root);PrefabUtility.SaveAsPrefabAsset(root,path);Debug.Log("ENTRY_Q_SMOKE_PASS: Controller volume reused; Entry gameplay components retained");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        const string grenadePath="Assets/Prefab/Character/Entry/Smoke Grenade.prefab";var grenade=PrefabUtility.LoadPrefabContents(grenadePath);
        try{var device=Node(grenade.transform,"EntrySmokeDevice");foreach(var r in grenade.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(device))r.enabled=false;
            var scale=grenade.transform.lossyScale;device.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));SmokeDevice(device);PrefabUtility.SaveAsPrefabAsset(grenade,grenadePath);}
        finally{PrefabUtility.UnloadPrefabContents(grenade);}
    }
    public static void AuthorAndVerify()
    {
        if(!Application.isBatchMode)throw new Exception("Run only in an isolated batch editor");
        if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/Art","Entry");white=Mat("PearlWhite",new Color(.86f,.91f,.93f));cyan=Mat("CyanEdge",new Color(.08f,.75f,.88f),true);graphite=Mat("Graphite",new Color(.045f,.06f,.07f));
        CreateMesh();Player();Smoke();AssetDatabase.SaveAssets();VerifySavedHandUsage();Debug.Log("ENTRY_AUTHORING_COMPLETE");
    }
    public static void VerifySavedHandUsage()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try{
            var motion=root.GetComponent<FirstPersonSkillMotion>();
            var camera=root.GetComponent<PlayerCameraController>().PlayerCamera;
            var model=(Transform)new SerializedObject(motion).FindProperty("_model").objectReferenceValue;
            var bones=model.GetComponentsInChildren<Transform>(true);
            var left=bones.First(t=>t.name=="mixamorig:LeftHand");
            var right=bones.First(t=>t.name=="mixamorig:RightHand");
            var positions=bones.Select(t=>t.localPosition).ToArray();
            var rotations=bones.Select(t=>t.localRotation).ToArray();
            var cp=camera.transform.localPosition;var cr=camera.transform.localRotation;
            var casts=new[]{EntrySkillPresentation.Cast.Dash,EntrySkillPresentation.Cast.Smoke,EntrySkillPresentation.Cast.Stealth,EntrySkillPresentation.Cast.Blades};
            for(int i=0;i<casts.Length;i++){
                var kind=(FirstPersonSkillMotion.Motion)i;
                for(int frame=0;frame<=33;frame++){
                    motion.Apply(kind,frame/60f,1);
                    if(i<3){
                        var idle=EntrySkillPresentation.UsesHand(casts[i],true)?right:left;
                        var active=idle==right?left:right;
                        var idlePos=camera.transform.InverseTransformPoint(idle.position);
                        var activePos=camera.transform.InverseTransformPoint(active.position);
                        if(idlePos.y>=activePos.y-.12f)throw new Exception($"Unused hand is raised: {casts[i]} frame={frame} idle={idlePos} active={activePos}");
                    }
                    motion.Restore(kind);
                    for(int b=0;b<bones.Length;b++)if(Vector3.Distance(positions[b],bones[b].localPosition)>.00001f||Quaternion.Angle(rotations[b],bones[b].localRotation)>.02f)throw new Exception("Hand test changed base rig");
                    if(camera.transform.localPosition!=cp||Quaternion.Angle(cr,camera.transform.localRotation)>.001f)throw new Exception("Hand test moved aim camera");
                }
            }
            if(EntrySkillPresentation.UsesHand(EntrySkillPresentation.Cast.Smoke,true)||!EntrySkillPresentation.UsesHand(EntrySkillPresentation.Cast.Smoke,false)||!EntrySkillPresentation.UsesHand(EntrySkillPresentation.Cast.Stealth,true)||EntrySkillPresentation.UsesHand(EntrySkillPresentation.Cast.Stealth,false))throw new Exception("One-hand policy mismatch");
            Debug.Log("ENTRY_HAND_USAGE_PASS: C/Q right only, E left only, X both; 136 saved-clip samples; base rig and aim camera restored");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void InspectWorldRig()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try{
            var world=root.transform.Find("Model");
            foreach(var a in world.GetComponentsInChildren<Animator>(true))Debug.Log("ENTRY_WORLD_ANIM "+AnimationUtility.CalculateTransformPath(a.transform,root.transform)+" active="+a.gameObject.activeInHierarchy+" rot="+a.transform.rotation.eulerAngles);
            foreach(var r in world.GetComponentsInChildren<SkinnedMeshRenderer>(true))Debug.Log("ENTRY_WORLD_SKIN "+AnimationUtility.CalculateTransformPath(r.transform,root.transform)+" active="+r.gameObject.activeInHierarchy+" root="+r.rootBone+" bones="+string.Join(";",r.bones.Where(b=>b&&b.name.EndsWith("Hand")).Select(b=>AnimationUtility.CalculateTransformPath(b,root.transform))));
            var rig=new SerializedObject(root.GetComponent<EntrySkillPresentation>());var p=rig.FindProperty("worldArmBones");
            for(int i=0;i<p.arraySize;i++){var t=(Transform)p.GetArrayElementAtIndex(i).objectReferenceValue;Debug.Log("ENTRY_WORLD_REF "+i+" "+AnimationUtility.CalculateTransformPath(t,root.transform)+" active="+t.gameObject.activeInHierarchy+" position="+root.transform.InverseTransformPoint(t.position));}
            var other=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Initiator/Initiator.prefab");
            if(other)foreach(var m in other.transform.Find("Model").GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct())Debug.Log("ENTRY_DEPTH_MATERIAL "+m.name+" shader="+m.shader.name+" queue="+m.renderQueue+" path="+AssetDatabase.GetAssetPath(m));
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Preview()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only");
        var folder="C:/Users/USER/AppData/Local/Temp/EntryPreview";System.IO.Directory.CreateDirectory(folder);
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maps/TestMap.prefab"));map.GetComponent<MapInfo>().SetBarriersActive(false);
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));actor.transform.position=new Vector3(-33,1,5);
        var camera=actor.GetComponent<PlayerCameraController>().PlayerCamera;camera.enabled=true;camera.gameObject.SetActive(true);camera.fieldOfView=70;camera.nearClipPlane=.03f;
        foreach(var r in actor.transform.Find("Model").GetComponentsInChildren<Renderer>(true))r.forceRenderingOff=true;
        foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
        var light=new GameObject("PreviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=new Color(.7f,.7f,.7f);
        var motion=actor.GetComponent<FirstPersonSkillMotion>();var model=(new SerializedObject(motion).FindProperty("_model").objectReferenceValue as Transform);model.gameObject.SetActive(true);
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        foreach(FirstPersonSkillMotion.Motion kind in Enum.GetValues(typeof(FirstPersonSkillMotion.Motion))){
            motion.Apply(kind,kind==FirstPersonSkillMotion.Motion.Ultimate?.32f:0,1);
            foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("EntryClaw_"))t.gameObject.SetActive(kind==FirstPersonSkillMotion.Motion.Ultimate);
            foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t.name=="EntryClawMount")t.gameObject.SetActive(kind==FirstPersonSkillMotion.Motion.Ultimate);
            var rig=new SerializedObject(actor.GetComponent<EntrySkillPresentation>());var held=rig.FindProperty("heldSmoke").objectReferenceValue as GameObject;held.SetActive(kind==FirstPersonSkillMotion.Motion.Emp);
            // Several edit-mode renders in one frame otherwise reuse the first GPU skinning pose.
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.enabled).ToArray();
            var baked=new System.Collections.Generic.List<GameObject>();var meshes=new System.Collections.Generic.List<Mesh>();
            foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);var g=new GameObject("PreviewBakedPose",typeof(MeshFilter),typeof(MeshRenderer));g.layer=skin.gameObject.layer;g.transform.SetParent(skin.transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;baked.Add(g);}
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();System.IO.File.WriteAllBytes(folder+"/"+kind+".png",pixels.EncodeToPNG());
            foreach(var g in baked)Object.DestroyImmediate(g);foreach(var mesh in meshes)Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;
            foreach(string side in new[]{"LeftHand","RightHand"}){var hand=model.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:"+side);Debug.Log("ENTRY_PREVIEW "+kind+" "+side+"="+camera.transform.InverseTransformPoint(hand.position));}
            motion.Restore(kind);
        }
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);Debug.Log("ENTRY_PREVIEW_COMPLETE");
    }
}
#endif
