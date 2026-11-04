using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var model = new ExecutiveDashboardViewModel();
            var today = VietnamTime.Today;
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            // 1. Doanh thu tháng này & % Tăng trưởng
            var thisMonthOrders = await _context.Orders
                .Where(o => o.OrderStatus != "Cancelled" && o.CreatedAt >= thisMonthStart)
                .ToListAsync();
            model.MonthlyRevenue = thisMonthOrders.Sum(o => o.FinalAmount);

            var lastMonthOrders = await _context.Orders
                .Where(o => o.OrderStatus != "Cancelled" && o.CreatedAt >= lastMonthStart && o.CreatedAt < thisMonthStart)
                .ToListAsync();
            var lastMonthRevenue = lastMonthOrders.Sum(o => o.FinalAmount);

            if (lastMonthRevenue > 0)
            {
                model.RevenueGrowthPercent = Math.Round((double)((model.MonthlyRevenue - lastMonthRevenue) / lastMonthRevenue * 100), 1);
            }
            else
            {
                model.RevenueGrowthPercent = model.MonthlyRevenue > 0 ? 100.0 : 0.0;
            }

            // 2. Đơn hàng hôm nay & Doanh thu hôm nay
            var todayOrders = await _context.Orders
                .Where(o => o.OrderStatus != "Cancelled" && o.CreatedAt >= today)
                .ToListAsync();
            model.TodayOrdersCount = todayOrders.Count;
            model.TodayRevenue = todayOrders.Sum(o => o.FinalAmount);

            // 3. Số nhân sự active & Tổng định biên
            model.ActiveEmployeesCount = await _context.Employees.CountAsync(e => e.Status == "DANG_LAM_VIEC" || e.Status == "ACTIVE");
            model.TotalEmployeesCount = await _context.Employees.CountAsync();

            // 4. Đơn chờ duyệt (Đơn nghỉ phép/công tác + Đơn PO nhập hàng)
            var pendingLeaveCount = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveRequestStatus.PendingApproval || l.Status == LeaveRequestStatus.DepartmentApproved);
            var pendingPOCount = await _context.PurchaseOrders.CountAsync(p => p.Status == PurchaseOrderStatus.PendingApproval);
            model.PendingApprovalsCount = pendingLeaveCount + pendingPOCount;

            // 5. Sản phẩm cảnh báo tồn kho (Tồn kho <= Ngưỡng tồn an toàn)
            model.LowStockCount = await _context.Inventories
                .CountAsync(i => i.StockQuantity <= i.MinStockAlert);

            // 6. Công nợ quá hạn
            model.OverdueDebtTotal = await _context.DebtRecords
                .Where(d => d.Status != DebtStatus.Paid && (d.Status == DebtStatus.Overdue || d.DueDate < today))
                .SumAsync(d => d.RemainingAmount);

            // 7. Biểu đồ Doanh thu 12 tháng gần nhất
            for (int i = 11; i >= 0; i--)
            {
                var mStart = thisMonthStart.AddMonths(-i);
                var mEnd = mStart.AddMonths(1);
                var label = "T" + mStart.ToString("MM/yy");

                var mRev = await _context.Orders
                    .Where(o => o.OrderStatus != "Cancelled" && o.CreatedAt >= mStart && o.CreatedAt < mEnd)
                    .SumAsync(o => o.FinalAmount);

                model.Revenue12MonthsLabels.Add(label);
                model.Revenue12MonthsData.Add(mRev);
            }

            // 8. Tỷ trọng doanh thu theo Chi nhánh
            var branchRevenues = await _context.Orders
                .Where(o => o.OrderStatus != "Cancelled")
                .Include(o => o.Branch)
                .GroupBy(o => o.Branch != null ? o.Branch.Name : "Trụ sở chính")
                .Select(g => new { BranchName = g.Key, TotalRevenue = g.Sum(o => o.FinalAmount) })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToListAsync();

            foreach (var br in branchRevenues)
            {
                model.RevenueByBranchLabels.Add(br.BranchName);
                model.RevenueByBranchData.Add(br.TotalRevenue);
            }

            // 9. Top 10 sản phẩm bán chạy nhất
            model.TopProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .GroupBy(od => new { od.ProductId, od.Product.SKU, od.Product.Name, CategoryName = od.Product.Category != null ? od.Product.Category.Name : "Khác", od.Product.ImageUrl })
                .Select(g => new TopProductViewModel
                {
                    SKU = g.Key.SKU,
                    Name = g.Key.Name,
                    CategoryName = g.Key.CategoryName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => (x.UnitPrice * x.Quantity)),
                    ImageUrl = g.Key.ImageUrl
                })
                .OrderByDescending(p => p.QuantitySold)
                .Take(10)
                .ToListAsync();

            // 10. Nhật ký hoạt động gần nhất (Activity Feed)
            var recentAuditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var log in recentAuditLogs)
            {
                string badge = log.Action switch
                {
                    "CREATE" => "bg-success",
                    "UPDATE" => "bg-warning text-dark",
                    "DELETE" => "bg-danger",
                    "APPROVE" => "bg-primary",
                    _ => "bg-secondary"
                };

                string icon = log.EntityName switch
                {
                    "Order" => "bi-cart-check",
                    "PurchaseOrder" => "bi-journal-bookmark",
                    "LeaveRequest" => "bi-file-earmark-check",
                    "Product" => "bi-box-seam",
                    "Employee" => "bi-person-badge",
                    _ => "bi-activity"
                };

                model.RecentActivities.Add(new DashboardActivityItemViewModel
                {
                    Action = log.Action,
                    EntityName = log.EntityName,
                    Description = $"{log.Action} dữ liệu {log.EntityName} #{log.RecordId}",
                    Username = log.Username ?? "Hệ thống",
                    CreatedAt = log.CreatedAt,
                    BadgeClass = badge,
                    IconClass = icon
                });
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult KhongCoQuyen()
        {
            return View();
        }

        // Trang lỗi chung. Được UseExceptionHandler và UseStatusCodePagesWithReExecute gọi lại,
        // nên phải công khai để người chưa đăng nhập không bị chuyển sang trang đăng nhập khi gặp lỗi.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GlobalQuickSearch(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new
                {
                    navigation = new[]
                    {
                        new { title = "Điểm Bán Hàng & POS Thu Ngân", subtitle = "Giao dịch bán lẻ, tạo đơn hàng", url = "/BanHang/POS", icon = "bi-cart-check" },
                        new { title = "Quản Trị Nhân Sự (HRM)", subtitle = "Danh sách nhân viên, hồ sơ, phòng ban", url = "/NhanSu/Index", icon = "bi-people" },
                        new { title = "Quản Trị Tồn Kho Thực Tế", subtitle = "Kiểm tra số lượng tồn, điều chuyển kho", url = "/Kho/Index", icon = "bi-box-seam" },
                        new { title = "Kiểm Kê & Cân Bằng Kho", subtitle = "Đối soát số lượng tồn thực tế vs phần mềm", url = "/Kho/KiemKe", icon = "bi-clipboard2-check" },
                        new { title = "Đơn Mua Hàng & Nhập Kho (PO)", subtitle = "Đặt hàng nhà cung cấp, duyệt và nhận hàng", url = "/PurchaseOrder/Index", icon = "bi-journal-bookmark" },
                        new { title = "Quản Trị Công Nợ (AR / AP)", subtitle = "Phải thu khách hàng, phải trả nhà cung cấp", url = "/CongNo/Index", icon = "bi-cash-coin" },
                        new { title = "Nhật Ký Kiểm Toán (Audit Trail)", subtitle = "Xem lịch sử thay đổi dữ liệu", url = "/AuditLog/Index", icon = "bi-shield-check" }
                    },
                    employees = new object[0],
                    products = new object[0],
                    orders = new object[0]
                });
            }

            string kw = keyword.Trim().ToLower();

            // 1. Tìm nhân viên
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.FullName.ToLower().Contains(kw) ||
                            e.EmployeeCode.ToLower().Contains(kw) ||
                            (e.Email != null && e.Email.ToLower().Contains(kw)) ||
                            (e.Phone != null && e.Phone.Contains(kw)))
                .Take(4)
                .Select(e => new
                {
                    id = e.Id,
                    title = $"{e.FullName} ({e.EmployeeCode})",
                    subtitle = $"Phòng {e.Department.Name} • {e.Position.Name}",
                    url = $"/NhanSu/Details/{e.Id}",
                    icon = "bi-person-badge"
                })
                .ToListAsync();

            // 2. Tìm sản phẩm
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name.ToLower().Contains(kw) ||
                            p.SKU.ToLower().Contains(kw) ||
                            (p.Barcode != null && p.Barcode.Contains(kw)))
                .Take(4)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Name,
                    subtitle = $"Mã SKU: {p.SKU} • Giá: {p.SalePrice:N0} đ • {p.Category.Name}",
                    url = $"/Kho/Index?keyword={p.SKU}",
                    icon = "bi-box-seam"
                })
                .ToListAsync();

            // 3. Tìm đơn hàng bán lẻ
            var orders = await _context.Orders
                .Where(o => o.OrderCode.ToLower().Contains(kw) ||
                            o.CustomerName.ToLower().Contains(kw) ||
                            (o.CustomerPhone != null && o.CustomerPhone.Contains(kw)))
                .Take(4)
                .Select(o => new
                {
                    id = o.Id,
                    title = $"Đơn hàng {o.OrderCode}",
                    subtitle = $"Khách: {o.CustomerName} • {o.FinalAmount:N0} đ • Trạng thái: {o.OrderStatus}",
                    url = $"/DonHang/ChiTiet/{o.Id}",
                    icon = "bi-receipt"
                })
                .ToListAsync();

            // 4. Tìm đơn mua hàng (PO) & Nhà cung cấp
            var purchaseOrders = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.POCode.ToLower().Contains(kw) ||
                             po.Supplier.Name.ToLower().Contains(kw))
                .Take(3)
                .Select(po => new
                {
                    id = po.Id,
                    title = $"Đơn mua hàng {po.POCode} ({po.Supplier.Name})",
                    subtitle = $"Tổng tiền: {po.FinalAmount:N0} đ • Trạng thái: {po.Status}",
                    url = $"/PurchaseOrder/ChiTiet/{po.Id}",
                    icon = "bi-truck"
                })
                .ToListAsync();

            // 5. Tìm đơn từ trực tuyến (Phiên 4)
            var leaveRequests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Where(l => l.RequestCode.ToLower().Contains(kw) ||
                            l.Employee.FullName.ToLower().Contains(kw) ||
                            l.Reason.ToLower().Contains(kw))
                .Take(3)
                .Select(l => new
                {
                    id = l.Id,
                    title = $"Đơn từ {l.RequestCode} - {l.Employee.FullName}",
                    subtitle = $"{l.RequestType} • {l.TotalDays} ngày • Trạng thái: {l.Status}",
                    url = $"/DonTu/Index?status={l.Status}",
                    icon = "bi-file-earmark-check"
                })
                .ToListAsync();

            return Json(new
            {
                navigation = new[]
                {
                    new { title = "Điểm Bán Hàng & POS Thu Ngân", subtitle = "Giao dịch bán lẻ", url = "/BanHang/POS", icon = "bi-cart-check" },
                    new { title = "Quản Trị Nhân Sự (HRM)", subtitle = "Danh sách nhân viên", url = "/NhanSu/Index", icon = "bi-people" },
                    new { title = "Quản Lý Đơn Từ & Phê Duyệt", subtitle = "Quy trình nghỉ phép & công tác", url = "/DonTu/Index", icon = "bi-file-earmark-check" },
                    new { title = "Bảng Lương Tự Động & Chi Trả", subtitle = "Bảng thanh toán lương & phiếu lương", url = "/BangLuong/Index", icon = "bi-cash-stack" },
                    new { title = "Heatmap Chấm Công & Kỷ Luật", subtitle = "Bản đồ nhiệt chấm công theo tháng", url = "/ChamCong/Heatmap", icon = "bi-grid-3x3-gap" },
                    new { title = "Báo Cáo Biến Động & Nhân Sự", subtitle = "Phân tích turnover và cơ cấu độ tuổi", url = "/NhanSu/BaoCao", icon = "bi-pie-chart" },
                    new { title = "Quản Trị Tồn Kho Thực Tế", subtitle = "Kiểm tra tồn kho", url = "/Kho/Index", icon = "bi-box-seam" },
                    new { title = "Kiểm Kê & Cân Bằng Kho", subtitle = "Đối soát số lượng tồn", url = "/Kho/KiemKe", icon = "bi-clipboard2-check" },
                    new { title = "Đơn Mua Hàng & PO", subtitle = "Nhập hàng từ nhà cung cấp", url = "/PurchaseOrder/Index", icon = "bi-journal-bookmark" },
                    new { title = "Quản Trị Công Nợ (AR / AP)", subtitle = "Báo cáo tuổi nợ và đối tác", url = "/CongNo/Index", icon = "bi-cash-coin" },
                    new { title = "Nhật Ký Kiểm Toán (Audit Trail)", subtitle = "Xem lịch sử thay đổi", url = "/AuditLog/Index", icon = "bi-shield-check" }
                }.Where(n => n.title.ToLower().Contains(kw) || n.subtitle.ToLower().Contains(kw)).ToArray(),
                employees,
                products,
                orders,
                purchaseOrders,
                leaveRequests
            });
        }
    }
}