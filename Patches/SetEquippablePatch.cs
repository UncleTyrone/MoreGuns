using HarmonyLib;
using UnityEngine;
#if IL2CPP
using GameAvatar = Il2CppScheduleOne.AvatarFramework.Avatar;
#else
using GameAvatar = ScheduleOne.AvatarFramework.Avatar;
#endif

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class SetEquippablePatch
    {
        [HarmonyPatch(typeof(GameAvatar), nameof(GameAvatar.SetEquippable))]
        [HarmonyPrefix]
        public static bool Prefix(ref AvatarEquippable __result, string assetPath, GameAvatar __instance)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return true;
            }
            if (!Tools.Alive(__instance))
            {
                return false;
            }

            UnityEngine.Object resourceAsset = Resources.Load(assetPath);
            if (resourceAsset != null)
            {
                return true;
            }

            try
            {
                if (__instance.CurrentEquippable != null)
                    __instance.CurrentEquippable.Unequip();
            }
            catch
            {
                // current equippable already torn down
            }

            UnityEngine.Object customAsset = MoreGunsMod.TryGetAsset(assetPath);
            if (customAsset == null)
            {
                return true;
            }

            GameObject prefab = customAsset.As<GameObject>();
            if (prefab == null)
            {
                return true;
            }

            GameObject equippable = UnityEngine.Object.Instantiate(prefab);
            if (equippable == null)
            {
                return true;
            }

            AvatarEquippable avatarEquippable = equippable.GetComponent<AvatarEquippable>();
            if (avatarEquippable == null)
            {
                return true;
            }

            try
            {
                GameAccess.SetCurrentEquippable(__instance, avatarEquippable);
                avatarEquippable.Equip(__instance);
                __result = avatarEquippable;
            }
            catch
            {
                return true;
            }

            return false;
        }
    }
}
