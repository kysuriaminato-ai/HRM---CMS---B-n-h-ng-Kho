using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    [Route("DanhGiaKpi")]
    public class DanhGiaKpiController : Controller
    {
        private readonly IPerformanceReviewService _kpiService;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ICurrentUserService _currentUser;

        public DanhGiaKpiController(
            IPerformanceReviewService kpiService,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            ICurrentUserService currentUser)
        {
            _kpiService = kpiService;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _currentUser = currentUser;
        }

        private int CurrentUserId => _currentUser.UserId ?? 1;

        // 1. DANH SÁCH ĐÁNH GIÁ KPI
        [HttpGet("")]
        [HttpGet("Index")]
        [KiemTraQuyen("hrm.employee.view,hrm.payroll.view,hrm.kpi.view")]
        public async Task<IActionResult> Index(int? month, int? year, int? departmentId, string? grade, string? status)
        {
            int targetMonth = month ?? DateTime.Now.Month;
            int targetYear = year ?? DateTime.Now.Year;

            var reviews = await _kpiService.GetKpiReviewsAsync(targetMonth, targetYear, departmentId, grade, status);
            var departments = await _departmentService.GetFlatListAsync();

            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.SelectedGrade = grade;
            ViewBag.SelectedStatus = status;
            ViewBag.Departments = departments;

            return View(reviews);
        }

        // 2. TẠO MỚI PHIẾU ĐÁNH GIÁ KPI
        [HttpGet("TaoMoi")]
        [KiemTraQuyen("hrm.employee.manage,hrm.kpi.manage")]
        public async Task<IActionResult> TaoMoi()
        {
            var employeesResult = await _employeeService.GetPagedAsync(new EmployeeFilterDto { Page = 1, PageSize = 500 });
            ViewBag.Employees = employeesResult.OrderBy(e => e.FullName).ToList();

            var model = new CreateKpiReviewDto
            {
                PeriodMonth = DateTime.Now.Month,
                PeriodYear = DateTime.Now.Year,
                Criteria = new System.Collections.Generic.List<KpiCriteriaDto>
                {
                    new KpiCriteriaDto { CriteriaName = "Hoàn thành công việc chuyên môn đúng tiến độ", Weight = 40 },
                    new KpiCriteriaDto { CriteriaName = "Chất lượng sản phẩm / dịch vụ & độ chính xác", Weight = 30 },
                    new KpiCriteriaDto { CriteriaName = "Kỷ luật, thái độ & văn hóa doanh nghiệp", Weight = 15 },
                    new KpiCriteriaDto { CriteriaName = "Đổi mới sáng tạo & cải tiến quy trình", Weight = 15 }
                }
            };

            return View(model);
        }

        [HttpPost("TaoMoi")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage,hrm.kpi.manage")]
        public async Task<IActionResult> TaoMoi(CreateKpiReviewDto dto)
        {
            if (!ModelState.IsValid)
            {
                var employeesResult = await _employeeService.GetPagedAsync(new EmployeeFilterDto { Page = 1, PageSize = 500 });
                ViewBag.Employees = employeesResult.OrderBy(e => e.FullName).ToList();
                return View(dto);
            }

            var result = await _kpiService.CreateKpiReviewAsync(dto, CurrentUserId);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Có lỗi khi tạo phiếu đánh giá KPI!");
                var employeesResult = await _employeeService.GetPagedAsync(new EmployeeFilterDto { Page = 1, PageSize = 500 });
                ViewBag.Employees = employeesResult.OrderBy(e => e.FullName).ToList();
                return View(dto);
            }

            TempData["SuccessMessage"] = "Đã khởi tạo phiếu đánh giá KPI thành công!";
            return RedirectToAction(nameof(ChiTiet), new { id = result.Value });
        }

        // 3. CHI TIẾT & BẢNG ĐIỀU KHIỂN ĐÁNH GIÁ (RADAR NĂNG LỰC)
        [HttpGet("ChiTiet/{id}")]
        [KiemTraQuyen("hrm.employee.view,hrm.kpi.view,hrm.self.service")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var review = await _kpiService.GetKpiReviewByIdAsync(id);
            if (review == null) return NotFound();

            return View(review);
        }

        // 4. BƯỚC 1: NHÂN VIÊN TỰ ĐÁNH GIÁ
        [HttpPost("TuDanhGia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuDanhGia(SelfEvaluateKpiDto dto)
        {
            int empId = _currentUser.EmployeeId ?? CurrentUserId;
            var result = await _kpiService.SelfEvaluateAsync(dto, empId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã lưu kết quả tự đánh giá KPI thành công!";
            }

            return RedirectToAction(nameof(ChiTiet), new { id = dto.KpiReviewId });
        }

        // 5. BƯỚC 2: QUẢN LÝ ĐÁNH GIÁ & CHẤM ĐIỂM TIỀM NĂNG
        [HttpPost("QuanLyDanhGia")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage,hrm.kpi.manage")]
        public async Task<IActionResult> QuanLyDanhGia(ManagerEvaluateKpiDto dto)
        {
            var result = await _kpiService.ManagerEvaluateAsync(dto, CurrentUserId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Quản lý đã hoàn tất đánh giá hiệu suất & xếp hạng thành công!";
            }

            return RedirectToAction(nameof(ChiTiet), new { id = dto.KpiReviewId });
        }

        // 6. BƯỚC 3: BAN GIÁM ĐỐC / HR DUYỆT ĐÁNH GIÁ
        [HttpPost("PheDuyet")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage,hrm.kpi.manage")]
        public async Task<IActionResult> PheDuyet(ApproveKpiDto dto)
        {
            var result = await _kpiService.ApproveKpiAsync(dto, CurrentUserId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã phê duyệt phiếu đánh giá KPI và đồng bộ thưởng vào kỳ lương!";
            }

            return RedirectToAction(nameof(ChiTiet), new { id = dto.KpiReviewId });
        }

        // 7. MA TRẬN NHÂN TÀI 9-BOX (9-BOX TALENT MATRIX)
        [HttpGet("NineBox")]
        [KiemTraQuyen("hrm.employee.view,hrm.kpi.view")]
        public async Task<IActionResult> NineBox(int? month, int? year, int? departmentId)
        {
            int targetMonth = month ?? DateTime.Now.Month;
            int targetYear = year ?? DateTime.Now.Year;

            var matrix = await _kpiService.GetNineBoxMatrixAsync(targetMonth, targetYear, departmentId);
            var departments = await _departmentService.GetFlatListAsync();

            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.Departments = departments;

            return View(matrix);
        }
    }
}
