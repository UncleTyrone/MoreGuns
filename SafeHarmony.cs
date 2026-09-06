using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace MoreGuns
{
    /// <summary>
    /// Applies Harmony patches one class at a time. Bulk PatchAll during Main/save load
    /// races MonoMod CompileMethodHook with Il2CppInterop JIT → Fatal CLR 0x80131506.
    /// </summary>
    internal static class SafeHarmony
    {
        public static void Apply(HarmonyLib.Harmony harmony, params Type[] patchClasses)
        {
            foreach (Type patchClass in patchClasses)
                TryPatch(harmony, patchClass);
        }

        public static IEnumerator ApplyBatched(HarmonyLib.Harmony harmony, params Type[] patchClasses)
        {
            int ok = 0;
            foreach (Type patchClass in patchClasses)
            {
                if (TryPatch(harmony, patchClass))
                    ok++;
                yield return null;
            }
            MelonLogger.Msg($"MoreGuns Harmony applied {ok}/{patchClasses.Length} patch classes (batched).");
        }

        private static bool TryPatch(HarmonyLib.Harmony harmony, Type patchClass)
        {
            try
            {
                new PatchClassProcessor(harmony, patchClass).Patch();
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Harmony class {patchClass.Name} failed to bind: {ex.Message}");
                return false;
            }
        }
    }
}
