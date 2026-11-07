using System;
using System.IO;
using UnityEngine;

internal static class ServerExportPaths
{
    public static string FilePath(string fileName)
    {
        string configured = Environment.GetEnvironmentVariable("VECTOR_EXPORT_ROOT");
        string root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VectorExports")
            : configured);
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).TrimEnd(Path.DirectorySeparatorChar);
        if (root.Equals(project, StringComparison.OrdinalIgnoreCase) || root.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("VECTOR_EXPORT_ROOT must be outside the Unity project.");
        Directory.CreateDirectory(root);
        return Path.Combine(root, fileName);
    }
}
