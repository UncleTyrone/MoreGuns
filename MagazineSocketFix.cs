using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreGuns
{
    /// <summary>
    /// Scrub leftover AK magazine meshes from mesh-swapped gun clones only.
    /// Never run this on AK47. RPG rocket Magazine is scale ~50 — never treat that as leftover.
    /// </summary>
    internal static class MagazineSocketFix
    {
        private static readonly HashSet<int> LeftoverAkMagazineMeshIds = new HashSet<int>();

        public static void CacheLeftoverMeshesFromGun(GameObject root)
        {
            if (root == null)
                return;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t.name != "Magazine")
                    continue;
                if (t.gameObject.activeSelf)
                    continue;

                Vector3 s = t.localScale;
                Vector3 p = t.localPosition;
                bool looksOrphan = (s.x >= 5f && s.x < 40f) // SMG/sniper leftover ~10, not RPG rocket ~50
                    || Mathf.Abs(p.z) > 1f || Mathf.Abs(p.x) > 1.5f
                    || (s.x > 0f && s.x < 0.05f);

                if (!looksOrphan)
                    continue;

                MeshFilter[] filters = t.GetComponentsInChildren<MeshFilter>(true);
                for (int f = 0; f < filters.Length; f++)
                {
                    Mesh mesh = filters[f]?.sharedMesh;
                    if (mesh != null)
                        LeftoverAkMagazineMeshIds.Add(mesh.GetInstanceID());
                }
            }
        }

        [Obsolete("Use CacheLeftoverMeshesFromGun on smg/sniper/rpg instead.")]
        public static void CacheAkMagazineMeshes(GameObject akRoot)
        {
        }

        public static void FixGunHierarchy(GameObject root)
        {
            if (root == null)
                return;

            string rootName = root.name ?? "";
            if (IsAkRootName(rootName))
                return;

            bool isRpg = rootName.IndexOf("rpg", StringComparison.OrdinalIgnoreCase) >= 0;

            Transform[] all;
            try { all = root.GetComponentsInChildren<Transform>(true); }
            catch { return; }

            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                if (t.name == "MagazineMesh")
                {
                    try { UnityEngine.Object.Destroy(t.gameObject); }
                    catch { }
                    continue;
                }

                if (t.name != "Magazine")
                    continue;

                if (isRpg)
                {
                    // RPG loaded rocket is Magazine @ scale ~50. Only remove tiny inactive AK leftover.
                    if (!IsRpgAkLeftover(t))
                        continue;
                }
                else if (!IsLegacyAkMagazine(t))
                {
                    continue;
                }

                try
                {
                    UnityEngine.Object.Destroy(t.gameObject);
                }
                catch
                {
                    t.gameObject.SetActive(false);
                    DisableRenderers(t);
                }
            }

            StripReloadClips(root);
        }

        private static bool IsAkRootName(string rootName)
        {
            return rootName.IndexOf("ak47", StringComparison.OrdinalIgnoreCase) >= 0
                || rootName.Equals("AK47", StringComparison.OrdinalIgnoreCase)
                || rootName.Equals("K47", StringComparison.OrdinalIgnoreCase)
                || rootName.IndexOf("AK47_", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Inactive tiny AK mag left under the RPG tube — not the scale-50 rocket.</summary>
        private static bool IsRpgAkLeftover(Transform magazine)
        {
            if (magazine.gameObject.activeSelf)
                return false;

            Vector3 scale = magazine.localScale;
            if (scale.x >= 40f)
                return false; // rocket / intentional large mag

            if (scale.x > 0f && scale.x < 0.05f)
                return true;

            MeshFilter[] filters = magazine.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i]?.sharedMesh;
                if (mesh == null)
                    continue;
                if (LeftoverAkMagazineMeshIds.Contains(mesh.GetInstanceID()))
                    return true;
                string n = mesh.name ?? "";
                if (n.IndexOf("AK", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool IsLegacyAkMagazine(Transform magazine)
        {
            Vector3 scale = magazine.localScale;
            Vector3 pos = magazine.localPosition;

            // SMG/sniper leftovers are ~scale 10. RPG rocket is ~50 — exclude that range here too.
            if ((scale.x >= 5f && scale.x < 40f)
                || (scale.y >= 5f && scale.y < 40f)
                || (scale.z >= 5f && scale.z < 40f)
                || Mathf.Abs(pos.z) > 1f || Mathf.Abs(pos.x) > 1.5f)
                return true;

            if (magazine.gameObject.activeSelf)
                return false;

            MeshFilter[] filters = magazine.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i]?.sharedMesh;
                if (mesh == null)
                    continue;

                if (LeftoverAkMagazineMeshIds.Contains(mesh.GetInstanceID()))
                    return true;

                string n = mesh.name ?? "";
                if (n.IndexOf("AK", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            if (scale.x > 0f && scale.x < 0.05f)
            {
                for (int i = 0; i < filters.Length; i++)
                {
                    if (filters[i]?.sharedMesh != null)
                        return true;
                }
            }

            return false;
        }

        private static void DisableRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }
        }

        private static void StripReloadClips(GameObject root)
        {
            try
            {
                Animation[] anims = root.GetComponentsInChildren<Animation>(true);
                for (int a = 0; a < anims.Length; a++)
                {
                    Animation anim = anims[a];
                    if (anim == null)
                        continue;
                    TryRemoveClip(anim, "Other Reload");
                    TryRemoveClip(anim, "OtherReload");
                    TryRemoveClip(anim, "AK47 Reload");
                    TryRemoveClip(anim, "AK47Reload");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"StripReloadClips: {ex.Message}");
            }
        }

        private static void TryRemoveClip(Animation anim, string clipName)
        {
            try
            {
                AnimationClip clip = anim.GetClip(clipName);
                if (clip != null)
                    anim.RemoveClip(clip);
            }
            catch { }
        }
    }
}
