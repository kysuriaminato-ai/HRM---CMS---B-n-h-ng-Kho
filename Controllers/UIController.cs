using System.Security.Claims;
using ERP_HRM_CMS.Models;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    public record StyleguideSampleRow(
        string Code,
        string Title,
        string Module,
        string ModuleLabel,
        string Owner,
        decimal Amount,
        string Workflow,
        string StatusCode,
        DateTime UpdatedAt);

    public class StyleguideTableViewModel
    {
        public IReadOnlyList<StyleguideSampleRow> Items { get; init; } = Array.Empty<StyleguideSampleRow>();
        public int PageIndex { get; init; } = 1;
        public int TotalPages { get; init; } = 1;
        public int TotalCount { get; init; }
        public int PageSize { get; init; } = 5;
        public string SortBy { get; init; } = "code";
        public string SortDir { get; init; } = "asc";
        public string? Keyword { get; init; }
        public string? ModuleFilter { get; init; }
        public string? StatusFilter { get; init; }
    }

    /// <summary>
    /// Trang trình diễn hệ thống Design Tokens, Shared UI Components và Animations (Giai đoạn A).
    /// Giới hạn nghiêm ngặt chỉ dành cho tài khoản Super Admin (kiểm tra trực tiếp qua ISuperAdminService).
    /// </summary>
    [Authorize]
    public class UIController : Controller
    {
        private readonly ISuperAdminService _superAdmin;

        private static readonly IReadOnlyList<StyleguideSampleRow> SampleRows = new List<StyleguideSampleRow>
        {
            new("PO-2026-001", "Nhập lô laptop Dell Latitude 5540", "warehouse", "Kho & Mua hàng", "Trần Quốc Bảo", 185_000_000m, "PurchaseOrder", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PurchaseOrder, PurchaseOrderStatus.PendingApproval), new DateTime(2026, 4, 10, 9, 15, 0)),
            new("PO-2026-002", "Mua vật tư đóng gói quý II", "warehouse", "Kho & Mua hàng", "Lê Hoàng Nam", 24_500_000m, "PurchaseOrder", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PurchaseOrder, PurchaseOrderStatus.Approved), new DateTime(2026, 4, 10, 10, 30, 0)),
            new("PO-2026-003", "Nhập màn hình LG UltraSharp 27\"", "warehouse", "Kho & Mua hàng", "Phạm Minh Châu", 96_800_000m, "PurchaseOrder", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PurchaseOrder, PurchaseOrderStatus.Received), new DateTime(2026, 4, 9, 16, 45, 0)),
            new("PO-2026-004", "Đơn thiết bị mạng dự phòng (hủy)", "warehouse", "Kho & Mua hàng", "Đỗ Thanh Tùng", 14_200_000m, "PurchaseOrder", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PurchaseOrder, PurchaseOrderStatus.Cancelled), new DateTime(2026, 4, 8, 14, 20, 0)),

            new("AR-2026-101", "Công nợ đại lý Minh Phát tháng 04", "sales", "Bán hàng & POS", "Nguyễn Thị Mai", 68_400_000m, "Debt", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.Debt, DebtStatus.Unpaid), new DateTime(2026, 4, 11, 8, 50, 0)),
            new("AR-2026-102", "Thu đợt 1 hợp đồng chuỗi cửa hàng Việt Á", "sales", "Bán hàng & POS", "Vũ Hải Đăng", 142_000_000m, "Debt", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.Debt, DebtStatus.PartiallyPaid), new DateTime(2026, 4, 10, 15, 10, 0)),
            new("AR-2026-103", "Tất toán đơn sỉ công ty Tân Thành", "sales", "Bán hàng & POS", "Hoàng Thu Trang", 53_900_000m, "Debt", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.Debt, DebtStatus.Paid), new DateTime(2026, 4, 9, 11, 0, 0)),
            new("AP-2026-104", "Công nợ nhà phân phối Hoàng Gia quá hạn", "sales", "Bán hàng & POS", "Bùi Đức Anh", 91_000_000m, "Debt", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.Debt, DebtStatus.Overdue), new DateTime(2026, 4, 7, 17, 30, 0)),

            new("LV-2026-201", "Đơn xin nghỉ phép năm 3 ngày", "hrm", "Nhân sự (HRM)", "Đặng Ngọc Ánh", 0m, "LeaveRequest", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.PendingApproval), new DateTime(2026, 4, 11, 9, 5, 0)),
            new("LV-2026-202", "Đề xuất công tác chi nhánh Đà Nẵng", "hrm", "Nhân sự (HRM)", "Ngô Quang Huy", 8_500_000m, "LeaveRequest", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.DepartmentApproved), new DateTime(2026, 4, 10, 13, 40, 0)),
            new("LV-2026-203", "Nghỉ chế độ thai sản theo quy định", "hrm", "Nhân sự (HRM)", "Phan Thanh Thảo", 0m, "LeaveRequest", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.DirectorApproved), new DateTime(2026, 4, 8, 10, 15, 0)),
            new("LV-2026-204", "Đơn nghỉ đột xuất vượt định mức", "hrm", "Nhân sự (HRM)", "Lý Văn Khôi", 0m, "LeaveRequest", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.Rejected), new DateTime(2026, 4, 6, 16, 0, 0)),

            new("PR-2026-04A", "Kỳ lương nháp khối Văn phòng T04/2026", "payroll", "Lương & Phúc lợi", "Trịnh Thu Hà", 420_000_000m, "PayrollPeriod", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PayrollPeriod, PayrollPeriodStatus.Draft), new DateTime(2026, 4, 11, 10, 0, 0)),
            new("PR-2026-03B", "Bảng tính lương khối Kinh doanh T03/2026", "payroll", "Lương & Phúc lợi", "Trịnh Thu Hà", 615_500_000m, "PayrollPeriod", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PayrollPeriod, PayrollPeriodStatus.Calculated), new DateTime(2026, 4, 5, 14, 15, 0)),
            new("PR-2026-03C", "Chốt sổ lương khối Kho vận T03/2026", "payroll", "Lương & Phúc lợi", "Mai Xuân Trường", 278_300_000m, "PayrollPeriod", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PayrollPeriod, PayrollPeriodStatus.Locked), new DateTime(2026, 4, 4, 18, 0, 0)),
            new("PR-2026-02D", "Chi trả lương toàn công ty T02/2026", "payroll", "Lương & Phúc lợi", "Mai Xuân Trường", 1_280_000_000m, "PayrollPeriod", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PayrollPeriod, PayrollPeriodStatus.Paid), new DateTime(2026, 3, 5, 11, 20, 0)),

            new("SL-2026-501", "Phiếu lương tháng 04 - Khối Kỹ thuật", "payroll", "Lương & Phúc lợi", "Đinh Gia Bảo", 28_500_000m, "SalarySlip", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.SalarySlip, SalarySlipStatus.Draft), new DateTime(2026, 4, 11, 11, 10, 0)),
            new("SL-2026-502", "Phiếu lương tháng 03 - Trưởng nhóm Kinh doanh", "payroll", "Lương & Phúc lợi", "Nguyễn Thị Mai", 34_200_000m, "SalarySlip", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.SalarySlip, SalarySlipStatus.Sent), new DateTime(2026, 4, 3, 9, 45, 0)),
            new("SL-2026-503", "Phiếu lương tháng 03 - Chuyên viên Nhân sự", "payroll", "Lương & Phúc lợi", "Đặng Ngọc Ánh", 19_800_000m, "SalarySlip", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.SalarySlip, SalarySlipStatus.Confirmed), new DateTime(2026, 4, 4, 15, 30, 0)),

            new("CMS-2026-01", "Bài viết ra mắt giải pháp ERP hợp nhất 2026", "cms", "Nội dung (CMS)", "Võ Khánh Linh", 0m, "Common", "PUBLISHED", new DateTime(2026, 4, 10, 12, 0, 0)),
            new("CMS-2026-02", "Cập nhật menu điều hướng trang tuyển dụng", "cms", "Nội dung (CMS)", "Võ Khánh Linh", 0m, "Common", "PENDING", new DateTime(2026, 4, 9, 14, 50, 0)),
            new("SYS-2026-01", "Đồng bộ ma trận phân quyền RBAC quý II", "system", "Hệ thống", "Super Admin", 0m, "Common", "ACTIVE", new DateTime(2026, 4, 11, 7, 30, 0))
        };

        public UIController(ISuperAdminService superAdmin)
        {
            _superAdmin = superAdmin;
        }

        [HttpGet]
        public async Task<IActionResult> Styleguide(CancellationToken cancellationToken)
        {
            var denied = await EnsureSuperAdminAsync(cancellationToken);
            if (denied != null)
            {
                return denied;
            }

            ViewData["Module"] = "system";
            ViewData["Title"] = "UI Styleguide";

            var model = QuerySampleTable(null, null, null, "code", "asc", 1, 5);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> StyleguideTableData(
            string? keyword,
            string? moduleFilter,
            string? statusFilter,
            string sortBy = "code",
            string sortDir = "asc",
            int page = 1,
            int pageSize = 5,
            CancellationToken cancellationToken = default)
        {
            var denied = await EnsureSuperAdminAsync(cancellationToken);
            if (denied != null)
            {
                return denied;
            }

            var model = QuerySampleTable(keyword, moduleFilter, statusFilter, sortBy, sortDir, page, pageSize);
            return PartialView("_StyleguideTablePartial", model);
        }

        private async Task<IActionResult?> EnsureSuperAdminAsync(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool allowed = int.TryParse(userIdClaim, out int userId)
                && await _superAdmin.LaTaiKhoanQuanTriTongAsync(userId, cancellationToken);

            if (allowed)
            {
                return null;
            }

            bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (isAjax)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Chỉ Super Admin mới được phép truy cập trang UI Styleguide."
                })
                {
                    StatusCode = 403
                };
            }

            Response.StatusCode = 403;
            return View("~/Views/Auth/AccessDenied.cshtml", new ErrorViewModel
            {
                StatusCode = 403,
                ReturnUrl = Request.Path + Request.QueryString,
                RequestId = HttpContext.TraceIdentifier,
                Title = "Chỉ dành cho Super Admin",
                Message = "Trang /UI/Styleguide chỉ mở cho tài khoản Quản trị viên tổng (Super Admin) để kiểm định Design System và bộ UI Component dùng chung."
            });
        }

        private static StyleguideTableViewModel QuerySampleTable(
            string? keyword,
            string? moduleFilter,
            string? statusFilter,
            string? sortBy,
            string? sortDir,
            int page,
            int pageSize)
        {
            IEnumerable<StyleguideSampleRow> query = SampleRows;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim();
                query = query.Where(r =>
                    r.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.Title.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.Owner.Contains(kw, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(moduleFilter))
            {
                string mf = moduleFilter.Trim();
                query = query.Where(r => r.Module.Equals(mf, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                string sf = statusFilter.Trim();
                query = query.Where(r =>
                {
                    var badge = Helpers.WorkflowStatusUiHelper.ResolveObject(r.StatusCode, r.Workflow);
                    return badge.Variant.Equals(sf, StringComparison.OrdinalIgnoreCase)
                        || r.StatusCode.Equals(sf, StringComparison.OrdinalIgnoreCase);
                });
            }

            string normalizedSort = (sortBy ?? "code").Trim().ToLowerInvariant();
            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            query = (normalizedSort, desc) switch
            {
                ("title", false)     => query.OrderBy(r => r.Title),
                ("title", true)      => query.OrderByDescending(r => r.Title),
                ("module", false)    => query.OrderBy(r => r.ModuleLabel),
                ("module", true)     => query.OrderByDescending(r => r.ModuleLabel),
                ("owner", false)     => query.OrderBy(r => r.Owner),
                ("owner", true)      => query.OrderByDescending(r => r.Owner),
                ("amount", false)    => query.OrderBy(r => r.Amount),
                ("amount", true)     => query.OrderByDescending(r => r.Amount),
                ("updatedat", false) => query.OrderBy(r => r.UpdatedAt),
                ("updatedat", true)  => query.OrderByDescending(r => r.UpdatedAt),
                (_, true)            => query.OrderByDescending(r => r.Code),
                _                    => query.OrderBy(r => r.Code)
            };

            var filtered = query.ToList();
            int safePageSize = Math.Clamp(pageSize, 5, 50);
            int totalCount = filtered.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)safePageSize));
            int safePage = Math.Clamp(page, 1, totalPages);

            var paged = filtered
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToList();

            return new StyleguideTableViewModel
            {
                Items = paged,
                PageIndex = safePage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = safePageSize,
                SortBy = normalizedSort,
                SortDir = desc ? "desc" : "asc",
                Keyword = keyword,
                ModuleFilter = moduleFilter,
                StatusFilter = statusFilter
            };
        }
    }
}

