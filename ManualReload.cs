using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using UnityEngine;

namespace MoreGuns
{
    /// <summary>
    /// Ammo transfer that mirrors vanilla Reload timing/visuals without Mag Equippable instantiate.
    /// Vanilla: onReloadStart → MagazineReload → wait ReloadStartTime → then move ammo.
    /// </summary>
    internal static class ManualReload
    {
        public static bool TryReload(
            Equippable_RangedWeapon weapon,
            WeaponBase source = null,
            bool useGunReloadAnim = true)
        {
            if (weapon == null)
                return false;

            source ??= GunSettings.ResolveWeaponBase(weapon);

            try
            {
                try
                {
                    if (weapon.IsReloading)
                        GameAccess.Set(weapon, "IsReloading", false);
                }
                catch { }

                IntegerItemInstance gunAmmo = ResolveGunAmmo(weapon);
                if (gunAmmo == null)
                {
                    MelonLogger.Warning("ManualReload: no IntegerItemInstance on equipped weapon.");
                    return false;
                }

                int capacity = Mathf.Max(1, weapon.MagazineSize);
                if (source != null && source.config != null)
                    capacity = Mathf.Max(capacity, source.config.MagazineSize.Value);

                if (gunAmmo.Value >= capacity)
                    return true;

                string magId = ResolveMagazineId(weapon, source);
                if (string.IsNullOrEmpty(magId))
                {
                    MelonLogger.Warning("ManualReload: magazine ID is missing.");
                    return false;
                }

                if (!TryFindMagazine(magId, out ItemSlot magSlot, out IntegerItemInstance magAmmo))
                    return false;

                int available = magAmmo.Value;
                if (available <= 0)
                    return false;

                int take = Mathf.Min(capacity - gunAmmo.Value, available);
                float animSeconds = 2.5f;
                try
                {
                    if (weapon.ReloadStartTime > 0.2f)
                        animSeconds = weapon.ReloadStartTime;
                }
                catch { }

                MelonCoroutines.Start(ReloadRoutine(
                    weapon, source, gunAmmo, magSlot, magAmmo, take, capacity, animSeconds, useGunReloadAnim));
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"ManualReload failed: {ex}");
                return false;
            }
        }

