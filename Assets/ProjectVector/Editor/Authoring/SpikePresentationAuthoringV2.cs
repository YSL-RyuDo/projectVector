#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class SpikePresentationAuthoring
{
    static Quaternion PalmFrame(Transform hand,Vector3 neutralUp)
    {
        var children=hand.GetComponentsInChildren<Transform>();
        var finger=children.FirstOrDefault(b=>b.name.EndsWith("HandMiddle1"))
            ??children.FirstOrDefault(b=>b.name=="Fingers1L"||b.name=="Fingers1R")
            ??children.First(b=>b.name.EndsWith("HandIndex1"));
        var forward=(finger.position-hand.position).normalized;
        var thumb=children.FirstOrDefault(b=>b.name.EndsWith("Thumb1")||b.name=="Thumb1R");
        bool left=hand.name.Contains("Left")||hand.name=="HandL";
        // The Controller rig has a single finger chain. Its authored neutral
        // palm faces down; hand.up is an import axis, not an anatomical normal.
        var normal=thumb?(left?Vector3.Cross(thumb.position-hand.position,forward):Vector3.Cross(forward,thumb.position-hand.position)).normalized:-neutralUp;
        return Quaternion.LookRotation(hand.InverseTransformDirection(forward),hand.InverseTransformDirection(Vector3.ProjectOnPlane(normal,forward).normalized));
    }
    public static void InspectGrip()
    {
        foreach(string c in Characters)
        {
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/"+c+"/"+c+".prefab"));
            foreach(string path in new[]{"Model","CameraRoot/Controller Model_Riging (1)"})
            {
                var model=root.transform.Find(path);
                var bones=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.bones).Distinct().ToArray();
                Debug.Log("HAND_AUDIT "+c+" "+path+" "+string.Join(",",bones.Where(b=>b&&(b.name.Contains("Hand")||b.name.Contains("Finger")||b.name.Contains("Thumb"))).Select(b=>b.name)));
                foreach(var b in bones.Where(b=>b&&(b.name.EndsWith("LeftHand")||b.name.EndsWith("HandL"))))
                    Debug.Log("HAND_POINTS "+c+" "+path+" wrist="+b.position.ToString("F3")+" children="+string.Join(";",b.GetComponentsInChildren<Transform>().Where(t=>t!=b).Select(t=>t.name+"="+(t.position-b.position).ToString("F3"))));
            }
            Object.DestroyImmediate(root);
        }
    }
    static void BuildAudioV2()
    {
        // Reuse the pack's recorded mechanical detail, under the authored connection cue.
        var sample=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/KSW/Scifi Guns SFX Pack/Gun3_load.wav");
        if(!sample)throw new Exception("Missing equipment audio");
        sample.LoadAudioData();var source=new float[sample.samples*sample.channels];sample.GetData(source,0);
        const int rate=44100;
        void Write(string name,float duration,Func<float,float> tone)
        {
            int count=(int)(duration*rate);
            using(var w=new System.IO.BinaryWriter(System.IO.File.Create(Folder+"/"+name+".wav")))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
                for(int i=0;i<count;i++)w.Write((short)(Mathf.Clamp(tone(i/(float)rate),-.95f,.95f)*32767));
            }
            AssetDatabase.ImportAsset(Folder+"/"+name+".wav");
        }
        float Mechanical(float t)
        {
            int i=Mathf.Clamp((int)(t*sample.frequency),0,sample.samples-1)*sample.channels;
            float sum=0;for(int c=0;c<sample.channels;c++)sum+=source[i+c];return sum/sample.channels;
        }
        Write("Equip",.55f,t=>Mechanical(t+.08f)*.65f*Mathf.Clamp01((.55f-t)*8));
        Write("Contact",.09f,t=>(Mathf.Sin(t*2*Mathf.PI*1450)*.22f+Mechanical(t+.21f)*.35f)*Mathf.Exp(-t*45)*Mathf.Min(1,t*500));
        Write("Detonate",3.4f,t=>{
            float envelope=Mathf.Exp(-t*1.65f)*Mathf.Min(1,t*80);
            float phase=2*Mathf.PI*(55*t+60*(1-Mathf.Exp(-t*5)));
            float low=Mathf.Sin(phase)*.47f;
            float crackle=Mechanical(Mathf.Repeat(t*.75f,.8f))*.3f*Mathf.Exp(-t*2);
            float sweep=Mathf.Sin(2*Mathf.PI*(180*t+170*(1-Mathf.Exp(-t*2))))*.17f;
            return (low+sweep+crackle)*envelope;
        });
        equip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Equip.wav");burst=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Detonate.wav");
    }
    static void VerifyRestore()
    {
        foreach(string character in Characters)
        {
            var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/"+character+"/"+character+".prefab"));
            try
            {
                foreach(var a in g.GetComponentsInChildren<Animator>(true))a.enabled=false;
                var p=g.GetComponent<SpikeInteractionPresentation>();
                foreach(var rig in new[]{p.firstPerson,p.thirdPerson})
                {
                    var pos=rig.bones.Select(b=>b.localPosition).ToArray();var rot=rig.bones.Select(b=>b.localRotation).ToArray();
                    var feet=rig==p.thirdPerson?rig.bones.Where(b=>b.name.EndsWith("Foot")||b.name=="FootL"||b.name=="FootR").ToArray():Array.Empty<Transform>();
                    var supportFoot=feet.FirstOrDefault(b=>b.name.Contains("Left")||b.name=="FootL");
                    float ground=supportFoot?supportFoot.position.y:0;
                    var hands=new[]{"Left","Right"}.Select(side=>rig.bones.First(b=>b.name.EndsWith(side+"Hand")||b.name==(side=="Left"?"HandL":"HandR"))).ToArray();
                    var palmFrames=hands.Select(h=>PalmFrame(h,g.transform.up)).ToArray();
                    foreach(byte action in new byte[]{1,2})foreach(float time in new[]{.05f,.1f,.25f,.5f,1.2f,2.77f,2.8f,3f})
                    {
                        SpikeInteractionPresentation.Sample(rig,action,time);
                        if(supportFoot&&Mathf.Abs(supportFoot.position.y-ground)>.05f)throw new Exception(character+" support foot lost floor contact during interaction at "+time);
                        if(feet.Length>0&&time>=1.5f)
                        {
                            var rear=feet.First(b=>b!=supportFoot);
                            if(rear.position.y>ground+.16f||rear.parent.position.y>ground+.20f||rear.parent.position.y<g.transform.position.y-.035f)throw new Exception(character+" rear leg is not grounded");
                        }
                        if(action==1&&(time==1.2f||time==2.77f))
                        {
                            for(int side=0;side<2;side++)
                            {
                                float sign=side==0?-1:1;var hand=hands[side];
                                var expected=time==1.2f?Vector3.left*sign:Vector3.down;
                                var palm=hand.rotation*palmFrames[side]*Vector3.up;
                                if(Vector3.Dot(palm,rig.device.transform.rotation*expected)<.94f)throw new Exception(character+" palm faces away from contact surface");
                                if(time==2.77f)
                                {
                                    var grip=rig.device.transform.TransformPoint(new Vector3(sign*.07f,.335f,-.07f));
                                    if(Vector3.Distance(hand.position,grip)>.045f)throw new Exception(character+" "+(rig==p.firstPerson?"FP":"TP")+" "+side+" hand missed device: "+Vector3.Distance(hand.position,grip));
                                }
                            }
                        }
                        SpikeInteractionPresentation.Sample(rig,0,0);
                        if(rig.device.activeSelf||rig.tool.activeSelf)throw new Exception("Device visible after cancel");
                        for(int i=0;i<rig.bones.Length;i++)if(Vector3.Distance(pos[i],rig.bones[i].localPosition)>.0001f||Quaternion.Angle(rot[i],rig.bones[i].localRotation)>.05f)throw new Exception(character+" did not restore bone "+rig.bones[i].name);
                    }
                    foreach(byte action in new byte[]{1,2})
                    {
                        SpikeInteractionPresentation.Sample(rig,action,action==1?3:5);
                        foreach(float elapsed in new[]{0f,.15f,.3f,SpikeInteractionPresentation.StandRecoveryDuration})
                        {
                            SpikeInteractionPresentation.SampleRecovery(rig,elapsed);
                            if(rig.device.activeSelf||rig.tool.activeSelf)throw new Exception("Equipment visible while standing up");
                            for(int i=0;i<rig.bones.Length;i++)
                                if((elapsed>=SpikeInteractionPresentation.StandRecoveryDuration||!rig.recoveryBones[i])&&(Vector3.Distance(pos[i],rig.bones[i].localPosition)>.0001f||Quaternion.Angle(rot[i],rig.bones[i].localRotation)>.05f))throw new Exception(character+" recovery overrides ready pose: "+rig.bones[i].name);
                            if(supportFoot&&supportFoot.position.y<ground-.06f)throw new Exception(character+" support foot penetrates floor while standing up");
                        }
                        // A new interaction and a hard reset must interrupt recovery.
                        SpikeInteractionPresentation.Sample(rig,action,1);
                        SpikeInteractionPresentation.Sample(rig,0,0);
                        for(int i=0;i<rig.bones.Length;i++)if(Vector3.Distance(pos[i],rig.bones[i].localPosition)>.0001f)throw new Exception("Recovery interruption left a stale pose");
                    }
                }
            }
            finally{Object.DestroyImmediate(g);}
        }
        Debug.Log("DEVICE_RESTORE_PASS: 4 characters, 2 views, floor contact and final hand contact verified, plant/interrupt poses restored");
        Debug.Log("DEVICE_RECOVERY_PASS: both actions, four characters, equipment hidden, lower-body return, aiming pose and interrupt restored");
        VerifyDeviceStates();
    }
    static void VerifyDeviceStates()
    {
        var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        try
        {
            var v=g.GetComponent<SpikeDeviceVisual>();var block=new MaterialPropertyBlock();
            Color Emission(Renderer r){r.GetPropertyBlock(block);return block.GetColor("_EmissionColor");}
            v.RenderDormant(0);var idle=Emission(v.statusLamp);
            v.RenderDormant(.98f);
            if((Emission(v.statusLamp)-idle).maxColorComponent<.05f)throw new Exception("Dormant status light is static");
            if(Vector3.Distance(v.core.localPosition,v.coreClosedPosition)>.001f)throw new Exception("Dormant core must be closed");
            v.Pose(1,SpikePresentation.Cyan,1);v.Signal(.1f);var active=Emission(v.flowSegments[0]);
            v.Signal(.8f);
            if(active==Emission(v.flowSegments[0]))throw new Exception("Optical flow did not advance");
            if(Vector3.Distance(v.core.localPosition,v.coreOpenPosition)>.001f||v.dataRotor)throw new Exception("Active core/legacy orbit state invalid");
            v.RenderSafe(1.5f);var positions=g.GetComponentsInChildren<Transform>().Select(t=>t.localPosition).ToArray();
            var rotations=g.GetComponentsInChildren<Transform>().Select(t=>t.localRotation).ToArray();
            v.RenderSafe(3);
            var transforms=g.GetComponentsInChildren<Transform>();
            if(Vector3.Distance(v.core.localPosition,v.coreClosedPosition)>.001f)throw new Exception("Safe core did not close");
            for(int i=0;i<transforms.Length;i++)if(Vector3.Distance(positions[i],transforms[i].localPosition)>.0001f||Quaternion.Angle(rotations[i],transforms[i].localRotation)>.01f)throw new Exception("Safe device keeps moving");
            if(v.flowSegments.Any(r=>Emission(r).maxColorComponent>.001f))throw new Exception("Safe optical tracks still active");
        }
        finally{Object.DestroyImmediate(g);}
        Debug.Log("DEVICE_STATE_PASS: idle breathing, deploy, optical flow, safe closure and rest");
    }
    static GameObject Machine(string asset, Transform parent, Vector3 position, Quaternion rotation, float scale)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/"+asset+".prefab");
        var g=Object.Instantiate(source,parent);g.name=asset+" module";
        g.transform.localPosition=position;g.transform.localRotation=rotation;g.transform.localScale=Vector3.one*scale;
        foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        foreach(var b in g.GetComponentsInChildren<MonoBehaviour>())Object.DestroyImmediate(b);
        foreach(var r in g.GetComponentsInChildren<Renderer>())
            r.sharedMaterials=r.sharedMaterials.Select((m,i)=>Save(SpikeAssetAudit.Compatible(m),"Network-"+asset+"-"+i+".mat")).ToArray();
        return g;
    }
    static void CombineFixed(Transform parent,string prefix)
    {
        var fixedParts=parent.GetComponentsInChildren<MeshRenderer>().Where(r=>r.transform.parent==parent&&r.sharedMaterial!=light).ToArray();
        foreach(var group in fixedParts.GroupBy(r=>r.sharedMaterial))
        {
            if(group.Count()<2)continue;
            var mesh=new Mesh{name=prefix+" "+group.Key.name};
            mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);
            mesh=Save(mesh,prefix+"-"+group.Key.name+".asset");
            var node=Part(prefix+" "+group.Key.name,parent,Vector3.zero,Vector3.one,group.Key,mesh);
            foreach(var r in group)Object.DestroyImmediate(r.gameObject);
        }
    }
    static void BuildNetworkDevice()
    {
        var root=new GameObject("AI access device");var t=root.transform;
        var v=root.AddComponent<SpikeDeviceVisual>();
        // Authored modular machinery supplies the open frame, machined ring and rotor cage.
        // These are visual-only scaled instances; none of the source assets are modified.
        Machine("shield_generator",t,Vector3.zero,Quaternion.identity,.048f);
        Part("Isolation skid",t,new(0,.012f,0),new(.27f,.024f,.24f),dark);
        var bridge=Part("Sloped network bridge",t,new(0,.052f,-.104f),new(.225f,.055f,.055f),steel);bridge.transform.localRotation=Quaternion.Euler(18,0,0);
        Part("Black ceramic face",t,new(0,.067f,-.136f),new(.19f,.053f,.009f),dark);
        Part("Connection interface",t,new(0,.071f,-.143f),new(.15f,.035f,.004f),glass);
        v.chargeSegments=new Renderer[7];
        for(int i=0;i<7;i++)
            v.chargeSegments[i]=Part("Link activity "+i,t,new(-.052f+i*.0175f,.071f,-.146f),new(.009f,.005f,.002f),light).GetComponent<Renderer>();
        v.statusLamp=Part("Connection status",t,new(.09f,.09f,-.144f),new(.012f,.003f,.012f),light,cylinder).GetComponent<Renderer>();v.statusLamp.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int s=-1;s<=1;s+=2)
        {
            Part("Shield grip",t,new(s*.118f,.11f,-.015f),new(.024f,.11f,.075f),dark);
            Part("Hand guard",t,new(s*.136f,.11f,-.015f),new(.018f,.13f,.08f),shell);
            Part("Side link rail",t,new(s*.147f,.11f,-.017f),new(.004f,.078f,.025f),light);
            for(int i=0;i<3;i++)Part("Contact tooth",t,new(s*.065f,.025f,-.15f+i*.012f),new(.015f,.008f,.006f),steel);
            Part("Latch",t,new(s*.088f,.12f,-.11f),new(.026f,.025f,.025f),steel);
        }
        v.core=Node("Retractable compute spindle",t,new(0,.16f,0));
        v.coreClosedPosition=new(0,.16f,0);v.coreOpenPosition=new(0,.235f,0);
        Machine("rotor",v.core,Vector3.zero,Quaternion.Euler(0,0,90),.066f);
        Part("Optical core",v.core,Vector3.zero,new(.068f,.20f,.068f),glass,cylinder);
        Part("Upper shielding",v.core,new(0,.124f,0),new(.135f,.025f,.135f),steel,cylinder);
        Part("Upper ceramic cap",v.core,new(0,.14f,0),new(.125f,.013f,.125f),shell,cylinder);
        Part("Dark contact iris",v.core,new(0,.15f,0),new(.065f,.006f,.065f),dark,cylinder);
        Part("Iris indicator",v.core,new(0,.154f,0),new(.025f,.004f,.025f),light,cylinder);
        v.flowSegments=new Renderer[18];v.dataRotor=null;
        for(int face=0;face<3;face++)
        {
            var channel=Node("Recessed optical channel "+face,v.core,Vector3.zero);channel.localRotation=Quaternion.Euler(0,face*120,0);
            for(int step=0;step<6;step++)
            {
                v.flowSegments[face*6+step]=Part("Signal path "+step,channel,new(0,-.082f+step*.03f,-.036f),new(.028f,.019f,.003f),light).GetComponent<Renderer>();
            }
        }
        // Four graphite grip feet stay on the floor throughout deployment.
        v.stabilizers=new Transform[4];v.petals=new Transform[3];
        for(int i=0;i<3;i++)
        {
            var pivot=Node("Locking guard "+i,t,new(0,.075f,0));pivot.localRotation=Quaternion.Euler(0,i*120,0);v.petals[i]=pivot;
            Part("Ceramic spindle guard",pivot,new(.086f,.09f,0),new(.018f,.15f,.035f),shell);
            Part("Guard hinge",pivot,new(.087f,0,0),new(.031f,.026f,.044f),steel);
        }
        for(int i=0;i<4;i++)
        {
            var foot=Node("Dock foot "+i,t,new(0,.017f,0));foot.localRotation=Quaternion.Euler(0,i*90,0);v.stabilizers[i]=foot;
            Part("Sliding foot",foot,new(.112f,0,0),new(.12f,.018f,.033f),steel);
            Part("Dock pad",foot,new(.168f,-.005f,0),new(.038f,.018f,.052f),dark);
        }
        CombineFixed(t,"DeviceFrame");
        v.lamps=root.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==light).ToArray();
        v.RenderDormant(0);
        PrefabUtility.SaveAsPrefabAsset(root,ModelPath);Object.DestroyImmediate(root);
    }

    static void BuildProbeV2(Transform t)
    {
        Part("Rubberized diagnostic grip",t,new(0,-.034f,0),new(.058f,.064f,.21f),dark);
        Part("Chamfered scanner upper",t,new(0,.004f,.018f),new(.078f,.034f,.24f),steel);
        Part("Ceramic top plate",t,new(0,.025f,.025f),new(.072f,.012f,.20f),shell);
        Part("Recessed diagnostic screen",t,new(0,.033f,.022f),new(.054f,.006f,.107f),glass);
        for(int i=0;i<6;i++)Part("Probe progress cell",t,new(-.017f+i*.007f,.037f,.022f),new(.004f,.002f,.059f),light);
        for(int i=0;i<5;i++)Part("Grip rib",t,new(0,-.044f,-.075f+i*.031f),new(.064f,.026f,.012f),steel);
        Part("Connector block",t,new(0,0,.16f),new(.05f,.036f,.047f),dark);
        for(int s=-1;s<=1;s+=2)
        {
            var pin=Part("Contact probe",t,new(s*.018f,0,.204f),new(.009f,.078f,.009f),steel,cylinder);pin.transform.localRotation=Quaternion.Euler(90,0,0);
            Part("Insulated contact",t,new(s*.018f,0,.243f),new(.012f,.012f,.016f),light);
        }
        Part("Confirmation key",t,new(.022f,.035f,-.073f),new(.012f,.006f,.018f),warning);
        CombineFixed(t,"DisconnectProbe");
    }

    static Material ParticleMaterial(string name, Color color, bool smoke=false)
    {
        var m=new Material(Shader.Find("Vector/SpikeParticle"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoke",smoke?1:0);
        m.SetFloat("_DstBlend",smoke?(float)BlendMode.OneMinusSrcAlpha:(float)BlendMode.One);
        return Save(m,name+".mat");
    }
    static Gradient Gradient(Color a,Color b,Color c)
    {
        var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(a,0),new GradientColorKey(b,.2f),new GradientColorKey(c,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.06f),new GradientAlphaKey(.7f,.55f),new GradientAlphaKey(0,1)});return g;
    }
    static ParticleSystem BurstLayer(Transform root,string name,int count,float lifetime,float speed,float size,Material material,Gradient colors)
    {
        var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.localPosition=new Vector3(0,.25f,0);
        var p=g.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.useAutoRandomSeed=false;p.randomSeed=(uint)(314+count);
        var m=p.main;m.playOnAwake=false;m.loop=false;m.duration=.12f;m.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.65f,lifetime);m.startSpeed=new ParticleSystem.MinMaxCurve(speed*.65f,speed);m.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);m.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);m.simulationSpace=ParticleSystemSimulationSpace.World;
        var e=p.emission;e.rateOverTime=0;e.SetBursts(new[]{new ParticleSystem.Burst(.01f,(short)count)});
        var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.15f;
        var col=p.colorOverLifetime;col.enabled=true;col.color=colors;
        var r=g.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        return p;
    }
    static void BuildEffectV2()
    {
        var root=new GameObject("Spike detonation");var e=root.AddComponent<SpikeDetonation>();
        var heat=ParticleMaterial("Hot plasma",Color.white);
        heat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);EditorUtility.SetDirty(heat);
        var dust=ParticleMaterial("Pressure dust",Color.white,true);
        var flash=Gradient(new Color(.8f,1,1),new Color(.14f,.72f,1f),new Color(.025f,.1f,.32f));
        var blue=Gradient(new Color(.8f,1,1),new Color(.2f,.72f,1),new Color(.04f,.11f,.23f));
        var gray=Gradient(new Color(.36f,.52f,.65f),new Color(.15f,.28f,.42f),new Color(.12f,.19f,.29f));
        var fire=BurstLayer(root.transform,"Core discharge",8,.38f,4.4f,1.0f,heat,flash);
        var grow=fire.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.18f,1,1.7f));
        var sparks=BurstLayer(root.transform,"Optical discharge trails",42,.75f,10,.035f,heat,blue);e.sparks=sparks;
        var sm=sparks.main;sm.gravityModifier=0;var sr=sparks.GetComponent<ParticleSystemRenderer>();sr.renderMode=ParticleSystemRenderMode.Stretch;sr.lengthScale=2.2f;sr.velocityScale=.045f;
        var smoke=BurstLayer(root.transform,"Residual ion haze",8,1.5f,1.3f,.7f,dust,gray);var mm=smoke.main;mm.startDelay=.09f;var sz=smoke.sizeOverLifetime;sz.enabled=true;sz.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.2f,1,1.4f));
        var vel=smoke.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;vel.y=.5f;
        var ground=BurstLayer(root.transform,"Ground ion trail",14,.8f,5.8f,.28f,heat,blue);
        var sh=ground.shape;sh.shapeType=ParticleSystemShapeType.Circle;sh.rotation=new Vector3(90,0,0);sh.radius=.3f;
        var gs=ground.sizeOverLifetime;gs.enabled=true;gs.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.15f,1,1.8f));
        var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var coreMesh=Save(Object.Instantiate(sphere.GetComponent<MeshFilter>().sharedMesh),"ReleaseCore.asset");Object.DestroyImmediate(sphere);
        var corona=Save(new Material(Shader.Find("Vector/SpikeCorona")),"ReleaseCorona.mat");
        e.core=Part("Brief pressure corona",root.transform,new(0,.25f,0),Vector3.one,corona,coreMesh).transform;
        // Thin floor front carries the readable boundary; the volume is broken into lobes.
        e.shockRing=Part("Pressure front",root.transform,new(0,.05f,0),Vector3.one,fx,ring).transform;
        e.innerRing=Part("Secondary pressure front",root.transform,new(0,.08f,0),Vector3.one,fx,ring).transform;
        e.fading=new[]{e.shockRing.GetComponent<Renderer>(),e.innerRing.GetComponent<Renderer>(),e.core.GetComponent<Renderer>()};
        e.layers=new[]{fire,sparks,smoke,ground};
        e.flash=new GameObject("Transient flash").AddComponent<Light>();e.flash.transform.SetParent(root.transform,false);e.flash.transform.localPosition=Vector3.up*.6f;e.flash.type=LightType.Point;e.flash.range=10;e.flash.color=new Color(.65f,.82f,1);e.flash.intensity=0;
        e.source=Audio(root);e.source.maxDistance=75;e.burst=burst;
        PrefabUtility.SaveAsPrefabAsset(root,EffectPath);Object.DestroyImmediate(root);
    }
}
#endif
