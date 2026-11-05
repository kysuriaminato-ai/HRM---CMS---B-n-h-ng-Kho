using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;
using ERP_HRM_CMS.Services.Kho;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class BanHangController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICartService _cartService;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDataScopeService _dataScope;
        private readonly IPermissionService _permissionService;

        public BanHangController(
            AppDbContext context,
            ICartService cartService,
            INotificationService notificationService,
            ICurrentUserService currentUser,
            IDataScopeService dataScope,
            IPermissionService permissionService)
        {
            _context = context;
            _cartService = cartService;
            _notificationService = notificationService;
            _currentUser = currentUser;
            _dataScope = dataScope;
            _permissionService = permissionService;
        }

        private string CurrentSessionId => HttpContext.Session.Id;

        [HttpGet]
        [KiemTraQuyen("order.pos")]
        public IActionResult Index(int? branchId)
        {
            return RedirectToAction(nameof(POS), new { branchId });
        }

        [HttpGet]
        [KiemTraQuyen("order.pos")]
        public async Task<IActionResult> POS(int? branchId)
        {
            int currentUserId = _currentUser.GetRequiredUserId();
            var branchesQuery = _context.Branches.AsNoTracking().Where(b => b.IsActive);

            // Lọc danh sách chi nhánh theo phạm vi dữ liệu (IDataScopeService)
            if (_dataScope.GetOperationsScopeLevel() is DataScopeLevel.Branch or DataScopeLevel.Department && _currentUser.BranchId.HasValue)
            {
                int myBranchId = _currentUser.BranchId.Value;
                branchesQuery = branchesQuery.Where(b => b.Id == myBranchId);
            }

            var branches = await branchesQuery.OrderBy(b => b.Id).ToListAsync();
            int? resolvedBranch = _dataScope.ResolveAllowedBranchId(branchId);
            int selectedBranchId = resolvedBranch ?? branches.FirstOrDefault()?.Id ?? 1;

            bool canManageOrders = _currentUser.IsSuperAdmin
                                || await _permissionService.HasPermissionAsync(currentUserId, "order.manage");
            decimal maxDiscountRate = PosDiscountPolicy.GetMaxDiscountRate(_currentUser.IsSuperAdmin, canManageOrders);

            ViewBag.Branches = branches;
            ViewBag.SelectedBranchId = selectedBranchId;
            ViewBag.MaxDiscountPercent = (int)(maxDiscountRate * 100);
            ViewBag.Categories = await _context.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync();

            var cartModel = await _cartService.GetCartViewModelAsync(selectedBranchId, currentUserId, CurrentSessionId);
            return View(cartModel);
        }

        // API Tìm nhanh sản phẩm theo Barcode/SKU/Tên cho POS — Bảo vệ bằng [KiemTraQuyen("order.pos")] (Khắc phục P0-02)
        [HttpGet]
        [KiemTraQuyen("order.pos")]
        public async Task<IActionResult> TimKiemSanPhamPos(string keyword, int branchId)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return Json(new List<object>());

            int effectiveBranchId = _dataScope.ResolveAllowedBranchId(branchId) ?? branchId;
            keyword = keyword.Trim();

            var items = await (from p in _context.Products.AsNoTracking()
                               join i in _context.Inventories.AsNoTracking().Where(inv => inv.BranchId == effectiveBranchId)
                               on p.Id equals i.ProductId into pInv
                               from inv in pInv.DefaultIfEmpty()
                               where p.IsActive && (p.SKU.Contains(keyword) ||
                                                   p.Name.Contains(keyword) ||
                                                   (p.Barcode != null && p.Barcode.Contains(keyword)))
                               select new
                               {
                                   p.Id,
                                   p.SKU,
                                   p.Barcode,
                                   p.Name,
                                   p.SalePrice,
                                   p.Unit,
                                   Stock = inv != null ? inv.StockQuantity : 0
                               }).Take(10).ToListAsync();

            return Json(items);
        }

        // Xử lý tạo đơn hàng từ POS & trừ kho thực tế tức thì — Ràng buộc trần giảm giá phía Server (Khắc phục S6 / P0-03)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("order.pos")]
        public async Task<IActionResult> XuatDonPOS(POSCheckoutDto dto)
        {
            bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                       || Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            int currentUserId = _currentUser.GetRequiredUserId();

            // Kiểm tra phạm vi chi nhánh hợp lệ
            if (_dataScope.GetOperationsScopeLevel() is DataScopeLevel.Branch or DataScopeLevel.Department
                && _currentUser.BranchId.HasValue
                && dto.BranchId != _currentUser.BranchId.Value)
            {
                const string scopeErr = "Bạn không có quyền xuất đơn POS cho chi nhánh khác.";
                if (isAjax) return StatusCode(403, new { success = false, message = scopeErr });
                TempData["ErrorMessage"] = scopeErr;
                return RedirectToAction(nameof(POS), new { branchId = _currentUser.BranchId.Value });
            }

            var cart = await _cartService.GetCartViewModelAsync(dto.BranchId, currentUserId, CurrentSessionId);
            if (!cart.Items.Any())
            {
                const string emptyErr = "Giỏ hàng hiện đang trống!";
                if (isAjax) return BadRequest(new { success = false, message = emptyErr });
                TempData["ErrorMessage"] = emptyErr;
                return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
            }

            // Kiểm tra trần giảm giá hợp lệ phía Server (PosDiscountPolicy)
            bool canManageOrders = _currentUser.IsSuperAdmin
                                || await _permissionService.HasPermissionAsync(currentUserId, "order.manage");

            var discountValidation = PosDiscountPolicy.Validate(
                subTotal: cart.SubTotal,
                discountAmount: dto.DiscountAmount,
                note: dto.Note,
                isSuperAdmin: _currentUser.IsSuperAdmin,
                canManageOrders: canManageOrders);

            if (discountValidation.IsFailure)
            {
                if (isAjax)
                {
                    return BadRequest(new
                    {
                        success = false,
                        code = discountValidation.ErrorCode,
                        message = discountValidation.ErrorMessage
                    });
                }

                TempData["ErrorMessage"] = discountValidation.ErrorMessage;
                return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
            }

            decimal validatedDiscount = discountValidation.GetRequiredValue();

            // Nạp tồn kho một lần duy nhất cho toàn bộ sản phẩm trong giỏ (tránh N+1 query H5)
            var productIds = cart.Items.Select(x => x.ProductId).Distinct().ToList();
            var stockByProduct = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.BranchId == dto.BranchId && productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, i => i);

            foreach (var item in cart.Items)
            {
                stockByProduct.TryGetValue(item.ProductId, out var inv);
                if (inv == null || inv.StockQuantity < item.Quantity)
                {
                    string stockErr = $"Lỗi tồn kho: Sản phẩm '{item.ProductName}' chỉ còn {inv?.StockQuantity ?? 0} {item.Unit} khả dụng!";
                    if (isAjax) return BadRequest(new { success = false, message = stockErr });
                    TempData["ErrorMessage"] = stockErr;
                    return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
                }
            }

            decimal finalAmount = cart.SubTotal - validatedDiscount;

            var order = new Order
            {
                OrderCode = MaChungTu.Sinh("ORD"),
                BranchId = dto.BranchId,
                CustomerName = (dto.CustomerName ?? "Khách lẻ tại quầy").Trim(),
                CustomerPhone = (dto.CustomerPhone ?? "0900000000").Trim(),
                CustomerAddress = dto.CustomerAddress,
                TotalAmount = cart.SubTotal,
                DiscountAmount = validatedDiscount,
                FinalAmount = finalAmount,
                OrderStatus = "Completed",
                PaymentStatus = "Paid",
                PaymentMethod = dto.PaymentMethod,
                CreatedBy = currentUserId,
                CreatedAt = VietnamTime.Now,
                Note = dto.Note
            };

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart.Items)
                {
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });

                    if (!await InventoryStockService.TruTonAsync(_context, dto.BranchId, item.ProductId, item.Quantity, VietnamTime.Now))
                    {
                        throw new TonKhoKhongDuException($"Sản phẩm '{item.ProductName}' vừa hết hàng tại chi nhánh. Đơn hàng chưa được lập.");
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"PXK-POS-{order.Id}-{item.ProductId}",
                        TransactionType = "OUT",
                        BranchId = dto.BranchId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Note = $"Xuất kho bán lẻ theo đơn hàng {order.OrderCode}",
                        CreatedBy = currentUserId,
                        CreatedAt = VietnamTime.Now
                    });
                }

                if (validatedDiscount > 0)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserId = currentUserId,
                        Username = _currentUser.Username ?? "Cashier",
                        Action = "POS_DISCOUNT_APPLIED",
                        EntityName = "Order",
                        RecordId = order.Id.ToString(),
                        NewValues = $"Giảm giá POS: {validatedDiscount:N0} đ / Tạm tính {cart.SubTotal:N0} đ. Lý do: {dto.Note ?? "Trong hạn mức cho phép"}",
                        IpAddress = _currentUser.IpAddress,
                        CreatedAt = VietnamTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await _cartService.ClearCartAsync(currentUserId, CurrentSessionId);

                await dbTransaction.CommitAsync();
            }
            catch (TonKhoKhongDuException ex)
            {
                await dbTransaction.RollbackAsync();
                if (isAjax) return BadRequest(new { success = false, message = ex.Message });
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                const string genericErr = "Đã xảy ra lỗi trong quá trình xử lý đơn hàng POS. Vui lòng thử lại!";
                if (isAjax) return StatusCode(500, new { success = false, message = genericErr });
                TempData["ErrorMessage"] = genericErr;
                return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
            }

            await _notificationService.SendToRoleAsync(
                "SALES_DIRECTOR",
                "Đơn hàng POS mới!",
                $"Đơn hàng {order.OrderCode} trị giá {order.FinalAmount:N0} đ vừa được thanh toán bởi khách {order.CustomerName}.",
                $"/DonHang/ChiTiet/{order.Id}",
                "ORDER");

            var updatedStocks = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.BranchId == dto.BranchId && productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, i => i);

            foreach (var item in cart.Items)
            {
                if (updatedStocks.TryGetValue(item.ProductId, out var inv) && inv.StockQuantity <= inv.MinStockAlert)
                {
                    await _notificationService.SendToRoleAsync(
                        "WAREHOUSE_STAFF",
                        "Cảnh báo tồn kho tối thiểu!",
                        $"Sản phẩm '{item.ProductName}' tại chi nhánh hiện chỉ còn {inv.StockQuantity} {item.Unit} (dưới ngưỡng an toàn {inv.MinStockAlert})!",
                        "/Kho/Index",
                        "STOCK_ALERT");
                }
            }

            if (isAjax)
            {
                return Json(new { success = true, orderId = order.Id, orderCode = order.OrderCode, finalAmount = order.FinalAmount });
            }

            TempData["SuccessMessage"] = $"Lập đơn hàng {order.OrderCode} thành công! Tiền thu: {order.FinalAmount:N0} đ";
            return RedirectToAction("ChiTiet", "DonHang", new { id = order.Id });
        }
    }
}
