using UnityEngine;
using UnityEngine.Rendering;

// Scene-local opt-in. Never edits the shared render asset or other scenes' settings.
[DefaultExecutionOrder(-10000)]
public sealed class PracticeSceneRendering : MonoBehaviour
{
    public RenderPipelineAsset trainingPipeline;
    private RenderPipelineAsset previous;
    private bool applied;
    private void OnEnable()
    {
        if (trainingPipeline == null || applied) return;
        previous = QualitySettings.renderPipeline;
        QualitySettings.renderPipeline = trainingPipeline;
        applied = true;
    }
    private void OnDisable()
    {
        if (!applied) return;
        if (QualitySettings.renderPipeline == trainingPipeline) QualitySettings.renderPipeline = previous;
        applied = false;
    }
}
