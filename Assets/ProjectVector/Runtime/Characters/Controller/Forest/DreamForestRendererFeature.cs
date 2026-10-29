using UnityEngine;
using UnityEngine.Rendering.Universal;

// Uses URP's supported full-screen pass for both RenderGraph and compatibility mode.
public sealed class DreamForestRendererFeature : FullScreenPassRendererFeature
{
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (!Application.isPlaying || camera.cameraType != CameraType.Game ||
            renderingData.cameraData.renderType == CameraRenderType.Overlay || !passMaterial) return;
        var area = DreamForestPresentation.Inside(camera.transform.position);
        if (!area) return; // No color copy or depth pass outside this skill.
        passMaterial.SetFloat("_ForestStrength", area.Strength);
        base.AddRenderPasses(renderer,ref renderingData);
    }
}
