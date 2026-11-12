using ERP_HRM_CMS.Models.Entities;

namespace ERP.Application.Common.Interfaces
{
    public interface IGoldPriceProvider
    {
        Task<GoldPrice?> GetLatestAsync(CancellationToken cancellationToken = default);
    }
}

