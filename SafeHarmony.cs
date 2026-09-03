using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace MoreGuns
{
    /// <summary>
    /// Applies Harmony patches one class at a time and refuses methods with no IL body.
    /// MonoMod's JIT hook (CompileMethodHook) fatals the CLR with 0x80131306 /
    /// CORPROF_E_FUNCTION_NOT_IL when it is asked to detour a Polyfill shim, a
    /// DynamicMethod, or any other non-IL member — which PatchAll will happily attempt.
    /// </summary>
    internal static class SafeHarmony
    {
        public static void Apply(HarmonyLib.Harmony harmony, params Type[] patchClasses)
        {
            foreach (Type patchClass in patchClasses)
            {
                try
                {
                    if (!AllTargetsHaveIL(patchClass))
                    {
                        MelonLogger.Warning($"Skipping Harmony class {patchClass.Name}: a target has no IL body.");
                        continue;
                    }

                    new PatchClassProcessor(harmony, patchClass).Patch();
                    MelonLogger.Msg($"Bound Harmony class {patchClass.Name}.");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Harmony class {patchClass.Name} failed to bind: {ex.Message}");
                }
            }
        }

        private static bool AllTargetsHaveIL(Type patchClass)
        {
            foreach (MethodInfo method in patchClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                HarmonyPatch[] attrs = (HarmonyPatch[])Attribute.GetCustomAttributes(method, typeof(HarmonyPatch));
                if (attrs.Length == 0)
                    continue;

                Type targetType = null;
                string targetName = null;
                foreach (HarmonyPatch attr in attrs)
                {
                    if (attr.info?.declaringType != null)
                        targetType = attr.info.declaringType;
                    if (!string.IsNullOrEmpty(attr.info?.methodName))
                        targetName = attr.info.methodName;
                }

                if (targetType == null || string.IsNullOrEmpty(targetName))
                    continue;

                MethodInfo target = AccessTools.Method(targetType, targetName);
                if (target == null)
                {
                    MelonLogger.Warning($"Skipping {patchClass.Name}.{method.Name}: {targetType.Name}.{targetName} was not found.");
                    return false;
                }

                if (!HasIL(target))
                {
                    MelonLogger.Warning($"Skipping {patchClass.Name}: {targetType.Name}.{targetName} has no IL body.");
                    return false;
                }
            }

            return true;
        }

        private static bool HasIL(MethodBase method)
        {
            if (method == null || method.IsAbstract || method.ContainsGenericParameters)
                return false;
            try
            {
                return method.GetMethodBody() != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
