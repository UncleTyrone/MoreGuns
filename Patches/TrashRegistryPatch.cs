using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using UnityEngine;

namespace MoreGuns.Patches
{
    public static class TrashRegistryPatch
    {
        private const float READY_POLL_INTERVAL = 0.25F;
        private const float READY_TIMEOUT = 60F;
        private const float WATCH_INTERVAL = 0.5F;

        private static bool registered;

        public static IEnumerator Watch()
        {
            registered = false;
            float waited = 0F;
            while (!registered && waited < READY_TIMEOUT)
            {
                TrashManager manager = FindManager();
                if (manager != null)
                {
                    yield return RegisterWhenReady(manager);
                    yield break;
                }

                waited += WATCH_INTERVAL;
                yield return new WaitForSeconds(WATCH_INTERVAL);
            }
        }

        private static TrashManager FindManager()
        {
            try
            {
                return UnityEngine.Object.FindObjectOfType<TrashManager>();
            }
            catch
            {
                return null;
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

            registered = true;
        }
    }
}
