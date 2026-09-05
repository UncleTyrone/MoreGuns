using MelonLoader;
using MoreGuns.Patches;
using System;
using System.Collections;
using UnityEngine;

namespace MoreGuns.Guns
{
    public class WeaponBase
    {
        public string name;
        public string ID;

        public GameObject gunEquippable;
        public Equippable_RangedWeapon gunRangedWeapon;

        public GameObject gunHandgun;
        public GameObject magAvatarEquippable;

        public IntegerItemDefinition gunIntItemDef;
        public IntegerItemDefinition magIntItemDef;

        public DialogueController_ArmsDealer.WeaponOption rangedGun;
        public DialogueController_ArmsDealer.WeaponOption ammoGun;

        public GameObject gunMagTrash;
        public TrashItem gunMagTrashItem;
        public GameObject magEquippable;
        public Dictionary<string, AnimationClip> animations = new Dictionary<string, AnimationClip>();

        public GunConfiguration config;

        public GunTuning settings;

        public bool IsConfigurationFinished { get; private set; }

        public static List<WeaponBase> allWeapons = new List<WeaponBase>();
        public static Dictionary<string, WeaponBase> weaponsByName = new Dictionary<string, WeaponBase>();

        /// <summary>
        /// True once every weapon that started loading has either finished or given up. Consumers
        /// such as the shop injector need this because loading runs on a coroutine.
        /// </summary>
        public static bool AllWeaponsLoaded => startedLoads > 0 && pendingLoads <= 0;
        private static int pendingLoads;
        private static int startedLoads;

        public void Init(string name, string ID, GunTuning settings)
        {
            this.name = name;
            this.ID = ID;
            this.settings = settings;

            startedLoads++;
            pendingLoads++;
            MelonLogger.Msg($"Initializing {ID}");
            MelonCoroutines.Start(LoadGun());
        }

        public void RefreshShopListings()
        {
            Patches.ArmsDealerInterfacePatch.RefreshListings();
        }

        private IEnumerator LoadGun()
        {
            string equippablePath = $"assets/resources/weapons/{ID}/{ID}_equippable.prefab";
            string magDefPath = $"assets/resources/weapons/{ID}/magazine/{ID}_magazine.asset";
            string gunDefPath = $"assets/resources/weapons/{ID}/{ID}.asset";
            string magTrashPath = $"assets/resources/weapons/{ID}/magazine/{ID}_magazine_trash.prefab";
            string handgunPath = $"assets/resources/avatar/equippables/{ID}.prefab";
            string magAvatarPath = $"assets/resources/weapons/{ID}/magazine/{ID}_magazine_avatarequippable.prefab";
            string magEquippablePath = $"assets/resources/weapons/{ID}/magazine/{ID}_magazine_equippable.prefab";

            UnityEngine.Object[] loaded = new UnityEngine.Object[7];
            string[] paths = { equippablePath, magDefPath, gunDefPath, magTrashPath, handgunPath, magAvatarPath, magEquippablePath };

            for (int i = 0; i < paths.Length; i++)
            {
                var request = MoreGunsMod.assetBundle.LoadAssetAsync(paths[i]);
                yield return request;

                if (request.asset == null)
                {
                    // Mag viewmodel is required for reload; everything else is fatal.
                    MelonLogger.Error($"Could not load asset '{paths[i]}' for {ID}. {ID} will not be available.");
                    pendingLoads--;
                    yield break;
                }

                loaded[i] = request.asset;
            }

            try
            {
                FinishLoadGun(loaded, equippablePath);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to initialize {ID}: {ex}");
            }
            finally
            {
                pendingLoads--;
            }
        }

