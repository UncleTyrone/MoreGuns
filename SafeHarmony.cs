using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace MoreGuns
{
    /// <summary>
    /// Applies Harmony patches one class at a time. On Il2Cpp, interop methods never have
    /// a .NET MethodBody so we cannot pre-check — we just try and catch failures.
    /// </summary>
    internal static class SafeHarmony
    {
        public static void Apply(HarmonyLib.Harmony harmony, params Type[] patchClasses)
        {
            foreach (Type patchClass in patchClasses)
            {
                try
                {
                    new PatchClassProcessor(harmony, patchClass).Patch();
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Harmony class {patchClass.Name} failed to bind: {ex.Message}");
                }
            }
        }
    }
}
