using MelonLoader;
using MoreGuns;
using MoreGuns.Gui;
using MoreGuns.Guns;
using MoreGuns.Patches;
using MoreGuns.Sync;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[assembly: MelonInfo(typeof(MoreGunsMod), "MoreGuns", "1.6.3", "Voidane")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace MoreGuns
{
    public class MoreGunsMod : MelonMod
    {
        public static Transform map;
        public static Transform container;
        public static Transform midcanal;
        public static Transform stanNPC;
#if IL2CPP
        public static Il2CppAssetBundle assetBundle;
#else
        public static AssetBundle assetBundle;
#endif

        public static readonly Dictionary<string, UnityEngine.Object> Resources =
            new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);

        public static bool isInitialized;
        public static HarmonyLib.Harmony harmony;
        private static bool harmonyApplied;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Thank you for using More Guns! Discord: discord.gg/XB7ruKtJje");

#if IL2CPP
            assetBundle = Il2CppAssetBundleManager.LoadFromMemory(Assets.VoidanesGuns);
#else
            assetBundle = AssetBundle.LoadFromMemory(Assets.VoidanesGuns);
#endif
            if (assetBundle == null)
            {
                MelonLogger.Error("The asset bundle could not be loaded. MoreGuns will not run.");
                isInitialized = false;
                return;
            }

            isInitialized = true;
            Config.Initialize();

            new AK47();
            new MiniGun();
            new Sniper();
            new SMG();
            new RPG();
        }

        public override void OnApplicationQuit()
        {
            harmony?.UnpatchSelf();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!isInitialized)
                return;

            if (sceneName != "Main")
            {
                ItemRegistryPatch.Reset();
                return;
            }

            // Apply Harmony after scene load to avoid the CLR 0x80131506 crash.
            // PatchAll during OnInitializeMelon triggers MonoMod's JIT hook too early.
            if (!harmonyApplied)
            {
#if IL2CPP
                harmony = new HarmonyLib.Harmony("com.voidane.moregunsil2cpp");
#else
                harmony = new HarmonyLib.Harmony("com.voidane.moregunsmono");
#endif
                try
                {
                    harmony.PatchAll(typeof(MoreGunsMod).Assembly);
                    harmonyApplied = true;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Harmony PatchAll failed: {ex}");
                }
            }

            // ShopInterface.Awake often runs before PatchAll, so find/inject explicitly.
            MelonCoroutines.Start(ArmsDealerInterfacePatch.FindAndInjectAfterHarmony());

            try
            {
                NetworkController.SyncConfiguration();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to sync MoreGuns configuration: {ex}");
            }

            MelonCoroutines.Start(ItemRegistryPatch.RegisterWhenReady());

            Reticle.Initialize();

            GameObject hud = GameObject.Find("UI/HUD");
            if (hud == null)
            {
                MelonLogger.Warning("Could not find 'UI/HUD'; the reload message and windup indicator will be unavailable.");
                return;
            }

            ReloadMessage.Initialize(hud.transform);
            WindupIndicator.Initialize(hud.transform);
        }

        public static void RegisterAsset(string path, UnityEngine.Object asset)
        {
            Resources[path] = asset;
        }

        public static UnityEngine.Object TryGetAsset(string path)
        {
            if (path != null && Resources.TryGetValue(path, out UnityEngine.Object asset))
            {
                return asset;
            }
            return null;
        }

        private static readonly Dictionary<string, RuntimeAnimatorController> animatorControllers =
            new Dictionary<string, RuntimeAnimatorController>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Sprite> sprites =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static bool animatorControllersLoaded;
        private static bool spritesLoaded;

        public static Sprite GetSprite(string name)
        {
            if (string.IsNullOrEmpty(name) || assetBundle == null)
                return null;

            EnsureSpritesLoaded();
            return sprites.TryGetValue(name, out Sprite sprite) ? sprite : null;
        }

        private static void EnsureSpritesLoaded()
        {
            if (spritesLoaded || assetBundle == null)
                return;

            spritesLoaded = true;
            try
            {
                string[] paths =
                {
                    "assets/sprite/ak47__icon.asset",
                    "assets/sprite/ak47__magazine_icon.asset",
                    "assets/sprite/minigun__icon.asset",
                    "assets/sprite/minigun__magazine_icon.asset",
                    "assets/sprite/sniper__icon.asset",
                    "assets/sprite/sniper__magazine_icon.asset",
                    "assets/sprite/smg__icon.asset",
                    "assets/sprite/smg__magazine_icon.asset",
                    "assets/sprite/rpg__icon.asset",
                    "assets/sprite/rpg__magazine_icon.asset",
                    "AK47__Icon",
                    "AK47__Magazine_Icon",
                    "MiniGun__Icon",
                    "MiniGun__Magazine_Icon",
                    "Sniper__Icon",
                    "Sniper__Magazine_Icon",
                    "SMG__Icon",
                    "SMG__Magazine_Icon",
                    "RPG__Icon",
                    "RPG__Magazine_Icon",
                };

                foreach (string path in paths)
                    TryCacheSprite(assetBundle.LoadAsset(path));

                // Always scan the whole bundle. New-gun icons are Texture2D sprites, not
                // Assets/Sprite/*.asset — stopping after AK47/MiniGun left them uncached.
                UnityEngine.Object[] all = assetBundle.LoadAllAssets();
                if (all != null)
                {
                    foreach (UnityEngine.Object asset in all)
                        TryCacheSprite(asset);
                }

                // Last resort: build sprites from icon Texture2Ds by name.
                string[] iconTexNames =
                {
                    "Sniper__Icon", "Sniper__Magazine_Icon",
                    "SMG__Icon", "SMG__Magazine_Icon",
                    "RPG__Icon", "RPG__Magazine_Icon",
                    "AK47__Icon", "AK47__Magazine_Icon",
                    "MiniGun__Icon", "MiniGun__Magazine_Icon",
                };
                foreach (string texName in iconTexNames)
                {
                    if (sprites.ContainsKey(texName))
                        continue;
                    TryCacheSpriteFromTexture(assetBundle.LoadAsset(texName), texName);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to preload sprites: {ex.Message}");
            }
        }

        private static void TryCacheSprite(UnityEngine.Object asset)
        {
            if (asset == null)
                return;
            Sprite sprite = asset.As<Sprite>();
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
                return;
            sprites[sprite.name] = sprite;
        }

        private static void TryCacheSpriteFromTexture(UnityEngine.Object asset, string name)
        {
            if (asset == null || string.IsNullOrEmpty(name))
                return;
            Texture2D tex = asset.As<Texture2D>();
            if (tex == null)
                return;
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = name;
            sprites[name] = sprite;
        }

        /// <summary>
        /// Controllers live in the bundle as dependencies but are not always wired onto the
        /// prefab after Il2Cpp AssetBundle load. Resolve by name from LoadAllAssets.
        /// </summary>
        public static RuntimeAnimatorController GetAnimatorController(string name)
        {
            if (string.IsNullOrEmpty(name) || assetBundle == null)
                return null;

            EnsureAnimatorControllersLoaded();
            return animatorControllers.TryGetValue(name, out RuntimeAnimatorController controller)
                ? controller
                : null;
        }

        private static void EnsureAnimatorControllersLoaded()
        {
            if (animatorControllersLoaded || assetBundle == null)
                return;

            animatorControllersLoaded = true;
            try
            {
                TryLoadAnimatorByPath("assets/animatorcontroller/handgunanimator.controller", "HandgunAnimator");
                TryLoadAnimatorByPath("assets/animatorcontroller/minigunanimator.controller", "MiniGunAnimator");
                TryLoadAnimatorByPath("HandgunAnimator", "HandgunAnimator");
                TryLoadAnimatorByPath("MiniGunAnimator", "MiniGunAnimator");

                if (animatorControllers.Count == 0)
                {
                    UnityEngine.Object[] all = assetBundle.LoadAllAssets();
                    if (all != null)
                    {
                        foreach (UnityEngine.Object asset in all)
                        {
                            if (asset == null)
                                continue;
                            RuntimeAnimatorController controller = asset.As<RuntimeAnimatorController>();
                            if (controller == null)
                                continue;
                            if (!string.IsNullOrEmpty(controller.name))
                                animatorControllers[controller.name] = controller;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to preload animator controllers: {ex.Message}");
            }
        }

        private static void TryLoadAnimatorByPath(string path, string cacheName)
        {
            if (animatorControllers.ContainsKey(cacheName))
                return;

            UnityEngine.Object asset = assetBundle.LoadAsset(path);
            if (asset == null)
                return;

            RuntimeAnimatorController controller = asset.As<RuntimeAnimatorController>();
            if (controller == null)
                return;

            animatorControllers[cacheName] = controller;
            if (!string.IsNullOrEmpty(controller.name))
                animatorControllers[controller.name] = controller;
        }

        public static void StopProcess()
        {
            harmony?.UnpatchSelf();
            isInitialized = false;
        }
    }
}
