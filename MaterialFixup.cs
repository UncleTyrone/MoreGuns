using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoreGuns
{
    /// <summary>
    /// AssetRipper/Unity rebuild ships incomplete URP shader blobs. Rebound materials to the
    /// game's built-in URP shaders so guns stay opaque and particles keep alpha.
    /// </summary>
    internal static class MaterialFixup
    {
        private static Shader _lit;
        private static Shader _unlit;
        private static Shader _particles;
        private static bool _resolved;

        public static void FixHierarchy(GameObject root)
        {
            if (root == null)
                return;

            EnsureShaders();
            if (_lit == null && _unlit == null && _particles == null)
                return;

            Renderer[] renderers;
            try
            {
                renderers = root.GetComponentsInChildren<Renderer>(true);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"MaterialFixup: could not read renderers on {root.name}: {ex.Message}");
                return;
            }

            if (renderers == null)
                return;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                try
                {
                    Material[] shared = renderer.sharedMaterials;
                    if (shared == null || shared.Length == 0)
                        continue;

                    bool changed = false;
                    for (int i = 0; i < shared.Length; i++)
                    {
                        Material mat = shared[i];
                        if (mat == null)
                            continue;
                        try
                        {
                            if (FixMaterial(mat))
                                changed = true;
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Warning($"MaterialFixup: skipped '{mat.name}': {ex.Message}");
                        }
                    }

                    if (changed)
                        renderer.sharedMaterials = shared;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"MaterialFixup: renderer '{renderer.name}' failed: {ex.Message}");
                }
            }
        }

        private static void EnsureShaders()
        {
            if (_resolved)
                return;
            _resolved = true;

            _lit = Shader.Find("Universal Render Pipeline/Lit");
            _unlit = Shader.Find("Universal Render Pipeline/Unlit");
            _particles = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Particles/Simple Lit");

            if (_lit == null && _unlit == null)
                MelonLogger.Warning("Could not find URP Lit/Unlit shaders; gun materials may stay transparent.");
        }

        private static bool FixMaterial(Material mat)
        {
            string name = mat.name ?? string.Empty;
            string lower = name.ToLowerInvariant();
            bool particleLike = lower.Contains("particle") || lower.Contains("muzzle") || lower.Contains("flash")
                || lower.Contains("smoke") || lower.Contains("tracer") || lower.Contains("ray");
            bool glassLike = lower.Contains("glass");

            Shader target;
            if (particleLike)
                target = _particles ?? _unlit ?? _lit;
            else if (glassLike)
                target = _lit ?? _unlit;
            else
                target = _lit ?? _unlit;

            if (target == null)
                return false;

            Texture baseMap = GetTexture(mat, "_BaseMap") ?? GetTexture(mat, "_MainTex");
            Color color = Color.white;
            try
            {
                if (mat.HasProperty("_BaseColor"))
                    color = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    color = mat.GetColor("_Color");
            }
            catch { color = Color.white; }

            mat.shader = target;

            if (baseMap != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", baseMap);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", baseMap);
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (particleLike)
            {
                SetSurfaceTransparent(mat);
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 0f);
                if (mat.HasProperty("_Cutoff"))
                    mat.SetFloat("_Cutoff", 0.1f);
            }
            else if (!glassLike)
            {
                SetSurfaceOpaque(mat);
            }

            return true;
        }

        private static Texture GetTexture(Material mat, string property)
        {
            try
            {
                return mat.HasProperty(property) ? mat.GetTexture(property) : null;
            }
            catch
            {
                return null;
            }
        }

        private static void SetSurfaceOpaque(Material mat)
        {
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 1f);
        }

        private static void SetSurfaceTransparent(Material mat)
        {
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 0f);
        }
    }
}
