using System;
using UnityEngine;

namespace MoreGuns
{
    public static class Tools
    {
        public static bool Alive(UnityEngine.Object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;
#if IL2CPP
            try
            {
                if (obj.WasCollected || obj.Pointer == IntPtr.Zero)
                    return false;
            }
            catch
            {
                return false;
            }
#endif
            try
            {
                return obj != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsLocalPlayerHeld(Equippable_RangedWeapon weapon)
        {
            if (!Alive(weapon))
                return false;
            try
            {
                Player local = Player.Local;
                if (local == null)
                    return false;

                Transform playerRoot = local.transform;
                Transform current = weapon.transform;
                for (int i = 0; i < 12 && Alive(current); i++)
                {
                    if (current == playerRoot)
                        return true;
                    current = current.parent;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static class LegalStatus
        {
            public static ELegalStatus StringConvertToELegalStatus(string eLegalStatus)
            {
                if (Enum.TryParse<ELegalStatus>(eLegalStatus, out ELegalStatus result))
                {
                    return result;
                }

                throw new ArgumentException($"Could not convert '{eLegalStatus}' to an ELegalStatus.");
            }
        }

        public static class Rank
        {
            public static FullRank Parse(string rank, string tier)
            {
                if (!Enum.TryParse<ERank>(rank?.Trim(), out ERank resultRank))
                {
                    throw new ArgumentException($"Could not convert '{rank}' to an ERank.");
                }

                if (!int.TryParse(tier?.Trim(), out int resultTier))
                {
                    throw new ArgumentException($"Could not convert '{tier}' to a rank tier.");
                }

                return new FullRank { Rank = resultRank, Tier = resultTier };
            }
        }
    }
}
