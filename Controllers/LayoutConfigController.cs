using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    [KiemTraQuyen("layout.manage")]
    public class LayoutConfigController : Controller
    {
        private readonly AppDbContext _context;

        public LayoutConfigController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var blocks = await _context.SiteLayouts
                .OrderBy(b => b.OrderIndex)
                .ToListAsync();

            return View(blocks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThaiBlock([FromBody] CapNhatBlockDto dto)
        {
            var block = await _context.SiteLayouts.FindAsync(dto.BlockId);
            if (block == null) return NotFound(new { success = false, message = "Khối giao diện không tồn tại" });

            block.IsActive = dto.IsActive;
            block.UpdatedAt = DateTime.Now;
            block.UpdatedBy = User.Identity?.Name ?? "Admin";

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật trạng thái khối thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SapXepThuTu([FromBody] List<ThuTuBlockDto> orderDtos)
        {
            if (orderDtos == null || !orderDtos.Any()) return BadRequest();

            foreach (var item in orderDtos)
            {
                var block = await _context.SiteLayouts.FindAsync(item.BlockId);
                if (block != null)
                {
                    block.OrderIndex = item.OrderIndex;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lưu thứ tự bố cục thành công!" });
        }
    }

    public class CapNhatBlockDto
    {
        public int BlockId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ThuTuBlockDto
    {
        public int BlockId { get; set; }
        public int OrderIndex { get; set; }
    }
}
