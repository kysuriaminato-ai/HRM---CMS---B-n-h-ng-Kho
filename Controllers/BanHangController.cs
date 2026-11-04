using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;
using ERP_HRM_CMS.Services.Kho;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class BanHangController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICartService _cartService;
        private readonly INotificationService _notificationService;

        public BanHangController(AppDbContext context, ICartService cartService, INotificationService notificationService)
        {
            _context = context;
            _cartService = cartService;
            _notificationService = notificationService;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
        private string CurrentSessionId => HttpContext.Session.Id;

        [HttpGet]
        public IActionResult Index(int? branchId)
        {
            return RedirectToAction(nameof(POS), new { branchId });
        }

        [HttpGet]
        [KiemTraQuyen("order.pos")]
        public async Task<IActionResult> POS(int? branchId)
        {
            var branches = await _context.Branches.ToListAsync();
            int selectedBranchId = branchId ?? branches.FirstOrDefault()?.Id ?? 1;

            ViewBag.Branches = branches;
            ViewBag.SelectedBranchId = selectedBranchId;
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

            var cartModel = await _cartService.GetCartViewModelAsync(selectedBranchId, CurrentUserId, CurrentSessionId);
            return View(cartModel);
        }

        // API Tìm nhanh sản phẩm theo Barcode/SKU/Tên cho POS
        [HttpGet]
        public async Task<IActionResult> TimKiemSanPhamPos(string keyword, int branchId)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return Json(new List<object>());

            keyword = keyword.Trim().ToLower();

            var items = await (from p in _context.Products
                               join i in _context.Inventories.Where(inv => inv.BranchId == branchId)
                               on p.Id equals i.ProductId into pInv
                               from inv in pInv.DefaultIfEmpty()
                               where p.IsActive && (p.SKU.ToLower().Contains(keyword) ||
                                                   p.Name.ToLower().Contains(keyword) ||
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

        // Xử lý tạo đơn hàng từ POS & trừ kho thực tế tức thì
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("order.pos")]
        public async Task<IActionResult> XuatDonPOS(POSCheckoutDto dto)
        {
            var cart = await _cartService.GetCartViewModelAsync(dto.BranchId, CurrentUserId, CurrentSessionId);
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng hiện đang trống!";
                return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
            }

            // Real-time Validation: Đối chiếu lại tồn kho lần cuối
            foreach (var item in cart.Items)
            {
                var inv = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == dto.BranchId);
                if (inv == null || inv.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Lỗi tồn kho: Sản phẩm '{item.ProductName}' chỉ còn {inv?.StockQuantity ?? 0} {item.Unit} khả dụng!";
                    return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
                }
            }

            decimal finalAmount = cart.SubTotal - dto.DiscountAmount;

            var order = new Order
            {
                OrderCode = MaChungTu.Sinh("ORD"),
                BranchId = dto.BranchId,
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone.Trim(),
                CustomerAddress = dto.CustomerAddress,
                TotalAmount = cart.SubTotal,
                DiscountAmount = dto.DiscountAmount,
                FinalAmount = Math.Max(0, finalAmount),
                OrderStatus = "Completed",
                PaymentStatus = "Paid",
                PaymentMethod = dto.PaymentMethod,
                CreatedBy = CurrentUserId,
                CreatedAt = VietnamTime.Now,
                Note = dto.Note
            };

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Lưu chi tiết đơn hàng & trừ kho thực tế + ghi nhật ký giao dịch kho
                foreach (var item in cart.Items)
                {
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });

                    // Trừ tồn bằng UPDATE có điều kiện; nếu giao dịch khác vừa bán hết thì hủy toàn bộ đơn
                    if (!await InventoryStockService.TruTonAsync(_context, dto.BranchId, item.ProductId, item.Quantity, VietnamTime.Now))
                    {
                        throw new TonKhoKhongDuException($"Sản phẩm '{item.ProductName}' vừa hết hàng tại chi nhánh. Đơn hàng chưa được lập.");
                    }

                    // Ghi nhận phiếu xuất kho tự động
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"PXK-POS-{order.Id}-{item.ProductId}",
                        TransactionType = "OUT",
                        BranchId = dto.BranchId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Note = $"Xuất kho bán lẻ theo đơn hàng {order.OrderCode}",
                        CreatedBy = CurrentUserId ?? 1,
                        CreatedAt = VietnamTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await _cartService.ClearCartAsync(CurrentUserId, CurrentSessionId);

                await dbTransaction.CommitAsync();
            }
            catch (TonKhoKhongDuException ex)
            {
                await dbTransaction.RollbackAsync();
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xử lý đơn hàng POS. Vui lòng thử lại!";
                return RedirectToAction(nameof(POS), new { branchId = dto.BranchId });
            }

            // Bắn thông báo Real-time SignalR cho Quản lý Kinh Doanh
            await _notificationService.SendToRoleAsync(
                "SALE_DIRECTOR",
                "Đơn hàng POS mới!",
                $"Đơn hàng {order.OrderCode} trị giá {order.FinalAmount:N0} đ vừa được thanh toán bởi khách {order.CustomerName}.",
                $"/DonHang/ChiTiet/{order.Id}",
                "ORDER");

            // Kiểm tra và cảnh báo nếu có mặt hàng nào chạm ngưỡng tồn kho tối thiểu
            foreach (var item in cart.Items)
            {
                var inv = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == dto.BranchId);
                if (inv != null && inv.StockQuantity <= inv.MinStockAlert)
                {
                    await _notificationService.SendToRoleAsync(
                        "WAREHOUSE_STAFF",
                        "Cảnh báo tồn kho tối thiểu!",
                        $"Sản phẩm '{item.ProductName}' tại chi nhánh hiện chỉ còn {inv.StockQuantity} {item.Unit} (dưới ngưỡng an toàn {inv.MinStockAlert})!",
                        "/Kho/Index",
                        "STOCK_ALERT");
                }
            }

            TempData["SuccessMessage"] = $"Lập đơn hàng {order.OrderCode} thành công! Tiền thu: {order.FinalAmount:N0} đ";
            return RedirectToAction("ChiTiet", "DonHang", new { id = order.Id });
        }
    }
}
