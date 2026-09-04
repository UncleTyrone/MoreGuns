using HarmonyLib;
using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using System.Collections.Generic;
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

        public static void EnsureTrashId(WeaponBase weapon)
        {
            if (weapon?.gunMagTrashItem == null || string.IsNullOrEmpty(weapon.ID))
                return;

            string want = weapon.ID + "mag";
            string current = weapon.gunMagTrashItem.ID;
            // Duplicates ship as empty or the placeholder "trashid".
            if (string.IsNullOrEmpty(current)
                || string.Equals(current, "trashid", System.StringComparison.OrdinalIgnoreCase))
            {
                weapon.gunMagTrashItem.ID = want;
            }
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

            List<TrashItem> allTrashItems = manager.TrashPrefabs != null
                ? manager.TrashPrefabs.ToList()
                : new List<TrashItem>();
            int added = 0;

            foreach (WeaponBase weapon in WeaponBase.allWeapons)
            {
                TrashItem trash = weapon.gunMagTrashItem;
                if (trash == null)
                    continue;

                EnsureTrashId(weapon);

                if (allTrashItems.Contains(trash))
                    continue;

                allTrashItems.Add(trash);
                added++;
            }

            if (added > 0 || allTrashItems.Count > 0)
                manager.TrashPrefabs = allTrashItems.ToArray();
        }
    }
}
