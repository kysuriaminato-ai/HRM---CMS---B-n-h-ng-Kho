using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly AppDbContext _context;

        public SupplierController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH NHÀ CUNG CẤP
        [HttpGet]
        [KiemTraQuyen("scm.supplier.view")]
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(s => s.Code.ToLower().Contains(keyword) ||
                                         s.Name.ToLower().Contains(keyword) ||
                                         (s.Phone != null && s.Phone.Contains(keyword)) ||
                                         (s.Email != null && s.Email.ToLower().Contains(keyword)) ||
                                         (s.TaxCode != null && s.TaxCode.Contains(keyword)));
            }

            var suppliers = await query.OrderBy(s => s.Name).ToListAsync();
            ViewBag.Keyword = keyword;
            ViewBag.TotalDebt = suppliers.Sum(s => s.TotalDebt);
            return View(suppliers);
        }

        // 2. THÊM MỚI NHÀ CUNG CẤP
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("scm.supplier.manage")]
        public async Task<IActionResult> Create(Supplier model)
        {
            if (await _context.Suppliers.AnyAsync(s => s.Code == model.Code))
            {
                TempData["ErrorMessage"] = $"Mã nhà cung cấp '{model.Code}' đã tồn tại trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Suppliers.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thêm mới nhà cung cấp '{model.Name}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập vào chưa hợp lệ, vui lòng kiểm tra lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. CẬP NHẬT NHÀ CUNG CẤP
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("scm.supplier.manage")]
        public async Task<IActionResult> Edit(Supplier model)
        {
            var existing = await _context.Suppliers.FindAsync(model.Id);
            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                existing.Name = model.Name;
                existing.TaxCode = model.TaxCode;
                existing.Phone = model.Phone;
                existing.Email = model.Email;
                existing.Address = model.Address;
                existing.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật thông tin nhà cung cấp '{existing.Name}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu chỉnh sửa chưa hợp lệ.";
            }

            return RedirectToAction(nameof(Index));
        }

        // 4. API LẤY CHI TIẾT NCC (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers
                .Select(s => new
                {
                    s.Id,
                    s.Code,
                    s.Name,
                    s.TaxCode,
                    s.Phone,
                    s.Email,
                    s.Address,
                    s.TotalDebt,
                    s.IsActive
                })
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null) return NotFound();
            return Json(supplier);
        }
    }
}
