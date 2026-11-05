using ERP.Application.Common.Interfaces;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class BangLuongController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPayrollService _payrollService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDataScopeService _dataScope;

        public BangLuongController(
            AppDbContext context,
            IPayrollService payrollService,
            ICurrentUserService currentUser,
            IDataScopeService dataScope)
        {
            _context = context;
            _payrollService = payrollService;
            _currentUser = currentUser;
            _dataScope = dataScope;
        }

        // 1. BẢNG TỔNG HỢP LƯƠNG TOÀN CÔNG TY
        [HttpGet]
        [KiemTraQuyen("hrm.payroll.view")]
        public async Task<IActionResult> Index(int? month, int? year, int? departmentId, string? keyword)
        {
            int targetMonth = month ?? VietnamTime.Today.Month;
            int targetYear = year ?? VietnamTime.Today.Year;

            var period = await _context.PayrollPeriods
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.Department)
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.Position)
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(p => p.Month == targetMonth && p.Year == targetYear);

            // Nếu kỳ lương chưa được tính, tự động tính toán sơ bộ để người dùng có số liệu tức thì
            if (period == null)
            {
                period = await _payrollService.CalculatePayrollAsync(targetMonth, targetYear, 22, GetCurrentUserId());
            }

            // Áp dụng DataScope lên danh sách phiếu lương nếu tài khoản không ở phạm vi toàn công ty
            if (period != null && _dataScope.GetHrmScopeLevel() != DataScopeLevel.Company)
            {
                period.SalarySlips = period.SalarySlips
                    .Where(s => s.Employee != null &&
                                _dataScope.CanAccessEmployee(s.EmployeeId, s.Employee.DepartmentId, s.Employee.Department?.BranchId ?? 0))
                    .ToList();
            }

            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Keyword = keyword;
            ViewBag.Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();

            // So sánh với kỳ trước & Kiểm tra khóa kỳ công
            ViewBag.Comparison = await _payrollService.GetPayrollComparisonAsync(targetMonth, targetYear);
            var attLock = await _context.AttendanceLocks.FirstOrDefaultAsync(l => l.Month == targetMonth && l.Year == targetYear);
            ViewBag.IsAttendanceLocked = attLock?.IsLocked ?? false;

            return View(period);
        }

        // 2. TÍNH TOÁN LẠI LƯƠNG TỰ ĐỘNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.payroll.manage")]
        public async Task<IActionResult> TinhLuong(int month, int year, int standardDays)
        {
            if (standardDays <= 0) standardDays = 22;
            int currentUserId = GetCurrentUserId();

            var period = await _payrollService.CalculatePayrollAsync(month, year, standardDays, currentUserId);

            TempData["SuccessMessage"] = $"Đã tính toán thành công bảng lương Kỳ Tháng {month:D2}/{year} cho {period.SalarySlips.Count} nhân sự!";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        // 3. GỬI PHIẾU LƯƠNG (NOTIFICATION SIGNALR REAL-TIME)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.payroll.send")]
        public async Task<IActionResult> GuiPhieuLuong(int payrollPeriodId, int? employeeId)
        {
            var period = await _context.PayrollPeriods.FindAsync(payrollPeriodId);
            if (period == null) return NotFound();

            bool result = await _payrollService.SendPayslipsAsync(payrollPeriodId, employeeId);

            if (result)
            {
                TempData["SuccessMessage"] = employeeId.HasValue
                    ? "Đã gửi phiếu lương thành công cho nhân sự!"
                    : $"Đã phát hành và gửi phiếu lương toàn bộ kỳ lương {period.Name} tới tất cả nhân viên!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi phát hành phiếu lương!";
            }

            return RedirectToAction(nameof(Index), new { month = period.Month, year = period.Year });
        }

        // 4. XEM PHIẾU LƯƠNG CÁ NHÂN CHI TIẾT (CHUẨN IN A5/A4 & THIẾT KẾ ĐẲNG CẤP)
        [HttpGet]
        [KiemTraQuyen("hrm.payroll.view")]
        public async Task<IActionResult> PhieuLuong(int id)
        {
            var slip = await _dataScope.ApplySalarySlipScope(_context.SalarySlips)
                .Include(s => s.PayrollPeriod)
                .Include(s => s.Employee)
                    .ThenInclude(e => e.Department)
                .Include(s => s.Employee)
                    .ThenInclude(e => e.Position)
                .Include(s => s.Employee)
                    .ThenInclude(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slip == null) return NotFound();

            return View(slip);
        }

        // 5. XUẤT BẢNG LƯƠNG RA FILE EXCEL (CLOSEDXML)
        [HttpGet]
        [KiemTraQuyen("hrm.payroll.view")]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var period = await _context.PayrollPeriods
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.Department)
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.Position)
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (period == null) return NotFound();

            if (_dataScope.GetHrmScopeLevel() != DataScopeLevel.Company)
            {
                period.SalarySlips = period.SalarySlips
                    .Where(s => s.Employee != null &&
                                _dataScope.CanAccessEmployee(s.EmployeeId, s.Employee.DepartmentId, s.Employee.Department?.BranchId ?? 0))
                    .ToList();
            }

            var fileBytes = _payrollService.GeneratePayrollExcel(period);
            string fileName = $"BangLuong_{period.Month:D2}_{period.Year}_{VietnamTime.Now:yyyyMMddHHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // 6. XUẤT BẢNG KÊ TRÍCH NỘP BẢO HIỂM (PHẦN NGƯỜI SỬ DỤNG LAO ĐỘNG)
        [HttpGet]
        [KiemTraQuyen("hrm.payroll.view")]
        public async Task<IActionResult> ExportBangKeBaoHiem(int id)
        {
            var period = await _context.PayrollPeriods
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (period == null) return NotFound();

            if (_dataScope.GetHrmScopeLevel() != DataScopeLevel.Company)
            {
                period.SalarySlips = period.SalarySlips
                    .Where(s => s.Employee != null &&
                                _dataScope.CanAccessEmployee(s.EmployeeId, s.Employee.DepartmentId, s.Employee.Department?.BranchId ?? 0))
                    .ToList();
            }

            var fileBytes = _payrollService.GenerateEmployerInsuranceExcel(period);
            string fileName = $"BangKeBaoHiem_{period.Month:D2}_{period.Year}_{VietnamTime.Now:yyyyMMddHHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // 7. XUẤT ỦY NHIỆM CHI CHUYỂN KHOẢN NGÂN HÀNG (GIỚI HẠN ADMIN_BANGLUONG HOẶC SUPERADMIN & GHI AUDITLOG)
        [HttpGet]
        public async Task<IActionResult> XuatUyNhiemChi(int id)
        {
            bool isAuthorized = User.IsInRole("SUPERADMIN") ||
                                User.IsInRole("ADMIN_BANGLUONG") ||
                                User.HasClaim("permission", "ADMIN_BANGLUONG") ||
                                User.HasClaim("role", "SUPERADMIN") ||
                                User.HasClaim("role", "ADMIN_BANGLUONG");

            if (!isAuthorized)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = _currentUser.UserId,
                    Username = _currentUser.Username ?? User.Identity?.Name ?? "Anonymous",
                    Action = "SECURITY_VIOLATION",
                    EntityName = "PayrollPeriod",
                    RecordId = id.ToString(),
                    NewValues = "Truy cập trái phép xuất Ủy nhiệm chi ngân hàng khi không có quyền ADMIN_BANGLUONG hoặc SUPERADMIN",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = VietnamTime.Now
                });
                await _context.SaveChangesAsync();

                return StatusCode(StatusCodes.Status403Forbidden, "Bạn không có quyền xuất Ủy nhiệm chi chuyển khoản ngân hàng!");
            }

            var period = await _context.PayrollPeriods
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.Department)
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.Position)
                .Include(p => p.SalarySlips)
                    .ThenInclude(s => s.Employee)
                        .ThenInclude(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (period == null) return NotFound();

            if (_dataScope.GetHrmScopeLevel() != DataScopeLevel.Company)
            {
                period.SalarySlips = period.SalarySlips
                    .Where(s => s.Employee != null &&
                                _dataScope.CanAccessEmployee(s.EmployeeId, s.Employee.DepartmentId, s.Employee.Department?.BranchId ?? 0))
                    .ToList();
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _currentUser.UserId,
                Username = _currentUser.Username ?? User.Identity?.Name ?? "System",
                Action = "EXPORT_BANK_TRANSFER",
                EntityName = "PayrollPeriod",
                RecordId = id.ToString(),
                NewValues = $"Xuất file Ủy nhiệm chi chuyển khoản ngân hàng kỳ {period.Month:D2}/{period.Year}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = VietnamTime.Now
            });
            await _context.SaveChangesAsync();

            var fileBytes = _payrollService.GenerateBankTransferExcel(period);
            string fileName = $"UyNhiemChi_{period.Month:D2}_{period.Year}_{VietnamTime.Now:yyyyMMddHHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // 8. KHÓA KỲ LƯƠNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.payroll.manage")]
        public async Task<IActionResult> KhoaBangLuong(int id)
        {
            var period = await _context.PayrollPeriods.FindAsync(id);
            if (period == null) return NotFound();

            period.Status = ERP_HRM_CMS.Models.Enums.PayrollPeriodStatus.Locked;
            period.LockedAt = VietnamTime.Now;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _currentUser.UserId,
                Username = _currentUser.Username ?? User.Identity?.Name ?? "System",
                Action = "LOCK_PAYROLL",
                EntityName = "PayrollPeriod",
                RecordId = id.ToString(),
                NewValues = $"Khóa kỳ lương {period.Month:D2}/{period.Year}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = VietnamTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã khóa thành công kỳ lương {period.Month:D2}/{period.Year}!";
            return RedirectToAction(nameof(Index), new { month = period.Month, year = period.Year });
        }

        // 9. CHUYỂN TRẠNG THÁI CHI TRẢ & PHÁT HÀNH PHIẾU LƯƠNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.payroll.manage")]
        public async Task<IActionResult> ChuyenChiTra(int id)
        {
            var period = await _context.PayrollPeriods.FindAsync(id);
            if (period == null) return NotFound();

            period.Status = ERP_HRM_CMS.Models.Enums.PayrollPeriodStatus.Paid;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _currentUser.UserId,
                Username = _currentUser.Username ?? User.Identity?.Name ?? "System",
                Action = "PAY_PAYROLL",
                EntityName = "PayrollPeriod",
                RecordId = id.ToString(),
                NewValues = $"Chuyển trạng thái Đã chi trả và phát hành phiếu lương kỳ {period.Month:D2}/{period.Year}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = VietnamTime.Now
            });

            await _context.SaveChangesAsync();
            await _payrollService.SendPayslipsAsync(id, null);

            TempData["SuccessMessage"] = $"Đã xác nhận chi trả và phát hành phiếu lương toàn bộ nhân sự kỳ {period.Month:D2}/{period.Year}!";
            return RedirectToAction(nameof(Index), new { month = period.Month, year = period.Year });
        }

        private int GetCurrentUserId() => _currentUser.GetRequiredUserId();
    }
}
