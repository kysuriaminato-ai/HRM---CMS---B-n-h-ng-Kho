using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class BangLuongController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPayrollService _payrollService;

        public BangLuongController(AppDbContext context, IPayrollService payrollService)
        {
            _context = context;
            _payrollService = payrollService;
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

            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Keyword = keyword;
            ViewBag.Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();

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
            var slip = await _context.SalarySlips
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

            var fileBytes = _payrollService.GenerateEmployerInsuranceExcel(period);
            string fileName = $"BangKeBaoHiem_{period.Month:D2}_{period.Year}_{VietnamTime.Now:yyyyMMddHHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid)) return uid;
            return HttpContext.Session.GetInt32("UserId") ?? 1;
        }
    }
}
