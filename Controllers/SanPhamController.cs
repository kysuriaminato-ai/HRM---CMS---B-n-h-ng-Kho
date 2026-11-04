using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class SanPhamController : Controller
    {
        private readonly AppDbContext _context;

        public SanPhamController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("product.view")]
        public async Task<IActionResult> Index(string? keyword, int? categoryId, int? branchId, bool? lowStockOnly)
        {
            var branches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();
            int selectedBranchId = branchId ?? branches.FirstOrDefault()?.Id ?? 1;

            var query = from p in _context.Products.Include(p => p.Category)
                        join i in _context.Inventories.Where(inv => inv.BranchId == selectedBranchId)
                            on p.Id equals i.ProductId into pInv
                        from inv in pInv.DefaultIfEmpty()
                        select new ProductStockDto
                        {
                            Id = p.Id,
                            CategoryId = p.CategoryId,
                            SKU = p.SKU,
                            Barcode = p.Barcode,
                            Name = p.Name,
                            CategoryName = p.Category.Name,
                            Unit = p.Unit,
                            CostPrice = p.CostPrice,
                            SalePrice = p.SalePrice,
                            ImageUrl = p.ImageUrl,
                            IsActive = p.IsActive,
                            StockQuantity = inv != null ? inv.StockQuantity : 0,
                            MinStockAlert = inv != null ? inv.MinStockAlert : 5
                        };

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(kw) || p.SKU.ToLower().Contains(kw) || (p.Barcode != null && p.Barcode.Contains(kw)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (lowStockOnly == true)
                query = query.Where(p => p.StockQuantity <= p.MinStockAlert);

            var list = await query.OrderBy(p => p.Name).ToListAsync();

            ViewBag.Branches = new SelectList(branches, "Id", "Name", selectedBranchId);
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", categoryId);

            var vm = new ProductListViewModel
            {
                Products = list,
                Categories = await _context.Categories.ToListAsync(),
                Keyword = keyword,
                CategoryId = categoryId,
                SelectedBranchId = selectedBranchId,
                LowStockOnly = lowStockOnly
            };

            return View(vm);
        }

        [HttpGet]
        [KiemTraQuyen("product.manage")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View(new ProductCreateEditViewModel
            {
                SKU = MaChungTu.Sinh("SKU"),
                SalePrice = 100000,
                CostPrice = 70000,
                InitialStock = 20
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("product.manage")]
        public async Task<IActionResult> Create(ProductCreateEditViewModel model)
        {
            if (await _context.Products.AnyAsync(p => p.SKU == model.SKU))
                ModelState.AddModelError("SKU", "Mã SKU này đã tồn tại trên hệ thống.");

            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    CategoryId = model.CategoryId,
                    SKU = model.SKU.Trim().ToUpper(),
                    Barcode = model.Barcode,
                    Name = model.Name.Trim(),
                    Slug = string.IsNullOrWhiteSpace(model.Slug) ? model.SKU.ToLower() : model.Slug.Trim().ToLower(),
                    Unit = model.Unit,
                    CostPrice = model.CostPrice,
                    SalePrice = model.SalePrice,
                    Description = model.Description,
                    ImageUrl = model.ImageUrl,
                    IsActive = model.IsActive,
                    CreatedAt = VietnamTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Tạo bản ghi tồn kho cho tất cả các chi nhánh
                var branches = await _context.Branches.ToListAsync();
                foreach (var b in branches)
                {
                    _context.Inventories.Add(new Inventory
                    {
                        ProductId = product.Id,
                        BranchId = b.Id,
                        StockQuantity = model.InitialStock,
                        MinStockAlert = model.MinStockAlert,
                        MaxStockLimit = 1000,
                        UpdatedAt = VietnamTime.Now
                    });
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm mới hàng hóa '{product.Name}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }
    }
}