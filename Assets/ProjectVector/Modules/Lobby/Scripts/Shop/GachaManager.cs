using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Presentation only: the server has already persisted every result before Play is called.
public sealed class GachaManager : MonoBehaviour
{
    public enum Phase { Idle, Prepare, Warp, RingHits, Materialize, Summary }
    public Phase CurrentPhase { get; private set; }
    public bool IsPlaying => routine != null;
    private Coroutine routine;
    [Header("Saved sequence UI — editable in Lobby hierarchy")]
    public GameObject overlay;
    public TMP_Text label;
    public Image flash, glitch;
    public Image revealBackdrop;
    public VectorShopPreview revealPreview;
    private GameObject stage;
    private RenderTexture texture;
    private Material lineMaterial;
    private Action onComplete;
    private Camera output;
    private CinemachineCamera virtualCamera;
    private readonly List<LineRenderer> rewardCells = new();
    private readonly HashSet<int> litCells = new();
    public int ActivatedCellCount => litCells.Count;
    private ParticleSystem speedParticles;
    private VolumeProfile postProfile;
    private bool savedFog, fogOverridden;
    private readonly List<LineRenderer> tunnel = new();
    private bool completionInvoked;
    private bool skipIntro, advanceRequested;
    public bool AwaitingAdvance { get; private set; }
    public int RevealedMythCount { get; private set; }

