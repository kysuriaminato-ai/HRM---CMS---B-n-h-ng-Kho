using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.ViewComponents
{
    public class DynamicBannerViewComponent : ViewComponent
    {
        private static readonly TimeSpan BannerCacheTtl = TimeSpan.FromMinutes(10);

        private readonly AppDbContext _context;
        private readonly IAppCacheService _cache;

        public DynamicBannerViewComponent(AppDbContext context, IAppCacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync(string position = "HomeSlider")
        {
            var now = VietnamTime.Now;
            var allActiveInPosition = await _cache.GetOrCreateAsync(
                AppCacheKeys.DynamicBanner(position),
                ct => _context.Banners
                    .AsNoTracking()
                    .Where(b => b.IsActive && b.Position == position)
                    .OrderBy(b => b.OrderIndex)
                    .ToListAsync(ct),
                BannerCacheTtl);

            var banners = allActiveInPosition
                .Where(b => (b.StartDate == null || b.StartDate <= now) &&
                            (b.EndDate == null || b.EndDate >= now))
                .ToList();

            return View(banners);
        }
    }
}
