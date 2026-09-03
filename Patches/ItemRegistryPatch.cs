using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using UnityEngine;

namespace MoreGuns.Patches
{
    public static class ItemRegistryPatch
    {
        private static bool isRegistering;

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

            for (int i = 0; i < 30; i++)
            {
                RegisterWeapons();
                yield return new WaitForSeconds(2F);
            }
        }

        public static void RegisterWeapons()
        {
            Registry registry = null;
            try { registry = Registry.Instance; }
            catch { /* singleton name varies */ }
            RegisterInto(registry);
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
                // ItemExists failed; try adding anyway.
            }

            try { registry.AddToRegistry(definition); }
            catch { GameAccess.Call(registry, "AddToRegistry", definition); }
            return true;
        }
    }
}
