using System;

namespace ERP_HRM_CMS.Helpers
{
    public sealed class LoyaltyTierInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal NextMilestone { get; set; }
        public string NextTierName { get; set; } = string.Empty;
        public decimal RemainingAmount { get; set; }
        public decimal ProgressPercent { get; set; }
        public string Color { get; set; } = string.Empty;

        // Tiện ích tương thích và alias
        public string TierCode => Code;
        public string TierName => Name;
        public decimal NextTierMilestone => NextMilestone;
        public decimal AmountToNextTier => RemainingAmount;
        public double Progress => (double)ProgressPercent;

        public void Deconstruct(out string code, out string name, out decimal nextMilestone, out decimal progressPercent)
        {
            code = Code;
            name = Name;
            nextMilestone = NextMilestone;
            progressPercent = ProgressPercent;
        }
    }

    public static class LoyaltyHelper
    {
        public const decimal GoldMilestone = 50_000_000m;
        public const decimal PlatinumMilestone = 200_000_000m;
        public const decimal DiamondMilestone = 500_000_000m;

        public static LoyaltyTierInfo GetTier(decimal totalRevenue)
        {
            if (totalRevenue < GoldMilestone)
            {
                decimal progress = totalRevenue <= 0
                    ? 0m
                    : Math.Round((totalRevenue / GoldMilestone) * 100m, 1);
                progress = Math.Clamp(progress, 0m, 100m);

                return new LoyaltyTierInfo
                {
                    Code = "SILVER",
                    Name = "Bạc",
                    NextMilestone = GoldMilestone,
                    NextTierName = "Vàng",
                    RemainingAmount = Math.Max(0m, GoldMilestone - totalRevenue),
                    ProgressPercent = progress,
                    Color = "#b9c2cc"
                };
            }

            if (totalRevenue < PlatinumMilestone)
            {
                decimal tierRange = PlatinumMilestone - GoldMilestone;
                decimal spentInTier = totalRevenue - GoldMilestone;
                decimal progress = Math.Round((spentInTier / tierRange) * 100m, 1);
                progress = Math.Clamp(progress, 0m, 100m);

                return new LoyaltyTierInfo
                {
                    Code = "GOLD",
                    Name = "Vàng",
                    NextMilestone = PlatinumMilestone,
                    NextTierName = "Bạch Kim",
                    RemainingAmount = Math.Max(0m, PlatinumMilestone - totalRevenue),
                    ProgressPercent = progress,
                    Color = "#d4af37"
                };
            }

            if (totalRevenue < DiamondMilestone)
            {
                decimal tierRange = DiamondMilestone - PlatinumMilestone;
                decimal spentInTier = totalRevenue - PlatinumMilestone;
                decimal progress = Math.Round((spentInTier / tierRange) * 100m, 1);
                progress = Math.Clamp(progress, 0m, 100m);

                return new LoyaltyTierInfo
                {
                    Code = "PLATINUM",
                    Name = "Bạch Kim",
                    NextMilestone = DiamondMilestone,
                    NextTierName = "Kim Cương",
                    RemainingAmount = Math.Max(0m, DiamondMilestone - totalRevenue),
                    ProgressPercent = progress,
                    Color = "#e5e4e2"
                };
            }

            return new LoyaltyTierInfo
            {
                Code = "DIAMOND",
                Name = "Kim Cương",
                NextMilestone = 0m,
                NextTierName = string.Empty,
                RemainingAmount = 0m,
                ProgressPercent = 100m,
                Color = "#8fd3ff"
            };
        }

        public static LoyaltyTierInfo GetTierInfo(decimal totalRevenue) => GetTier(totalRevenue);
        public static LoyaltyTierInfo CalculateTier(decimal totalRevenue) => GetTier(totalRevenue);
    }
}

