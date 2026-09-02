using HarmonyLib;
using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class ItemRegistryPatch
    {
        private static bool isRegistering;

        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Registry), "_GetItem", new[] { typeof(string), typeof(bool) });
        }

        // Registry.RemoveRuntimeItems() wipes anything AddToRegistry added. Re-inject our
        // guns on the actual lookup so give ak47 still works after a scene/host load.
        public static void Prefix(Registry __instance, string ID)
        {
            if (isRegistering || __instance == null || !IsOurId(ID))
                return;
            RegisterInto(__instance);
        }

        public static void Reset()
        {
            isRegistering = false;
        }

        public static IEnumerator RegisterWhenReady()
        {
            float waited = 0F;
            while (!WeaponBase.AllWeaponsLoaded || WeaponBase.allWeapons.Count == 0)
            {
                if (waited >= 60F)
                    break;
                waited += 0.25F;
                yield return new WaitForSeconds(0.25F);
            }

            RegisterWeapons();
        }

        public static void RegisterWeapons()
        {
            Registry registry = null;
            try { registry = Registry.Instance; }
            catch { /* singleton name varies */ }
            RegisterInto(registry);
        }

        private static bool IsOurId(string id)
        {
            if (string.IsNullOrEmpty(id) || WeaponBase.weaponsByName.Count == 0)
                return false;
            if (WeaponBase.weaponsByName.ContainsKey(id))
                return true;
            if (id.Length > 3 && id.EndsWith("mag", StringComparison.OrdinalIgnoreCase))
            {
                string gunId = id.Substring(0, id.Length - 3);
                if (WeaponBase.weaponsByName.ContainsKey(gunId))
                    return true;
            }
            foreach (string key in WeaponBase.weaponsByName.Keys)
            {
                if (string.Equals(key, id, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(key + "mag", id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void RegisterInto(Registry registry)
        {
            if (registry == null || isRegistering || WeaponBase.allWeapons.Count == 0)
                return;

            isRegistering = true;
            try
            {
                foreach (WeaponBase weapon in WeaponBase.allWeapons)
                {
                    if (weapon?.gunIntItemDef == null || string.IsNullOrEmpty(weapon.ID))
                        continue;

                    weapon.gunIntItemDef.ID = weapon.ID;
                    if (weapon.magIntItemDef != null)
                        weapon.magIntItemDef.ID = weapon.ID + "mag";

                    bool addedGun = AddIfMissing(registry, weapon.gunIntItemDef);
                    bool addedMag = AddIfMissing(registry, weapon.magIntItemDef);
                    if (addedGun || addedMag)
                        MelonLogger.Msg($"Registered {weapon.ID} item definition and magazine.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to register More Guns items: {ex}");
            }
            finally
            {
                isRegistering = false;
            }
        }

        private static bool AddIfMissing(Registry registry, ItemDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.ID))
                return false;
            try
            {
                if (Registry.ItemExists(definition.ID))
                    return false;
            }
            catch
            {
                // ItemExists goes through _GetItem; if that fails, try adding anyway.
            }

            try { registry.AddToRegistry(definition); }
            catch { GameAccess.Call(registry, "AddToRegistry", definition); }
            return true;
        }
    }
}
