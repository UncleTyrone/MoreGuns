using HarmonyLib;
using MoreGuns.Gui;
using MoreGuns.Guns;

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
            GunSettings settings = __instance.GetComponent<GunSettings>();
            if (settings == null)
                return;

            Reticle.SetActive(Config.EnableCrosshairForGuns.Value);

            if (settings.requireWindup)
                WindupIndicator.Show(true);

            SetMoveSpeedMultiplier(settings.speedMultiplier);
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