    public void Play(IReadOnlyList<VectorShopReward> rewards, IReadOnlyList<VectorShopItem> items, Func<VectorShopItem, GameObject> model, Action complete)
    {
        if (IsPlaying || rewards == null || rewards.Count == 0) return;
        if (overlay == null || label == null || flash == null || glitch == null || revealPreview == null)
        { Debug.LogError("[SHOP] Sequence UI must be assigned in the saved scene."); complete?.Invoke(); return; }
        onComplete = complete;
        completionInvoked = false;
        skipIntro = advanceRequested = false;
        AwaitingAdvance = false;
        RevealedMythCount = 0;
        routine = StartCoroutine(Run(rewards, items, model));
    }
    public void Skip()
    {
        if (routine == null) return;
        if (CurrentPhase == Phase.Summary) return;
        if (CurrentPhase == Phase.Materialize) { if (AwaitingAdvance) advanceRequested = true; return; }
        skipIntro = true;
    }
    public void Dismiss()
    {
        if (routine != null) StopCoroutine(routine);
        Finish();
    }
    private void OnDisable() => Dismiss();
    private void OnDestroy() => Cleanup();
    private IEnumerator Run(IReadOnlyList<VectorShopReward> rewards, IReadOnlyList<VectorShopItem> items, Func<VectorShopItem, GameObject> model)
    {
        CreateStage();
        SetAdvanceButton("건너뛰기  ESC", true);
        CurrentPhase = Phase.Prepare;
        label.text = "";
        yield return Wait(.35f);
        CurrentPhase = Phase.Warp;
        yield return Animate(2.0f, t =>
        {
            virtualCamera.transform.localPosition = new Vector3(0, 0, Mathf.SmoothStep(0, 32, t));
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(52, 78, t);
            virtualCamera.InternalUpdateCameraState(Vector3.up, Time.unscaledDeltaTime);
            output.transform.SetPositionAndRotation(virtualCamera.State.GetFinalPosition(), virtualCamera.State.GetFinalOrientation());
            output.fieldOfView = virtualCamera.State.Lens.FieldOfView;
        });
        CurrentPhase = Phase.RingHits;
        if(speedParticles!=null) speedParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        yield return Animate(.35f, t => output.fieldOfView = Mathf.Lerp(78, 54, t));
        int revealIndex = 0;
        foreach (var reward in rewards)
        {
            if (skipIntro) break;
            var item = items.FirstOrDefault(i => i.id == reward.item); if (item == null) continue;
            Color color = VectorShopPanel.GradeColors[item.grade];
            ++revealIndex;
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere); orb.name = "RewardOrb";
            orb.transform.SetParent(stage.transform, false); orb.layer = 31;
            Destroy(orb.GetComponent<Collider>());
            orb.GetComponent<Renderer>().sharedMaterial = lineMaterial;
            var properties = new MaterialPropertyBlock(); properties.SetColor("_BaseColor", color * 2);
            orb.GetComponent<Renderer>().SetPropertyBlock(properties);
            var from = new Vector3(0, 0, 95);
            int cell = NextCell(item.grade);
            var target = CellHitPoint(cell);
            var trail=orb.AddComponent<TrailRenderer>(); trail.sharedMaterial=lineMaterial;trail.time=.22f;trail.startWidth=.09f;trail.endWidth=0;trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0);
            yield return Animate(.52f, t => { orb.transform.localPosition = Vector3.Lerp(from, target, t * t); orb.transform.localScale = Vector3.one * Mathf.Lerp(.12f, .28f, t); });
            Burst(target, color);
            litCells.Add(cell);
            var hitCell = rewardCells[cell];
            hitCell.startColor = hitCell.endColor = color;
            Destroy(orb);
            if (item.grade == 0) yield return Flash(color);
            yield return Animate(.18f, t => hitCell.widthMultiplier = Mathf.Lerp(.54f, .42f, t));
        }
        var myths = rewards.Select(r => items.FirstOrDefault(i => i.id == r.item)).Where(i => i != null && i.grade == 0).ToArray();
        foreach (var myth in myths)
        {
            CurrentPhase = Phase.Materialize;
            advanceRequested = false;
            AwaitingAdvance = false;
            SetAdvanceButton("다음  ESC", false);
            if(revealBackdrop!=null) revealBackdrop.gameObject.SetActive(true);
            revealPreview.gameObject.SetActive(true); revealPreview.Show(model(myth));
            ++RevealedMythCount;
            label.text = $"신화 획득  /  {myth.name}  ({RevealedMythCount}/{myths.Length})"; label.color = VectorShopPanel.GradeColors[0];
            yield return revealPreview.Reveal(2.1f, new Color(.75f,.34f,.035f));
            AwaitingAdvance = true;
            SetAdvanceButton(RevealedMythCount == myths.Length ? "결과 보기  ESC" : "다음  ESC", true);
            yield return new WaitUntil(() => advanceRequested);
            AwaitingAdvance = false;
        }
        CurrentPhase = Phase.Summary;
        SetAdvanceButton("결과 확인", false);
        overlay.SetActive(false);
        InvokeComplete();
        // Keep the presentation behind the results until the authored confirmation button is pressed.
        yield return new WaitUntil(() => false);
    }
    private void SetAdvanceButton(string text, bool interactable)
    {
        var button = overlay.transform.Find("SkipButton")?.GetComponent<Button>();
        if (button == null) return;
        button.interactable = interactable;
        var caption = button.GetComponentInChildren<TMP_Text>(true);
        if (caption != null) caption.text = text;
    }
    private IEnumerator Animate(float duration, Action<float> update)
    { for (float t = 0; t < duration && !skipIntro; t += Time.unscaledDeltaTime) { update(Mathf.Clamp01(t / duration)); yield return null; } update(1); }
    private IEnumerator Wait(float duration) { yield return new WaitForSecondsRealtime(duration); }
    private IEnumerator Flash(Color color)
    {
        flash.gameObject.SetActive(true);
        yield return Animate(.24f, t =>
        {
            flash.color = new Color(1, .97f, .88f, (1 - t) * .10f);
            float y = .2f + .6f * t;
            glitch.rectTransform.anchorMin = new Vector2(0, y);
            glitch.rectTransform.anchorMax = new Vector2(1, y + .025f);
            glitch.color = Color.clear;
        });
        flash.gameObject.SetActive(false);
    }
    private void CreateStage()
    {
        overlay.SetActive(true); revealPreview.gameObject.SetActive(false); flash.gameObject.SetActive(false); label.color = Color.white;
        if(revealBackdrop!=null) revealBackdrop.gameObject.SetActive(false);
        stage = new GameObject("GachaPresentationStage"); stage.transform.position = new Vector3(20000, 20000, 20000);
        stage.AddComponent<VectorGachaChamber>().Build();
        var cameraObject = new GameObject("GachaCamera", typeof(Camera)); cameraObject.transform.SetParent(stage.transform, false);
        output = cameraObject.GetComponent<Camera>(); output.clearFlags = CameraClearFlags.SolidColor; output.backgroundColor = new Color(.55f, .61f, .70f); output.cullingMask = 1 << 31; output.farClipPlane = 160;
        RenderPipelineManager.beginCameraRendering += BeginPresentationCamera;
        RenderPipelineManager.endCameraRendering += EndPresentationCamera;
        texture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR); output.allowHDR=true; output.targetTexture = texture; overlay.GetComponent<RawImage>().texture = texture;
        var cameraData=output.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;cameraData.volumeLayerMask=1<<31;cameraData.volumeTrigger=output.transform;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var volumeObject=new GameObject("LocalChamberPost");volumeObject.layer=31;volumeObject.transform.SetParent(stage.transform,false);
        var bounds=volumeObject.AddComponent<BoxCollider>();bounds.isTrigger=true;bounds.size=Vector3.one*300;
        var volume=volumeObject.AddComponent<Volume>();volume.isGlobal=false;volume.blendDistance=0;volume.priority=5;
        postProfile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=postProfile;
        var bloom=postProfile.Add<Bloom>();bloom.threshold.Override(1);bloom.intensity.Override(.3f);bloom.scatter.Override(.55f);
        var vcamObject = new GameObject("GachaCinemachineRig"); vcamObject.SetActive(false); vcamObject.transform.SetParent(stage.transform, false);
        virtualCamera = vcamObject.AddComponent<CinemachineCamera>(); virtualCamera.enabled = false;
        // Manual evaluation keeps this rig out of every existing gameplay CinemachineBrain.
        virtualCamera.Lens.FieldOfView = 45; vcamObject.SetActive(true);
        var shader = Resources.Load<Shader>("Shop/VectorEffects");
        lineMaterial = new Material(shader);
        rewardCells.Clear(); litCells.Clear();
        for (int i = 0; i < 30; ++i)
        {
            var color = VectorShopPanel.GradeColors[i % 3] * .16f; color.a = 1;
            var cell = Arc(5.02f,43.50f,i*12+1.5f,i*12+10.5f,color,.42f,8);
            cell.name = $"RewardCell_{i:00}_Grade{i % 3}";
            rewardCells.Add(cell);
        }
        SpeedLines();
    }
    private int NextCell(int grade)
    {
        for (int i = grade; i < rewardCells.Count; i += 3) if (!litCells.Contains(i)) return i;
        return grade;
    }
    private Vector3 CellHitPoint(int cell)
    {
        float angle = (cell * 12 + 6) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle) * 5.02f, Mathf.Sin(angle) * 5.02f, 43.5f);
    }
    private LineRenderer Ring(float radius, float z, int segments, Color color, float width)
    {
        var go = new GameObject("GateRing", typeof(LineRenderer)); go.transform.SetParent(stage.transform, false); go.layer = 31;
        var line = go.GetComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true; line.positionCount = segments; line.widthMultiplier = width; line.sharedMaterial = lineMaterial; line.startColor = line.endColor = color;
        for (int i = 0; i < segments; ++i) { float a = i * Mathf.PI * 2 / segments; line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, z)); }
        return line;
    }
    private LineRenderer Arc(float radius,float z,float startDeg,float endDeg,Color color,float width,int segments=28)
    {
        var go = new GameObject("GradeGateArc", typeof(LineRenderer)); go.transform.SetParent(stage.transform, false); go.layer = 31;
        var line = go.GetComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = false; line.positionCount = segments; line.widthMultiplier = width; line.sharedMaterial = lineMaterial; line.startColor = line.endColor = color;
        for (int i = 0; i < segments; ++i)
        {
            float a = Mathf.Lerp(startDeg,endDeg,i/(float)(segments-1)) * Mathf.Deg2Rad;
            line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,z));
        }
        return line;
    }
    private void SpeedLines()
    {
        var go = new GameObject("SpeedLines", typeof(ParticleSystem)); go.transform.SetParent(stage.transform, false); go.transform.localPosition = new Vector3(0,0,75); go.transform.localRotation = Quaternion.Euler(0,180,0); go.layer = 31;
        var ps = go.GetComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        speedParticles=ps;
        var main = ps.main; main.startLifetime = 1.5f; main.startSpeed = 0; main.startSize = .018f; main.startColor = new Color(.75f,.85f,1); main.maxParticles = 300; main.useUnscaledTime = true;
        var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local; velocity.z = 45;
        var emission = ps.emission; emission.rateOverTime = 85;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 5; shape.radiusThickness = .2f;
        var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = lineMaterial; renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.lengthScale = 5; renderer.velocityScale = .15f;
        ps.Play();
    }
    private void Burst(Vector3 position, Color color)
    {
        var go = new GameObject("GradeImpact", typeof(ParticleSystem)); go.transform.SetParent(stage.transform, false); go.transform.localPosition = position; go.layer = 31;
        var ps = go.GetComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = false; main.startLifetime = .32f; main.startSpeed = 3; main.startSize = .025f; main.startColor = color; main.useUnscaledTime = true;
        var emission = ps.emission; emission.rateOverTime = 0;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = lineMaterial; ps.Play(); ps.Emit(18);
        var sparkRenderer=ps.GetComponent<ParticleSystemRenderer>();sparkRenderer.renderMode=ParticleSystemRenderMode.Stretch;sparkRenderer.velocityScale=.04f;sparkRenderer.lengthScale=1.8f;
    }
    private void Finish()
    {
        routine = null;
        AwaitingAdvance = false;
        Cleanup();
        CurrentPhase = Phase.Idle;
    }
    private void InvokeComplete()
    {
        if (completionInvoked) return;
        completionInvoked = true;
        var complete = onComplete;
        onComplete = null;
        complete?.Invoke();
    }
    private void Cleanup()
    {
        RenderPipelineManager.beginCameraRendering -= BeginPresentationCamera;
        RenderPipelineManager.endCameraRendering -= EndPresentationCamera;
        if (fogOverridden) { RenderSettings.fog = savedFog; fogOverridden = false; }
        tunnel.Clear();
        rewardCells.Clear(); litCells.Clear();
        if (overlay != null) { overlay.GetComponent<RawImage>().texture = null; overlay.SetActive(false); }
        if (stage != null) { stage.SetActive(false); Destroy(stage); }
        if (texture != null) { texture.Release(); Destroy(texture); }
        if (lineMaterial != null) Destroy(lineMaterial);
        if(postProfile!=null) {foreach(var component in postProfile.components) Destroy(component);Destroy(postProfile);postProfile=null;}
        stage = null; texture = null; lineMaterial = null;
    }
    private void BeginPresentationCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != output) return;
        savedFog = RenderSettings.fog; fogOverridden = true; RenderSettings.fog = false;
    }
    private void EndPresentationCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != output || !fogOverridden) return;
        RenderSettings.fog = savedFog; fogOverridden = false;
    }
}
