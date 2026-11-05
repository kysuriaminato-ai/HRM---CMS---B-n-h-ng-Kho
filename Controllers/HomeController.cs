using ERP.Application.Common.Interfaces;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPermissionService _permissionService;
        private readonly IDataScopeService _dataScope;

        public HomeController(
            AppDbContext context,
            ICurrentUserService currentUser,
            IPermissionService permissionService,
            IDataScopeService dataScope)
        {
            _context = context;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _dataScope = dataScope;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            int userId = _currentUser.GetRequiredUserId();
            bool isSuperAdmin = _currentUser.IsSuperAdmin;
            var effectivePerms = (await _permissionService.GetEffectivePermissionsAsync(userId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool HasPerm(string code) => isSuperAdmin || effectivePerms.Contains(code);

            var model = new ExecutiveDashboardViewModel
            {
                CanViewSalesKpi = HasPerm("report.sales") || HasPerm("order.view") || HasPerm("order.manage"),
                CanViewHrKpi = HasPerm("hrm.employee.view") || HasPerm("hrm.employee.manage"),
                CanViewApprovalsKpi = HasPerm("hrm.leave.approve") || HasPerm("hrm.leave.view") || HasPerm("scm.po.approve"),
                CanViewInventoryKpi = HasPerm("inventory.view") || HasPerm("report.inventory"),
                CanViewDebtKpi = HasPerm("debt.view") || HasPerm("debt.manage"),
                CanViewAuditFeed = HasPerm("auth.audit.view"),
                CanUsePos = HasPerm("order.pos")
            };

            model.IsSelfServiceView = !model.CanViewSalesKpi
                                   && !model.CanViewHrKpi
                                   && !model.CanViewApprovalsKpi
                                   && !model.CanViewInventoryKpi
                                   && !model.CanViewDebtKpi;

            model.ScopeBadgeLabel = _dataScope.GetScopeLevel() switch
            {
                DataScopeLevel.Company => "Toàn công ty",
                DataScopeLevel.Branch => "Phạm vi Chi nhánh",
                DataScopeLevel.Department => "Phạm vi Phòng ban",
                _ => "Không gian Cá nhân"
            };

            var today = VietnamTime.Today;
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = thisMonthStart.AddMonths(1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            // Nếu là tài khoản nhân viên tự phục vụ (EMPLOYEE), chỉ truy vấn dữ liệu cá nhân
            if (model.IsSelfServiceView)
            {
                if (_currentUser.EmployeeId.HasValue)
                {
                    int empId = _currentUser.EmployeeId.Value;
                    model.MyWorkDaysThisMonth = await _context.AttendanceDailies
                        .AsNoTracking()
                        .CountAsync(a => a.EmployeeId == empId && a.WorkDate >= thisMonthStart && a.WorkDate < nextMonthStart);

                    model.MyPendingLeavesCount = await _context.LeaveRequests
                        .AsNoTracking()
                        .CountAsync(l => l.EmployeeId == empId &&
                                         (l.Status == LeaveRequestStatus.PendingApproval || l.Status == LeaveRequestStatus.DepartmentApproved));

                    model.MyApprovedLeaveDaysThisYear = await _context.LeaveRequests
                        .AsNoTracking()
                        .Where(l => l.EmployeeId == empId &&
                                    l.Status == LeaveRequestStatus.DirectorApproved &&
                                    l.FromDate.Year == today.Year)
                        .SumAsync(l => (decimal?)l.TotalDays) ?? 0m;
                }

                return View(model);
            }

            // 1. Khối Doanh thu & Đơn hàng (chỉ truy vấn khi có quyền + lọc theo DataScope, dùng SQL GROUP BY / SumAsync thay vì ToListAsync)
            if (model.CanViewSalesKpi)
            {
                var scopedOrders = _dataScope.ApplyOrderScope(_context.Orders.AsNoTracking())
                    .Where(o => o.OrderStatus != "Cancelled");

                model.MonthlyRevenue = await scopedOrders
                    .Where(o => o.CreatedAt >= thisMonthStart && o.CreatedAt < nextMonthStart)
                    .SumAsync(o => (decimal?)o.FinalAmount) ?? 0m;

                decimal lastMonthRevenue = await scopedOrders
                    .Where(o => o.CreatedAt >= lastMonthStart && o.CreatedAt < thisMonthStart)
                    .SumAsync(o => (decimal?)o.FinalAmount) ?? 0m;

                model.RevenueGrowthPercent = lastMonthRevenue > 0
                    ? Math.Round((double)((model.MonthlyRevenue - lastMonthRevenue) / lastMonthRevenue * 100), 1)
                    : (model.MonthlyRevenue > 0 ? 100.0 : 0.0);

                var todayStats = await scopedOrders
                    .Where(o => o.CreatedAt >= today)
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.FinalAmount)
                    })
                    .FirstOrDefaultAsync();

                model.TodayOrdersCount = todayStats?.Count ?? 0;
                model.TodayRevenue = todayStats?.Revenue ?? 0m;

                // Tối ưu 12 truy vấn tuần tự thành 1 truy vấn SQL GROUP BY duy nhất
                var twelveMonthsStart = thisMonthStart.AddMonths(-11);
                var monthlyAggregates = await scopedOrders
                    .Where(o => o.CreatedAt >= twelveMonthsStart && o.CreatedAt < nextMonthStart)
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Revenue = g.Sum(o => o.FinalAmount)
                    })
                    .ToListAsync();

                var monthDict = monthlyAggregates.ToDictionary(x => (x.Year, x.Month), x => x.Revenue);
                for (int i = 11; i >= 0; i--)
                {
                    var mStart = thisMonthStart.AddMonths(-i);
                    model.Revenue12MonthsLabels.Add("T" + mStart.ToString("MM/yy"));
                    model.Revenue12MonthsData.Add(monthDict.TryGetValue((mStart.Year, mStart.Month), out var rev) ? rev : 0m);
                }

                // Tỷ trọng doanh thu theo Chi nhánh
                var branchRevenues = await scopedOrders
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

                // Top 10 sản phẩm bán chạy (lọc theo phạm vi đơn hàng được phép xem)
                var scopedOrderIds = scopedOrders.Select(o => o.Id);
                model.TopProducts = await _context.OrderDetails
                    .AsNoTracking()
                    .Where(od => scopedOrderIds.Contains(od.OrderId))
                    .GroupBy(od => new
                    {
                        od.ProductId,
                        od.Product.SKU,
                        od.Product.Name,
                        CategoryName = od.Product.Category != null ? od.Product.Category.Name : "Khác",
                        od.Product.ImageUrl
                    })
                    .Select(g => new TopProductViewModel
                    {
                        SKU = g.Key.SKU,
                        Name = g.Key.Name,
                        CategoryName = g.Key.CategoryName,
                        QuantitySold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                        ImageUrl = g.Key.ImageUrl
                    })
                    .OrderByDescending(p => p.QuantitySold)
                    .Take(10)
                    .ToListAsync();
            }

            // 2. Khối Nhân sự (chỉ truy vấn khi có quyền + lọc theo DataScope)
            if (model.CanViewHrKpi)
            {
                var scopedEmployees = _dataScope.ApplyEmployeeScope(_context.Employees.AsNoTracking());
                model.ActiveEmployeesCount = await scopedEmployees
                    .CountAsync(e => e.Status == "DANG_LAM_VIEC" || e.Status == "ACTIVE" || e.Status == "Active");
                model.TotalEmployeesCount = await scopedEmployees.CountAsync();
            }

            // 3. Khối Chờ phê duyệt
            if (model.CanViewApprovalsKpi)
            {
                int pendingLeaveCount = 0;
                if (HasPerm("hrm.leave.approve") || HasPerm("hrm.leave.view"))
                {
                    pendingLeaveCount = await _dataScope.ApplyLeaveRequestScope(_context.LeaveRequests.AsNoTracking())
                        .CountAsync(l => l.Status == LeaveRequestStatus.PendingApproval || l.Status == LeaveRequestStatus.DepartmentApproved);
                }

                int pendingPoCount = 0;
                if (HasPerm("scm.po.approve") || HasPerm("scm.po.view"))
                {
                    pendingPoCount = await _dataScope.ApplyPurchaseOrderScope(_context.PurchaseOrders.AsNoTracking())
                        .CountAsync(p => p.Status == PurchaseOrderStatus.PendingApproval);
                }

                model.PendingApprovalsCount = pendingLeaveCount + pendingPoCount;
            }

            // 4. Khối Cảnh báo tồn kho
            if (model.CanViewInventoryKpi)
            {
                model.LowStockCount = await _dataScope.ApplyInventoryScope(_context.Inventories.AsNoTracking())
                    .CountAsync(i => i.StockQuantity <= i.MinStockAlert);
            }

            // 5. Khối Công nợ quá hạn
            if (model.CanViewDebtKpi)
            {
                model.OverdueDebtTotal = await _context.DebtRecords
                    .AsNoTracking()
                    .Where(d => d.Status != DebtStatus.Paid && (d.Status == DebtStatus.Overdue || d.DueDate < today))
                    .SumAsync(d => (decimal?)d.RemainingAmount) ?? 0m;
            }

            // 6. Nhật ký hoạt động gần đây (chỉ hiển thị cho tài khoản có quyền xem AuditLog)
            if (model.CanViewAuditFeed)
            {
                var recentAuditLogs = await _context.AuditLogs
                    .AsNoTracking()
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
                        "APPROVE" or "SELF_APPROVE_BY_SUPERADMIN" => "bg-primary",
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
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult KhongCoQuyen(string? returnUrl = null)
        {
            Response.StatusCode = 403;
            return View(new ErrorViewModel
            {
                StatusCode = 403,
                ReturnUrl = returnUrl,
                RequestId = HttpContext.TraceIdentifier
            });
        }

        // Trang lỗi chung. Được UseExceptionHandler và UseStatusCodePagesWithReExecute gọi lại,
        // nên phải công khai để người chưa đăng nhập không bị chuyển sang trang đăng nhập khi gặp lỗi.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Error(int? statusCode = null)
        {
            var reExecute = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>();
            int code = statusCode ?? (Response.StatusCode >= 400 ? Response.StatusCode : 500);
            Response.StatusCode = code;

            var vm = new ErrorViewModel
            {
                StatusCode = code,
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                OriginalPath = reExecute?.OriginalPath
            };

            return code switch
            {
                403 => View("Error403", vm),
                404 => View("Error404", vm),
                _ => View("Error500", vm)
            };
        }

        /// <summary>
        /// Tìm kiếm nhanh toàn cục (Ctrl+K): Bắt buộc lọc theo quyền hiệu lực thực tế &amp; phạm vi dữ liệu (IDataScopeService).
        /// Tuyệt đối không lộ SĐT/Email nhân viên hoặc dữ liệu kinh doanh cho tài khoản không có quyền (Khắc phục S2 / P0-02).
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GlobalQuickSearch(string? keyword)
        {
            int userId = _currentUser.GetRequiredUserId();
            bool isSuperAdmin = _currentUser.IsSuperAdmin;
            var effectivePerms = (await _permissionService.GetEffectivePermissionsAsync(userId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool HasPerm(string code) => isSuperAdmin || effectivePerms.Contains(code);

            bool canViewEmployees = HasPerm("hrm.employee.view");
            bool canViewProducts = HasPerm("product.view") || HasPerm("order.pos") || HasPerm("inventory.view");
            bool canViewOrders = HasPerm("order.view") || HasPerm("order.manage");
            bool canViewPo = HasPerm("scm.po.view") || HasPerm("scm.po.manage");
            bool canViewLeaves = HasPerm("hrm.leave.view") || HasPerm("hrm.self.service");

            var allowedNavItems = BuildPermittedNavigationShortcuts(HasPerm);

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new
                {
                    navigation = allowedNavItems.Take(7).ToArray(),
                    employees = Array.Empty<object>(),
                    products = Array.Empty<object>(),
                    orders = Array.Empty<object>(),
                    purchaseOrders = Array.Empty<object>(),
                    leaveRequests = Array.Empty<object>()
                });
            }

            string kw = keyword.Trim();

            // 1. Tìm nhân viên: Chỉ truy vấn khi có quyền hrm.employee.view + lọc theo DataScope, KHÔNG bao giờ trả hoặc cho dò SĐT/Email
            object[] employees = Array.Empty<object>();
            if (canViewEmployees)
            {
                employees = await _dataScope.ApplyEmployeeScope(_context.Employees.AsNoTracking())
                    .Where(e => e.FullName.Contains(kw) || e.EmployeeCode.Contains(kw))
                    .OrderBy(e => e.FullName)
                    .Take(4)
                    .Select(e => (object)new
                    {
                        id = e.Id,
                        title = $"{e.FullName} ({e.EmployeeCode})",
                        subtitle = $"Phòng {e.Department.Name} • {e.Position.Name}",
                        url = $"/NhanSu/Details/{e.Id}",
                        icon = "bi-person-badge"
                    })
                    .ToArrayAsync();
            }

            // 2. Tìm sản phẩm: Chỉ truy vấn khi có quyền product.view / order.pos / inventory.view
            object[] products = Array.Empty<object>();
            if (canViewProducts)
            {
                string productTargetPrefix = HasPerm("inventory.view") ? "/Kho/Index?keyword=" : "/SanPham/Index?keyword=";
                products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && (p.Name.Contains(kw) || p.SKU.Contains(kw) || (p.Barcode != null && p.Barcode.Contains(kw))))
                    .OrderBy(p => p.Name)
                    .Take(4)
                    .Select(p => (object)new
                    {
                        id = p.Id,
                        title = p.Name,
                        subtitle = $"Mã SKU: {p.SKU} • Giá: {p.SalePrice:N0} đ • {p.Category.Name}",
                        url = $"{productTargetPrefix}{Uri.EscapeDataString(p.SKU)}",
                        icon = "bi-box-seam"
                    })
                    .ToArrayAsync();
            }

            // 3. Tìm đơn hàng bán lẻ: Chỉ truy vấn khi có quyền order.view + lọc theo DataScope
            object[] orders = Array.Empty<object>();
            if (canViewOrders)
            {
                orders = await _dataScope.ApplyOrderScope(_context.Orders.AsNoTracking())
                    .Where(o => o.OrderCode.Contains(kw) || o.CustomerName.Contains(kw))
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(4)
                    .Select(o => (object)new
                    {
                        id = o.Id,
                        title = $"Đơn hàng {o.OrderCode}",
                        subtitle = $"Khách: {o.CustomerName} • {o.FinalAmount:N0} đ • Trạng thái: {o.OrderStatus}",
                        url = $"/DonHang/ChiTiet/{o.Id}",
                        icon = "bi-receipt"
                    })
                    .ToArrayAsync();
            }

            // 4. Tìm đơn mua hàng (PO): Chỉ truy vấn khi có quyền scm.po.view + lọc theo DataScope
            object[] purchaseOrders = Array.Empty<object>();
            if (canViewPo)
            {
                purchaseOrders = await _dataScope.ApplyPurchaseOrderScope(_context.PurchaseOrders.AsNoTracking())
                    .Where(po => po.POCode.Contains(kw) || po.Supplier.Name.Contains(kw))
                    .OrderByDescending(po => po.CreatedAt)
                    .Take(3)
                    .Select(po => (object)new
                    {
                        id = po.Id,
                        title = $"Đơn mua hàng {po.POCode} ({po.Supplier.Name})",
                        subtitle = $"Tổng tiền: {po.FinalAmount:N0} đ • Trạng thái: {po.Status}",
                        url = $"/PurchaseOrder/ChiTiet/{po.Id}",
                        icon = "bi-truck"
                    })
                    .ToArrayAsync();
            }

            // 5. Tìm đơn từ trực tuyến: Lọc chặt theo DataScope (EMPLOYEE chỉ thấy đơn của chính mình)
            object[] leaveRequests = Array.Empty<object>();
            if (canViewLeaves)
            {
                leaveRequests = await _dataScope.ApplyLeaveRequestScope(_context.LeaveRequests.AsNoTracking())
                    .Where(l => l.RequestCode.Contains(kw) || l.Employee.FullName.Contains(kw) || l.Reason.Contains(kw))
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(3)
                    .Select(l => (object)new
                    {
                        id = l.Id,
                        title = $"Đơn từ {l.RequestCode} - {l.Employee.FullName}",
                        subtitle = $"{l.RequestType} • {l.TotalDays} ngày • Trạng thái: {l.Status}",
                        url = $"/DonTu/Index?status={l.Status}",
                        icon = "bi-file-earmark-check"
                    })
                    .ToArrayAsync();
            }

            var filteredNav = allowedNavItems
                .Where(n => n.title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                         || n.subtitle.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Json(new
            {
                navigation = filteredNav,
                employees,
                products,
                orders,
                purchaseOrders,
                leaveRequests
            });
        }

        private static List<QuickNavShortcut> BuildPermittedNavigationShortcuts(Func<string, bool> hasPerm)
        {
            var list = new List<QuickNavShortcut>();

            if (hasPerm("order.pos"))
                list.Add(new("Điểm Bán Hàng & POS Thu Ngân", "Giao dịch bán lẻ, tạo đơn hàng", "/BanHang/POS", "bi-cart-check"));

            if (hasPerm("hrm.employee.view"))
            {
                list.Add(new("Quản Trị Nhân Sự (HRM)", "Danh sách nhân viên, hồ sơ, phòng ban", "/NhanSu/Index", "bi-people"));
                list.Add(new("Báo Cáo Biến Động & Nhân Sự", "Phân tích turnover và cơ cấu nhân sự", "/NhanSu/BaoCao", "bi-pie-chart"));
            }

            if (hasPerm("hrm.leave.view") || hasPerm("hrm.self.service"))
                list.Add(new("Quản Lý Đơn Từ & Nghỉ Phép", "Quy trình nghỉ phép & công tác", "/DonTu/Index", "bi-file-earmark-check"));

            if (hasPerm("hrm.payroll.view"))
                list.Add(new("Bảng Lương Tự Động & Chi Trả", "Bảng thanh toán lương & phiếu lương", "/BangLuong/Index", "bi-cash-stack"));

            if (hasPerm("hrm.attendance.view"))
                list.Add(new("Heatmap Chấm Công & Kỷ Luật", "Bản đồ nhiệt chấm công theo tháng", "/ChamCong/Heatmap", "bi-grid-3x3-gap"));

            if (hasPerm("inventory.view"))
            {
                list.Add(new("Quản Trị Tồn Kho Thực Tế", "Kiểm tra số lượng tồn, điều chuyển kho", "/Kho/Index", "bi-box-seam"));
                list.Add(new("Kiểm Kê & Cân Bằng Kho", "Đối soát số lượng tồn thực tế vs phần mềm", "/Kho/KiemKe", "bi-clipboard2-check"));
            }

            if (hasPerm("scm.po.view"))
                list.Add(new("Đơn Mua Hàng & Nhập Kho (PO)", "Đặt hàng nhà cung cấp, duyệt và nhận hàng", "/PurchaseOrder/Index", "bi-journal-bookmark"));

            if (hasPerm("debt.view"))
                list.Add(new("Quản Trị Công Nợ (AR / AP)", "Phải thu khách hàng, phải trả nhà cung cấp", "/CongNo/Index", "bi-cash-coin"));

            if (hasPerm("auth.audit.view"))
                list.Add(new("Nhật Ký Kiểm Toán (Audit Trail)", "Xem lịch sử thay đổi dữ liệu", "/AuditLog/Index", "bi-shield-check"));

            list.Add(new("Đổi Mật Khẩu Cá Nhân", "Bảo mật tài khoản đăng nhập", "/Auth/ChangePassword", "bi-key"));
            return list;
        }

        private sealed record QuickNavShortcut(string title, string subtitle, string url, string icon);
    }
}
