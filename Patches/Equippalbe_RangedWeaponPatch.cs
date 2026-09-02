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
            if (Time.timeScale == 0F || Singleton<PauseMenu>.Instance.IsPaused)
                return;

            if (__instance.gameObject.TryGetComponent<GunSettings>(out GunSettings settings))
            {
                bool isAttemptingToShoot = GameInput.GetButton(GameInput.ButtonCode.PrimaryClick);
                bool isWindingUp = GameInput.GetButton(GameInput.ButtonCode.SecondaryClick);

                if (settings.requireWindup)
                {
                    PlayAnimation anim = __instance.transform.GetChild(0).GetComponent<PlayAnimation>();
                    AudioSourceController windupSound = anim.transform.Find("Windup Sound").GetComponent<AudioSourceController>();
                    AudioSourceController shutdownSound = anim.transform.Find("Shutdown Sound").GetComponent<AudioSourceController>();

                    timeSinceWindingUp += Time.deltaTime;
                    WindupIndicator.SetValueByTime(timeSinceWindingUp, settings.windupTime);

                    if (isWindingUp)
                    {
                        if (timeSinceWindingUp <= settings.windupTime || !isAttemptingToShoot)
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

                if (settings.isAutomatic && (settings.requireWindup && timeSinceWindingUp > settings.windupTime) || settings.isAutomatic && !settings.requireWindup)
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
                                    {
                                        __instance.Fire();
                                    }
                                    else
                                    {
                                        GameAccess.Cock(__instance);
                                    }
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
        }

        [HarmonyPatch(typeof(Equippable_RangedWeapon), "Fire")]
        [HarmonyPrefix]
        public static bool Prefix(Equippable_RangedWeapon __instance)
        {
            if (__instance.gameObject.TryGetComponent<GunSettings>(out GunSettings settings))
            {
                if (settings.requireWindup && (timeSinceWindingUp < settings.windupTime))
                {
                    return false;
                }
            }
            else
            {

            }
            return true;
        }
    }
}
