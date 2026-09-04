using MelonLoader;
using MoreGuns.Guns;
using MoreGuns.Patches;
using UnityEngine;

namespace MoreGuns.Dialogue
{
    public static class StanDialogue
    {
        public const string stanDialogueMainOptions = "72f05df5-cfd4-4be3-9239-fc31bb44f49b";

        /// <summary>
        /// Stan "Reload Guns" — fills MoreGuns weapons in the hotbar from matching magazine
        /// stacks (hotbar + inventory). Supports multiple guns and multiple mag stacks.
        /// </summary>
        public static void StartSpecialGunReloads()
        {
            try
            {
                PlayerInventory inv = PlayerSingleton<PlayerInventory>.Instance;
                if (inv?.hotbarSlots == null)
                    return;

                PlayerCamera cam = PlayerSingleton<PlayerCamera>.Instance;

                for (int i = 0; i < inv.hotbarSlots.Count; i++)
                {
                    ItemSlot gunSlot = inv.hotbarSlots[i];
                    ItemInstance gunInst = gunSlot?.ItemInstance;
                    if (gunInst?.Definition == null)
                        continue;

                    string gunId = gunInst.Definition.ID;
                    if (string.IsNullOrEmpty(gunId)
                        || !WeaponBase.weaponsByName.TryGetValue(gunId, out WeaponBase source)
                        || source == null)
                        continue;

                    IntegerItemInstance gunAmmo = gunInst.As<IntegerItemInstance>();
                    if (gunAmmo == null)
                        continue;

                    int capacity = ResolveCapacity(source);
                    if (gunAmmo.Value >= capacity)
                        continue;

                    string magId = ResolveMagId(source);
                    if (string.IsNullOrEmpty(magId))
                        continue;

                    // Pull from any matching mag stacks until this gun is full.
                    int guard = 64;
                    while (gunAmmo.Value < capacity && guard-- > 0)
                    {
                        if (!TryFindMagazine(inv, magId, out ItemSlot magSlot, out IntegerItemInstance magAmmo))
                            break;

                        int available = magAmmo.Value;
                        if (available <= 0)
                        {
                            RemoveEmptyMag(magSlot);
                            SpawnMagTrash(source, cam);
                            continue;
                        }

                        int take = Mathf.Min(capacity - gunAmmo.Value, available);
                        int newAmmo = gunAmmo.Value + take;
                        gunAmmo.SetValue(newAmmo);
                        try { gunAmmo.Value = newAmmo; } catch { }

                        int left = available - take;
                        if (left <= 0)
                        {
                            magAmmo.SetValue(0);
                            try { magAmmo.Value = 0; } catch { }
                            RemoveEmptyMag(magSlot);
                            SpawnMagTrash(source, cam);
                        }
                        else
                        {
                            magAmmo.SetValue(left);
                            try { magAmmo.Value = left; } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception in StartSpecialGunReloads: {ex.Message}");
                MelonLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        private static int ResolveCapacity(WeaponBase source)
        {
            try
            {
                if (source.config?.MagazineSize != null)
                    return Mathf.Max(1, source.config.MagazineSize.Value);
            }
            catch { }

            try
            {
                if (source.gunRangedWeapon != null)
                    return Mathf.Max(1, source.gunRangedWeapon.MagazineSize);
            }
            catch { }

            return 30;
        }

        private static string ResolveMagId(WeaponBase source)
        {
            if (source.magIntItemDef != null && !string.IsNullOrEmpty(source.magIntItemDef.ID))
                return source.magIntItemDef.ID;
            if (!string.IsNullOrEmpty(source.ID))
                return source.ID + "mag";
            return null;
        }

        private static bool TryFindMagazine(
            PlayerInventory inv,
            string magId,
            out ItemSlot slot,
            out IntegerItemInstance magAmmo)
        {
            slot = null;
            magAmmo = null;

            if (inv.hotbarSlots != null)
            {
                for (int i = 0; i < inv.hotbarSlots.Count; i++)
                {
                    if (MatchMagSlot(inv.hotbarSlots[i], magId, out slot, out magAmmo))
                        return true;
                }
            }

            try
            {
                var all = inv.GetAllInventorySlots();
                if (all != null)
                {
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (MatchMagSlot(all[i], magId, out slot, out magAmmo))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool MatchMagSlot(
            ItemSlot s,
            string magId,
            out ItemSlot slot,
            out IntegerItemInstance magAmmo)
        {
            slot = null;
            magAmmo = null;
            ItemInstance inst = s?.ItemInstance;
            if (inst?.Definition == null)
                return false;
            if (!string.Equals(inst.Definition.ID, magId, StringComparison.OrdinalIgnoreCase))
                return false;

            IntegerItemInstance integer = inst.As<IntegerItemInstance>();
            if (integer == null)
                return false;

            slot = s;
            magAmmo = integer;
            return true;
        }

        private static void RemoveEmptyMag(ItemSlot magSlot)
        {
            if (magSlot?.ItemInstance == null)
                return;
            try { magSlot.ItemInstance.ChangeQuantity(-1); }
            catch
            {
                try { magSlot.ChangeQuantity(-1); }
                catch { }
            }
        }

        private static void SpawnMagTrash(WeaponBase source, PlayerCamera cam)
        {
            try
            {
                if (source == null || cam == null)
                    return;

                TrashRegistryPatch.EnsureTrashId(source);
                TrashItem prefab = source.gunMagTrashItem;
                if (prefab == null)
                    return;

                if (string.IsNullOrEmpty(prefab.ID)
                    || string.Equals(prefab.ID, "trashid", StringComparison.OrdinalIgnoreCase))
                    prefab.ID = source.ID + "mag";

                Vector3 position = cam.transform.position - cam.transform.up * 0.4f;
                // CreateTrashItem NREs when the trash ID was never registered under a real key.
                UnityEngine.Object.Instantiate(prefab.gameObject, position, UnityEngine.Random.rotation);
            }
            catch { }
        }
    }
}
