using MelonLoader;
using System;
using UnityEngine;

namespace MoreGuns.Guns
{
    /// <summary>
    /// Per-gun gameplay tweaks that go beyond MelonPreferences (fire cadence, cocking,
    /// RPG rocket visibility, explosions).
    /// </summary>
    internal static class WeaponBehavior
    {
        public static void ApplyTuning(WeaponBase weapon)
        {
            if (weapon?.gunRangedWeapon == null || weapon.config == null)
                return;

            Equippable_RangedWeapon ranged = weapon.gunRangedWeapon;
            bool cfgDirty = false;

            switch (weapon.ID)
            {
                case "smg":
                    cfgDirty |= Ensure(weapon.config.Damage, 28f);
                    cfgDirty |= Ensure(weapon.config.ImpactForce, 120f);
                    cfgDirty |= Ensure(weapon.config.MagazineSize, 30);
                    ranged.FireCooldown = 0.055f;
                    ranged.MinSpread = 3f;
                    ranged.MaxSpread = 9f;
                    ranged.Range = 45f;
                    ranged.MustBeCocked = false;
                    break;

                case "sniper":
                    cfgDirty |= Ensure(weapon.config.Damage, 140f);
                    cfgDirty |= Ensure(weapon.config.ImpactForce, 450f);
                    cfgDirty |= Ensure(weapon.config.MagazineSize, 5);
                    // No bolt/cock — plain slow semi-auto to avoid desync with MagReload anims.
                    ranged.MustBeCocked = false;
                    ranged.CockedByDefault = true;
                    ranged.AutoCockAfterReload = false;
                    ranged.CockTime = 0.01f;
                    ranged.FireCooldown = 0.65f;
                    ranged.MinSpread = 0.15f;
                    ranged.MaxSpread = 0.8f;
                    ranged.Range = 200f;
                    ranged.MinAimFOVReduction = 12f;
                    ranged.MaxAimFOVReduction = 22f;
                    break;

                case "rpg":
                    cfgDirty |= Ensure(weapon.config.Damage, 200f);
                    cfgDirty |= Ensure(weapon.config.ImpactForce, 600f);
                    cfgDirty |= Ensure(weapon.config.MagazineSize, 1);
                    ranged.FireCooldown = 1.75f;
                    ranged.MustBeCocked = false;
                    ranged.MinSpread = 0.5f;
                    ranged.MaxSpread = 1.5f;
                    ranged.Range = 120f;
                    break;
            }

            ranged.Damage = weapon.config.Damage.Value;
            ranged.ImpactForce = weapon.config.ImpactForce.Value;
            ranged.MagazineSize = weapon.config.MagazineSize.Value;

            if (cfgDirty)
            {
                try { weapon.config.Category.SaveToFile(); }
                catch { /* non-fatal */ }
            }
        }

        private static bool Ensure(MelonPreferences_Entry<float> entry, float intended)
        {
            if (Mathf.Approximately(entry.Value, intended))
                return false;
            // Fix known bad leftovers (new guns stuck at 60 from early testing).
            if (Mathf.Approximately(entry.Value, 60f) || entry.Value < intended * 0.4f || entry.Value > intended * 2.5f)
            {
                entry.Value = intended;
                return true;
            }
            return false;
        }

        private static bool Ensure(MelonPreferences_Entry<int> entry, int intended)
        {
            if (entry.Value == intended)
                return false;
            // 7 was the broken leftover magazine size for smg/sniper/rpg.
            if (entry.Value == 7 || entry.Value <= 0)
            {
                entry.Value = intended;
                return true;
            }
            return false;
        }

        public static void SyncMagazineVisualToAmmo(Equippable_RangedWeapon weapon)
        {
            if (weapon == null)
                return;

            bool show = weapon.Ammo > 0;
            SetMagazineObjectsActive(weapon.transform, show);

            try
            {
                AvatarEquippable avatar = weapon.AvatarEquippable;
                if (avatar != null)
                    SetMagazineObjectsActive(avatar.transform, show);
            }
            catch
            {
                // AvatarEquippable may be unavailable during unequip.
            }
        }

        public static void CreateExplosionAtAimPoint(Equippable_RangedWeapon weapon)
        {
            if (weapon == null)
                return;

            try
            {
                CombatManager combat = NetworkSingleton<CombatManager>.Instance;
                PlayerCamera camera = PlayerSingleton<PlayerCamera>.Instance;
                if (combat == null || camera == null)
                    return;

                Vector3 origin = camera.transform.position;
                Vector3 direction = camera.transform.forward;
                float range = Mathf.Max(10f, weapon.Range);
                LayerMask mask = combat.RangedWeaponLayerMask;

                Vector3 explodeAt = origin + direction * range;
                if (Physics.SphereCast(origin, Mathf.Max(0.05f, weapon.RayRadius), direction, out RaycastHit hit, range, mask))
                    explodeAt = hit.point;

                float blastDamage = Mathf.Max(weapon.Damage, 180f);
                var data = new ExplosionData(7.5f, blastDamage, 450f, true, EExplosionType.Default);
                combat.CreateExplosion(explodeAt, data);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"RPG explosion failed: {ex.Message}");
            }
        }

        private static void SetMagazineObjectsActive(Transform root, bool active)
        {
            if (root == null)
                return;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                string n = t.name ?? "";
                // RPG rocket may be named Magazine, Rocket, or contain those tokens.
                bool isMag = n.Equals("Magazine", StringComparison.OrdinalIgnoreCase)
                    || n.Equals("Rocket", StringComparison.OrdinalIgnoreCase)
                    || n.IndexOf("MagazineMesh", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("rpg_rocket", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("RPG7 Rocket", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isMag)
                    continue;

                if (t.GetComponent<MeshFilter>() == null
                    && t.GetComponent<MeshRenderer>() == null
                    && t.GetComponentInChildren<MeshFilter>(true) == null)
                    continue;

                if (t.gameObject.activeSelf != active)
                    t.gameObject.SetActive(active);
            }
        }
    }
}
