using MelonLoader;
using System;
using UnityEngine;
#if IL2CPP
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
#endif

namespace MoreGuns.Guns
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    public class GunSettings : MonoBehaviour
    {
#if IL2CPP
        public Il2CppValueField<bool> isAutomatic;
        public Il2CppValueField<float> speedMultiplier;
        public Il2CppValueField<bool> cameraJolt;
        public Il2CppValueField<bool> requireWindup;
        public Il2CppValueField<float> windupTime;
        public Il2CppValueField<bool> canManualyReload;
        public Il2CppValueField<bool> syncMagazineToAmmo;
        public Il2CppValueField<bool> explosiveRounds;

        public GunSettings(IntPtr ptr) : base(ptr) { }

        public GunSettings() : base(ClassInjector.DerivedConstructorPointer<GunSettings>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public bool IsAutomatic => isAutomatic.Value;
        public float SpeedMultiplier => speedMultiplier.Value;
        public bool CameraJolt => cameraJolt.Value;
        public bool RequireWindup => requireWindup.Value;
        public float WindupTime => windupTime.Value;
        public bool CanManualyReload => canManualyReload.Value;
        public bool SyncMagazineToAmmo => syncMagazineToAmmo.Value;
        public bool ExplosiveRounds => explosiveRounds.Value;
#else
        public bool isAutomatic;
        public float speedMultiplier;
        public bool cameraJolt;
        public bool requireWindup;
        public float windupTime;
        public bool canManualyReload;
        public bool syncMagazineToAmmo;
        public bool explosiveRounds;

        public bool IsAutomatic => isAutomatic;
        public float SpeedMultiplier => speedMultiplier;
        public bool CameraJolt => cameraJolt;
        public bool RequireWindup => requireWindup;
        public float WindupTime => windupTime;
        public bool CanManualyReload => canManualyReload;
        public bool SyncMagazineToAmmo => syncMagazineToAmmo;
        public bool ExplosiveRounds => explosiveRounds;
#endif

        public void Apply(GunTuning tuning)
        {
            if (tuning == null)
                return;
#if IL2CPP
            isAutomatic.Value = tuning.isAutomatic;
            speedMultiplier.Value = tuning.speedMultiplier;
            cameraJolt.Value = tuning.cameraJolt;
            requireWindup.Value = tuning.requireWindup;
            windupTime.Value = tuning.windupTime;
            canManualyReload.Value = tuning.canManualyReload;
            syncMagazineToAmmo.Value = tuning.syncMagazineToAmmo;
            explosiveRounds.Value = tuning.explosiveRounds;
#else
            isAutomatic = tuning.isAutomatic;
            speedMultiplier = tuning.speedMultiplier;
            cameraJolt = tuning.cameraJolt;
            requireWindup = tuning.requireWindup;
            windupTime = tuning.windupTime;
            canManualyReload = tuning.canManualyReload;
            syncMagazineToAmmo = tuning.syncMagazineToAmmo;
            explosiveRounds = tuning.explosiveRounds;
#endif
        }

        /// <summary>
        /// Attach settings to a held/runtime clone only. Never put GunSettings on the shared
        /// AssetBundle prefab — Il2Cpp Instantiates of injected components hard-crash.
        /// </summary>
        public static GunSettings EnsureOn(Equippable_RangedWeapon weapon)
        {
            if (weapon == null)
                return null;

            try
            {
                GunSettings existing = weapon.GetComponent<GunSettings>();
                if (existing != null)
                    return existing;
            }
            catch
            {
                return null;
            }

            WeaponBase source = ResolveWeaponBase(weapon);
            if (source?.settings == null)
                return null;

            try
            {
                GunSettings created = weapon.gameObject.AddComponent<GunSettings>();
                created.Apply(source.settings);
                return created;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"GunSettings.EnsureOn failed: {ex.Message}");
                return null;
            }
        }

        public static WeaponBase ResolveWeaponBase(Equippable_RangedWeapon weapon)
        {
            if (weapon == null)
                return null;

            try
            {
                IntegerItemInstance item = GameAccess.Get<IntegerItemInstance>(weapon, "weaponItem");
                string id = item?.Definition?.ID;
                if (!string.IsNullOrEmpty(id) && WeaponBase.weaponsByName.TryGetValue(id, out WeaponBase byId))
                    return byId;
            }
            catch { }

            try
            {
                string name = weapon.gameObject != null ? weapon.gameObject.name : null;
                if (string.IsNullOrEmpty(name))
                    return null;

                // Prefer exact-ish match: "AK47_Equippable(Clone)" / "smg_equippable(Clone)"
                string key = name.Replace("(Clone)", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("_Equippable", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("_equippable", "", StringComparison.OrdinalIgnoreCase)
                    .Trim()
                    .ToLowerInvariant();

                if (WeaponBase.weaponsByName.TryGetValue(key, out WeaponBase byKey))
                    return byKey;

                foreach (WeaponBase w in WeaponBase.allWeapons)
                {
                    if (w == null || string.IsNullOrEmpty(w.ID))
                        continue;
                    if (name.IndexOf(w.ID, StringComparison.OrdinalIgnoreCase) >= 0)
                        return w;
                }
            }
            catch { }

            return null;
        }
    }
}
