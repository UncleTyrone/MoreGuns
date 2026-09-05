using HarmonyLib;
using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using UnityEngine;

namespace MoreGuns.Patches
{
    [HarmonyPatch]
    public static class ArmsDealerInterfacePatch
    {
        private const string SHOP_OBJECT_NAME = "ArmsDealerInterface";
        private const float READY_POLL_INTERVAL = 0.25F;
        private const float READY_TIMEOUT = 60F;

        private static readonly List<ShopInterface> knownShops = new List<ShopInterface>();

        [HarmonyPatch(typeof(ShopInterface), "Awake")]
        [HarmonyPostfix]
        public static void PostfixAwake(ShopInterface __instance)
        {
            if (__instance == null || __instance.gameObject.name != SHOP_OBJECT_NAME)
                return;

            if (!knownShops.Contains(__instance))
                knownShops.Add(__instance);

            MelonCoroutines.Start(InjectWhenReady(__instance));
        }

        /// <summary>
        /// Harmony is applied after Main loads, so ShopInterface.Awake may already have run.
        /// Call this after PatchAll to catch the shop without relying on Awake.
        /// </summary>
        public static IEnumerator FindAndInjectAfterHarmony()
        {
            float waited = 0F;
            while (waited < READY_TIMEOUT)
            {
                GameObject go = GameObject.Find(SHOP_OBJECT_NAME);
                if (go != null)
                {
                    ShopInterface shop = go.GetComponent<ShopInterface>();
                    if (shop != null)
                    {
                        if (!knownShops.Contains(shop))
                            knownShops.Add(shop);

                        while (!WeaponBase.AllWeaponsLoaded && waited < READY_TIMEOUT)
                        {
                            waited += READY_POLL_INTERVAL;
                            yield return new WaitForSeconds(READY_POLL_INTERVAL);
                        }

                        yield return null;
                        Inject(shop);
                        yield break;
                    }
                }

                waited += READY_POLL_INTERVAL;
                yield return new WaitForSeconds(READY_POLL_INTERVAL);
            }

            MelonLogger.Warning($"Could not find {SHOP_OBJECT_NAME} after {READY_TIMEOUT}s.");
        }

        private static IEnumerator InjectWhenReady(ShopInterface shop)
        {
            float waited = 0F;
            while (!WeaponBase.AllWeaponsLoaded)
            {
                if (shop == null)
                    yield break;

                if (waited >= READY_TIMEOUT)
                {
                    MelonLogger.Warning($"Weapons were still loading after {READY_TIMEOUT}s; adding whatever is ready to {SHOP_OBJECT_NAME}.");
                    break;
                }

                waited += READY_POLL_INTERVAL;
                yield return new WaitForSeconds(READY_POLL_INTERVAL);
            }

            yield return null;
            yield return null;

            if (shop == null)
                yield break;

            Inject(shop);
        }

        public static void InjectAllKnownShops()
        {
            knownShops.RemoveAll(shop => shop == null);
            foreach (ShopInterface shop in knownShops)
            {
                try { Inject(shop); }
                catch (Exception ex) { MelonLogger.Warning($"Shop inject failed: {ex.Message}"); }
            }
        }

        private static void Inject(ShopInterface shop)
        {
            if (shop == null)
                return;

            if (shop.Listings == null)
            {
                MelonLogger.Warning($"{SHOP_OBJECT_NAME} has no listing collection; skipping MoreGuns listings.");
                return;
            }

            if (!knownShops.Contains(shop))
                knownShops.Add(shop);

            ShopListing template = FindTemplate(shop);
            int added = 0;

            foreach (WeaponBase weapon in WeaponBase.allWeapons)
            {
                try
                {
                    if (AddListing(shop, weapon.gunIntItemDef, template)) added++;
                    if (AddListing(shop, weapon.magIntItemDef, template)) added++;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Failed to add shop listing for {weapon?.ID}: {ex.Message}");
                }
            }

            if (added > 0)
            {
                GameAccess.RefreshShownItems(shop);
                MelonLogger.Msg($"Added {added} MoreGuns listing(s) to {SHOP_OBJECT_NAME}.");
            }
        }

        private static ShopListing FindTemplate(ShopInterface shop)
        {
            if (shop.Listings == null)
                return null;

            foreach (ShopListing listing in shop.Listings)
            {
                if (listing != null && listing.Item != null)
                    return listing;
            }

            return null;
        }

        private static bool AddListing(ShopInterface shop, StorableItemDefinition item, ShopListing template)
        {
            if (item == null || string.IsNullOrEmpty(item.ID))
                return false;

            if (shop.GetListing(item.ID) != null)
                return false;

            ShopListing listing = new ShopListing();
            listing.name = item.Name;
            listing.Item = item;
            listing.CanBeDelivered = template != null && template.CanBeDelivered;
            listing.LimitedStock = template != null && template.LimitedStock;
            listing.DefaultStock = template != null ? template.DefaultStock : 0;
            listing.RestockRate = template != null ? template.RestockRate : ShopListing.ERestockRate.Never;
            listing.ConditionalVisibility = false;

            if (item.ShopCategories == null)
            {
#if IL2CPP
                item.ShopCategories = new Il2CppSystem.Collections.Generic.List<ShopListing.CategoryInstance>();
#else
                item.ShopCategories = new List<ShopListing.CategoryInstance>();
#endif
            }

            shop.Listings.Add(listing);
            listing.Initialize(shop);

            if (listing.LimitedStock)
                listing.SetStock(listing.DefaultStock, false);

            GameAccess.CreateListingUI(shop, listing);
            return true;
        }

        public static void RefreshListings()
        {
            knownShops.RemoveAll(shop => shop == null);

            foreach (ShopInterface shop in knownShops)
                GameAccess.RefreshShownItems(shop);
        }
    }
}
