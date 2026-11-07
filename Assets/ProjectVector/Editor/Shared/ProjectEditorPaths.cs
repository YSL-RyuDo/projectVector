using System;
using System.IO;
using System.Linq;
using UnityEditor;

internal static class ProjectEditorPaths
{
    public static string Scene(string name)
    {
        var paths = AssetDatabase.FindAssets(name + " t:Scene", new[] { "Assets/ProjectVector" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileNameWithoutExtension(path) == name).ToArray();
        if (paths.Length != 1)
            throw new InvalidOperationException($"Expected one saved scene named {name}; found {paths.Length}.");
        return paths[0];
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains(".."))
            throw new ArgumentException("Expected project asset folder", nameof(path));
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