        private void FinishLoadGun(UnityEngine.Object[] loaded, string equippablePath)
        {
            gunEquippable = loaded[0].As<GameObject>();
            magIntItemDef = loaded[1].As<IntegerItemDefinition>();
            gunIntItemDef = loaded[2].As<IntegerItemDefinition>();
            gunMagTrash = loaded[3].As<GameObject>();
            gunHandgun = loaded[4].As<GameObject>();
            magAvatarEquippable = loaded[5].As<GameObject>();
            magEquippable = loaded[6].As<GameObject>();

            gunRangedWeapon = gunEquippable.GetComponent<Equippable_RangedWeapon>();
            gunMagTrashItem = gunMagTrash.GetComponent<TrashItem>();

            if (gunRangedWeapon == null)
            {
                MelonLogger.Error($"'{equippablePath}' has no Equippable_RangedWeapon component. {ID} will not be available.");
                return;
            }

            // Never AddComponent<GunSettings> on the shared AssetBundle prefab.
            // Il2Cpp Instantiates of injected MonoBehaviours hard-crash (give/equip).
            // Settings are attached to held clones in RangedWeaponEquipPatch / EnsureOn.
            WireLoadedAssets();

            try
            {
                MaterialFixup.FixHierarchy(gunEquippable);
                MaterialFixup.FixHierarchy(gunHandgun);
                MaterialFixup.FixHierarchy(gunMagTrash);
                MaterialFixup.FixHierarchy(magAvatarEquippable);
                MaterialFixup.FixHierarchy(magEquippable);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Material fixup failed for {ID}: {ex.Message}");
            }

            try
            {
                // Keep onReloadStart for AK. Mesh-swaps still point at Other Reload which
                // shatters custom mag sockets — ManualReload plays AK47 Reload instead.
                if (!string.Equals(ID, "ak47", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(ID, "minigun", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(ID, "rpg", StringComparison.OrdinalIgnoreCase))
                {
                    PreferAk47ReloadClip(gunEquippable);
                    ClearReloadEvents(gunRangedWeapon);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Reload clip prefer failed for {ID}: {ex.Message}");
            }

            CreateConfig();
            SetCustomItemUI();

            try { LoadAnimations(); }
            catch (Exception ex) { MelonLogger.Warning($"Animation load failed for {ID}: {ex.Message}"); }

            ApplySettingsFromConfig();
            WeaponBehavior.ApplyTuning(this);

            MoreGunsMod.RegisterAsset($"Avatar/Equippables/{this.name}", gunHandgun);
            MoreGunsMod.RegisterAsset($"Avatar/Equippables/{ID}", gunHandgun);
            MoreGunsMod.RegisterAsset($"Weapons/{ID}/Magazine/{this.name}_Magazine_AvatarEquippable", magAvatarEquippable);

            if (!string.IsNullOrEmpty(ID))
            {
                gunIntItemDef.ID = ID;
                if (magIntItemDef != null)
                    magIntItemDef.ID = ID + "mag";
            }

            if (string.Equals(ID, "ak47", StringComparison.OrdinalIgnoreCase))
            {
                // Do not cache live AK mag meshes — they match leftovers and caused deletes.
            }
            else if (!string.Equals(ID, "minigun", StringComparison.OrdinalIgnoreCase))
            {
                try { MagazineSocketFix.CacheLeftoverMeshesFromGun(gunEquippable); }
                catch (Exception ex) { MelonLogger.Warning($"Leftover mag cache failed for {ID}: {ex.Message}"); }
            }

            allWeapons.Add(this);
            weaponsByName[ID] = this;

            // pendingLoads is decremented in LoadGun's finally — do not decrement here or
            // AllWeaponsLoaded stays false forever and shop/registry wait the full 60s timeout.
            MelonLogger.Msg($"Finished initializing {ID}.");
            ItemRegistryPatch.RegisterWeapons();
            // Shop inject runs after Harmony in OnSceneWasLoaded (Awake is often already past).
        }

        private void WireLoadedAssets()
        {
            if (gunIntItemDef != null && gunRangedWeapon != null)
                gunIntItemDef.Equippable = gunRangedWeapon;

            WireItemIcons();
            WireMagazineEquippable();

            if (gunRangedWeapon == null)
                return;

            if (magIntItemDef != null)
                gunRangedWeapon.Magazine = magIntItemDef;

            if (gunMagTrashItem != null)
            {
                gunRangedWeapon.ReloadTrash = gunMagTrashItem;
                Patches.TrashRegistryPatch.EnsureTrashId(this);
            }

            if (gunHandgun != null)
            {
                AvatarEquippable avatar = gunHandgun.GetComponent<AvatarEquippable>()
                    ?? gunHandgun.GetComponentInChildren<AvatarEquippable>(true);
                try
                {
                    AvatarRangedWeapon ranged = gunHandgun.GetComponent<AvatarRangedWeapon>()
                        ?? gunHandgun.GetComponentInChildren<AvatarRangedWeapon>(true);
                    if (ranged != null)
                        avatar = ranged;
                }
                catch { }

                if (avatar != null)
                {
                    // Required so FishNet SetEquippable_Networked resolves on other clients.
                    string pathByName = $"Avatar/Equippables/{this.name}";
                    string pathById = $"Avatar/Equippables/{ID}";
                    try { avatar.AssetPath = pathByName; }
                    catch { GameAccess.Set(avatar, "AssetPath", pathByName); }

                    // AvatarGun→AvatarRangedWeapon remap often drops AlignmentPoint/FireSound/Muzzle refs.
                    Patches.SetEquippablePatch.EnsureAvatarWeaponRefs(avatar);

                    gunRangedWeapon.AvatarEquippable = avatar;
                    MoreGunsMod.RegisterAsset(pathByName, gunHandgun);
                    MoreGunsMod.RegisterAsset(pathById, gunHandgun);
                }
                else
                {
                    MelonLogger.Error($"{ID}: avatar equippable prefab has no AvatarEquippable — remotes will not see/hear this gun.");
                }
            }
        }

        private void WireMagazineEquippable()
        {
            if (magIntItemDef == null || magEquippable == null)
                return;

            // Serialized Mag.Equippable refs often fail under Il2Cpp (type:3). Always wire
            // the loaded Magazine_Equippable so Reload can spawn the hand-mag viewmodel.
            Equippable_Viewmodel magView = magEquippable.GetComponent<Equippable_Viewmodel>()
                ?? magEquippable.GetComponent<Equippable>() as Equippable_Viewmodel
                ?? magEquippable.GetComponentInChildren<Equippable_Viewmodel>(true);

            if (magView == null)
            {
                MelonLogger.Error($"{ID}: magazine equippable prefab has no Equippable_Viewmodel — reload will fail.");
                return;
            }

            magIntItemDef.Equippable = magView;

            if (magAvatarEquippable != null)
            {
                AvatarEquippable magAvatar = magAvatarEquippable.GetComponent<AvatarEquippable>()
                    ?? magAvatarEquippable.GetComponentInChildren<AvatarEquippable>(true);
                if (magAvatar != null)
                    magView.AvatarEquippable = magAvatar;
            }
        }

        /// <summary>
        /// Ensure an "AK47 Reload" clip exists on mesh-swapped guns (alias Other Reload if needed).
        /// </summary>
        private void PreferAk47ReloadClip(GameObject gunRoot)
        {
            if (gunRoot == null)
                return;

            Animation[] anims = gunRoot.GetComponentsInChildren<Animation>(true);
            for (int a = 0; a < anims.Length; a++)
            {
                Animation anim = anims[a];
                if (anim == null)
                    continue;

                AnimationClip akReload = anim.GetClip("AK47 Reload");
                AnimationClip other = anim.GetClip("Other Reload");
                if (akReload == null && other != null)
                    anim.AddClip(other, "AK47 Reload");
            }
        }

        /// <summary>
        /// Drop Other Reload UnityEvent so ManualReload can Play("AK47 Reload") instead.
        /// </summary>
        private static void ClearReloadEvents(Equippable_RangedWeapon weapon)
        {
            if (weapon == null)
                return;
            try
            {
                weapon.onReloadStart = new UnityEngine.Events.UnityEvent();
                weapon.onReloadEnd = new UnityEngine.Events.UnityEvent();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Could not clear reload events: {ex.Message}");
            }
        }

        private void WireItemIcons()
        {
            // Always replace Icon from the bundle by name. Serialized Icon refs on the new
            // gun item defs often resolve to the wrong texture under Il2Cpp (blank / UV sheet / AK crop).
            string gunSpriteName = string.Equals(ID, "minigun", StringComparison.OrdinalIgnoreCase)
                ? "MiniGun__Icon"
                : $"{name}__Icon";
            string magSpriteName = string.Equals(ID, "minigun", StringComparison.OrdinalIgnoreCase)
                ? "MiniGun__Magazine_Icon"
                : $"{name}__Magazine_Icon";

            Sprite gunIcon = MoreGunsMod.GetSprite(gunSpriteName)
                ?? MoreGunsMod.GetSprite($"{ID}__Icon")
                ?? (gunIntItemDef != null ? gunIntItemDef.Icon : null);

            Sprite magIcon = MoreGunsMod.GetSprite(magSpriteName)
                ?? MoreGunsMod.GetSprite($"{ID}__Magazine_Icon")
                ?? (magIntItemDef != null ? magIntItemDef.Icon : null);

            if (gunIntItemDef != null && gunIcon != null)
                gunIntItemDef.Icon = gunIcon;
            if (magIntItemDef != null && magIcon != null)
                magIntItemDef.Icon = magIcon;
        }

        public void CreateConfig()
        {
            config = new GunConfiguration(this);
        }

        private void SetCustomItemUI()
        {
            UnityEngine.Object definition = Resources.Load("Weapons/M1911/M1911");

            if (definition == null)
            {
                MelonLogger.Error("Cast to ItemDefinition failed - type mismatch in IL2CPP");
                return;
            }

            var il2cppDefinition = definition.As<ItemDefinition>();

            if (il2cppDefinition != null)
            {
                gunIntItemDef.CustomItemUI = il2cppDefinition.CustomItemUI;
                magIntItemDef.CustomItemUI = il2cppDefinition.CustomItemUI;
            }
            else
            {
                MelonLogger.Error("IL2CPP conversion failed");
            }
        }

        public void ApplySettingsFromConfig()
        {
            if (gunRangedWeapon == null || config == null)
                return;
            gunRangedWeapon.Damage = config.Damage.Value;
            gunRangedWeapon.ImpactForce = config.ImpactForce.Value;
            gunRangedWeapon.MinAimFOVReduction = config.MinAimFOVReduction.Value;
            gunRangedWeapon.MaxAimFOVReduction = config.MaxAimFOVReduction.Value;
            gunRangedWeapon.AccuracyChangeDuration = config.AccuracyChangeDuration.Value;
            gunRangedWeapon.MagazineSize = config.MagazineSize.Value;

            gunIntItemDef.Name = config.DisplayItemName.Value;
            gunIntItemDef.Description = config.DisplayDescription.Value;
            gunIntItemDef.legalStatus = config.LegalStatus.Value;
            gunIntItemDef.RequiredRank = config.RequiredRank.Value;

            magIntItemDef.Name = config.MagDisplayItemName.Value;
            magIntItemDef.Description = config.MagDisplayDescription.Value;
            magIntItemDef.legalStatus = config.MagLegalStatus.Value;
            magIntItemDef.RequiredRank = config.MagRequiredRank.Value;

            CreateDialogueControllerOptions();
        }

        // TODO update networking with the new Shoplisting to work with.
        private void CreateDialogueControllerOptions()
        {
            gunIntItemDef.Name = config.ItemName.Value;
            gunIntItemDef.BasePurchasePrice = config.PurchasePrice.Value;
            rangedGun = new DialogueController_ArmsDealer.WeaponOption
            {
                IsAvailable = config.Available.Value,
                NotAvailableReason = config.AvailableReason.Value,
                Item = gunIntItemDef
            };

            magIntItemDef.Name = config.MagItemName.Value;
            magIntItemDef.BasePurchasePrice = config.MagPurchasePrice.Value;
            ammoGun = new DialogueController_ArmsDealer.WeaponOption
            {
                IsAvailable = config.MagAvailable.Value,
                NotAvailableReason = config.MagAvailableReason.Value,
                Item = magIntItemDef
            };

            IsConfigurationFinished = true;
        }

        private void LoadAnimations()
        {
            RuntimeAnimatorController animatorController = ResolveAnimatorController();
            if (animatorController == null)
            {
                MelonLogger.Warning($"{ID} has no animator controller; avatar animations will fall back to the default set.");
                return;
            }

            // Prefab refs often deserialize as null under Il2Cpp even though the controller is in the bundle.
            if (gunRangedWeapon.AnimatorController == null)
                gunRangedWeapon.AnimatorController = animatorController;
            // FP arms use ViewmodelAvatar — Equippable_RangedWeapon has no SetAnimatorController.
            try
            {
                ViewmodelAvatar vm = ViewmodelAvatar.Instance;
                if (vm != null)
                    vm.SetAnimatorController(animatorController);
            }
            catch { }

            // Several clips can match a keyword, so the first match wins rather than throwing on a duplicate key.
            foreach (AnimationClip anim in animatorController.animationClips)
            {
                if (anim == null)
                    continue;

                if (anim.name.Contains("Idle") && !animations.ContainsKey("BothHands_Grip_Lowered"))
                    animations["BothHands_Grip_Lowered"] = anim;

                if (anim.name.Contains("Aiming") && !animations.ContainsKey("BothHands_Grip_Raised"))
                    animations["BothHands_Grip_Raised"] = anim;

                if (anim.name.Contains("Fire") && !animations.ContainsKey("BothHands_Grip_Recoil"))
                    animations["BothHands_Grip_Recoil"] = anim;
            }
        }

        private RuntimeAnimatorController ResolveAnimatorController()
        {
            RuntimeAnimatorController fromWeapon = gunRangedWeapon.AnimatorController
                ?? GameAccess.Get<RuntimeAnimatorController>(gunRangedWeapon, "AnimatorController");
            if (fromWeapon != null)
                return fromWeapon;

            string want = string.Equals(ID, "minigun", StringComparison.OrdinalIgnoreCase)
                ? "MiniGunAnimator"
                : "HandgunAnimator";

            return MoreGunsMod.GetAnimatorController(want);
        }
    }
}
