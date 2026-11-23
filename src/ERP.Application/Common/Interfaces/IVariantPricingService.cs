using ERP_HRM_CMS.Models.Entities;

namespace ERP.Application.Common.Interfaces
{
    public interface IVariantPricingService
    {
        decimal GetSellPrice(ProductVariant variant, GoldPrice? goldPrice);
    }
}

