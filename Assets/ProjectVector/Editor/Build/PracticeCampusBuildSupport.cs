#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

// The scene-specific shadow variants must survive URP stripping in normal player builds.
// Build-only inclusion is restored even when a build fails; shared quality presets stay unchanged.
[InitializeOnLoad]
public sealed class PracticeCampusBuildSupport : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    const string Active="Vector.Campus.BuildInclusion", Previous="Vector.Campus.BuildInclusion.Previous";
    public int callbackOrder => -10000;
    static PracticeCampusBuildSupport(){EditorApplication.update+=RestoreAfterFailedBuild;}
    public void OnPreprocessBuild(BuildReport report)
    {
        if(!EditorBuildSettings.scenes.Any(s=>s.enabled && s.path=="Assets/ProjectVector/Modules/Practice/Scenes/WeaponTest.unity"))return;
        var settings=GraphicsSettings.GetRenderPipelineSettings<IncludeAdditionalRPAssets>();
        if(settings==null)throw new BuildFailedException("URP build inclusion settings unavailable for the training pipeline.");
        SessionState.SetBool(Previous,settings.includeReferencedInScenes);SessionState.SetBool(Active,true);
        settings.includeReferencedInScenes=true;
    }
    public void OnPostprocessBuild(BuildReport report)=>Restore();
    static void RestoreAfterFailedBuild(){if(!BuildPipeline.isBuildingPlayer)Restore();}
    public static void Restore()
    {
        if(!SessionState.GetBool(Active,false))return;
        var settings=GraphicsSettings.GetRenderPipelineSettings<IncludeAdditionalRPAssets>();
        if(settings==null)return;
        settings.includeReferencedInScenes=SessionState.GetBool(Previous,false);
        SessionState.SetBool(Active,false);
        // Only this settings asset may have been serialized by Unity during the build.
        var asset=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/ProjectVector/Shared/Rendering/Settings/UniversalRenderPipelineGlobalSettings.asset");
        if(asset!=null)AssetDatabase.SaveAssetIfDirty(asset);
    }
}
#endif
