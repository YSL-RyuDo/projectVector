using UnityEngine;
using UnityEngine.Rendering;

// Map-local quality override. The existing maps and the user's quality asset are never edited.
public sealed class ConstructionMapEnvironment : MonoBehaviour
{
    public RenderPipelineAsset pipeline;
    RenderPipelineAsset previous;
    int previousAA;
    bool applied;
    void OnEnable(){if(Application.isPlaying)Apply();}
    public void Apply()
    {
        if(applied || !pipeline)return;
        previous=QualitySettings.renderPipeline;previousAA=QualitySettings.antiAliasing;QualitySettings.renderPipeline=pipeline;applied=true;
    }
    void OnDisable()=>Restore();
    void OnDestroy()=>Restore();
    public void Restore()
    {
        if(!applied)return;
        if(QualitySettings.renderPipeline==pipeline){QualitySettings.renderPipeline=previous;QualitySettings.antiAliasing=previousAA;}
        applied=false;
    }
}
