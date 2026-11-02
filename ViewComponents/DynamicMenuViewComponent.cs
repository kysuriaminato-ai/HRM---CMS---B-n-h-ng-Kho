using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.ViewComponents
{
    public class DynamicMenuViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public DynamicMenuViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var menus = await _context.Menus
                .Where(m => m.IsActive && m.Position == "MainNav")
                .Include(m => m.SubMenus.Where(s => s.IsActive).OrderBy(s => s.OrderIndex))
                .OrderBy(m => m.OrderIndex)
                .ToListAsync();

            var rootMenus = menus.Where(m => m.ParentId == null).ToList();
            return View(rootMenus);
        }
    }
}