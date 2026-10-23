Shader "Vector/ControllerSmoke"
{
    Properties
    {
        _Deep ("Navy core", Color) = (.12,.16,.25,1)
        _Light ("Blue grey cloud", Color) = (.48,.57,.7,1)
        _Accent ("Sky blue edge", Color) = (.24,.58,.76,1)
        _CloudShadow ("Exterior fold shadow", Color) = (.64,.70,.86,1)
        _CloudBody ("Exterior silver blue", Color) = (.86,.90,.98,1)
        _CloudHighlight ("Exterior silver crest", Color) = (.97,.985,1,1)
        _BrushMap ("Painted smoke brush mask", 2D) = "white" {}
        _BrushScale ("Painted billow scale", Range(.5,3)) = 1.15
        _FlowSpeed ("Exterior rolling speed", Range(0,.3)) = .24
        _EdgeMist ("Exterior edge wisps (cosmetic only)", Range(0,.35)) = .30
        _ContactSoftness ("Contact wisps width (mesh space)", Range(.005,.06)) = .035
        _BrushWarp ("Paint flow distortion", Range(0,.3)) = .18
        _BrushSoftness ("Fine brush filtering (mip floor)", Range(0,5)) = 2.6
        _PatternStrength ("Directional smoke band contrast", Range(0,1)) = .72
        _BandDensity ("Long smoke layers", Range(3,12)) = 8
        _FlowAxis ("Tilted flow axis", Vector) = (-.32,.93,.16,0)
        [HideInInspector] _CloudTime ("Instance animation time", Float) = 0
        _InteriorMist ("Interior mist (not concealment)", Range(0,.25)) = .10
        _Phase ("Settle", Range(0,1)) = 1
        _End ("End turbulence", Range(0,1)) = 1
        _Opacity ("Expiry residue", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Cull Front ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Deep, _Light, _Accent;
            half4 _CloudShadow, _CloudBody, _CloudHighlight;
            float _FlowSpeed, _CloudTime, _EdgeMist, _ContactSoftness, _BrushWarp, _BrushScale, _BrushSoftness;
            float _PatternStrength, _BandDensity;
            float4 _FlowAxis;
            float4 _BrushMap_TexelSize;
            float _InteriorMist, _Phase, _End, _Opacity;
            CBUFFER_END
            TEXTURE2D(_BrushMap); SAMPLER(sampler_BrushMap);
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; };
            // Only the drawing envelope grows. The opaque boundary and the
            // gameplay occluder remain exactly radius .5 in this mesh space.
            Varyings vert(Attributes v) { Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz*1.13);o.positionCS=TransformWorldToHClip(o.positionWS);return o; }
            float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            float noise(float3 p) {
                float3 i=floor(p), f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
            }
            float cloudField(float3 p) {
                // Two independently advected scales: no rigidly rotating sphere,
                // texture seams, bright contour lines or high-frequency static.
                float t=_CloudTime*_FlowSpeed;
                float coarse=noise(p*4.8+float3(t*.32,-t*.46,t*.12));
                float medium=noise(p*10.5+float3(-t*.52,-t*.29,t*.35)+coarse*.75);
                return coarse*.7+medium*.30;
            }
            float brushSample(float2 uv) {
                // Minimum texture footprint removes scratchy dry-brush flecks,
                // while screen derivatives still prevent distant shimmer.
                float footprint=exp2(_BrushSoftness)*max(_BrushMap_TexelSize.x,_BrushMap_TexelSize.y);
                float2 dx=ddx(uv),dy=ddy(uv);
                dx=length(dx)<footprint?float2(footprint,0):dx;
                dy=length(dy)<footprint?float2(0,footprint):dy;
                return SAMPLE_TEXTURE2D_GRAD(_BrushMap,sampler_BrushMap,uv,dx,dy).r;
            }
            half3 exteriorCloud(float3 surface,float3 direction) {
                float t=_CloudTime*_FlowSpeed;
                float3 axis=length(_FlowAxis.xyz)>.001?normalize(_FlowAxis.xyz):float3(0,1,0);
                float3 reference=abs(axis.z)<.95?float3(0,0,1):float3(1,0,0);
                float3 side=normalize(cross(axis,reference)),front=cross(side,axis);
                float height=dot(surface,axis);
                float2 radial=float2(dot(surface,side),dot(surface,front));
                float longitude=atan2(radial.y,radial.x);
                float stream=longitude-t*.52;
                // Only periodic functions use longitude: no texture seam at +/- PI.
                // Different phase speeds deform the layers instead of rotating a ball.
                float2 around=float2(cos(stream),sin(stream));
                float taper=noise(float3(around*2.5,height*13+4));
                float curl=sin(stream*2+height*5)*(.1+_BrushWarp)
                    +sin(stream*5-height*8+t*.21)*.075;
                float fray=noise(float3(around*3,height*20-t*.2));
                float phase=height*_BandDensity+curl+sin(height*17+1.3)*.18+(fray-.5)*.09;
                float width=lerp(.065,.24,smoothstep(.28,.72,taper));
                float distance=abs(frac(phase)-.5);
                float band=1-smoothstep(width,width+.105,distance);
                float broken=lerp(.5,1,smoothstep(.28,.62,taper));
                // Fine strands share the long flow but slip at a second speed.
                float finePhase=phase*2+sin(stream*3+t*.3)*.12+t*.075;
                float fine=1-smoothstep(.025,.095,abs(frac(finePhase)-.5));
                float fineMask=noise(float3(cos(stream+.7)*2,sin(stream+.7)*2,height*9+8));
                float grain=brushSample(float2(dot(surface,side)*.38+t*.02,
                    height*3.4+curl*.12)*_BrushScale);
                float flow=band*broken*(.65+grain*.35)+fine*smoothstep(.4,.7,fineMask)*.22;
                // Keep the poles calm instead of pinching angular bands into a star.
                flow*=smoothstep(.015,.10,length(radial));
                half3 col=lerp(_CloudBody.rgb,_CloudHighlight.rgb,.28+cloudField(surface)*.2);
                col=lerp(col,_CloudShadow.rgb,saturate(flow)*_PatternStrength);
                float crest=1-smoothstep(.018,.075,abs(distance-width-.035));
                col=lerp(col,_CloudHighlight.rgb,crest*broken*.15);
                float rim=pow(1-saturate(abs(dot(normalize(surface),direction))),3);
                col=lerp(col,_CloudBody.rgb,rim*.2);
                return col*lerp(.97,1,_End);
            }
            half4 frag(Varyings v):SV_Target {
                float3 originWS=_WorldSpaceCameraPos;
                if(unity_OrthoParams.w>.5) {
                    float3 back=UNITY_MATRIX_V[2].xyz;
                    originWS=v.positionWS+back*dot(originWS-v.positionWS,back);
                }
                float3 origin=TransformWorldToObject(originWS);
                float3 direction=normalize(TransformWorldToObject(v.positionWS)-origin);
                float b=dot(origin,direction), c=dot(origin,origin)-.25, discriminant=b*b-c;
                float root=sqrt(max(0,discriminant));
                float entry=max(0,-b-root), exit=-b+root;
                float2 uv=v.positionCS.xy/_ScaledScreenParams.xy;
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 scene=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float limit=dot(TransformWorldToObject(scene)-origin,direction);
                if(discriminant<0) {
                    // Feather only OUTSIDE the exact opaque gameplay sphere.
                    // A soft, moving fringe cannot open holes in its core.
                    float outer=b*b-dot(origin,origin)+.56*.56;
                    if(outer<=0)discard;
                    float start=max(0,-b-sqrt(outer));
                    if(limit<=start)discard;
                    float3 edge=origin+direction*max(0,-b);
                    float distance=sqrt(max(0,dot(origin,origin)-b*b));
                    float flow=cloudField(edge);
                    float width=lerp(.009,.056,smoothstep(.28,.68,flow));
                    float softness=1-smoothstep(.5,.5+width,distance);
                    float contact=smoothstep(0,max(.005,_ContactSoftness),limit-start);
                    float wisps=lerp(.4,1,smoothstep(.25,.65,flow));
                    return half4(_CloudBody.rgb,_EdgeMist*softness*contact*wisps*_Opacity);
                }
                if(exit<=0)discard;
                bool inside=dot(origin,origin)<.25;
                float boundary=inside?exit:entry;
                // A hollow smoke boundary, not a solid volume. Depth in front of the
                // shell stays visible; crossing the near (outside) or far (inside)
                // shell always conceals. No full-screen darkness inside the smoke.
                if(limit<boundary) {
                    if(!inside) {
                        float gap=boundary-limit;
                        if(gap>=_ContactSoftness)discard;
                        // Light ground/contact wisps, never a hole in the shell.
                        float wisp=smoothstep(.2,.7,cloudField(TransformWorldToObject(scene)));
                        return half4(_CloudBody.rgb,_EdgeMist*(1-smoothstep(0,_ContactSoftness,gap))*wisp*_Opacity);
                    }
                    float mist=_InteriorMist*saturate(limit/max(exit,.001));
                    return half4(lerp(_Light.rgb,half3(.62,.7,.79),.35),mist*_Opacity);
                }
                float3 surface=origin+direction*boundary;
                if(!inside)return half4(exteriorCloud(surface,direction),_Opacity);
                float3 drift=float3(_Time.y*.1,-_Time.y*.07,0);
                // Broad, sculpted cloud folds rather than fine blurry speckle.
                float warp=noise(surface*5+drift*.4);
                float3 curled=surface+float3(warp*.08,warp*.04,0);
                float n=noise(curled*12+drift)*.6+noise(curled*25-drift*.6)*.28+noise(curled*49+drift*.3)*.12;
                float folds=smoothstep(.29,.68,n);
                float top=saturate(surface.y+.5);
                half3 col=lerp(_Deep.rgb,_Light.rgb,saturate(.24+top*.16+folds*.54));
                // Broken, subdued flow crests: cyan is a detail, not a glowing shell.
                float crest=smoothstep(.56,.59,n)*(1-smoothstep(.59,.63,n));
                crest*=smoothstep(.32,.7,warp)*.26;
                col=lerp(col,_Accent.rgb,crest);
                col=lerp(col,_Accent.rgb,(1-_Phase)*.15*_End);
                col*=lerp(.9,1,_End);
                // The inside wall is lighter and calmer; geometry before it receives
                // only the small mist above, including other players in this smoke.
                if(inside)col=lerp(col,_Light.rgb,.62);
                float alpha=1;
                return half4(col,alpha*_Opacity);
            }
            ENDHLSL
        }
    }
}
