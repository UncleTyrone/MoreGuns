#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildMoreGunsBundle
{
    public const string BundleName = "voidanesguns";
    public const string OutputFolder = "Assets/Bundles";

    [MenuItem("MoreGuns/Build MoreGuns Bundle")]
    public static void Build()
    {
        AssignFolderToBundle("Assets/resources");
        AssignFolderToBundle("Assets/Resources");
        AssignFolderToBundle("Assets/ui");
        AssignFolderToBundle("Assets/Sprite");
        AssignFolderToBundle("Assets/Texture2D");
        AssignFolderToBundle("Assets/Material");
        AssignFolderToBundle("Assets/Mesh");
        AssignFolderToBundle("Assets/Shader");
        AssignFolderToBundle("Assets/AudioClip");
        AssignFolderToBundle("Assets/AnimatorController");
        AssignFolderToBundle("Assets/AnimationClip");
        AssignFolderToBundle("Assets/Models");
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(OutputFolder);
        BuildPipeline.BuildAssetBundles(
            OutputFolder,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);

        string built = Path.Combine(OutputFolder, BundleName);
        if (!File.Exists(built))
        {
            Debug.LogError($"Bundle '{BundleName}' was not produced. Assign assets to that AssetBundle name in the Inspector.");
            return;
        }

        string dest = FindModResourcesBundle();
        if (dest == null)
        {
            Debug.LogError("Could not find MoreGuns/Resources/voidanesguns. Copy Assets/Bundles/voidanesguns there manually.");
            return;
        }

        File.Copy(built, dest, true);
        Debug.Log($"Copied bundle to {dest}. Rebuild MoreGuns.dll, then copy the DLL into Mods.");
    }

    [MenuItem("MoreGuns/Duplicate Then Build Bundle")]
    public static void DuplicateThenBuild()
    {
        DuplicateAk47Guns.Duplicate();
        Build();
    }

    private static string FindModResourcesBundle()
    {
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath);
        while (dir != null)
        {
            string resources = Path.Combine(dir.FullName, "Resources");
            string csproj = Path.Combine(dir.FullName, "MoreGuns.csproj");
            if (Directory.Exists(resources) && File.Exists(csproj))
                return Path.Combine(resources, "voidanesguns");

            dir = dir.Parent;
        }

        return null;
    }

    private static void AssignFolderToBundle(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        string[] guids = AssetDatabase.FindAssets("", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path))
                continue;
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null)
                continue;
            importer.SetAssetBundleNameAndVariant(BundleName, "");
        }
    }
}
#endif
