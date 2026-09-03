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

[assembly: MelonInfo(typeof(MoreGunsMod), "MoreGuns", "1.5.3", "Voidane")]
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

            try
            {
                NetworkController.SyncConfiguration();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to sync MoreGuns configuration: {ex}");
            }

            MelonCoroutines.Start(ItemRegistryPatch.RegisterWhenReady());
            MelonCoroutines.Start(ArmsDealerInterfacePatch.Watch());
            MelonCoroutines.Start(TrashRegistryPatch.Watch());
            MelonCoroutines.Start(ApplyEssentialPatchesWhenReady());

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

        /// <summary>
        /// Harmony is installed only after Main is up. Creating a Harmony instance (and PatchAll)
        /// during OnInitializeMelon installs MonoMod's JIT hook, which fatals the CLR
        /// (0x80131306 / CORPROF_E_FUNCTION_NOT_IL) when it later sees a Polyfill shim.
        /// </summary>
        private static IEnumerator ApplyEssentialPatchesWhenReady()
        {
            yield return null;
            yield return null;

            if (harmonyApplied)
                yield break;

#if IL2CPP
            harmony = new HarmonyLib.Harmony("com.voidane.moregunsil2cpp");
#else
            harmony = new HarmonyLib.Harmony("com.voidane.moregunsmono");
#endif
            SafeHarmony.Apply(
                harmony,
                typeof(SetEquippablePatch),
                typeof(DialoguePatch),
                typeof(Equippalbe_RangedWeaponPatch));
            harmonyApplied = true;
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

        public static void StopProcess()
        {
            harmony?.UnpatchSelf();
            isInitialized = false;
        }
    }
}
