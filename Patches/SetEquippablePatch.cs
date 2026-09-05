using HarmonyLib;
using MelonLoader;
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
                return true;
            if (!Tools.Alive(__instance))
                return false;

            // Vanilla assets (M1911, PumpShotgun, …) — leave alone.
            UnityEngine.Object resourceAsset = Resources.Load(assetPath);
            if (resourceAsset != null)
                return true;

            try
            {
                if (__instance.CurrentEquippable != null)
                    __instance.CurrentEquippable.Unequip();
            }
            catch { }

            UnityEngine.Object customAsset = MoreGunsMod.TryGetAsset(assetPath);
            if (customAsset == null)
                return true;

            GameObject prefab = customAsset.As<GameObject>();
            if (prefab == null)
                return true;

            GameObject equippable = UnityEngine.Object.Instantiate(prefab);
            if (equippable == null)
                return true;

            // Do NOT run MagazineSocketFix on third-person avatar clones — that scrub is for
            // local FP viewmodels only and was deleting remote/police gun meshes.

            AvatarEquippable avatarEquippable = ResolveAvatarEquippable(equippable);
            if (avatarEquippable == null)
            {
                MelonLogger.Warning($"SetEquippable: no AvatarEquippable on '{assetPath}'.");
                UnityEngine.Object.Destroy(equippable);
                return true;
            }

            // FishNet identity for ReceiveEquippableMessage / fire sounds on remotes.
            try { avatarEquippable.AssetPath = assetPath; }
            catch { GameAccess.Set(avatarEquippable, "AssetPath", assetPath); }

            // Bundle script is authored as AvatarGun and remapped to AvatarRangedWeapon;
            // serialized Transform/Audio refs often arrive null even though children exist.
            EnsureAvatarWeaponRefs(avatarEquippable);

            if (!AvatarReadyForEquip(__instance, avatarEquippable))
            {
                MelonLogger.Warning($"SetEquippable: avatar hand alignment not ready for '{assetPath}'.");
                UnityEngine.Object.Destroy(equippable);
                __result = null;
                return false;
            }

            try
            {
                GameAccess.SetCurrentEquippable(__instance, avatarEquippable);
                avatarEquippable.Equip(__instance);
                __result = avatarEquippable;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"SetEquippable equip failed for '{assetPath}': {ex.Message}");
                try { GameAccess.SetCurrentEquippable(__instance, null); } catch { }
                try { UnityEngine.Object.Destroy(equippable); } catch { }
                __result = null;
                return false;
            }

            return false;
        }

        /// <summary>
        /// AvatarEquippable.PositionAnimationModel NREs when AlignmentPoint or the avatar's
        /// hand alignment bone is missing. Skip positioning instead of aborting Equip.
        /// </summary>
        [HarmonyPatch(typeof(AvatarEquippable), "PositionAnimationModel")]
        [HarmonyPrefix]
        public static bool PositionAnimationModelPrefix(AvatarEquippable __instance)
        {
            if (!Tools.Alive(__instance))
                return false;

            EnsureAlignmentPoint(__instance);
            if (!Tools.Alive(__instance.AlignmentPoint))
                return false;

            GameAvatar avatar = GameAccess.Get<GameAvatar>(__instance, "avatar");
            if (!Tools.Alive(avatar) || avatar.Animation == null)
                return false;

            Transform handAlign = ResolveHandAlignment(avatar, __instance);
            return Tools.Alive(handAlign);
        }

        /// <summary>
        /// Fire() sends networked Shoot before applying damage. Null FireSound in Shoot throws
        /// and aborts Fire mid-way — which is why custom guns dealt no damage.
        /// </summary>
        [HarmonyPatch(typeof(AvatarRangedWeapon), "Shoot")]
        [HarmonyPrefix]
        public static bool ShootPrefix(AvatarRangedWeapon __instance)
        {
            if (!Tools.Alive(__instance))
                return false;

            EnsureAvatarWeaponRefs(__instance);
            if (Tools.Alive(__instance.FireSound))
                return true;

            // Skip vanilla Shoot (FireSound.DuplicateAndPlayOneShot would NRE). Damage still
            // applies in Equippable_RangedWeapon.Fire after this message returns.
            try
            {
                if (!string.IsNullOrEmpty(__instance.RecoilAnimationTrigger))
                {
                    GameAccess.Call(__instance, "ResetTrigger", __instance.RecoilAnimationTrigger);
                    GameAccess.Call(__instance, "SetTrigger", __instance.RecoilAnimationTrigger);
                }
            }
            catch { }

            return false;
        }

        internal static void EnsureAvatarWeaponRefs(AvatarEquippable equippable)
        {
            EnsureAlignmentPoint(equippable);
            EnsureRifleHoldAnimations(equippable);

            AvatarRangedWeapon ranged = null;
            try { ranged = equippable as AvatarRangedWeapon; }
            catch { }
            if (!Tools.Alive(ranged))
            {
                try { ranged = equippable.GetComponent<AvatarRangedWeapon>(); }
                catch { }
            }
            if (!Tools.Alive(ranged))
                return;

            EnsureMuzzlePoint(ranged);
            EnsureFireSound(ranged);
            EnsureRangedAnimationTriggers(ranged);
        }

        /// <summary>
        /// AvatarGun→AvatarRangedWeapon remap drops hold poses. Force two-hand rifle grips for
        /// all MoreGuns long guns (AK/SMG/sniper/minigun/RPG) — same path cops and co-op remotes use.
        /// </summary>
        private static string _rifleHold;
        private static string _rifleLowered;
        private static string _rifleRaised;
        private static string _rifleRecoil;
        private static bool _rifleAnimCached;
        private static bool _rifleTriggerTypeValid;
        private static AvatarEquippable.ETriggerType _rifleTriggerType;

        private static void EnsureRifleHoldAnimations(AvatarEquippable equippable)
        {
            if (!Tools.Alive(equippable))
                return;
            if (!IsLongGunEquippable(equippable))
                return;

            CacheRifleAnimationsFromVanilla();

            if (!string.IsNullOrEmpty(_rifleHold))
            {
                try { equippable.AnimationTrigger = _rifleHold; }
                catch { GameAccess.Set(equippable, "AnimationTrigger", _rifleHold); }
            }

            if (_rifleTriggerTypeValid)
            {
                try { equippable.TriggerType = _rifleTriggerType; }
                catch { GameAccess.Set(equippable, "TriggerType", _rifleTriggerType); }
            }
        }

        private static void EnsureRangedAnimationTriggers(AvatarRangedWeapon ranged)
        {
            if (!Tools.Alive(ranged))
                return;
            if (!IsLongGunEquippable(ranged))
                return;

            CacheRifleAnimationsFromVanilla();

            ForceAnim(ranged, "LoweredAnimationTrigger", _rifleLowered);
            ForceAnim(ranged, "RaisedAnimationTrigger", _rifleRaised);
            ForceAnim(ranged, "RecoilAnimationTrigger", _rifleRecoil);
        }

        private static bool IsLongGunEquippable(AvatarEquippable equippable)
        {
            if (!Tools.Alive(equippable))
                return false;

            string path = null;
            try { path = equippable.AssetPath; }
            catch { path = GameAccess.Get<string>(equippable, "AssetPath"); }

            if (IsLongGunPath(path))
                return true;

            // Prefab name before AssetPath is wired (WireLoadedAssets).
            try
            {
                string name = equippable.gameObject != null ? equippable.gameObject.name : null;
                if (IsLongGunPath(name))
                    return true;
            }
            catch { }

            // All MoreGuns avatar guns are long guns today — treat any remapped ranged TP gun
            // that still has the pistol default as needing the two-hand pose.
            try
            {
                string trigger = equippable.AnimationTrigger;
                if (string.IsNullOrEmpty(trigger)
                    || string.Equals(trigger, "RightArm_Hold_ClosedHand", StringComparison.OrdinalIgnoreCase))
                    return equippable is AvatarRangedWeapon;
            }
            catch { }

            return false;
        }

        private static bool IsLongGunPath(string pathOrName)
        {
            if (string.IsNullOrEmpty(pathOrName))
                return false;
            return pathOrName.IndexOf("ak47", StringComparison.OrdinalIgnoreCase) >= 0
                || pathOrName.IndexOf("smg", StringComparison.OrdinalIgnoreCase) >= 0
                || pathOrName.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0
                || pathOrName.IndexOf("minigun", StringComparison.OrdinalIgnoreCase) >= 0
                || pathOrName.IndexOf("rpg", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ForceAnim(AvatarRangedWeapon ranged, string field, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            try
            {
                switch (field)
                {
                    case "LoweredAnimationTrigger":
                        ranged.LoweredAnimationTrigger = value;
                        break;
                    case "RaisedAnimationTrigger":
                        ranged.RaisedAnimationTrigger = value;
                        break;
                    case "RecoilAnimationTrigger":
                        ranged.RecoilAnimationTrigger = value;
                        break;
                    default:
                        GameAccess.Set(ranged, field, value);
                        break;
                }
            }
            catch { GameAccess.Set(ranged, field, value); }
        }

        private static void CacheRifleAnimationsFromVanilla()
        {
            if (_rifleAnimCached)
                return;
            _rifleAnimCached = true;

            try
            {
                UnityEngine.Object loaded = Resources.Load("Avatar/Equippables/PumpShotgun");
                GameObject prefab = loaded != null ? loaded.As<GameObject>() : null;
                AvatarRangedWeapon template = null;
                if (prefab != null)
                {
                    template = prefab.GetComponent<AvatarRangedWeapon>()
                        ?? prefab.GetComponentInChildren<AvatarRangedWeapon>(true);
                }

                if (Tools.Alive(template))
                {
                    try { _rifleHold = template.AnimationTrigger; } catch { }
                    try { _rifleLowered = template.LoweredAnimationTrigger; } catch { }
                    try { _rifleRaised = template.RaisedAnimationTrigger; } catch { }
                    try { _rifleRecoil = template.RecoilAnimationTrigger; } catch { }
                    try
                    {
                        _rifleTriggerType = template.TriggerType;
                        _rifleTriggerTypeValid = true;
                    }
                    catch { }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(_rifleHold))
                _rifleHold = "BothHands_Grip_Lowered";
            if (string.IsNullOrEmpty(_rifleLowered))
                _rifleLowered = "BothHands_Grip_Lowered";
            if (string.IsNullOrEmpty(_rifleRaised))
                _rifleRaised = "BothHands_Grip_Raised";
            if (string.IsNullOrEmpty(_rifleRecoil))
                _rifleRecoil = "BothHands_Grip_Recoil";
        }

        internal static void EnsureAlignmentPoint(AvatarEquippable equippable)
        {
            if (!Tools.Alive(equippable))
                return;

            try
            {
                if (Tools.Alive(equippable.AlignmentPoint))
                    return;
            }
            catch { }

            Transform found = FindNamedTransform(equippable.transform, "AlignmentPoint");
            if (found == null)
            {
                GameObject go = new GameObject("AlignmentPoint");
                go.transform.SetParent(equippable.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                found = go.transform;
            }

            try { equippable.AlignmentPoint = found; }
            catch { GameAccess.Set(equippable, "AlignmentPoint", found); }
        }

        private static void EnsureMuzzlePoint(AvatarRangedWeapon ranged)
        {
            try
            {
                if (Tools.Alive(ranged.MuzzlePoint))
                    return;
            }
            catch { }

            Transform found = FindNamedTransform(ranged.transform, "MuzzlePoint");
            if (found == null)
                found = ranged.transform;

            try { ranged.MuzzlePoint = found; }
            catch { GameAccess.Set(ranged, "MuzzlePoint", found); }
        }

        private static void EnsureFireSound(AvatarRangedWeapon ranged)
        {
            try
            {
                if (Tools.Alive(ranged.FireSound))
                    return;
            }
            catch { }

            AudioSourceController sound = null;
            Transform fireNamed = FindNamedTransform(ranged.transform, "Fire Sound")
                ?? FindNamedTransform(ranged.transform, "FireSound");
            if (fireNamed != null)
            {
                try
                {
                    sound = fireNamed.GetComponent<AudioSourceController>()
                        ?? fireNamed.GetComponentInChildren<AudioSourceController>(true);
                }
                catch { }
            }

            if (!Tools.Alive(sound))
            {
                try
                {
                    sound = ranged.GetComponentInChildren<AudioSourceController>(true);
                }
                catch { }
            }

            if (!Tools.Alive(sound))
                return;

            try { ranged.FireSound = sound; }
            catch { GameAccess.Set(ranged, "FireSound", sound); }
        }

        private static bool AvatarReadyForEquip(GameAvatar avatar, AvatarEquippable equippable)
        {
            if (!Tools.Alive(avatar) || avatar.Animation == null)
                return false;

            Transform container = ResolveHandContainer(avatar, equippable);
            Transform align = ResolveHandAlignment(avatar, equippable);
            return Tools.Alive(container) && Tools.Alive(align);
        }

        private static Transform ResolveHandContainer(GameAvatar avatar, AvatarEquippable equippable)
        {
            try
            {
                if (equippable.Hand == AvatarEquippable.EHand.Left)
                    return avatar.Animation.LeftHandContainer;
                return avatar.Animation.RightHandContainer;
            }
            catch
            {
                return null;
            }
        }

        private static Transform ResolveHandAlignment(GameAvatar avatar, AvatarEquippable equippable)
        {
            try
            {
                if (equippable.Hand == AvatarEquippable.EHand.Left)
                    return avatar.Animation.LeftHandAlignmentPoint;
                return avatar.Animation.RightHandAlignmentPoint;
            }
            catch
            {
                return null;
            }
        }

        private static Transform FindNamedTransform(Transform root, string name)
        {
            if (root == null)
                return null;

            Transform direct = root.Find(name);
            if (direct != null)
                return direct;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t != null && t.name == name)
                    return t;
            }

            return null;
        }

        private static AvatarEquippable ResolveAvatarEquippable(GameObject root)
        {
            if (root == null)
                return null;

            AvatarEquippable avatar = root.GetComponent<AvatarEquippable>();
            if (avatar != null)
                return avatar;

            // AvatarRangedWeapon : AvatarWeapon : AvatarEquippable — may live on a child.
            try
            {
                AvatarRangedWeapon ranged = root.GetComponent<AvatarRangedWeapon>()
                    ?? root.GetComponentInChildren<AvatarRangedWeapon>(true);
                if (ranged != null)
                    return ranged;
            }
            catch { }

            return root.GetComponentInChildren<AvatarEquippable>(true);
        }
    }
}
