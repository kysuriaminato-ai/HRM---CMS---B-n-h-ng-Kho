using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Services.Printing;

namespace ERP_HRM_CMS.Controllers
{
    /// <summary>
    /// Trung tâm in ấn. Mỗi action chỉ lấy dữ liệu rồi tạo một IPrintable và giao cho IPrintService.
    /// [OOP - KẾ THỪA] PhieuInController kế thừa Controller của ASP.NET Core.
    /// [OOP - ĐA HÌNH] Action InKyLuongDayDu đưa cả BangLuongPrintable lẫn PhieuLuongPrintable vào một List&lt;IPrintable&gt;.
    /// </summary>
    [Authorize]
    public class PhieuInController : Controller
    {
        private const string HtmlContentType = "text/html; charset=utf-8";

        private readonly AppDbContext _context;
        private readonly IPrintService _printService;

        public PhieuInController(AppDbContext context, IPrintService printService)
        {
            _context = context;
            _printService = printService;
        }

        /// <summary>In phiếu lương một nhân viên.</summary>
        [HttpGet]
        [KiemTraQuyen("hrm.payroll.view")]
        public async Task<IActionResult> PhieuLuong(int id)
        {
            var phieu = await _context.SalarySlips
                .Include(s => s.PayrollPeriod)
                .Include(s => s.Employee).ThenInclude(e => e.Department)
                .Include(s => s.Employee).ThenInclude(e => e.Position)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (phieu == null) return NotFound();

            var doc = new PhieuLuongPrintable(phieu.PayrollPeriod, phieu, phieu.Employee);
            return Content(_printService.Render(doc), HtmlContentType);
        }

        /// <summary>In bảng lương của một kỳ, có thể lọc theo phòng ban.</summary>
        [HttpGet]
        [KiemTraQuyen("hrm.payroll.view")]
        public async Task<IActionResult> BangLuong(int periodId, int? departmentId)
        {
            var ky = await _context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == periodId);
            if (ky == null) return NotFound();

            var query = _context.SalarySlips
                .Include(s => s.Employee).ThenInclude(e => e.Department)
                .Where(s => s.PayrollPeriodId == periodId);

            if (departmentId.HasValue)
            {
                query = query.Where(s => s.Employee.DepartmentId == departmentId.Value);
            }

            var cacPhieu = await query.OrderBy(s => s.Employee.EmployeeCode).ToListAsync();

            var doc = new BangLuongPrintable(ky, cacPhieu);
            return Content(_printService.Render(doc), HtmlContentType);
        }

        /// <summary>In cả kỳ lương: bảng tổng hợp rồi đến từng phiếu lương, trong một lần in (đa hình).</summary>
        [HttpGet]
        [KiemTraQuyen("hrm.payroll.view")]
        public async Task<IActionResult> InKyLuongDayDu(int periodId)
        {
            var ky = await _context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == periodId);
            if (ky == null) return NotFound();

            var cacPhieu = await _context.SalarySlips
                .Include(s => s.Employee).ThenInclude(e => e.Department)
                .Include(s => s.Employee).ThenInclude(e => e.Position)
                .Where(s => s.PayrollPeriodId == periodId)
                .OrderBy(s => s.Employee.EmployeeCode)
                .ToListAsync();

            // [OOP - ĐA HÌNH] Một danh sách, nhiều loại phiếu khác nhau
            var cacPhieuIn = new List<IPrintable> { new BangLuongPrintable(ky, cacPhieu) };
            foreach (var phieu in cacPhieu)
            {
                cacPhieuIn.Add(new PhieuLuongPrintable(ky, phieu, phieu.Employee));
            }

            return Content(_printService.RenderBatch(cacPhieuIn), HtmlContentType);
        }

        /// <summary>In hóa đơn bán hàng POS.</summary>
        [HttpGet]
        [KiemTraQuyen("order.view")]
        public async Task<IActionResult> HoaDon(long id)
        {
            var don = await _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (don == null) return NotFound();

            return Content(_printService.Render(new HoaDonPrintable(don)), HtmlContentType);
        }
    }
}
