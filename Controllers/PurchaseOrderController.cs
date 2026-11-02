using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class PurchaseOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public PurchaseOrderController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 1;

        // 1. DANH SÁCH ĐƠN MUA HÀNG (PO)
        [HttpGet]
        [KiemTraQuyen("scm.po.view")]
        public async Task<IActionResult> Index(string? keyword, string? status, int? supplierId, int? branchId)
        {
            var query = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Branch)
                .Include(p => p.Creator)
                .Include(p => p.Approver)
                .Include(p => p.PurchaseOrderDetails)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(p => p.POCode.ToLower().Contains(keyword) ||
                                         p.Supplier.Name.ToLower().Contains(keyword) ||
                                         (p.Note != null && p.Note.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (supplierId.HasValue && supplierId.Value > 0)
            {
                query = query.Where(p => p.SupplierId == supplierId.Value);
            }

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(p => p.BranchId == branchId.Value);
            }

            var orders = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.SupplierId = supplierId;
            ViewBag.BranchId = branchId;
            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            ViewBag.Branches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();

            ViewBag.TotalAmount = orders.Sum(p => p.FinalAmount);
            ViewBag.PendingCount = orders.Count(p => p.Status == "PendingApproval");
            ViewBag.ApprovedCount = orders.Count(p => p.Status == "Approved");
            ViewBag.ReceivedCount = orders.Count(p => p.Status == "Received");

            return View(orders);
        }

        // 2. GIAO DIỆN TẠO ĐƠN MUA HÀNG (PO)
        [HttpGet]
        [KiemTraQuyen("scm.po.manage")]
        public async Task<IActionResult> Create(int? supplierId)
        {
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name", supplierId);
            ViewBag.Branches = new SelectList(await _context.Branches.OrderBy(b => b.Name).ToListAsync(), "Id", "Name");
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

            var po = new PurchaseOrder
            {
                POCode = $"PO{DateTime.Now:yyMMddHHmm}",
                OrderDate = DateTime.Today,
                ExpectedDeliveryDate = DateTime.Today.AddDays(3),
                SupplierId = supplierId ?? 0
            };

            return View(po);
        }

        // 3. XỬ LÝ LẬP ĐƠN MUA HÀNG (PO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("scm.po.manage")]
        public async Task<IActionResult> Create(PurchaseOrder model, int[] productIds, int[] quantities, decimal[] unitPrices)
        {
            if (productIds == null || productIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một mặt hàng để đặt mua.";
                return RedirectToAction(nameof(Create), new { supplierId = model.SupplierId });
            }

            // Kiểm tra trùng mã
            if (await _context.PurchaseOrders.AnyAsync(p => p.POCode == model.POCode))
            {
                model.POCode = $"PO{DateTime.Now:yyMMddHHmmss}";
            }

            model.CreatedBy = CurrentUserId;
            model.CreatedAt = DateTime.Now;
            model.Status = "PendingApproval";

            decimal total = 0;
            for (int i = 0; i < productIds.Length; i++)
            {
                if (quantities[i] > 0)
                {
                    decimal lineTotal = quantities[i] * unitPrices[i];
                    total += lineTotal;

                    model.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                    {
                        ProductId = productIds[i],
                        Quantity = quantities[i],
                        UnitPrice = unitPrices[i],
                        TotalPrice = lineTotal
                    });
                }
            }

            model.TotalAmount = total;
            model.FinalAmount = Math.Max(0, total - model.DiscountAmount);
            model.PaidAmount = 0;
            model.DebtAmount = model.FinalAmount;

            _context.PurchaseOrders.Add(model);
            await _context.SaveChangesAsync();

            // Gửi thông báo SignalR
            await _notificationService.SendToRoleAsync(
                "WAREHOUSE_STAFF",
                "Đơn Mua Hàng Mới",
                $"Đơn mua hàng {model.POCode} đã được tạo với giá trị {model.FinalAmount:N0} đ. Đang chờ phê duyệt.",
                $"/PurchaseOrder/ChiTiet/{model.Id}",
                "INFO"
            );

            TempData["SuccessMessage"] = $"Tạo đơn mua hàng '{model.POCode}' thành công! Trạng thái: Chờ duyệt.";
            return RedirectToAction(nameof(ChiTiet), new { id = model.Id });
        }

        // 4. CHI TIẾT ĐƠN MUA HÀNG
        [HttpGet]
        [KiemTraQuyen("scm.po.view")]
        public async Task<IActionResult> ChiTiet(long id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Branch)
                .Include(p => p.Creator)
                .Include(p => p.Approver)
                .Include(p => p.PurchaseOrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return NotFound();
            return View(po);
        }

        // 5. DUYỆT ĐƠN MUA HÀNG (GIÁM ĐỐC / QUẢN LÝ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("scm.po.manage")]
        public async Task<IActionResult> DuyetDon(long id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return NotFound();

            if (po.Status != "PendingApproval")
            {
                TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái Chờ duyệt.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            po.Status = "Approved";
            po.ApprovedBy = CurrentUserId;

            await _context.SaveChangesAsync();

            // Thông báo tới thủ kho
            await _notificationService.SendToRoleAsync(
                "WAREHOUSE_STAFF",
                "Đơn PO Đã Được Phê Duyệt",
                $"Đơn hàng {po.POCode} từ {po.Supplier.Name} đã được duyệt. Sẵn sàng nhập kho khi hàng về.",
                $"/PurchaseOrder/ChiTiet/{po.Id}",
                "SUCCESS"
            );

            TempData["SuccessMessage"] = $"Đã duyệt đơn mua hàng {po.POCode} thành công!";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        // 6. XÁC NHẬN HÀNG VỀ KHO (TỰ ĐỘNG CỘNG TỒN KHO & GHI NHẬN CÔNG NỢ PHẢI TRẢ NCC)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("scm.po.manage")]
        public async Task<IActionResult> NhanHang(long id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Branch)
                .Include(p => p.PurchaseOrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return NotFound();

            if (po.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Chỉ có thể nhập kho đơn hàng đã được phê duyệt (Approved).";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                po.Status = "Received";
                po.ReceivedDate = DateTime.Now;
                po.DebtAmount = po.FinalAmount - po.PaidAmount;

                // 1. Tự động tăng tồn kho thực tế tại chi nhánh
                foreach (var item in po.PurchaseOrderDetails)
                {
                    var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == po.BranchId);
                    if (inv == null)
                    {
                        inv = new Inventory
                        {
                            ProductId = item.ProductId,
                            BranchId = po.BranchId,
                            StockQuantity = item.Quantity,
                            MinStockAlert = 5,
                            MaxStockLimit = 1000,
                            UpdatedAt = DateTime.Now
                        };
                        _context.Inventories.Add(inv);
                    }
                    else
                    {
                        inv.StockQuantity += item.Quantity;
                        inv.UpdatedAt = DateTime.Now;
                    }

                    // Ghi nhận lịch sử thẻ kho (InventoryTransaction)
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"IN-{po.POCode}-{item.ProductId}",
                        TransactionType = "IN",
                        BranchId = po.BranchId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Note = $"Nhập hàng theo đơn PO {po.POCode} từ NCC {po.Supplier.Name}",
                        CreatedBy = CurrentUserId,
                        CreatedAt = DateTime.Now
                    });
                }

                // 2. Ghi nhận công nợ phải trả NCC (DebtRecords)
                var debt = new DebtRecord
                {
                    DebtType = "PAYABLE",
                    PartnerType = "SUPPLIER",
                    PartnerName = po.Supplier.Name,
                    PartnerPhone = po.Supplier.Phone,
                    SupplierId = po.SupplierId,
                    PurchaseOrderId = po.Id,
                    DocumentCode = po.POCode,
                    TotalAmount = po.FinalAmount,
                    PaidAmount = po.PaidAmount,
                    RemainingAmount = po.FinalAmount - po.PaidAmount,
                    DueDate = DateTime.Now.AddDays(30), // Kỳ hạn thanh toán 30 ngày
                    Status = (po.FinalAmount - po.PaidAmount <= 0) ? "Paid" : (po.PaidAmount > 0 ? "PartiallyPaid" : "Unpaid"),
                    Note = $"Công nợ nhập hàng theo đơn {po.POCode}",
                    CreatedAt = DateTime.Now
                };
                _context.DebtRecords.Add(debt);

                // 3. Tăng công nợ trên hồ sơ Nhà cung cấp
                po.Supplier.TotalDebt += (po.FinalAmount - po.PaidAmount);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xác nhận nhập kho. Vui lòng thử lại!";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            // Thông báo Real-time
            await _notificationService.BroadcastAsync(
                "Hàng Về Kho Thành Công",
                $"Đơn hàng {po.POCode} đã nhập kho chi nhánh {po.Branch.Name}. Tồn kho đã được tự động cập nhật.",
                $"/Kho/Index?branchId={po.BranchId}",
                "SUCCESS"
            );

            TempData["SuccessMessage"] = $"Xác nhận nhập kho thành công! Đã tăng tồn kho chi nhánh và ghi nhận công nợ phải trả NCC.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        // 7. HỦY ĐƠN HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("scm.po.manage")]
        public async Task<IActionResult> HuyDon(long id, string? reason)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po == null) return NotFound();

            if (po.Status == "Received")
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng đã nhập kho.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            po.Status = "Cancelled";
            po.Note = $"{po.Note} | Lý do hủy: {reason}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy đơn mua hàng {po.POCode}.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
    }
}
