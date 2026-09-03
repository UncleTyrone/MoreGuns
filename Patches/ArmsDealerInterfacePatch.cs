using MelonLoader;
using MoreGuns.Guns;
using System.Collections;
using UnityEngine;

namespace MoreGuns.Patches
{
    public static class ArmsDealerInterfacePatch
    {
        private const string SHOP_OBJECT_NAME = "ArmsDealerInterface";
        private const float READY_POLL_INTERVAL = 0.25F;
        private const float READY_TIMEOUT = 60F;
        private const float WATCH_INTERVAL = 0.5F;

        private static readonly List<ShopInterface> knownShops = new List<ShopInterface>();

        public static IEnumerator Watch()
        {
            while (true)
            {
                ShopInterface shop = FindShop();
                if (shop != null && !knownShops.Contains(shop))
                    yield return InjectWhenReady(shop);

                yield return new WaitForSeconds(WATCH_INTERVAL);
            }
        }

        private static ShopInterface FindShop()
        {
            try
            {
                ShopInterface[] shops = UnityEngine.Object.FindObjectsOfType<ShopInterface>();
                if (shops == null)
                    return null;

                foreach (ShopInterface shop in shops)
                {
                    if (shop != null && shop.gameObject.name == SHOP_OBJECT_NAME)
                        return shop;
                }
            }
            catch
            {
                // shop type not ready this frame
            }

            return null;
        }

        /// <summary>
        /// Weapons are loaded from the asset bundle asynchronously, so the shop usually wakes up
        /// before they exist. Waiting here (instead of latching a one-shot flag) is what keeps the
        /// listings from being silently skipped.
        /// </summary>
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

        private static void Inject(ShopInterface shop)
        {
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
                if (AddListing(shop, weapon.gunIntItemDef, template)) added++;
                if (AddListing(shop, weapon.magIntItemDef, template)) added++;
            }

            if (added > 0)
            {
                GameAccess.RefreshShownItems(shop);
                MelonLogger.Msg($"Added {added} MoreGuns listing(s) to {SHOP_OBJECT_NAME}.");
            }
        }

        /// <summary>
        /// Copies stock/visibility behaviour off an existing vanilla listing so new shop fields
        /// stay correct across game updates instead of defaulting to zero stock.
        /// </summary>
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
            if (item == null)
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
