using HarmonyLib;
using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using UnityEngine;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class TrashRegistryPatch
    {
        private const float READY_POLL_INTERVAL = 0.25F;
        private const float READY_TIMEOUT = 60F;

        [HarmonyPatch(typeof(TrashManager), "Start")]
        [HarmonyPostfix]
        public static void Postfix(TrashManager __instance)
        {
            MelonCoroutines.Start(RegisterWhenReady(__instance));
        }

        private static IEnumerator RegisterWhenReady(TrashManager manager)
        {
            float waited = 0F;
            while (!WeaponBase.AllWeaponsLoaded && waited < READY_TIMEOUT)
            {
                if (manager == null)
                    yield break;

                waited += READY_POLL_INTERVAL;
                yield return new WaitForSeconds(READY_POLL_INTERVAL);
            }

            if (manager == null)
                yield break;

            List<TrashItem> allTrashItems = manager.TrashPrefabs.ToList();
            int added = 0;

            foreach (WeaponBase weapon in WeaponBase.allWeapons)
            {
                if (weapon.gunMagTrashItem == null || allTrashItems.Contains(weapon.gunMagTrashItem))
                    continue;

                allTrashItems.Add(weapon.gunMagTrashItem);
                added++;
            }

            if (added > 0)
            {
                manager.TrashPrefabs = allTrashItems.ToArray();
                MelonLogger.Msg($"Added {added} magazine trash prefab(s) to the TrashManager.");
            }
        }
    }
}
