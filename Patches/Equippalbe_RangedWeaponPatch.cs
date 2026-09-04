using HarmonyLib;
using MoreGuns.Gui;
using MoreGuns.Guns;
using UnityEngine;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class Equippalbe_RangedWeaponPatch
    {
        private static float timeSinceLastAutoFire = 0F;
        private static float timeSinceWindingUp = 0f;

        [HarmonyPatch(typeof(Equippable_RangedWeapon), "UpdateInput")]
        [HarmonyPostfix]
        public static void Postfix(Equippable_RangedWeapon __instance)
        {
            if (Time.timeScale == 0F)
                return;
            try
            {
                if (Singleton<PauseMenu>.Instance != null && Singleton<PauseMenu>.Instance.IsPaused)
                    return;
            }
            catch { return; }

            GunSettings settings = GunSettings.EnsureOn(__instance);
            if (settings == null)
                return;

            bool isAttemptingToShoot = GameInput.GetButton(GameInput.ButtonCode.PrimaryClick);
            bool isWindingUp = GameInput.GetButton(GameInput.ButtonCode.SecondaryClick);

            if (settings.RequireWindup)
            {
                PlayAnimation anim = null;
                AudioSourceController windupSound = null;
                try
                {
                    anim = __instance.transform.GetChild(0).GetComponent<PlayAnimation>();
                    windupSound = anim.transform.Find("Windup Sound").GetComponent<AudioSourceController>();
                }
                catch { return; }
                if (anim == null || windupSound == null) return;

                timeSinceWindingUp += Time.deltaTime;
                WindupIndicator.SetValueByTime(timeSinceWindingUp, settings.WindupTime);

                if (isWindingUp)
                {
                    if (timeSinceWindingUp <= settings.WindupTime || !isAttemptingToShoot)
                    {
                        anim.Play("MiniGun Windup");

                        if (!windupSound.IsPlaying)
                            windupSound.Play();
                    }
                }
                else
                {
                    WindupIndicator.SetValue(0);
                    timeSinceWindingUp = 0F;
                    windupSound.Stop();
                }
            }

            if (settings.IsAutomatic)
            {
                bool windupReady = !settings.RequireWindup || timeSinceWindingUp > settings.WindupTime;
                if (windupReady)
                {
                    timeSinceLastAutoFire += Time.deltaTime;
                    if (isAttemptingToShoot)
                    {
                        if (timeSinceLastAutoFire >= __instance.FireCooldown)
                        {
                            timeSinceLastAutoFire = 0F;
                            if (GameAccess.CanFire(__instance, false))
                            {
                                if (__instance.Ammo > 0)
                                {
                                    if (!__instance.MustBeCocked || __instance.IsCocked)
                                        __instance.Fire();
                                    else
                                        GameAccess.Cock(__instance);
                                }
                            }
                        }
                    }
                    else
                    {
                        timeSinceLastAutoFire = __instance.FireCooldown;
                    }
                }
            }

            if (settings.SyncMagazineToAmmo && !__instance.IsReloading)
                WeaponBehavior.SyncMagazineVisualToAmmo(__instance);
        }

        [HarmonyPatch(typeof(Equippable_RangedWeapon), "Fire")]
        [HarmonyPrefix]
        public static bool Prefix(Equippable_RangedWeapon __instance)
        {
            GunSettings settings = GunSettings.EnsureOn(__instance);
            if (settings == null)
                return true;

            if (settings.RequireWindup && timeSinceWindingUp < settings.WindupTime)
                return false;

            // Tracers normally spawn from the camera; remember barrel tip for trail override.
            MuzzleAligner.RememberFrom(__instance);
            return true;
        }

        [HarmonyPatch(typeof(FXManager), nameof(FXManager.CreateBulletTrail))]
        [HarmonyPrefix]
        public static void PrefixBulletTrail(ref Vector3 start)
        {
            if (!MuzzleAligner.HasLastMuzzle)
                return;

            // Keep aim direction from camera; only move the visible tracer origin to the muzzle.
            start = MuzzleAligner.LastMuzzleWorldPos;
        }

        [HarmonyPatch(typeof(Equippable_RangedWeapon), "Fire")]
        [HarmonyPostfix]
        public static void PostfixFire(Equippable_RangedWeapon __instance)
        {
            GunSettings settings = GunSettings.EnsureOn(__instance);
            if (settings == null)
                return;

            if (settings.ExplosiveRounds)
                WeaponBehavior.CreateExplosionAtAimPoint(__instance);

            if (settings.SyncMagazineToAmmo)
                WeaponBehavior.SyncMagazineVisualToAmmo(__instance);
        }

        [HarmonyPatch(typeof(Equippable_RangedWeapon), "Reload")]
        [HarmonyPostfix]
        public static void PostfixReload(Equippable_RangedWeapon __instance)
        {
            GunSettings settings = GunSettings.EnsureOn(__instance);
            if (settings != null && settings.SyncMagazineToAmmo)
                WeaponBehavior.SyncMagazineVisualToAmmo(__instance);
        }
    }
}
