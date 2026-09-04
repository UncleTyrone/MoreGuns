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
            WeaponBase source = GunSettings.ResolveWeaponBase(__instance);
            if (source == null)
                return true;

            string id = source.ID ?? "";

            if (string.Equals(id, "minigun", System.StringComparison.OrdinalIgnoreCase))
            {
                if (Config.AllowMinigunManualReload != null && Config.AllowMinigunManualReload.Value)
                {
                    ManualReload.TryReload(__instance, source, useGunReloadAnim: false);
                    return false;
                }

                ReloadMessage.Show("Take the MiniGun to Stan to reload.");
                return false;
            }

            GunTuning tuning = source.settings;
            if (tuning != null && !tuning.canManualyReload)
            {
                ReloadMessage.Show(true);
                return false;
            }

            GunSettings.EnsureOn(__instance);

            // RPG: ammo transfer only (no mag-out anim). AK/SMG/sniper: full gun reload clip.
            bool gunAnim = !string.Equals(id, "rpg", System.StringComparison.OrdinalIgnoreCase);
            ManualReload.TryReload(__instance, source, useGunReloadAnim: gunAnim);
            return false;
        }
    }
}
