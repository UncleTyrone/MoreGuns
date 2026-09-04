#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Duplicates AK47 prefabs/assets into sniper, smg, and rpg IDs (placeholder meshes).
/// </summary>
public static class DuplicateAk47Guns
{
    private static readonly string[] NewIds = { "sniper", "smg", "rpg" };

    [MenuItem("MoreGuns/Duplicate AK47 Into Sniper SMG RPG")]
    public static void Duplicate()
    {
        string akEquippable = FirstExisting(
            "Assets/resources/weapons/ak47/AK47_Equippable.prefab",
            "Assets/Resources/weapons/ak47/ak47_equippable.prefab");
        string akDef = FirstExisting(
            "Assets/resources/weapons/ak47/AK47.asset",
            "Assets/Resources/weapons/ak47/ak47.asset");
        string akMag = FirstExisting(
            "Assets/resources/weapons/ak47/magazine/AK47_Magazine.asset",
            "Assets/Resources/weapons/ak47/magazine/ak47_magazine.asset");
        string akTrash = FirstExisting(
            "Assets/resources/weapons/ak47/magazine/AK47_Magazine_Trash.prefab",
            "Assets/Resources/weapons/ak47/magazine/ak47_magazine_trash.prefab");
        string akMagAvatar = FirstExisting(
            "Assets/resources/weapons/ak47/magazine/AK47_Magazine_AvatarEquippable.prefab",
            "Assets/Resources/weapons/ak47/magazine/ak47_magazine_avatarequippable.prefab");
        string akAvatar = FirstExisting(
            "Assets/resources/avatar/equippables/AK47.prefab",
            "Assets/Resources/avatar/equippables/ak47.prefab");

        if (akEquippable == null || akDef == null || akMag == null || akTrash == null || akMagAvatar == null || akAvatar == null)
        {
            Debug.LogError(
                "AK47 assets missing. Expected files under Assets/resources/weapons/ak47/. " +
                $"equippable={akEquippable} def={akDef} mag={akMag} trash={akTrash} magAvatar={akMagAvatar} avatar={akAvatar}");
            return;
        }

        foreach (string id in NewIds)
        {
            string weaponDir = $"Assets/resources/weapons/{id}";
            string magDir = $"{weaponDir}/magazine";
            string avatarDir = "Assets/resources/avatar/equippables";
            Directory.CreateDirectory(weaponDir);
            Directory.CreateDirectory(magDir);
            Directory.CreateDirectory(avatarDir);

            CopyAsset(akEquippable, $"{weaponDir}/{id}_equippable.prefab");
            CopyAsset(akDef, $"{weaponDir}/{id}.asset");
            CopyAsset(akMag, $"{magDir}/{id}_magazine.asset");
            CopyAsset(akTrash, $"{magDir}/{id}_magazine_trash.prefab");
            CopyAsset(akMagAvatar, $"{magDir}/{id}_magazine_avatarequippable.prefab");
            CopyAsset(akAvatar, $"{avatarDir}/{id}.prefab");

            AssignBundle($"{weaponDir}/{id}_equippable.prefab");
            AssignBundle($"{weaponDir}/{id}.asset");
            AssignBundle($"{magDir}/{id}_magazine.asset");
            AssignBundle($"{magDir}/{id}_magazine_trash.prefab");
            AssignBundle($"{magDir}/{id}_magazine_avatarequippable.prefab");
            AssignBundle($"{avatarDir}/{id}.prefab");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Duplicated AK47 into sniper, smg, rpg. Then use MoreGuns → Build MoreGuns Bundle.");
    }

    private static string FirstExisting(params string[] paths)
    {
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                return path;
        }

        return null;
    }

    private static void CopyAsset(string source, string dest)
    {
        dest = dest.Replace("\\", "/");
        if (AssetDatabase.LoadAssetAtPath<Object>(dest) != null)
            AssetDatabase.DeleteAsset(dest);
        if (!AssetDatabase.CopyAsset(source, dest))
            Debug.LogError($"Failed to copy {source} -> {dest}");
    }

    private static void AssignBundle(string path)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        if (importer == null)
            return;
        importer.SetAssetBundleNameAndVariant(BuildMoreGunsBundle.BundleName, "");
        importer.SaveAndReimport();
    }
}
#endif
