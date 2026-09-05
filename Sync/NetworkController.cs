using MelonLoader;
using MoreGuns.Guns;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MoreGuns.Sync
{
    public static class NetworkController
    {
        private const string IDENTIFICATION_PREFIX = "moreguns_settings";

        // Number of colon-separated fields SyncHostToLobbyPayload writes per weapon.
        private const int FIELD_COUNT = 25;

        private static readonly string version = typeof(MoreGunsMod).Assembly.GetName().Version.ToString();
        public static bool IsSynced { get; private set; } = false;
        public static StringBuilder payload = new StringBuilder();

        public static bool forceHost = false;
        public static bool forceClient = false;

        public static void SyncConfiguration()
        {
            bool isHost = Lobby.Instance?.IsHost == true;
            bool isClient = Lobby.Instance?.IsHost == false && Lobby.Instance?.IsInLobby == true;

            payload = new StringBuilder();
            payload.Append($"{IDENTIFICATION_PREFIX}_{version}|");

            if (isHost || forceHost)
            {
                MelonCoroutines.Start(SyncHostToLobbyPayload());
            }
            else if (isClient || forceClient)
            {
                MelonCoroutines.Start(WaitOnLobbyPayload());
            }
            else
            {
                foreach (var weapon in WeaponBase.allWeapons)
                {
                    weapon.ApplySettingsFromConfig();
                }
                MelonCoroutines.Start(SyncHostToLobbyPayload());
            }
        }

        public static IEnumerator WaitOnLobbyPayload()
        {
            while (true)
            {
                string data = SteamMatchmaking.GetLobbyData(Compat.LobbySteamId(), "MoreGunsConfig");
                if (!string.IsNullOrEmpty(data) && data.IndexOf('@') >= 0)
                {
                    HostToClientConfigurationSync(data);
                    yield break;
                }
                yield return new WaitForSeconds(1F);
            }
        }

        private static IEnumerator SyncHostToLobbyPayload()
        {
            foreach (WeaponBase weapon in WeaponBase.allWeapons)
            {
                if (weapon == null || weapon.gunRangedWeapon == null || weapon.config == null)
                    continue;

                weapon.ApplySettingsFromConfig();
                while (!weapon.IsConfigurationFinished)
                    yield return new WaitForSeconds(0.05F);

                payload.Append($"@{weapon.ID}" +
                $":" +
                $"{weapon.gunRangedWeapon.Damage}:" +
                $"{weapon.gunRangedWeapon.ImpactForce}:" +
                $"{weapon.gunRangedWeapon.MinAimFOVReduction}:" +
                $"{weapon.gunRangedWeapon.MaxAimFOVReduction}:" +
                $"{weapon.gunRangedWeapon.AccuracyChangeDuration}:" +
                $"{weapon.gunRangedWeapon.MagazineSize}" +
                $":" +
                $"{weapon.gunIntItemDef.Name}:" +
                $"{weapon.gunIntItemDef.Description}:" +
                $"{weapon.gunIntItemDef.legalStatus}:" +
                $"{weapon.gunIntItemDef.RequiredRank.Rank}:" +
                $"{weapon.gunIntItemDef.RequiredRank.Tier}" +
                $":" +
                $"{weapon.magIntItemDef.Name}:" +
                $"{weapon.magIntItemDef.Description}:" +
                $"{weapon.magIntItemDef.legalStatus}:" +
                $"{weapon.magIntItemDef.RequiredRank.Rank}:" +
                $"{weapon.magIntItemDef.RequiredRank.Tier}" +
                $":" +
                $"{weapon.rangedGun.Name}:" +
                $"{weapon.rangedGun.Price}:" +
                $"{weapon.rangedGun.IsAvailable}:" +
                $"{weapon.rangedGun.NotAvailableReason}" +
                $":" +
                $"{weapon.ammoGun.Name}:" +
                $"{weapon.ammoGun.Price}:" +
                $"{weapon.ammoGun.IsAvailable}:" +
                $"{weapon.ammoGun.NotAvailableReason}");
            }

            // Publish only after the full payload is built — early SetLobbyData sent a header-only string.
            try
            {
                if (Lobby.Instance != null && (Lobby.Instance.IsHost || forceHost))
                    Lobby.Instance.SetLobbyData("MoreGunsConfig", payload.ToString());
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to publish MoreGuns lobby config: {ex.Message}");
            }
        }

        private static void HostToClientConfigurationSync(string data)
        {
            string[] dataVersion = data.Split('|').Where(item => !string.IsNullOrEmpty(item)).ToArray();
            if (!IsModValidForSync(dataVersion[0]))
            {
                MelonLogger.Warning($"MoreGuns is outdated with the host or server.");
                MelonLogger.Warning($"Your Version: {IDENTIFICATION_PREFIX}_{version}, Host Version: {dataVersion[0]}");
            }

            string[] weapons = dataVersion[1].Split('@').Where(item => !string.IsNullOrEmpty(item)).ToArray();
            foreach (string weapon in weapons)
            {
                string[] fields = weapon.Split(':');

                if (fields.Length < FIELD_COUNT)
                {
                    MelonLogger.Warning($"Skipping malformed weapon entry in host payload (got {fields.Length} fields, expected {FIELD_COUNT}).");
                    continue;
                }

                if (WeaponBase.weaponsByName.TryGetValue(fields[0], out WeaponBase weap))
                {
                    if (!float.TryParse(fields[1], out float gunRangedDamage)) continue;
                    if (!float.TryParse(fields[2], out float gunRangedImpactForce)) continue;
                    if (!float.TryParse(fields[3], out float gunRangedMinAimFOVReduction)) continue;
                    if (!float.TryParse(fields[4], out float gunRangedMaxAimFOVReduction)) continue;
                    if (!float.TryParse(fields[5], out float gunRangedAccuracyChangeDuration)) continue;
                    if (!int.TryParse(fields[6], out int gunRangedMagazineSize)) continue;

                    string gunIIDName = fields[7];
                    string gunIIDDescription = fields[8];
                    ELegalStatus gunIIDELegalStatus = Tools.LegalStatus.StringConvertToELegalStatus(fields[9]);
                    FullRank gunIIDRequiredRank = Tools.Rank.Parse(fields[10], fields[11]);

                    string magIIDName = fields[12];
                    string magIIDDescription = fields[13];
                    ELegalStatus magIIDELegalStatus = Tools.LegalStatus.StringConvertToELegalStatus(fields[14]);
                    FullRank magIIDRequiredRank = Tools.Rank.Parse(fields[15], fields[16]);

                    string rangedGunName = fields[17];
                    if (!float.TryParse(fields[18], out float rangedGunPrice)) continue;
                    if (!bool.TryParse(fields[19], out bool rangedGunAvailable)) continue;
                    string rangedGunNonAvailableReason = fields[20];

                    string ammoGunName = fields[21];
                    if (!float.TryParse(fields[22], out float ammoGunPrice)) continue;
                    if (!bool.TryParse(fields[23], out bool ammoGunAvailable)) continue;
                    string ammoGunNonAvailableReason = fields[24];

                    weap.gunRangedWeapon.Damage = gunRangedDamage;
                    weap.gunRangedWeapon.ImpactForce = gunRangedImpactForce;
                    weap.gunRangedWeapon.MinAimFOVReduction = gunRangedMinAimFOVReduction;
                    weap.gunRangedWeapon.MaxAimFOVReduction = gunRangedMaxAimFOVReduction;
                    weap.gunRangedWeapon.AccuracyChangeDuration = gunRangedAccuracyChangeDuration;
                    weap.gunRangedWeapon.MagazineSize = gunRangedMagazineSize;

                    weap.gunIntItemDef.Name = gunIIDName;
                    weap.gunIntItemDef.Description = gunIIDDescription;
                    weap.gunIntItemDef.legalStatus = gunIIDELegalStatus;
                    weap.gunIntItemDef.RequiredRank = gunIIDRequiredRank;

                    weap.magIntItemDef.Name = magIIDName;
                    weap.magIntItemDef.Description = magIIDDescription;
                    weap.magIntItemDef.legalStatus = magIIDELegalStatus;
                    weap.magIntItemDef.RequiredRank = magIIDRequiredRank;

                    weap.gunIntItemDef.Name = rangedGunName;
                    weap.gunIntItemDef.BasePurchasePrice = rangedGunPrice;
                    weap.rangedGun.IsAvailable = rangedGunAvailable;
                    weap.rangedGun.NotAvailableReason = rangedGunNonAvailableReason;

                    weap.magIntItemDef.Name = ammoGunName;
                    weap.magIntItemDef.BasePurchasePrice = ammoGunPrice;
                    weap.ammoGun.IsAvailable = ammoGunAvailable;
                    weap.ammoGun.NotAvailableReason = ammoGunNonAvailableReason;

                    weap.RefreshShopListings();
                }
            }
        }

        private static bool IsModValidForSync(string pIdentify)
        {
            string identify = $"{IDENTIFICATION_PREFIX}_{version}";
            return (pIdentify == identify);
        }
    }
}
