using System;

namespace MoreGuns
{
    public static class Tools
    {
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
