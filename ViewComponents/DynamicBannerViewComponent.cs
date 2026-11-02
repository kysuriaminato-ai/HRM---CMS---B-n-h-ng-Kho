using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;

namespace ERP_HRM_CMS.ViewComponents
{
    public class DynamicBannerViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public DynamicBannerViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string position = "HomeSlider")
        {
            var now = DateTime.Now;
            var banners = await _context.Banners
                .Where(b => b.IsActive && b.Position == position &&
                            (b.StartDate == null || b.StartDate <= now) &&
                            (b.EndDate == null || b.EndDate >= now))
                .OrderBy(b => b.OrderIndex)
                .ToListAsync();

            return View(banners);
        }
    }
}