        private static IEnumerator ReloadRoutine(
            Equippable_RangedWeapon weapon,
            WeaponBase source,
            IntegerItemInstance gunAmmo,
            ItemSlot magSlot,
            IntegerItemInstance magAmmo,
            int take,
            int capacity,
            float seconds,
            bool useGunReloadAnim)
        {
            try { GameAccess.Set(weapon, "IsReloading", true); } catch { }

            PlayReloadFeedback(weapon, source, useGunReloadAnim);

            float wait = Mathf.Clamp(seconds, 0.5f, 5f);
            bool isAk = source != null
                && string.Equals(source.ID, "ak47", StringComparison.OrdinalIgnoreCase);
            bool proceduralMag = useGunReloadAnim && !isAk;

            if (proceduralMag)
            {
                // AK reload clips break SMG/sniper sockets — animate the seated mag ourselves.
                yield return AnimateSeatedMagazine(weapon, wait);
            }
            else
            {
                yield return new WaitForSeconds(wait);
            }

            if (weapon == null || gunAmmo == null || magAmmo == null)
            {
                try { if (weapon != null) GameAccess.Set(weapon, "IsReloading", false); } catch { }
                yield break;
            }

            try
            {
                int newAmmo = Mathf.Min(capacity, gunAmmo.Value + take);
                int actuallyTook = newAmmo - gunAmmo.Value;
                if (actuallyTook <= 0)
                {
                    try { GameAccess.Set(weapon, "IsReloading", false); } catch { }
                    yield break;
                }

                gunAmmo.SetValue(newAmmo);
                try { gunAmmo.Value = newAmmo; } catch { }

                int left = magAmmo.Value - actuallyTook;
                bool emptiedMag = false;
                if (left <= 0)
                {
                    emptiedMag = true;
                    magAmmo.SetValue(0);
                    try { magAmmo.Value = 0; } catch { }

                    try { magSlot.ItemInstance.ChangeQuantity(-1); }
                    catch
                    {
                        try { magSlot.ChangeQuantity(-1); }
                        catch (Exception ex)
                        {
                            MelonLogger.Warning($"ManualReload: could not remove empty mag: {ex.Message}");
                        }
                    }
                }
                else
                {
                    magAmmo.SetValue(left);
                    try { magAmmo.Value = left; } catch { }
                }

                if (emptiedMag)
                    SpawnTrashSafe(weapon, source);

                if (weapon.MustBeCocked && weapon.AutoCockAfterReload)
                {
                    try { GameAccess.Set(weapon, "IsCocked", true); } catch { }
                    try { GameAccess.Set(weapon, "IsCocking", false); } catch { }
                }

                GunSettings settings = weapon.GetComponent<GunSettings>();
                if (settings != null && settings.SyncMagazineToAmmo)
                    WeaponBehavior.SyncMagazineVisualToAmmo(weapon);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"ManualReload apply failed: {ex}");
            }
            finally
            {
                try { GameAccess.Set(weapon, "IsReloading", false); } catch { }
            }
        }

        /// <summary>
        /// Match vanilla ReloadRoutine visuals: onReloadStart (gun clip + sound) + MagazineReload arms.
        /// Do not toggle Magazine GameObjects — AK47 Reload anim owns that (including the offset mag).
        /// </summary>
        private static void PlayReloadFeedback(
            Equippable_RangedWeapon weapon,
            WeaponBase source,
            bool useGunReloadAnim)
        {
            if (useGunReloadAnim)
            {
                bool isAk = source != null
                    && string.Equals(source.ID, "ak47", StringComparison.OrdinalIgnoreCase);

                if (isAk)
                {
                    try
                    {
                        if (weapon.onReloadStart != null)
                            weapon.onReloadStart.Invoke();
                    }
                    catch (Exception)
                    {
                        PlayGunReloadClip(weapon);
                        PlayReloadSound(weapon);
                    }
                }
                else
                {
                    // SMG/sniper: no AK/Other Reload clip (keys Magazine to scale ~10).
                    // Seated mag pop runs in AnimateSeatedMagazine during the wait.
                    try { MagazineSocketFix.FixGunHierarchy(weapon.gameObject); } catch { }
                    StopGunAnimation(weapon);
                    PlayReloadSound(weapon);
                }
            }
            else
            {
                PlayReloadSound(weapon);
            }

            string trigger = null;
            try { trigger = weapon.ReloadStartAnimTrigger; } catch { }
            if (string.IsNullOrEmpty(trigger))
                trigger = "MagazineReload";

            try
            {
                ViewmodelAvatar vm = ViewmodelAvatar.Instance;
                if (vm?.Animator != null)
                    vm.Animator.SetTrigger(trigger);
            }
            catch { }

            try
            {
                Player local = Player.Local;
                if (local != null)
                {
                    local.SetAnimationTrigger(trigger);
                    try { local.SendAnimationTrigger(trigger); } catch { }
                }
            }
            catch { }
        }

        private static void PlayReloadSound(Equippable_RangedWeapon weapon)
        {
            try
            {
                Transform reloadSound = FindDeep(weapon.transform, "Reload Sound");
                if (reloadSound != null)
                {
                    var src = reloadSound.GetComponent<AudioSourceController>();
                    if (src != null)
                        src.Play();
                }
            }
            catch { }
        }

        private static void StopGunAnimation(Equippable_RangedWeapon weapon)
        {
            try
            {
                Animation[] anims = weapon.GetComponentsInChildren<Animation>(true);
                for (int i = 0; i < anims.Length; i++)
                {
                    if (anims[i] != null && anims[i].isPlaying)
                        anims[i].Stop();
                }
            }
            catch { }
        }

        /// <summary>
        /// Mag-out / mag-in on the seated Magazine without AK animation curves.
        /// </summary>
        private static IEnumerator AnimateSeatedMagazine(Equippable_RangedWeapon weapon, float seconds)
        {
            Transform mag = null;
            Vector3 homePos = Vector3.zero;
            Quaternion homeRot = Quaternion.identity;
            Vector3 homeScale = Vector3.one;

            try
            {
                MagazineSocketFix.FixGunHierarchy(weapon.gameObject);
                mag = FindSeatedMagazine(weapon);
                if (mag != null)
                {
                    homePos = mag.localPosition;
                    homeRot = mag.localRotation;
                    homeScale = mag.localScale;
                }
            }
            catch (Exception)
            {
                // non-fatal setup failure — fall back to timed wait
            }

            if (mag == null)
            {
                yield return new WaitForSeconds(seconds);
                yield break;
            }

            // Local drop distance — SMG/sniper mags sit at scale ~0.1 under the receiver.
            float drop = Mathf.Clamp(0.22f + homeScale.y * 0.5f, 0.15f, 0.45f);
            Vector3 ejected = homePos + new Vector3(0f, -drop, 0f);

            float outTime = seconds * 0.32f;
            float holdTime = seconds * 0.20f;
            float naturalIn = Mathf.Max(0.2f, seconds - outTime - holdTime);
            // Seat-in felt slow — half the return tween, pad the rest so total reload time stays.
            float inTime = Mathf.Max(0.12f, naturalIn * 0.5f);
            float padAfter = Mathf.Max(0f, naturalIn - inTime);

            float t = 0f;
            while (t < outTime && mag != null)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / outTime));
                mag.localPosition = Vector3.Lerp(homePos, ejected, u);
                yield return null;
            }

            if (mag != null)
            {
                mag.localPosition = ejected;
                mag.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(holdTime);

            if (mag != null)
            {
                mag.localPosition = ejected;
                mag.localScale = homeScale;
                mag.localRotation = homeRot;
                mag.gameObject.SetActive(true);
            }

            t = 0f;
            while (t < inTime && mag != null)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / inTime));
                mag.localPosition = Vector3.Lerp(ejected, homePos, u);
                yield return null;
            }

            if (mag != null)
            {
                mag.localPosition = homePos;
                mag.localRotation = homeRot;
                mag.localScale = homeScale;
                mag.gameObject.SetActive(true);
            }

            if (padAfter > 0.01f)
                yield return new WaitForSeconds(padAfter);
        }

        private static Transform FindSeatedMagazine(Equippable_RangedWeapon weapon)
        {
            if (weapon == null)
                return null;

            Transform best = null;
            float bestScore = float.MaxValue;
            Transform[] all = weapon.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t.name != "Magazine")
                    continue;

                Vector3 s = t.localScale;
                Vector3 p = t.localPosition;
                // Skip leftover AK orphans (huge / far).
                if (s.x >= 5f || s.y >= 5f || s.z >= 5f
                    || Mathf.Abs(p.z) > 1f || Mathf.Abs(p.x) > 1.5f)
                    continue;

                if (t.GetComponent<MeshFilter>() == null
                    && t.GetComponentInChildren<MeshFilter>(true) == null)
                    continue;

                // Prefer the active seated mag closest to the gun root.
                float score = p.sqrMagnitude + (t.gameObject.activeSelf ? 0f : 100f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            return best;
        }

        private static void PlayGunReloadClip(Equippable_RangedWeapon weapon)
        {
            try
            {
                string[] names = { "AK47 Reload", "Other Reload", "Reload" };

                Animation[] anims = weapon.GetComponentsInChildren<Animation>(true);
                for (int a = 0; a < anims.Length; a++)
                {
                    Animation anim = anims[a];
                    if (anim == null)
                        continue;

                    for (int i = 0; i < names.Length; i++)
                    {
                        if (anim.GetClip(names[i]) == null)
                            continue;
                        anim.Play(names[i]);
                        return;
                    }
                }

                PlayAnimation helper = weapon.GetComponentInChildren<PlayAnimation>(true);
                if (helper != null)
                    helper.Play("AK47 Reload");
            }
            catch { }
        }

        private static string ResolveMagazineId(Equippable_RangedWeapon weapon, WeaponBase source)
        {
            try
            {
                if (weapon.Magazine != null && !string.IsNullOrEmpty(weapon.Magazine.ID))
                    return weapon.Magazine.ID;
            }
            catch { }

            if (source?.magIntItemDef != null && !string.IsNullOrEmpty(source.magIntItemDef.ID))
                return source.magIntItemDef.ID;

            if (source != null && !string.IsNullOrEmpty(source.ID))
                return source.ID + "mag";

            return null;
        }

        private static void SpawnTrashSafe(Equippable_RangedWeapon weapon, WeaponBase source)
        {
            try
            {
                PlayerCamera cam = PlayerSingleton<PlayerCamera>.Instance;
                if (cam == null)
                    return;

                TrashItem prefab = ResolveTrashPrefab(weapon, source);
                if (prefab == null)
                    return;

                if (source != null)
                    Patches.TrashRegistryPatch.EnsureTrashId(source);
                if (string.IsNullOrEmpty(prefab.ID)
                    || string.Equals(prefab.ID, "trashid", StringComparison.OrdinalIgnoreCase))
                {
                    if (source != null && !string.IsNullOrEmpty(source.ID))
                        prefab.ID = source.ID + "mag";
                }

                Vector3 position = cam.transform.position - cam.transform.up * 0.4f;
                try
                {
                    UnityEngine.Object.Instantiate(prefab.gameObject, position, UnityEngine.Random.rotation);
                }
                catch { }
            }
            catch { }
        }

        private static TrashItem ResolveTrashPrefab(Equippable_RangedWeapon weapon, WeaponBase source)
        {
            try
            {
                TrashItem linked = weapon.ReloadTrash;
                if (linked != null)
                    return linked;
            }
            catch { }

            return source?.gunMagTrashItem;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static IntegerItemInstance ResolveGunAmmo(Equippable_RangedWeapon weapon)
        {
            return GameAccess.Get<IntegerItemInstance>(weapon, "weaponItem");
        }

        private static bool TryFindMagazine(string magId, out ItemSlot slot, out IntegerItemInstance magAmmo)
        {
            slot = null;
            magAmmo = null;

            PlayerInventory inv = PlayerSingleton<PlayerInventory>.Instance;
            if (inv == null)
                return false;

            if (inv.hotbarSlots != null)
            {
                for (int i = 0; i < inv.hotbarSlots.Count; i++)
                {
                    if (MatchSlot(inv.hotbarSlots[i], magId, out slot, out magAmmo))
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
                        if (MatchSlot(all[i], magId, out slot, out magAmmo))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool MatchSlot(ItemSlot s, string magId, out ItemSlot slot, out IntegerItemInstance magAmmo)
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
    }
}
