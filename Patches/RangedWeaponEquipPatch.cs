using HarmonyLib;
using MoreGuns.Gui;
using MoreGuns.Guns;
using UnityEngine;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class RangedWeaponEquipPatch
    {
        private const string MOVE_SPEED_LABEL = "MoreGuns";

        [HarmonyPatch(typeof(Equippable_RangedWeapon), nameof(Equippable_RangedWeapon.Equip))]
        [HarmonyPostfix]
        public static void PostfixEquip(Equippable_RangedWeapon __instance)
        {
            WeaponBase source = GunSettings.ResolveWeaponBase(__instance);

            // Scrub leftovers even if GunSettings attach fails.
            if (source != null
                && !string.Equals(source.ID, "ak47", System.StringComparison.OrdinalIgnoreCase)
                && !string.Equals(source.ID, "minigun", System.StringComparison.OrdinalIgnoreCase))
            {
                try { MagazineSocketFix.FixGunHierarchy(__instance.gameObject); }
                catch { }

                try { MuzzleAligner.Align(__instance.gameObject, source.name ?? source.ID); }
                catch { }
            }

            GunSettings settings = GunSettings.EnsureOn(__instance);
            if (settings == null)
                return;

            Reticle.SetActive(Config.EnableCrosshairForGuns.Value);

            if (settings.RequireWindup)
                WindupIndicator.Show(true);

            SetMoveSpeedMultiplier(settings.SpeedMultiplier);

            if (settings.SyncMagazineToAmmo)
                WeaponBehavior.SyncMagazineVisualToAmmo(__instance);
        }

        [HarmonyPatch(typeof(Equippable_RangedWeapon), nameof(Equippable_RangedWeapon.Unequip))]
        [HarmonyPostfix]
        public static void PostfixUnequip(Equippable_RangedWeapon __instance)
        {
            Reticle.SetActive(false);
            WindupIndicator.Show(false);
            ClearMoveSpeedMultiplier();
        }

        private static void SetMoveSpeedMultiplier(float multiplier)
        {
            FloatStack stack = PlayerSingleton<PlayerMovement>.Instance?.MoveSpeedMultiplierStack;
            if (stack == null)
                return;

            stack.Remove(MOVE_SPEED_LABEL);
            stack.Add(new FloatStack.StackEntry(MOVE_SPEED_LABEL, multiplier, FloatStack.EStackMode.Multiplicative, 0));
            GameAccess.Recalculate(stack);
        }

        private static void ClearMoveSpeedMultiplier()
        {
            FloatStack stack = PlayerSingleton<PlayerMovement>.Instance?.MoveSpeedMultiplierStack;
            if (stack == null)
                return;

            stack.Remove(MOVE_SPEED_LABEL);
            GameAccess.Recalculate(stack);
        }
    }
}
