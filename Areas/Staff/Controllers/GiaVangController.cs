using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    [Area("Staff")]
    public class GiaVangController : Controller
    {
        private readonly AppDbContext _context;

        public GiaVangController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("product.gold")]
        public async Task<IActionResult> Index()
        {
            var prices = await _context.GoldPrices
                .OrderByDescending(g => g.EffectiveDate)
                .ToListAsync();
            return View(prices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("product.gold")]
        public async Task<IActionResult> LuuGiaVang(GoldPriceEditDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra trùng EffectiveDate
            bool exists = await _context.GoldPrices
                .AnyAsync(g => g.EffectiveDate == dto.EffectiveDate.Date && g.Id != dto.Id);

            if (exists)
            {
                ModelState.AddModelError("EffectiveDate", "Đã tồn tại biểu giá vàng cho ngày này.");
                return BadRequest(ModelState);
            }

            GoldPrice? price = null;
            if (dto.Id > 0)
            {
                price = await _context.GoldPrices.FindAsync(dto.Id);
                if (price == null) return NotFound();
            }
            else
            {
                price = new GoldPrice { CreatedAt = VietnamTime.Now };
                _context.GoldPrices.Add(price);
            }

            price.EffectiveDate = dto.EffectiveDate.Date;
            price.BuyPricePerGram = dto.BuyPricePerGram;
            price.SellPricePerGram = dto.SellPricePerGram;
            price.Note = dto.Note;
            price.UpdatedAt = VietnamTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}

