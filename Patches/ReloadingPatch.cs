using HarmonyLib;
using MoreGuns.Gui;
using MoreGuns.Guns;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class ReloadingPatch
    {
        [HarmonyPatch(typeof(Equippable_RangedWeapon), nameof(Equippable_RangedWeapon.Reload))]
        [HarmonyPrefix]
        public static bool Prefix(Equippable_RangedWeapon __instance)
        {
            if (IsMinigun(__instance))
            {
                if (Config.AllowMinigunManualReload != null && Config.AllowMinigunManualReload.Value)
                    return true;

                ReloadMessage.Show("Take the MiniGun to Stan to reload.");
                return false;
            }

            GunSettings settings = __instance.gameObject.GetComponent<GunSettings>();
            if (settings != null && !settings.canManualyReload)
            {
                ReloadMessage.Show(true);
                return false;
            }

            return true;
        }

        private static bool IsMinigun(Equippable_RangedWeapon weapon)
        {
            if (weapon == null || weapon.gameObject == null)
                return false;

            string name = weapon.gameObject.name.ToLowerInvariant();
            return name.Contains("minigun");
        }
    }
}
