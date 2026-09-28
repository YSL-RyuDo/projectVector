#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Scene-local MonoScript objects can be generated for secondary MonoBehaviours
// declared in another component's file. They are not stable player script assets.
public sealed class SceneScriptReferenceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report) => ValidateEnabledScenes();

    public static void ValidateEnabledScenes()
    {
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            string text = File.ReadAllText(scene.path);
            foreach (Match block in Regex.Matches(text, @"(?ms)^--- !u!115 &\d+\r?\n.*?(?=^--- !u!|\z)"))
            {
                string className = Regex.Match(block.Value, @"(?m)^  m_ClassName: (.*)$").Groups[1].Value.Trim();
                string assembly = Regex.Match(block.Value, @"(?m)^  m_AssemblyName: (.*)$").Groups[1].Value.Trim();
                if (assembly != "Assembly-CSharp" && !assembly.StartsWith("Assembly-CSharp-")) continue;
                throw new BuildFailedException(
                    $"{scene.path}: embedded MonoScript '{className}' cannot be used in a player scene. " +
                    $"Move {className} into {className}.cs and reconnect the component to that script asset.");
            }
        }
    }
}
#endif
