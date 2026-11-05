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
    [Route("DaoTao")]
    public class DaoTaoController : Controller
    {
        private readonly ITrainingService _trainingService;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ICurrentUserService _currentUser;

        public DaoTaoController(
            ITrainingService trainingService,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            ICurrentUserService currentUser)
        {
            _trainingService = trainingService;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _currentUser = currentUser;
        }

        private int CurrentUserId => _currentUser.UserId ?? 1;

        // 1. DANH SÁCH KHÓA ĐÀO TẠO NỘI BỘ
        [HttpGet("")]
        [HttpGet("Index")]
        [KiemTraQuyen("hrm.employee.view,hrm.training.view")]
        public async Task<IActionResult> Index(string? keyword, string? status)
        {
            var courses = await _trainingService.GetTrainingsAsync(keyword, status);
            ViewBag.Keyword = keyword;
            ViewBag.Status = status;

            return View(courses);
        }

        // 2. TẠO MỚI KHÓA ĐÀO TẠO
        [HttpGet("TaoMoi")]
        [KiemTraQuyen("hrm.employee.manage,hrm.training.manage")]
        public async Task<IActionResult> TaoMoi()
        {
            var departments = await _departmentService.GetFlatListAsync();
            ViewBag.Departments = departments;

            var model = new TrainingCreateEditDto
            {
                StartDate = DateTime.Now.Date.AddDays(7),
                EndDate = DateTime.Now.Date.AddDays(14),
                Budget = 0
            };

            return View(model);
        }

        [HttpPost("TaoMoi")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage,hrm.training.manage")]
        public async Task<IActionResult> TaoMoi(TrainingCreateEditDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _departmentService.GetFlatListAsync();
                return View(dto);
            }

            var result = await _trainingService.CreateTrainingAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Không thể tạo khóa đào tạo!");
                ViewBag.Departments = await _departmentService.GetFlatListAsync();
                return View(dto);
            }

            TempData["SuccessMessage"] = $"Đã tạo khóa đào tạo '{dto.Title}' thành công!";
            return RedirectToAction(nameof(HocVien), new { id = result.Value });
        }

        // 3. CHỈNH SỬA KHÓA ĐÀO TẠO
        [HttpGet("ChinhSua/{id}")]
        [KiemTraQuyen("hrm.employee.manage,hrm.training.manage")]
        public async Task<IActionResult> ChinhSua(int id)
        {
            var course = await _trainingService.GetTrainingByIdAsync(id);
            if (course == null) return NotFound();

            var dto = new TrainingCreateEditDto
            {
                Id = course.Id,
                Title = course.Title,
                TrainerName = course.TrainerName,
                Location = course.Location,
                Description = course.Description,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                Budget = course.Budget,
                Status = course.Status
            };

            ViewBag.Departments = await _departmentService.GetFlatListAsync();
            return View(dto);
        }

        [HttpPost("ChinhSua/{id}")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage,hrm.training.manage")]
        public async Task<IActionResult> ChinhSua(int id, TrainingCreateEditDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _departmentService.GetFlatListAsync();
                return View(dto);
            }

            dto.Id = id;
            var result = await _trainingService.UpdateTrainingAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Không thể cập nhật khóa đào tạo!");
                ViewBag.Departments = await _departmentService.GetFlatListAsync();
                return View(dto);
            }

            TempData["SuccessMessage"] = "Đã cập nhật thông tin khóa đào tạo thành công!";
            return RedirectToAction(nameof(HocVien), new { id });
        }

        // 4. QUẢN LÝ HỌC VIÊN & CHẤM ĐIỂM (CẤP CHỨNG CHỈ / NÂNG CẤP KỸ NĂNG TỰ ĐỘNG)
        [HttpGet("HocVien/{id}")]
        [KiemTraQuyen("hrm.employee.view,hrm.training.view")]
        public async Task<IActionResult> HocVien(int id)
        {
            var course = await _trainingService.GetTrainingByIdAsync(id);
            if (course == null) return NotFound();

            var employeesResult = await _employeeService.GetPagedAsync(new EmployeeFilterDto { Page = 1, PageSize = 500 });
            var assignedEmpIds = course.Participants.Select(p => p.EmployeeId).ToHashSet();
            ViewBag.AvailableEmployees = employeesResult.Where(e => !assignedEmpIds.Contains(e.Id)).ToList();
            ViewBag.Departments = await _departmentService.GetFlatListAsync();

            return View(course);
        }

        [HttpPost("GanHocVien")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage,hrm.training.manage")]
        public async Task<IActionResult> GanHocVien(AssignTrainingParticipantsDto dto)
        {
            var result = await _trainingService.AssignParticipantsAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã bổ sung học viên vào khóa đào tạo thành công!";
            }

            return RedirectToAction(nameof(HocVien), new { id = dto.TrainingId });
        }

        [HttpPost("ChamDiemHocVien")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage,hrm.training.manage")]
        public async Task<IActionResult> ChamDiemHocVien(GradeParticipantDto dto)
        {
            var result = await _trainingService.GradeParticipantAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã lưu kết quả thi và cập nhật kỹ năng/chứng chỉ tự động cho học viên!";
            }

            return RedirectToAction(nameof(HocVien), new { id = dto.TrainingId });
        }
    }
}
