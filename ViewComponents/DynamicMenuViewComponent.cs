using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.ViewComponents
{
    public class DynamicMenuViewComponent : ViewComponent
    {
        private static readonly TimeSpan MenuCacheTtl = TimeSpan.FromMinutes(30);

        private readonly AppDbContext _context;
        private readonly IAppCacheService _cache;

        public DynamicMenuViewComponent(AppDbContext context, IAppCacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var rootMenus = await _cache.GetOrCreateAsync(
                AppCacheKeys.DynamicMenuTree("MainNav"),
                async ct =>
                {
                    var menus = await _context.Menus
                        .AsNoTracking()
                        .Where(m => m.IsActive && m.Position == "MainNav")
                        .Include(m => m.SubMenus.Where(s => s.IsActive).OrderBy(s => s.OrderIndex))
                        .OrderBy(m => m.OrderIndex)
                        .ToListAsync(ct);

                    return menus.Where(m => m.ParentId == null).ToList();
                },
                MenuCacheTtl);

            return View(rootMenus);
        }
    }
}
