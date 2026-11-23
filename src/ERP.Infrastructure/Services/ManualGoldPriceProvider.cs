using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Services
{
    public class ManualGoldPriceProvider : IGoldPriceProvider
    {
        private readonly AppDbContext _context;

        public ManualGoldPriceProvider(AppDbContext context)
        {
            _context = context;
        }

        public async Task<GoldPrice?> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            var today = VietnamTime.Today;
            return await _context.GoldPrices
                .AsNoTracking()
                .Where(g => g.EffectiveDate <= today)
                .OrderByDescending(g => g.EffectiveDate)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}

