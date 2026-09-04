using MelonLoader;
using System;
using UnityEngine;

namespace MoreGuns
{
    /// <summary>
    /// Mesh-swapped guns still use the AK MuzzlePoint near the grip. Move MuzzlePoint /
    /// muzzle VFX to the barrel tip so flash/tracers leave the muzzle, not mid-gun / camera.
    /// </summary>
    internal static class MuzzleAligner
    {
        /// <summary>Last aligned muzzle world position for trail origin override.</summary>
        public static Vector3 LastMuzzleWorldPos { get; private set; }
        public static bool HasLastMuzzle { get; private set; }

        public static void Align(GameObject root, string bodyToken)
        {
            HasLastMuzzle = false;
            if (root == null)
                return;

            try
            {
                Transform muzzle = FindDeep(root.transform, "MuzzlePoint");
                if (muzzle == null)
                    return;

                // Prefer author-placed flash/light already at the barrel (SMG flash is at ~-1.05).
                Transform tipAnchor = FindBarrelAnchor(root.transform);
                if (tipAnchor != null)
                {
                    muzzle.position = tipAnchor.position;
                    muzzle.rotation = tipAnchor.rotation;
                }
                else
                {
                    Renderer body = FindBodyRenderer(root, bodyToken);
                    if (body == null)
                        return;

                    Vector3 tip = FindBarrelTip(body, muzzle);
                    muzzle.position = tip;
                }

                Vector3 tipPos = muzzle.position;
                SnapNearNamed(root.transform, "Muzzle Flash", tipPos, 0.5f);
                SnapNearNamed(root.transform, "Flash Orange", tipPos, 0.5f);
                SnapNearNamed(root.transform, "Flash Red", tipPos, 0.5f);
                SnapNearNamed(root.transform, "Point Light", tipPos, 0.5f);
                SnapNearNamed(root.transform, "Point Light (1)", tipPos, 0.5f);

                LastMuzzleWorldPos = tipPos;
                HasLastMuzzle = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"MuzzleAligner failed: {ex.Message}");
            }
        }

        public static void RememberFrom(Equippable_RangedWeapon weapon)
        {
            HasLastMuzzle = false;
            if (weapon == null)
                return;
            try
            {
                Transform muzzle = FindDeep(weapon.transform, "MuzzlePoint");
                if (muzzle == null)
                    return;
                LastMuzzleWorldPos = muzzle.position;
                HasLastMuzzle = true;
            }
            catch { }
        }

        private static Transform FindBarrelAnchor(Transform root)
        {
            // Flash / lights on SMG were moved to the barrel while MuzzlePoint stayed at the grip.
            string[] names = { "Muzzle Flash", "Flash Orange", "Flash Red", "Point Light", "Point Light (1)" };
            Transform best = null;
            float bestDist = 0.25f; // must be meaningfully away from grip origin

            for (int i = 0; i < names.Length; i++)
            {
                Transform t = FindDeep(root, names[i]);
                if (t == null)
                    continue;
                float d = t.localPosition.magnitude;
                if (d > bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            return best;
        }

        private static Renderer FindBodyRenderer(GameObject root, string bodyToken)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Renderer best = null;
            float bestScore = -1f;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                string n = r.gameObject.name ?? "";
                if (n.IndexOf("Magazine", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (n.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (n.IndexOf("Bullet", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (n.IndexOf("Smoke", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (n.IndexOf("Shell", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // Skip giant leftover meshes.
                Vector3 lossy = r.transform.lossyScale;
                if (lossy.x >= 5f || lossy.y >= 5f || lossy.z >= 5f)
                    continue;

                bool nameMatch = !string.IsNullOrEmpty(bodyToken)
                    && n.IndexOf(bodyToken, StringComparison.OrdinalIgnoreCase) >= 0;
                float size = r.bounds.size.sqrMagnitude;
                float score = size + (nameMatch ? 1000f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = r;
                }
            }

            return best;
        }

        private static Vector3 FindBarrelTip(Renderer body, Transform muzzle)
        {
            Bounds b = body.bounds;
            Vector3 dir = muzzle.forward;
            if (dir.sqrMagnitude < 0.01f && muzzle.parent != null)
                dir = -muzzle.parent.right;
            dir.Normalize();

            Vector3 tip = b.center;
            float best = float.MinValue;
            Vector3 c = b.center;
            Vector3 e = b.extents;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 corner = c + new Vector3(e.x * xi, e.y * yi, e.z * zi);
                float dot = Vector3.Dot(corner - c, dir);
                if (dot > best)
                {
                    best = dot;
                    tip = corner;
                }
            }

            return tip + dir * 0.02f;
        }

        private static void SnapNearNamed(Transform root, string name, Vector3 worldPos, float maxDist)
        {
            Transform t = FindDeep(root, name);
            if (t == null)
                return;
            if (Vector3.Distance(t.position, worldPos) <= maxDist
                || name.StartsWith("Flash", StringComparison.Ordinal)
                || name.StartsWith("Muzzle", StringComparison.Ordinal)
                || name.StartsWith("Point Light", StringComparison.Ordinal))
                t.position = worldPos;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
