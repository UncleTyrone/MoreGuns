using MelonLoader;
using MoreGuns.Gui;
using MoreGuns.Patches;
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

        public GunSettings(IntPtr ptr) : base(ptr) { }

        public GunSettings() : base(ClassInjector.DerivedConstructorPointer<GunSettings>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
#else
        public bool isAutomatic;
        public float speedMultiplier;
        public bool cameraJolt;
        public bool requireWindup;
        public float windupTime;
        public bool canManualyReload;
#endif

        internal static float WindupElapsed;

        private const string MOVE_SPEED_LABEL = "MoreGuns";
        private static float timeSinceLastAutoFire;
        private static int hudWeaponId;

        public void SetValues(bool automatic, float speed, bool jolt, bool windup, float windupSeconds, bool manualReload)
        {
#if IL2CPP
            isAutomatic.Value = automatic;
            speedMultiplier.Value = speed;
            cameraJolt.Value = jolt;
            requireWindup.Value = windup;
            windupTime.Value = windupSeconds;
            canManualyReload.Value = manualReload;
#else
            isAutomatic = automatic;
            speedMultiplier = speed;
            cameraJolt = jolt;
            requireWindup = windup;
            windupTime = windupSeconds;
            canManualyReload = manualReload;
#endif
        }

        public void CopyFrom(GunSettings other)
        {
            SetValues(other.isAutomatic, other.speedMultiplier, other.cameraJolt, other.requireWindup, other.windupTime, other.canManualyReload);
        }

        public void Update()
        {
            Equippable_RangedWeapon weapon = GetComponent<Equippable_RangedWeapon>();
            if (weapon == null || Time.timeScale == 0F)
                return;

            try
            {
                if (Singleton<PauseMenu>.Instance != null && Singleton<PauseMenu>.Instance.IsPaused)
                    return;
            }
            catch
            {
                return;
            }

            bool held = Tools.IsLocalPlayerHeld(weapon);
            int id = 0;
            try { id = weapon.GetInstanceID(); }
            catch { return; }

            if (held && hudWeaponId != id)
                ApplyHeldHud(weapon, id);
            else if (!held && hudWeaponId == id)
                ClearHeldHud();

            if (!held)
                return;

            CameraJoltPatch.ID = WeaponIdOf(weapon);
            ApplyCanReload(weapon);
            TickReloadHint(weapon);
            TickWindup(weapon);
            TickAutoFire(weapon);
        }

        public void OnDisable()
        {
            try
            {
                Equippable_RangedWeapon weapon = GetComponent<Equippable_RangedWeapon>();
                if (weapon != null && hudWeaponId == weapon.GetInstanceID())
                    ClearHeldHud();
            }
            catch
            {
                // instance already torn down
            }
        }

        private void ApplyHeldHud(Equippable_RangedWeapon weapon, int id)
        {
            hudWeaponId = id;
            Reticle.SetActive(Config.EnableCrosshairForGuns != null && Config.EnableCrosshairForGuns.Value);
            if (requireWindup)
                WindupIndicator.Show(true);
            SetMoveSpeedMultiplier(speedMultiplier);
        }

        private static void ClearHeldHud()
        {
            hudWeaponId = 0;
            WindupElapsed = 0F;
            WindupIndicator.SetValue(0);
            WindupIndicator.Show(false);
            Reticle.SetActive(false);
            ClearMoveSpeedMultiplier();
        }

        private void TickWindup(Equippable_RangedWeapon weapon)
        {
            if (!requireWindup)
            {
                WindupElapsed = 0F;
                return;
            }

            bool isWindingUp = GameInput.GetButton(GameInput.ButtonCode.SecondaryClick);
            PlayAnimation anim = null;
            AudioSourceController windupSound = null;
            try
            {
                if (weapon.transform.childCount < 1)
                    return;
                anim = weapon.transform.GetChild(0).GetComponent<PlayAnimation>();
                Transform windup = anim != null ? anim.transform.Find("Windup Sound") : null;
                windupSound = windup != null ? windup.GetComponent<AudioSourceController>() : null;
            }
            catch
            {
                return;
            }

            if (anim == null || windupSound == null)
                return;

            WindupElapsed += Time.deltaTime;
            WindupIndicator.SetValueByTime(WindupElapsed, windupTime);

            bool isAttemptingToShoot = GameInput.GetButton(GameInput.ButtonCode.PrimaryClick);
            if (isWindingUp)
            {
                if (WindupElapsed <= windupTime || !isAttemptingToShoot)
                {
                    anim.Play("MiniGun Windup");
                    if (!windupSound.IsPlaying)
                        windupSound.Play();
                }
            }
            else
            {
                WindupIndicator.SetValue(0);
                WindupElapsed = 0F;
                windupSound.Stop();
            }
        }

        private void TickAutoFire(Equippable_RangedWeapon weapon)
        {
            bool ready = isAutomatic && (!requireWindup || WindupElapsed > windupTime);
            if (!ready)
                return;

            timeSinceLastAutoFire += Time.deltaTime;
            if (!GameInput.GetButton(GameInput.ButtonCode.PrimaryClick))
            {
                timeSinceLastAutoFire = weapon.FireCooldown;
                return;
            }

            if (timeSinceLastAutoFire < weapon.FireCooldown)
                return;

            timeSinceLastAutoFire = 0F;
            if (!GameAccess.CanFire(weapon, false) || weapon.Ammo <= 0)
                return;

            if (!weapon.MustBeCocked || weapon.IsCocked)
                weapon.Fire();
            else
                GameAccess.Cock(weapon);
        }

        private void TickReloadHint(Equippable_RangedWeapon weapon)
        {
            if (ManualReloadAllowed(weapon))
                return;

            bool pressed = false;
            try { pressed = GameInput.GetButtonDown(GameInput.ButtonCode.Reload); }
            catch { return; }

            if (!pressed)
                return;

            if (IsMinigun(weapon))
                ReloadMessage.Show("Take the MiniGun to Stan to reload.");
            else
                ReloadMessage.Show(true);
        }

        private void ApplyCanReload(Equippable_RangedWeapon weapon)
        {
            GameAccess.Set(weapon, "CanReload", ManualReloadAllowed(weapon));
        }

        private bool ManualReloadAllowed(Equippable_RangedWeapon weapon)
        {
            if (IsMinigun(weapon) && Config.AllowMinigunManualReload != null && !Config.AllowMinigunManualReload.Value)
                return false;
            return canManualyReload;
        }

        private static bool IsMinigun(Equippable_RangedWeapon weapon)
        {
            if (!Tools.Alive(weapon))
                return false;
            try
            {
                return weapon.name.ToLowerInvariant().Contains("minigun");
            }
            catch
            {
                return false;
            }
        }

        private static string WeaponIdOf(Equippable_RangedWeapon weapon)
        {
            try
            {
                return weapon.name.Replace("_Equippable(Clone)", "").ToLower();
            }
            catch
            {
                return "";
            }
        }

        private static void SetMoveSpeedMultiplier(float multiplier)
        {
            FloatStack stack = PlayerSingleton<PlayerMovement>.Instance?.MoveSpeedMultiplierStack;
            if (stack == null)
                return;

            stack.Remove(MOVE_SPEED_LABEL);
            stack.Add(new FloatStack.StackEntry(MOVE_SPEED_LABEL, multiplier, FloatStack.EStackMode.Multiplicative, 0));
            GameAccess.Recalculate(stack);
        }

        private static void ClearMoveSpeedMultiplier()
        {
            FloatStack stack = PlayerSingleton<PlayerMovement>.Instance?.MoveSpeedMultiplierStack;
            if (stack == null)
                return;

            stack.Remove(MOVE_SPEED_LABEL);
            GameAccess.Recalculate(stack);
        }
    }
}
