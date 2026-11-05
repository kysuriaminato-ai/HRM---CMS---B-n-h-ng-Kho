using ERP.Application.Common.Interfaces;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.Kho;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class KhoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<KhoController> _logger;
        private readonly ICurrentUserService _currentUser;
        private readonly IDataScopeService _dataScope;

        public KhoController(
            AppDbContext context,
            ILogger<KhoController> logger,
            ICurrentUserService currentUser,
            IDataScopeService dataScope)
        {
            _context = context;
            _logger = logger;
            _currentUser = currentUser;
            _dataScope = dataScope;
        }

        private int CurrentUserId => _currentUser.GetRequiredUserId();

        private async Task<List<Branch>> GetScopedBranchesAsync()
        {
            var query = _context.Branches.AsQueryable();
            if (_dataScope.GetOperationsScopeLevel() == DataScopeLevel.Branch && _currentUser.BranchId.HasValue)
            {
                int myBranchId = _currentUser.BranchId.Value;
                query = query.Where(b => b.Id == myBranchId);
            }
            return await query.OrderBy(b => b.Name).ToListAsync();
        }

        // 1. DASHBOARD BẢNG ĐIỀU KHIỂN TỒN KHO THỰC TẾ & CẢNH BÁO
        [HttpGet]
        [KiemTraQuyen("inventory.view")]
        public async Task<IActionResult> Index(int? branchId, string? keyword, bool? lowStockOnly)
        {
            var branches = await GetScopedBranchesAsync();
            int? resolvedBranch = _dataScope.ResolveAllowedBranchId(branchId);
            int selectedBranch = resolvedBranch ?? branches.FirstOrDefault()?.Id ?? 1;

            var scopedInventories = _dataScope.ApplyInventoryScope(_context.Inventories);
            var query = from inv in scopedInventories.Include(i => i.Branch).Include(i => i.Product).ThenInclude(p => p.Category)
                        where inv.BranchId == selectedBranch
                        select new InventoryStockDto
                        {
                            Id = inv.Id,
                            ProductId = inv.ProductId,
                            SKU = inv.Product.SKU,
                            Barcode = inv.Product.Barcode,
                            ProductName = inv.Product.Name,
                            CategoryName = inv.Product.Category.Name,
                            Unit = inv.Product.Unit,
                            CostPrice = inv.Product.CostPrice,
                            SalePrice = inv.Product.SalePrice,
                            BranchId = inv.BranchId,
                            BranchName = inv.Branch.Name,
                            StockQuantity = inv.StockQuantity,
                            MinStockAlert = inv.MinStockAlert,
                            MaxStockLimit = inv.MaxStockLimit,
                            UpdatedAt = inv.UpdatedAt
                        };

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(x => x.ProductName.ToLower().Contains(kw) ||
                                         x.SKU.ToLower().Contains(kw) ||
                                         (x.Barcode != null && x.Barcode.Contains(kw)));
            }

            if (lowStockOnly == true)
            {
                query = query.Where(x => x.StockQuantity <= x.MinStockAlert);
            }

            var stockList = await query.OrderBy(x => x.StockQuantity).ToListAsync();

            // KPI toàn chi nhánh được tính trên database (không tải toàn bộ dòng tồn vào bộ nhớ)
            var tongHop = await InventoryStockService.TongHopTonKhoAsync(_context, selectedBranch);

            var today = VietnamTime.Today;
            int todayTransCount = await _dataScope.ApplyInventoryTransactionScope(_context.InventoryTransactions)
                .CountAsync(t => t.BranchId == selectedBranch && t.CreatedAt >= today && t.CreatedAt < today.AddDays(1));

            var vm = new WarehouseDashboardViewModel
            {
                TotalProducts = tongHop.SoMatHang,
                TotalBranches = branches.Count,
                TotalLowStockItems = tongHop.SoMatHangCanhBao,
                TotalInventoryAssetValue = tongHop.GiaTriTonKho,
                TodayTransactionCount = todayTransCount,
                StockList = stockList,
                Branches = branches,
                SelectedBranchId = selectedBranch,
                Keyword = keyword,
                LowStockOnly = lowStockOnly
            };

            ViewBag.Branches = new SelectList(branches, "Id", "Name", selectedBranch);
            return View(vm);
        }

        // 2. LẬP PHIẾU NHẬP KHO (INVENTORY IN)
        [HttpGet]
        [KiemTraQuyen("inventory.transaction")]
        public async Task<IActionResult> NhapKho(int? branchId)
        {
            int? resolvedBranch = _dataScope.ResolveAllowedBranchId(branchId);
            await LoadDropdowns(resolvedBranch);
            return View(new StockTransactionCreateDto
            {
                BranchId = resolvedBranch ?? 1,
                TransactionType = "IN",
                Quantity = 10,
                UnitPrice = 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("inventory.transaction")]
        public async Task<IActionResult> NhapKho(StockTransactionCreateDto dto)
        {
            if (!_dataScope.CanAccessBranch(dto.BranchId))
                return Forbid();

            if (dto.Quantity <= 0)
                ModelState.AddModelError("Quantity", "Số lượng nhập kho phải lớn hơn 0.");

            if (ModelState.IsValid)
            {
                // Sử dụng Database Transaction để đảm bảo tính toàn vẹn tuyệt đối
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var product = await _context.Products.FindAsync(dto.ProductId);
                    if (product == null)
                    {
                        ModelState.AddModelError("ProductId", "Sản phẩm không tồn tại.");
                        await LoadDropdowns(dto.BranchId);
                        return View(dto);
                    }

                    // 1. Tạo phiếu giao dịch kho
                    string transCode = await MaChungTu.SinhDuyNhatAsync("PNK", ma => _context.InventoryTransactions.AnyAsync(t => t.TransactionCode == ma));
                    var transaction = new InventoryTransaction
                    {
                        TransactionCode = transCode,
                        TransactionType = "IN",
                        BranchId = dto.BranchId,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice > 0 ? dto.UnitPrice : product.CostPrice,
                        Note = string.IsNullOrWhiteSpace(dto.Note) ? "Nhập hàng từ nhà cung cấp" : dto.Note.Trim(),
                        CreatedBy = CurrentUserId,
                        CreatedAt = VietnamTime.Now
                    };
                    _context.InventoryTransactions.Add(transaction);

                    // 2. Cộng tồn bằng UPDATE cộng dồn trên database (không mất cập nhật khi đồng thời).
                    //    Chưa có dòng tồn thì thêm mới; nếu hai giao dịch cùng thêm, unique index sẽ chặn và hủy giao dịch.
                    bool daCongTon = await InventoryStockService.CongTonAsync(_context, dto.BranchId, dto.ProductId, dto.Quantity, VietnamTime.Now);
                    if (!daCongTon)
                    {
                        _context.Inventories.Add(new Inventory
                        {
                            ProductId = dto.ProductId,
                            BranchId = dto.BranchId,
                            StockQuantity = dto.Quantity,
                            MinStockAlert = 5,
                            MaxStockLimit = 1000,
                            UpdatedAt = VietnamTime.Now
                        });
                    }

                    // 3. Cập nhật giá vốn bình quân nếu có đơn giá mới
                    if (dto.UnitPrice > 0)
                    {
                        product.CostPrice = dto.UnitPrice;
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Lập phiếu nhập kho {transCode} thành công! Đã cộng thêm {dto.Quantity} {product.Unit}.";
                    return RedirectToAction(nameof(Index), new { branchId = dto.BranchId });
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi nhập kho."); TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình nhập kho. Vui lòng thử lại hoặc liên hệ quản trị viên.";
                }
            }

            await LoadDropdowns(dto.BranchId);
            return View(dto);
        }

        // 3. LẬP PHIẾU XUẤT KHO (INVENTORY OUT)
        [HttpGet]
        [KiemTraQuyen("inventory.transaction")]
        public async Task<IActionResult> XuatKho(int? branchId)
        {
            int? resolvedBranch = _dataScope.ResolveAllowedBranchId(branchId);
            await LoadDropdowns(resolvedBranch);
            return View(new StockTransactionCreateDto
            {
                BranchId = resolvedBranch ?? 1,
                TransactionType = "OUT",
                Quantity = 1,
                UnitPrice = 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("inventory.transaction")]
        public async Task<IActionResult> XuatKho(StockTransactionCreateDto dto)
        {
            if (!_dataScope.CanAccessBranch(dto.BranchId))
                return Forbid();

            // Kiểm tra sơ bộ để hiện thông báo; việc trừ tồn thật sự được kiểm tra lại trong transaction bên dưới
            var inventory = await _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId && i.BranchId == dto.BranchId);

            if (inventory == null || inventory.StockQuantity < dto.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Tồn kho khả dụng không đủ để xuất! Hiện tại chỉ còn {inventory?.StockQuantity ?? 0} {inventory?.Product?.Unit ?? "sản phẩm"}.");
            }

            if (ModelState.IsValid)
            {
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Trừ tồn bằng UPDATE có điều kiện (StockQuantity >= số xuất). Không đủ thì hủy toàn bộ transaction.
                    bool duDeTru = await InventoryStockService.TruTonAsync(_context, dto.BranchId, dto.ProductId, dto.Quantity, VietnamTime.Now);
                    if (!duDeTru)
                    {
                        await dbTransaction.RollbackAsync();
                        ModelState.AddModelError("Quantity", "Tồn kho vừa thay đổi và không còn đủ để xuất. Vui lòng kiểm tra lại.");
                        await LoadDropdowns(dto.BranchId);
                        return View(dto);
                    }

                    string transCode = await MaChungTu.SinhDuyNhatAsync("PXK", ma => _context.InventoryTransactions.AnyAsync(t => t.TransactionCode == ma));
                    var transaction = new InventoryTransaction
                    {
                        TransactionCode = transCode,
                        TransactionType = "OUT",
                        BranchId = dto.BranchId,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice > 0 ? dto.UnitPrice : inventory!.Product.CostPrice,
                        Note = string.IsNullOrWhiteSpace(dto.Note) ? "Xuất kho điều chuyển / hao hụt" : dto.Note.Trim(),
                        CreatedBy = CurrentUserId,
                        CreatedAt = VietnamTime.Now
                    };
                    _context.InventoryTransactions.Add(transaction);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Lập phiếu xuất kho {transCode} thành công! Đã trừ {dto.Quantity} {inventory!.Product.Unit}.";
                    return RedirectToAction(nameof(Index), new { branchId = dto.BranchId });
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi xuất kho."); TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xuất kho. Vui lòng thử lại hoặc liên hệ quản trị viên.";
                }
            }

            await LoadDropdowns(dto.BranchId);
            return View(dto);
        }

        // 4. LỊCH SỬ THẺ KHO & NHẬT KÝ BIẾN ĐỘNG
        [HttpGet]
        [KiemTraQuyen("inventory.view")]
        public async Task<IActionResult> LichSuGiaoDich(int? branchId, string? type, DateTime? fromDate, DateTime? toDate, string? keyword)
        {
            var branches = await GetScopedBranchesAsync();
            int? resolvedBranch = _dataScope.ResolveAllowedBranchId(branchId);

            var query = _dataScope.ApplyInventoryTransactionScope(_context.InventoryTransactions)
                .Include(t => t.Branch)
                .Include(t => t.Product)
                .Include(t => t.Creator)
                .AsQueryable();

            if (resolvedBranch.HasValue && resolvedBranch > 0)
                query = query.Where(t => t.BranchId == resolvedBranch.Value);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(t => t.TransactionType == type);

            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(t => t.CreatedAt < toDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(t => t.TransactionCode.ToLower().Contains(kw) ||
                                         t.Product.Name.ToLower().Contains(kw) ||
                                         t.Product.SKU.ToLower().Contains(kw));
            }

            var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var vm = new InventoryTransactionListViewModel
            {
                Transactions = list,
                Branches = branches,
                SelectedBranchId = resolvedBranch,
                TransactionType = type,
                FromDate = fromDate,
                ToDate = toDate,
                Keyword = keyword,
                TotalInQuantity = list.Where(t => t.TransactionType == "IN").Sum(t => t.Quantity),
                TotalOutQuantity = list.Where(t => t.TransactionType == "OUT").Sum(t => t.Quantity),
                TotalInValue = list.Where(t => t.TransactionType == "IN").Sum(t => t.Quantity * t.UnitPrice),
                TotalOutValue = list.Where(t => t.TransactionType == "OUT").Sum(t => t.Quantity * t.UnitPrice)
            };

            ViewBag.Branches = new SelectList(branches, "Id", "Name", resolvedBranch);
            return View(vm);
        }

        // 5. API LẤY NHANH TỒN KHO VÀ GIÁ VỐN CHO AJAX
        [HttpGet]
        [KiemTraQuyen("inventory.view,inventory.transaction,order.pos")]
        public async Task<IActionResult> LayThongTinTonKho(int productId, int branchId)
        {
            if (!_dataScope.CanAccessBranch(branchId))
                return Forbid();

            var inv = await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == branchId);

            if (inv == null)
            {
                var prod = await _context.Products.FindAsync(productId);
                return Json(new { stock = 0, costPrice = prod?.CostPrice ?? 0, unit = prod?.Unit ?? "Cái" });
            }

            return Json(new
            {
                stock = inv.StockQuantity,
                costPrice = inv.Product.CostPrice,
                unit = inv.Product.Unit,
                minAlert = inv.MinStockAlert
            });
        }

        // 6. ĐIỀU CHUYỂN KHO GIỮA CÁC CHI NHÁNH
        [HttpGet]
        [KiemTraQuyen("inventory.transaction")]
        public async Task<IActionResult> DieuChuyen(int? fromBranchId)
        {
            var fromBranches = await GetScopedBranchesAsync();
            var allBranches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();
            var products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

            int resolvedFrom = _dataScope.ResolveAllowedBranchId(fromBranchId) ?? fromBranches.FirstOrDefault()?.Id ?? 1;
            int defaultTo = allBranches.FirstOrDefault(b => b.Id != resolvedFrom)?.Id ?? resolvedFrom;

            ViewBag.FromBranches = new SelectList(fromBranches, "Id", "Name", resolvedFrom);
            ViewBag.ToBranches = new SelectList(allBranches, "Id", "Name", defaultTo);
            ViewBag.Products = new SelectList(products, "Id", "Name");

            return View(new StockTransferDto
            {
                FromBranchId = resolvedFrom,
                ToBranchId = defaultTo,
                Quantity = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("inventory.transaction")]
        public async Task<IActionResult> DieuChuyen(StockTransferDto dto)
        {
            if (!_dataScope.CanAccessBranch(dto.FromBranchId))
                return Forbid();

            if (dto.FromBranchId == dto.ToBranchId)
            {
                ModelState.AddModelError("ToBranchId", "Chi nhánh nhận hàng phải khác chi nhánh xuất hàng.");
            }

            if (dto.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng điều chuyển phải lớn hơn 0.");
            }

            var fromInventory = await _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId && i.BranchId == dto.FromBranchId);

            if (fromInventory == null || fromInventory.StockQuantity < dto.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Tồn kho tại chi nhánh xuất không đủ! Chỉ còn {fromInventory?.StockQuantity ?? 0} {fromInventory?.Product?.Unit ?? "sản phẩm"}.");
            }

            if (ModelState.IsValid)
            {
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string transCode = await MaChungTu.SinhDuyNhatAsync("DCK", ma => _context.InventoryTransactions.AnyAsync(t => t.TransactionCode == ma));
                    decimal unitCost = fromInventory!.Product.CostPrice;

                    // 1. Trừ tồn chi nhánh xuất bằng UPDATE có điều kiện; không đủ thì hủy toàn bộ điều chuyển
                    bool duDeTru = await InventoryStockService.TruTonAsync(_context, dto.FromBranchId, dto.ProductId, dto.Quantity, VietnamTime.Now);
                    if (!duDeTru)
                    {
                        await dbTransaction.RollbackAsync();
                        ModelState.AddModelError("Quantity", "Tồn kho chi nhánh xuất vừa thay đổi và không còn đủ. Vui lòng kiểm tra lại.");
                        var branchesLoi = await GetScopedBranchesAsync();
                        var allBranchesLoi = await _context.Branches.OrderBy(b => b.Name).ToListAsync();
                        var productsLoi = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
                        ViewBag.FromBranches = new SelectList(branchesLoi, "Id", "Name", dto.FromBranchId);
                        ViewBag.ToBranches = new SelectList(allBranchesLoi, "Id", "Name", dto.ToBranchId);
                        ViewBag.Products = new SelectList(productsLoi, "Id", "Name", dto.ProductId);
                        return View(dto);
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"{transCode}-OUT",
                        TransactionType = "TRANSFER_OUT",
                        BranchId = dto.FromBranchId,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UnitPrice = unitCost,
                        Note = $"Điều chuyển kho sang chi nhánh #{dto.ToBranchId}. " + (dto.Note ?? string.Empty),
                        CreatedBy = CurrentUserId,
                        CreatedAt = VietnamTime.Now
                    });

                    // 2. Cộng tồn kho tại chi nhánh nhận & ghi giao dịch IN
                    // Cộng tồn chi nhánh nhận bằng UPDATE cộng dồn; chưa có dòng tồn thì thêm mới
                    bool daCongTon = await InventoryStockService.CongTonAsync(_context, dto.ToBranchId, dto.ProductId, dto.Quantity, VietnamTime.Now);
                    if (!daCongTon)
                    {
                        _context.Inventories.Add(new Inventory
                        {
                            ProductId = dto.ProductId,
                            BranchId = dto.ToBranchId,
                            StockQuantity = dto.Quantity,
                            MinStockAlert = 5,
                            MaxStockLimit = 1000,
                            UpdatedAt = VietnamTime.Now
                        });
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"{transCode}-IN",
                        TransactionType = "TRANSFER_IN",
                        BranchId = dto.ToBranchId,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UnitPrice = unitCost,
                        Note = $"Nhận điều chuyển từ chi nhánh #{dto.FromBranchId}. " + (dto.Note ?? string.Empty),
                        CreatedBy = CurrentUserId,
                        CreatedAt = VietnamTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Điều chuyển {dto.Quantity} {fromInventory.Product.Unit} thành công (Mã: {transCode})!";
                    return RedirectToAction(nameof(Index), new { branchId = dto.FromBranchId });
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi điều chuyển kho."); TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình điều chuyển kho. Vui lòng thử lại hoặc liên hệ quản trị viên.";
                }
            }

            var branches = await GetScopedBranchesAsync();
            var allBranches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();
            var products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            ViewBag.FromBranches = new SelectList(branches, "Id", "Name", dto.FromBranchId);
            ViewBag.ToBranches = new SelectList(allBranches, "Id", "Name", dto.ToBranchId);
            ViewBag.Products = new SelectList(products, "Id", "Name", dto.ProductId);

            return View(dto);
        }

        // Helper nạp dữ liệu Dropdowns
        private async Task LoadDropdowns(int? selectedBranchId)
        {
            var branches = await GetScopedBranchesAsync();
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, Display = $"{p.Name} ({p.SKU}) - Giá vốn: {p.CostPrice:N0} đ" })
                .ToListAsync();

            ViewBag.Branches = new SelectList(branches, "Id", "Name", selectedBranchId ?? branches.FirstOrDefault()?.Id ?? 1);
            ViewBag.Products = new SelectList(products, "Id", "Display");
        }

        // =========================================================================
        // 5. KIỂM KÊ KHO & CÂN BẰNG KHO (PHIÊN 3)
        // =========================================================================
        [HttpGet]
        [KiemTraQuyen("inventory.stocktaking")]
        public async Task<IActionResult> KiemKe(int? branchId)
        {
            var branches = await GetScopedBranchesAsync();
            int selectedBranch = _dataScope.ResolveAllowedBranchId(branchId) ?? branches.FirstOrDefault()?.Id ?? 1;

            var inventories = await _dataScope.ApplyInventoryScope(_context.Inventories)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Where(i => i.BranchId == selectedBranch)
                .OrderBy(i => i.Product.Name)
                .ToListAsync();

            var recentSheets = await _context.StocktakingSheets
                .Include(s => s.Creator)
                .Where(s => s.BranchId == selectedBranch)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.Branches = branches;
            ViewBag.SelectedBranchId = selectedBranch;
            ViewBag.RecentSheets = recentSheets;

            return View(inventories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("inventory.stocktaking")]
        public async Task<IActionResult> CanBangKho(int branchId, int[] productIds, int[] actualQuantities, string[] reasons, string? note)
        {
            if (!_dataScope.CanAccessBranch(branchId))
                return Forbid();

            if (productIds == null || productIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Không có dữ liệu mặt hàng kiểm kê.";
                return RedirectToAction(nameof(KiemKe), new { branchId });
            }

            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null) return NotFound();

            var sheet = new StocktakingSheet
            {
                Code = $"KK{VietnamTime.Now:yyMMddHHmmss}",
                BranchId = branchId,
                CheckDate = VietnamTime.Now,
                CreatedBy = CurrentUserId,
                Note = note,
                Status = "Balanced",
                CreatedAt = VietnamTime.Now
            };

            // Cả phiên kiểm kê nằm trong một transaction: nếu tồn đổi giữa chừng thì hủy toàn bộ phiên
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            int totalSys = 0;
            int totalAct = 0;
            int totalDiff = 0;
            decimal totalDiffVal = 0;

            for (int i = 0; i < productIds.Length; i++)
            {
                int pid = productIds[i];
                int actQty = actualQuantities[i];
                string reason = (reasons != null && i < reasons.Length) ? reasons[i] : "";

                var inv = await _context.Inventories
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .FirstOrDefaultAsync(x => x.BranchId == branchId && x.ProductId == pid);

                if (inv != null)
                {
                    int sysQty = inv.StockQuantity;
                    int diffQty = actQty - sysQty;
                    decimal diffVal = diffQty * inv.Product.CostPrice;

                    totalSys += sysQty;
                    totalAct += actQty;
                    totalDiff += diffQty;
                    totalDiffVal += diffVal;

                    sheet.StocktakingDetails.Add(new StocktakingDetail
                    {
                        ProductId = pid,
                        SystemQuantity = sysQty,
                        ActualQuantity = actQty,
                        DifferenceQuantity = diffQty,
                        CostPrice = inv.Product.CostPrice,
                        DifferenceValue = diffVal,
                        Reason = reason
                    });

                    // Nếu có chênh lệch -> Cập nhật tồn kho và ghi nhận thẻ kho
                    if (diffQty != 0)
                    {
                        // Chỉ ghi khi tồn vẫn bằng số đã đọc; nếu đã đổi (giao dịch khác) thì hủy phiên kiểm kê
                        bool daCapNhat = await InventoryStockService.DatTonTuyetDoiAsync(_context, branchId, pid, sysQty, actQty, VietnamTime.Now);
                        if (!daCapNhat)
                        {
                            await dbTransaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Tồn kho sản phẩm #{pid} vừa thay đổi trong lúc kiểm kê. Chưa lưu phiếu nào; vui lòng kiểm kê lại.";
                            return RedirectToAction(nameof(KiemKe), new { branchId });
                        }

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionCode = $"ADJ-{sheet.Code}-{pid}",
                            TransactionType = "ADJUST",
                            BranchId = branchId,
                            ProductId = pid,
                            Quantity = diffQty,
                            UnitPrice = inv.Product.CostPrice,
                            Note = $"Cân bằng kiểm kê phiếu {sheet.Code}. Lý do: {reason}",
                            CreatedBy = CurrentUserId,
                            CreatedAt = VietnamTime.Now
                        });
                    }
                }
            }

            sheet.TotalSystemQty = totalSys;
            sheet.TotalActualQty = totalAct;
            sheet.TotalDifferenceQty = totalDiff;
            sheet.TotalDifferenceValue = totalDiffVal;

            _context.StocktakingSheets.Add(sheet);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            TempData["SuccessMessage"] = $"Cân bằng kho thành công theo phiếu {sheet.Code}! Đã cập nhật chính xác số lượng tồn thực tế.";
            return RedirectToAction(nameof(KiemKe), new { branchId });
        }

        // =========================================================================
        // 6. XUẤT BÁO CÁO XUẤT NHẬP TỒN RA EXCEL (CLOSEDXML)
        // =========================================================================
        [HttpGet]
        [KiemTraQuyen("inventory.view")]
        public async Task<IActionResult> ExportXuatNhapTonExcel(int? branchId)
        {
            var branches = await GetScopedBranchesAsync();
            int selectedBranchId = _dataScope.ResolveAllowedBranchId(branchId) ?? branches.FirstOrDefault()?.Id ?? 1;
            var branch = branches.FirstOrDefault(b => b.Id == selectedBranchId) ?? await _context.Branches.FirstAsync();

            var inventories = await _dataScope.ApplyInventoryScope(_context.Inventories)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Where(i => i.BranchId == selectedBranchId)
                .OrderBy(i => i.Product.Category.Name)
                    .ThenBy(i => i.Product.Name)
                .ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("BaoCao_TonKho");

            // 1. Tiêu đề Công ty & Báo cáo
            worksheet.Cell("A1").Value = "TIÊN PHONG ENTERPRISE - HỆ THỐNG QUẢN TRỊ DOANH NGHIỆP ERP";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;
            worksheet.Cell("A1").Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#0F172A");

            worksheet.Cell("A2").Value = $"BÁO CÁO XUẤT - NHẬP - TỒN KHO THỰC TẾ (CHI NHÁNH: {branch.Name.ToUpper()})";
            worksheet.Cell("A2").Style.Font.Bold = true;
            worksheet.Cell("A2").Style.Font.FontSize = 12;
            worksheet.Cell("A2").Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#2563EB");

            worksheet.Cell("A3").Value = $"Thời điểm xuất báo cáo: {VietnamTime.Now:dd/MM/yyyy HH:mm} | Địa chỉ kho: {branch.Address}";
            worksheet.Cell("A3").Style.Font.Italic = true;
            worksheet.Cell("A3").Style.Font.FontSize = 10;
            worksheet.Cell("A3").Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#64748B");

            // 2. Header bảng dữ liệu
            string[] headers = { "STT", "Mã SKU", "Mã Vạch Barcode", "Tên Hàng Hóa", "Danh Mục", "ĐVT", "Giá Vốn (đ)", "Giá Bán (đ)", "Tồn Kho Thực Tế", "Ngưỡng Cảnh Báo", "Trạng Thái", "Tổng Giá Trị Tồn (đ)" };
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(5, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0F172A");
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            }
            worksheet.Row(5).Height = 26;

            // 3. Đổ dữ liệu hàng hóa
            int row = 6;
            int stt = 1;
            foreach (var inv in inventories)
            {
                worksheet.Cell(row, 1).Value = stt++;
                worksheet.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                worksheet.Cell(row, 2).Value = inv.Product.SKU;
                worksheet.Cell(row, 3).Value = inv.Product.Barcode ?? "";
                worksheet.Cell(row, 4).Value = inv.Product.Name;
                worksheet.Cell(row, 5).Value = inv.Product.Category.Name;
                worksheet.Cell(row, 6).Value = inv.Product.Unit;
                worksheet.Cell(row, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                worksheet.Cell(row, 7).Value = inv.Product.CostPrice;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(row, 8).Value = inv.Product.SalePrice;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(row, 9).Value = inv.StockQuantity;
                worksheet.Cell(row, 9).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                worksheet.Cell(row, 9).Style.Font.Bold = true;

                worksheet.Cell(row, 10).Value = inv.MinStockAlert;
                worksheet.Cell(row, 10).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                string status = inv.StockQuantity <= inv.MinStockAlert ? "CẢNH BÁO TỒN THẤP" : "AN TOÀN";
                worksheet.Cell(row, 11).Value = status;
                worksheet.Cell(row, 11).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                if (inv.StockQuantity <= inv.MinStockAlert)
                {
                    worksheet.Cell(row, 11).Style.Font.FontColor = ClosedXML.Excel.XLColor.Red;
                    worksheet.Cell(row, 11).Style.Font.Bold = true;
                }
                else
                {
                    worksheet.Cell(row, 11).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#10B981");
                }

                decimal totalVal = inv.StockQuantity * inv.Product.CostPrice;
                worksheet.Cell(row, 12).Value = totalVal;
                worksheet.Cell(row, 12).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(row, 12).Style.Font.Bold = true;

                // Alternate row color
                if (row % 2 == 1)
                {
                    worksheet.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F8FAFC");
                }

                row++;
            }

            // 4. Dòng Tổng cộng (Summary)
            worksheet.Cell(row, 1).Value = "TỔNG CỘNG";
            worksheet.Range(row, 1, row, 8).Merge();
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            worksheet.Cell(row, 9).FormulaA1 = $"SUM(I6:I{row - 1})";
            worksheet.Cell(row, 9).Style.Font.Bold = true;
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";

            worksheet.Cell(row, 12).FormulaA1 = $"SUM(L6:L{row - 1})";
            worksheet.Cell(row, 12).Style.Font.Bold = true;
            worksheet.Cell(row, 12).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#2563EB");
            worksheet.Cell(row, 12).Style.NumberFormat.Format = "#,##0";

            worksheet.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E2E8F0");
            worksheet.Row(row).Height = 22;

            // 5. Viền toàn bộ bảng & tự căn chỉnh độ rộng cột
            var tableRange = worksheet.Range(5, 1, row, 12);
            tableRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BaoCao_XuatNhapTon_{branch.Code}_{VietnamTime.Now:yyyyMMdd_HHmm}.xlsx"
            );
        }
    }

    public class StockTransferDto
    {
        public int FromBranchId { get; set; }
        public int ToBranchId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }
}
