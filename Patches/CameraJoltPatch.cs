using HarmonyLib;
using MoreGuns.Guns;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class CameraJoltPatch
    {
        public static string ID = "";

        [HarmonyPatch(typeof(PlayerCamera), "JoltCamera")]
        [HarmonyPrefix]
        public static bool Prefix(PlayerCamera __instance)
        {
            if (WeaponBase.weaponsByName.TryGetValue(ID, out var gun))
            {
                return gun.settings == null || gun.settings.cameraJolt;
            }
            return true;
        }

        [HarmonyPatch(typeof(Equippable_RangedWeapon), "Fire")]
        [HarmonyPrefix]
        public static void Prefix(Equippable_RangedWeapon __instance)
        {
            try
            {
                ID = __instance.gameObject.name.Replace("_Equippable(Clone)", "").ToLower();
            }
            catch
            {
                ID = "";
            }
        }
    }
}
