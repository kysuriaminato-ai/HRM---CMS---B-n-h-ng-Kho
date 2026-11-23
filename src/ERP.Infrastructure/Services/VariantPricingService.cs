using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Models.Entities;

namespace ERP.Infrastructure.Services
{
    public class VariantPricingService : IVariantPricingService
    {
        public decimal GetSellPrice(ProductVariant variant, GoldPrice? goldPrice)
        {
            ArgumentNullException.ThrowIfNull(variant);

            if (string.Equals(variant.PricingMode, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                if (!variant.FixedPrice.HasValue)
                {
                    throw new InvalidOperationException($"Biến thể '{variant.Sku}' có chế độ giá cố định nhưng chưa thiết lập FixedPrice.");
                }
                return variant.FixedPrice.Value;
            }

            if (string.Equals(variant.PricingMode, "GoldFormula", StringComparison.OrdinalIgnoreCase))
            {
                if (!variant.WeightGram.HasValue || variant.WeightGram.Value <= 0)
                {
                    throw new InvalidOperationException($"Biến thể '{variant.Sku}' tính giá theo vàng nhưng thiếu trọng lượng (WeightGram).");
                }

                var purity = variant.Material?.PurityFactor;
                if (!purity.HasValue || purity.Value <= 0)
                {
                    throw new InvalidOperationException($"Biến thể '{variant.Sku}' tính giá theo vàng nhưng chất liệu chưa có hệ số hàm lượng (PurityFactor).");
                }

                if (goldPrice == null || goldPrice.SellPricePerGram <= 0)
                {
                    throw new InvalidOperationException($"Không có giá vàng hợp lệ trong ngày để tính giá biến thể '{variant.Sku}'.");
                }

                // Công thức: (SellPricePerGram × WeightGram × PurityFactor) + LaborFee + StoneFee
                decimal rawGoldCost = goldPrice.SellPricePerGram * variant.WeightGram.Value * purity.Value;
                decimal total = rawGoldCost + variant.LaborFee + variant.StoneFee;

                // Làm tròn lên bội số 1.000 đ
                decimal rounded = Math.Ceiling(total / 1000m) * 1000m;
                return rounded;
            }

            throw new NotSupportedException($"Chế độ tính giá '{variant.PricingMode}' của biến thể '{variant.Sku}' không được hỗ trợ.");
        }
    }
}

