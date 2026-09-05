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
                // Equip() can overwrite AnimationTrigger with the prefab's one-hand pistol hold.
                EnsureAvatarWeaponRefs(avatarEquippable);
                MelonCoroutines.Start(ReapplyHoldNextFrame(avatarEquippable));
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

        private static System.Collections.IEnumerator ReapplyHoldNextFrame(AvatarEquippable equippable)
        {
            yield return null;
            yield return null;
            if (!Tools.Alive(equippable))
                yield break;
            EnsureAvatarWeaponRefs(equippable);
            // Nudge the animator onto the two-hand grip if it still has the pistol hold.
            try
            {
                string hold = equippable.AnimationTrigger;
                if (!string.IsNullOrEmpty(hold))
                {
                    GameAccess.Call(equippable, "ResetTrigger", hold);
                    GameAccess.Call(equippable, "SetTrigger", hold);
                }
            }
            catch { }
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
        /// NPC/avatar shots go through Shoot(Vector3). Null FireSound used to NRE and abort the
        /// whole method (no recoil/ammo). Skipping Shoot also killed any muzzle/flash feedback.
        /// Always ensure refs, then let vanilla run; spawn a visible tracer for non-local avatars.
        /// </summary>
        [HarmonyPatch(typeof(AvatarRangedWeapon), "Shoot", new[] { typeof(Vector3) })]
        [HarmonyPrefix]
        public static bool ShootPrefix(AvatarRangedWeapon __instance, Vector3 endPoint)
        {
            if (!Tools.Alive(__instance))
                return false;

            EnsureAvatarWeaponRefs(__instance);

            // Still no sound — don't enter vanilla Shoot (DuplicateAndPlayOneShot NREs).
            // Postfix still runs and spawns the visible tracer.
            if (!Tools.Alive(__instance.FireSound))
            {
                ManualAvatarShoot(__instance);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(AvatarRangedWeapon), "Shoot", new[] { typeof(Vector3) })]
        [HarmonyPostfix]
        public static void ShootPostfix(AvatarRangedWeapon __instance, Vector3 endPoint)
        {
            SpawnAvatarBulletTrail(__instance, endPoint);
        }

        /// <summary>
        /// Public entry for NPC Shoot messages — more reliable on Il2Cpp than patching protected Shoot.
        /// </summary>
        [HarmonyPatch(typeof(GameAvatar), nameof(GameAvatar.ReceiveEquippableMessage))]
        [HarmonyPostfix]
        public static void ReceiveEquippableMessagePostfix(GameAvatar __instance, string message, object data)
        {
            if (!Tools.Alive(__instance))
                return;
            if (!string.Equals(message, "Shoot", System.StringComparison.Ordinal))
                return;
            if (!TryReadVector3(data, out Vector3 endPoint))
                return;

            AvatarRangedWeapon ranged = null;
            try
            {
                AvatarEquippable eq = __instance.CurrentEquippable;
                ranged = eq as AvatarRangedWeapon;
                if (!Tools.Alive(ranged) && Tools.Alive(eq))
                    ranged = eq.GetComponent<AvatarRangedWeapon>();
            }
            catch { }

            SpawnAvatarBulletTrail(ranged, endPoint);
        }

        private static void ManualAvatarShoot(AvatarRangedWeapon ranged)
        {
            try { ranged.Attack(); }
            catch
            {
                try { GameAccess.Call(ranged, "Attack"); }
                catch { }
            }

            try
            {
                if (!string.IsNullOrEmpty(ranged.RecoilAnimationTrigger))
                {
                    GameAccess.Call(ranged, "ResetTrigger", ranged.RecoilAnimationTrigger);
                    GameAccess.Call(ranged, "SetTrigger", ranged.RecoilAnimationTrigger);
                }
            }
            catch { }

            try { GameAccess.Set(ranged, "timeSinceLastShot", 0f); }
            catch { }
        }

        private static bool TryReadVector3(object data, out Vector3 value)
        {
            value = default;
            if (data == null)
                return false;
            try
            {
                if (data is Vector3 v)
                {
                    value = v;
                    return true;
                }
            }
            catch { }

            try
            {
                value = (Vector3)data;
                return true;
            }
            catch { }

#if IL2CPP
            try
            {
                if (data is Il2CppSystem.Object boxed)
                {
                    value = boxed.Unbox<Vector3>();
                    return true;
                }
            }
            catch { }
#endif
            return false;
        }

        private static void SpawnAvatarBulletTrail(AvatarRangedWeapon ranged, Vector3 endPoint)
        {
            if (!Tools.Alive(ranged))
                return;

            // Local player FP Fire already creates a trail — don't double it on the TP avatar.
            try
            {
                Player owner = ranged.GetComponentInParent<Player>();
                if (owner != null && owner.IsOwner)
                    return;
            }
            catch { }

            // Deduplicate Shoot postfix + ReceiveEquippableMessage postfix on the same shot.
            int id;
            try { id = ranged.GetInstanceID(); }
            catch { id = 0; }
            float now = Time.unscaledTime;
            if (id != 0
                && id == _lastTrailWeaponId
                && now - _lastTrailTime < 0.04f
                && (endPoint - _lastTrailEnd).sqrMagnitude < 0.01f)
                return;
            _lastTrailWeaponId = id;
            _lastTrailTime = now;
            _lastTrailEnd = endPoint;

            // Never let player-muzzle redirect steal this NPC tracer origin.
            MuzzleAligner.RedirectPlayerTrail = false;

            Transform muzzle = null;
            try { muzzle = ranged.MuzzlePoint; }
            catch { }
            if (!Tools.Alive(muzzle))
                muzzle = ranged.transform;

            Vector3 start = muzzle.position;
            Vector3 delta = endPoint - start;
            float dist = delta.magnitude;
            if (dist < 0.08f)
                return;

            Vector3 dir = delta / dist;
            float speed = 95f;
            // Use the aim point distance — MaxUseRange can be huge and confuses the pool raycast.
            float range = dist;

            try
            {
                if (!Singleton<FXManager>.InstanceExists)
                    return;
                FXManager fx = Singleton<FXManager>.Instance;
                if (fx == null)
                    return;

                LayerMask mask = ~0;
                try
                {
                    if (NetworkSingleton<CombatManager>.InstanceExists)
                        mask = NetworkSingleton<CombatManager>.Instance.RangedWeaponLayerMask;
                }
                catch { }

                fx.CreateBulletTrail(start, dir, speed, range, mask);
            }
            catch { }
        }

        private static int _lastTrailWeaponId;
        private static float _lastTrailTime;
        private static Vector3 _lastTrailEnd;

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
            EnsureUseRanges(ranged);
            EnsureSuccessfulHitEvent(ranged);
        }

        /// <summary>
        /// AvatarGun→AvatarRangedWeapon remap often leaves onSuccessfulHit null. CombatBehaviour
        /// ClearWeapon/SetWeapon call Add/RemoveListener and NRE without this UnityEvent.
        /// </summary>
        private static void EnsureSuccessfulHitEvent(AvatarWeapon weapon)
        {
            if (!Tools.Alive(weapon))
                return;
            try
            {
                if (weapon.onSuccessfulHit != null)
                    return;
            }
            catch { }

            try
            {
                weapon.onSuccessfulHit = new UnityEngine.Events.UnityEvent();
            }
            catch
            {
                try { GameAccess.Set(weapon, "onSuccessfulHit", new UnityEngine.Events.UnityEvent()); }
                catch { }
            }
        }

        private static void EnsureUseRanges(AvatarRangedWeapon ranged)
        {
            if (!Tools.Alive(ranged))
                return;
            try
            {
                // AvatarGun→AvatarRangedWeapon remap often leaves MaxUseRange at the 1m default,
                // so NPC AI never thinks the player is in shoot range.
                if (ranged.MaxUseRange < 8f)
                    ranged.MaxUseRange = 18f;
                if (ranged.MinUseRange <= 0.01f || ranged.MinUseRange > 5f)
                    ranged.MinUseRange = 1.5f;
            }
            catch { }
        }

        /// <summary>
        /// AvatarGun→AvatarRangedWeapon remap drops hold poses.
        /// AK/sniper/minigun/RPG use shotgun-style two-hand grips; SMG uses M1911 two-hand pistol
        /// grips so both hands stay on the handle (rifle hold leaves a floating support hand).
        /// </summary>
        private static string _rifleHold;
        private static string _rifleLowered;
        private static string _rifleRaised;
        private static string _rifleRecoil;
        private static bool _rifleAnimCached;
        private static bool _rifleTriggerTypeValid;
        private static AvatarEquippable.ETriggerType _rifleTriggerType;

        private static string _pistolHold;
        private static string _pistolLowered;
        private static string _pistolRaised;
        private static string _pistolRecoil;
        private static bool _pistolAnimCached;
        private static bool _pistolTriggerTypeValid;
        private static AvatarEquippable.ETriggerType _pistolTriggerType;

        private static void EnsureRifleHoldAnimations(AvatarEquippable equippable)
        {
            if (!Tools.Alive(equippable))
                return;
            if (!IsLongGunEquippable(equippable))
                return;

            bool smg = IsSmgEquippable(equippable);
            if (smg)
                CachePistolAnimationsFromVanilla();
            else
                CacheRifleAnimationsFromVanilla();

            string hold = smg ? _pistolHold : _rifleHold;
            if (!string.IsNullOrEmpty(hold))
            {
                try { equippable.AnimationTrigger = hold; }
                catch { GameAccess.Set(equippable, "AnimationTrigger", hold); }
            }

            if (smg ? _pistolTriggerTypeValid : _rifleTriggerTypeValid)
            {
                AvatarEquippable.ETriggerType triggerType = smg ? _pistolTriggerType : _rifleTriggerType;
                try { equippable.TriggerType = triggerType; }
                catch { GameAccess.Set(equippable, "TriggerType", triggerType); }
            }
        }

        private static void EnsureRangedAnimationTriggers(AvatarRangedWeapon ranged)
        {
            if (!Tools.Alive(ranged))
                return;
            if (!IsLongGunEquippable(ranged))
                return;

            bool smg = IsSmgEquippable(ranged);
            if (smg)
            {
                CachePistolAnimationsFromVanilla();
                ForceAnim(ranged, "LoweredAnimationTrigger", _pistolLowered);
                ForceAnim(ranged, "RaisedAnimationTrigger", _pistolRaised);
                ForceAnim(ranged, "RecoilAnimationTrigger", _pistolRecoil);
            }
            else
            {
                CacheRifleAnimationsFromVanilla();
                ForceAnim(ranged, "LoweredAnimationTrigger", _rifleLowered);
                ForceAnim(ranged, "RaisedAnimationTrigger", _rifleRaised);
                ForceAnim(ranged, "RecoilAnimationTrigger", _rifleRecoil);
            }
        }

        private static bool IsSmgEquippable(AvatarEquippable equippable)
        {
            if (!Tools.Alive(equippable))
                return false;

            string path = null;
            try { path = equippable.AssetPath; }
            catch { path = GameAccess.Get<string>(equippable, "AssetPath"); }
            if (IsSmgPath(path))
                return true;

            try
            {
                string name = equippable.gameObject != null ? equippable.gameObject.name : null;
                if (IsSmgPath(name))
                    return true;
            }
            catch { }

            return false;
        }

        private static bool IsSmgPath(string pathOrName)
        {
            if (string.IsNullOrEmpty(pathOrName))
                return false;
            // Match SMG / smg but not accidental substrings inside other ids.
            string leaf = pathOrName;
            int slash = Math.Max(pathOrName.LastIndexOf('/'), pathOrName.LastIndexOf('\\'));
            if (slash >= 0 && slash + 1 < pathOrName.Length)
                leaf = pathOrName.Substring(slash + 1);
            return leaf.IndexOf("smg", StringComparison.OrdinalIgnoreCase) >= 0;
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

            TryCacheAvatarAnims(
                "Avatar/Equippables/PumpShotgun",
                out _rifleHold,
                out _rifleLowered,
                out _rifleRaised,
                out _rifleRecoil,
                out _rifleTriggerType,
                out _rifleTriggerTypeValid);

            if (string.IsNullOrEmpty(_rifleHold))
                _rifleHold = "Shotgun_Grip_Lowered";
            if (string.IsNullOrEmpty(_rifleLowered))
                _rifleLowered = "Shotgun_Grip_Lowered";
            if (string.IsNullOrEmpty(_rifleRaised))
                _rifleRaised = "Shotgun_Grip_Raised";
            if (string.IsNullOrEmpty(_rifleRecoil))
                _rifleRecoil = "Shotgun_Recoil";
        }

        private static void CachePistolAnimationsFromVanilla()
        {
            if (_pistolAnimCached)
                return;
            _pistolAnimCached = true;

            TryCacheAvatarAnims(
                "Avatar/Equippables/M1911",
                out _pistolHold,
                out _pistolLowered,
                out _pistolRaised,
                out _pistolRecoil,
                out _pistolTriggerType,
                out _pistolTriggerTypeValid);

            // Vanilla M1911 two-hand pistol hold (support hand on the grip).
            if (string.IsNullOrEmpty(_pistolHold))
                _pistolHold = "BothHands_Grip_Lowered";
            if (string.IsNullOrEmpty(_pistolLowered))
                _pistolLowered = "BothHands_Grip_Lowered";
            if (string.IsNullOrEmpty(_pistolRaised))
                _pistolRaised = "BothHands_Grip_Raised";
            if (string.IsNullOrEmpty(_pistolRecoil))
                _pistolRecoil = "BothHands_Grip_Recoil";
        }

        private static void TryCacheAvatarAnims(
            string resourcePath,
            out string hold,
            out string lowered,
            out string raised,
            out string recoil,
            out AvatarEquippable.ETriggerType triggerType,
            out bool triggerTypeValid)
        {
            hold = null;
            lowered = null;
            raised = null;
            recoil = null;
            triggerType = default;
            triggerTypeValid = false;

            try
            {
                UnityEngine.Object loaded = Resources.Load(resourcePath);
                GameObject prefab = loaded != null ? loaded.As<GameObject>() : null;
                AvatarRangedWeapon template = null;
                if (prefab != null)
                {
                    template = prefab.GetComponent<AvatarRangedWeapon>()
                        ?? prefab.GetComponentInChildren<AvatarRangedWeapon>(true);
                }

                if (!Tools.Alive(template))
                    return;

                try { hold = template.AnimationTrigger; } catch { }
                try { lowered = template.LoweredAnimationTrigger; } catch { }
                try { raised = template.RaisedAnimationTrigger; } catch { }
                try { recoil = template.RecoilAnimationTrigger; } catch { }
                try
                {
                    triggerType = template.TriggerType;
                    triggerTypeValid = true;
                }
                catch { }
            }
            catch { }
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
