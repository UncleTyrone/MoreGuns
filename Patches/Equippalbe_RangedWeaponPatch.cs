using HarmonyLib;
using MoreGuns.Guns;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class Equippalbe_RangedWeaponPatch
    {
        [HarmonyPatch(typeof(Equippable_RangedWeapon), "Fire")]
        [HarmonyPrefix]
        public static bool Prefix(Equippable_RangedWeapon __instance)
        {
            if (!Tools.IsLocalPlayerHeld(__instance))
                return true;

            GunSettings settings = Tools.SettingsOf(__instance);
            if (settings != null && settings.requireWindup && GunSettings.WindupElapsed < settings.windupTime)
                return false;

            return true;
        }
    }
}
