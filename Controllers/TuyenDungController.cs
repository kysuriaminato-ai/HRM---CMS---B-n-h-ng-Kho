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
    [Route("TuyenDung")]
    public class TuyenDungController : Controller
    {
        private readonly IRecruitmentService _recruitmentService;
        private readonly IOnboardingService _onboardingService;
        private readonly IEmployeeService _employeeService;
        private readonly ICurrentUserService _currentUser;

        public TuyenDungController(
            IRecruitmentService recruitmentService,
            IOnboardingService onboardingService,
            IEmployeeService employeeService,
            ICurrentUserService currentUser)
        {
            _recruitmentService = recruitmentService;
            _onboardingService = onboardingService;
            _employeeService = employeeService;
            _currentUser = currentUser;
        }

        private int CurrentUserId => _currentUser.UserId ?? 1;

        #region 1. Quản lý Tin Tuyển Dụng (Job Postings)

        [HttpGet("")]
        [HttpGet("Index")]
        [KiemTraQuyen("hrm.recruitment.view,hrm.recruitment.manage")]
        public async Task<IActionResult> Index(
            string? keyword, int? departmentId, int? branchId, string? status, int page = 1, int pageSize = 20)
        {
            var filter = new JobPostingFilterDto
            {
                Keyword = keyword,
                DepartmentId = departmentId,
                BranchId = branchId,
                Status = status,
                Page = page,
                PageSize = pageSize
            };

            var paged = await _recruitmentService.GetJobPostingsAsync(filter);
            var dropdowns = await _employeeService.GetDropdownDataAsync();

            ViewBag.Filter = filter;
            ViewBag.Departments = dropdowns.Departments;

            return View(paged);
        }

        [HttpGet("TaoMoi")]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> TaoMoi()
        {
            var dropdowns = await _employeeService.GetDropdownDataAsync();
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;

            var model = new JobPostingCreateEditDto
            {
                OpenDate = DateTime.Today,
                CloseDate = DateTime.Today.AddDays(30),
                Vacancies = 1,
                Status = "Open"
            };

            return View(model);
        }

        [HttpPost("TaoMoi")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> TaoMoi(JobPostingCreateEditDto model)
        {
            if (!ModelState.IsValid)
            {
                var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
                ViewBag.Departments = dropdowns.Departments;
                ViewBag.Positions = dropdowns.Positions;
                return View(model);
            }

            var result = await _recruitmentService.CreateJobPostingAsync(model, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Lỗi không xác định");
                var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
                ViewBag.Departments = dropdowns.Departments;
                ViewBag.Positions = dropdowns.Positions;
                return View(model);
            }

            TempData["SuccessMessage"] = "Tạo tin tuyển dụng thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("ChinhSua/{id:int}")]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> ChinhSua(int id)
        {
            var job = await _recruitmentService.GetJobPostingByIdAsync(id);
            if (job == null) return NotFound();

            var dropdowns = await _employeeService.GetDropdownDataAsync(job.DepartmentId, job.PositionId);
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;

            var model = new JobPostingCreateEditDto
            {
                Id = job.Id,
                Title = job.Title,
                Slug = job.Slug,
                DepartmentId = job.DepartmentId,
                PositionId = job.PositionId,
                BranchId = job.BranchId,
                Vacancies = job.Vacancies,
                Description = job.Description,
                Requirements = job.Requirements,
                Benefits = job.Benefits,
                SalaryRange = job.SalaryRange,
                WorkLocation = job.WorkLocation,
                Status = job.Status,
                OpenDate = job.OpenDate,
                CloseDate = job.CloseDate,
                MetaTitle = job.MetaTitle,
                MetaDescription = job.MetaDescription,
                MetaKeywords = job.MetaKeywords,
                OgImage = job.OgImage
            };

            return View(model);
        }

        [HttpPost("ChinhSua/{id:int}")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> ChinhSua(int id, JobPostingCreateEditDto model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
                ViewBag.Departments = dropdowns.Departments;
                ViewBag.Positions = dropdowns.Positions;
                return View(model);
            }

            var result = await _recruitmentService.UpdateJobPostingAsync(model, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Lỗi không xác định");
                var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
                ViewBag.Departments = dropdowns.Departments;
                ViewBag.Positions = dropdowns.Positions;
                return View(model);
            }

            TempData["SuccessMessage"] = "Cập nhật tin tuyển dụng thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Xoa/{id:int}")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> Xoa(int id)
        {
            var result = await _recruitmentService.DeleteJobPostingAsync(id, CurrentUserId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã xóa tin tuyển dụng.";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region 2. Kanban Pipeline Ứng Viên Kéo - Thả (ATS Pipeline)

        [HttpGet("Kanban")]
        [KiemTraQuyen("hrm.recruitment.view,hrm.recruitment.manage")]
        public async Task<IActionResult> Kanban(int? jobPostingId, string? keyword, decimal? minScore)
        {
            var candidates = await _recruitmentService.GetKanbanCandidatesAsync(jobPostingId, keyword, minScore);

            // Nhóm ứng viên vào 6 cột trạng thái
            var board = new
            {
                Applied = candidates.Where(c => c.Status == "Applied").ToList(),
                Screening = candidates.Where(c => c.Status == "Screening").ToList(),
                Interview = candidates.Where(c => c.Status == "Interview").ToList(),
                Offered = candidates.Where(c => c.Status == "Offered").ToList(),
                Hired = candidates.Where(c => c.Status == "Hired").ToList(),
                Rejected = candidates.Where(c => c.Status == "Rejected").ToList()
            };

            // Lấy danh sách tin tuyển dụng cho filter
            var jobs = await _recruitmentService.GetJobPostingsAsync(new JobPostingFilterDto { PageSize = 100 });
            ViewBag.JobPostings = jobs.Items;
            ViewBag.SelectedJobId = jobPostingId;
            ViewBag.Keyword = keyword;
            ViewBag.MinScore = minScore;

            var dropdowns = await _employeeService.GetDropdownDataAsync();
            ViewBag.Employees = dropdowns.Employees;

            return View(board);
        }

        [HttpPost("CapNhatTrangThai")]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> CapNhatTrangThai([FromBody] UpdateCandidateStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var result = await _recruitmentService.UpdateCandidateStatusAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Json(new { success = true, message = "Đã cập nhật trạng thái ứng viên." });
        }

        [HttpGet("ChiTietUngVien/{id:int}")]
        [KiemTraQuyen("hrm.recruitment.view,hrm.recruitment.manage")]
        public async Task<IActionResult> ChiTietUngVien(int id)
        {
            var candidate = await _recruitmentService.GetCandidateByIdAsync(id);
            if (candidate == null) return NotFound();

            var schedules = await _recruitmentService.GetInterviewSchedulesAsync(id, null);
            ViewBag.Schedules = schedules;

            return PartialView("_CandidateDrawerPartial", candidate);
        }

        #endregion

        #region 3. Lịch Phỏng Vấn (Interview Schedules)

        [HttpGet("LichPhongVan")]
        [KiemTraQuyen("hrm.recruitment.view,hrm.recruitment.manage")]
        public async Task<IActionResult> LichPhongVan(int? candidateId, int? interviewerUserId)
        {
            var list = await _recruitmentService.GetInterviewSchedulesAsync(candidateId, interviewerUserId);
            var dropdowns = await _employeeService.GetDropdownDataAsync();
            ViewBag.Employees = dropdowns.Employees;

            return View(list);
        }

        [HttpPost("DatLichPhongVan")]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> DatLichPhongVan([FromBody] InterviewScheduleCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu đặt lịch không hợp lệ." });
            }

            var result = await _recruitmentService.ScheduleInterviewAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Json(new { success = true, message = "Đã lên lịch phỏng vấn và gửi thông báo tới người phỏng vấn.", scheduleId = result.Value });
        }

        [HttpPost("DanhGiaPhongVan")]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> DanhGiaPhongVan([FromBody] InterviewEvaluationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu đánh giá không hợp lệ." });
            }

            var result = await _recruitmentService.EvaluateInterviewAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Json(new { success = true, message = "Đã lưu kết quả đánh giá phỏng vấn." });
        }

        #endregion

        #region 4. Golden Path: 1-Click Chuyển Ứng Viên Thành Nhân Viên

        [HttpGet("TiepNhanNhanSu/{candidateId:int}")]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> TiepNhanNhanSu(int candidateId)
        {
            var candidate = await _recruitmentService.GetCandidateByIdAsync(candidateId);
            if (candidate == null) return NotFound();

            var job = await _recruitmentService.GetJobPostingByIdAsync(candidate.JobPostingId);

            var dropdowns = await _employeeService.GetDropdownDataAsync(job?.DepartmentId, job?.PositionId);
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;
            ViewBag.Employees = dropdowns.Employees;

            var model = new ConvertCandidateToEmployeeDto
            {
                CandidateId = candidate.Id,
                FullName = candidate.FullName,
                Email = candidate.Email ?? string.Empty,
                Phone = candidate.Phone ?? string.Empty,
                DepartmentId = job?.DepartmentId ?? 0,
                PositionId = job?.PositionId ?? 0,
                BranchId = job?.BranchId,
                HireDate = DateTime.Today,
                BaseSalary = 10000000,
                ProbationMonths = 2
            };

            ViewBag.Candidate = candidate;
            return View(model);
        }

        [HttpPost("TiepNhanNhanSu/{candidateId:int}")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> TiepNhanNhanSu(int candidateId, ConvertCandidateToEmployeeDto model)
        {
            if (candidateId != model.CandidateId) return BadRequest();

            if (!ModelState.IsValid)
            {
                var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
                ViewBag.Departments = dropdowns.Departments;
                ViewBag.Positions = dropdowns.Positions;
                ViewBag.Employees = dropdowns.Employees;
                return View(model);
            }

            var result = await _recruitmentService.ConvertToEmployeeAsync(model, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Lỗi tiếp nhận nhân sự.");
                var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
                ViewBag.Departments = dropdowns.Departments;
                ViewBag.Positions = dropdowns.Positions;
                ViewBag.Employees = dropdowns.Employees;
                return View(model);
            }

            TempData["SuccessMessage"] = $"Tiếp nhận nhân sự thành công! Hệ thống đã tạo hồ sơ nhân viên mới, hợp đồng thử việc và kích hoạt quy trình Onboarding.";
            return RedirectToAction(nameof(Onboarding), new { employeeId = result.Value });
        }

        #endregion

        #region 5. Quy Trình Hội Nhập & Thôi Việc (Onboarding / Offboarding)

        [HttpGet("Onboarding")]
        [KiemTraQuyen("hrm.recruitment.manage,hrm.recruitment.view,hrm.employee.view")]
        public async Task<IActionResult> Onboarding(string? type, string? status, int? employeeId)
        {
            var checklists = await _onboardingService.GetChecklistsAsync(type, status, employeeId);
            ViewBag.TypeFilter = type;
            ViewBag.StatusFilter = status;
            ViewBag.EmployeeIdFilter = employeeId;

            return View(checklists);
        }

        [HttpPost("CapNhatTienDoTask")]
        [KiemTraQuyen("hrm.recruitment.manage,hrm.employee.view")]
        public async Task<IActionResult> CapNhatTienDoTask([FromBody] UpdateOnboardingTaskDto dto)
        {
            var result = await _onboardingService.UpdateTaskProgressAsync(dto.TaskId, dto.IsCompleted, dto.Note, CurrentUserId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Json(new { success = true, message = "Đã cập nhật tiến độ công việc." });
        }

        [HttpPost("TaoMoiChecklist")]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.recruitment.manage")]
        public async Task<IActionResult> TaoMoiChecklist(CreateChecklistDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu tạo quy trình không hợp lệ.";
                return RedirectToAction(nameof(Onboarding));
            }

            var result = await _onboardingService.CreateChecklistAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã khởi tạo quy trình mới thành công.";
            }

            return RedirectToAction(nameof(Onboarding), new { type = dto.Type });
        }

        #endregion
    }
}
