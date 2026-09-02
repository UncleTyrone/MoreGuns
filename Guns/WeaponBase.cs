using MelonLoader;
using MoreGuns.Patches;
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
        public Dictionary<string, AnimationClip> animations = new Dictionary<string, AnimationClip>();

        public GunConfiguration config;

        public GunSettings settings;

        public bool IsConfigurationFinished { get; private set; }

        public static List<WeaponBase> allWeapons = new List<WeaponBase>();
        public static Dictionary<string, WeaponBase> weaponsByName = new Dictionary<string, WeaponBase>();

        private static int pendingLoads;

        /// <summary>
        /// True once every weapon that started loading has either finished or given up. Consumers
        /// such as the shop injector need this because loading runs on a coroutine.
        /// </summary>
        public static bool AllWeaponsLoaded => pendingLoads == 0;

        public void Init(string name, string ID, GunSettings settings)
        {
            this.name = name;
            this.ID = ID;
            this.settings = settings;

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

            UnityEngine.Object[] loaded = new UnityEngine.Object[6];
            string[] paths = { equippablePath, magDefPath, gunDefPath, magTrashPath, handgunPath, magAvatarPath };

            for (int i = 0; i < paths.Length; i++)
            {
                var request = MoreGunsMod.assetBundle.LoadAssetAsync(paths[i]);
                yield return request;

                if (request.asset == null)
                {
                    MelonLogger.Error($"Could not load asset '{paths[i]}' for {ID}. {ID} will not be available.");
                    pendingLoads--;
                    yield break;
                }

                loaded[i] = request.asset;
            }

            gunEquippable = loaded[0].As<GameObject>();
            magIntItemDef = loaded[1].As<IntegerItemDefinition>();
            gunIntItemDef = loaded[2].As<IntegerItemDefinition>();
            gunMagTrash = loaded[3].As<GameObject>();
            gunHandgun = loaded[4].As<GameObject>();
            magAvatarEquippable = loaded[5].As<GameObject>();

            gunRangedWeapon = gunEquippable.GetComponent<Equippable_RangedWeapon>();
            gunMagTrashItem = gunMagTrash.GetComponent<TrashItem>();

            if (gunRangedWeapon == null)
            {
                MelonLogger.Error($"'{equippablePath}' has no Equippable_RangedWeapon component. {ID} will not be available.");
                pendingLoads--;
                yield break;
            }

            ApplyGunSettings(gunEquippable.AddComponent<GunSettings>());

            CreateConfig();
            SetCustomItemUI();
            LoadAnimations();
            ApplySettingsFromConfig();

            MoreGunsMod.RegisterAsset($"Avatar/Equippables/{this.name}", gunHandgun);
            MoreGunsMod.RegisterAsset($"Avatar/Equippables/{ID}", gunHandgun);
            MoreGunsMod.RegisterAsset($"Weapons/{ID}/Magazine/{this.name}_Magazine_AvatarEquippable", magAvatarEquippable);

            if (!string.IsNullOrEmpty(ID))
            {
                gunIntItemDef.ID = ID;
                if (magIntItemDef != null)
                    magIntItemDef.ID = ID + "mag";
            }

            allWeapons.Add(this);
            weaponsByName[ID] = this;

            pendingLoads--;
            MelonLogger.Msg($"Finished initializing {ID}.");
            ItemRegistryPatch.RegisterWeapons();
        }

        public void CreateConfig()
        {
            config = new GunConfiguration(this);
            MelonLogger.Msg("Created new config");
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
                MelonLogger.Msg("Successfully set CustomItemUI using IL2CPP conversion");
            }
            else
            {
                MelonLogger.Error("IL2CPP conversion failed");
            }
        }

        public void ApplySettingsFromConfig()
        {
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
            RuntimeAnimatorController animatorController = gunRangedWeapon.AnimatorController;
            if (animatorController == null)
            {
                MelonLogger.Warning($"{ID} has no animator controller; avatar animations will fall back to the default set.");
                return;
            }

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

        private void ApplyGunSettings(GunSettings _settings)
        {
            _settings.CopyFrom(settings);
        }
    }
}
