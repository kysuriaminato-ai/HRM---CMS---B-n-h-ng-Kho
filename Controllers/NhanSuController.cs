using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class NhanSuController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IPeopleAnalyticsService _analyticsService;
        private readonly IDepartmentService _departmentService;
        private readonly ICurrentUserService _currentUser;

        public NhanSuController(
            IEmployeeService employeeService,
            IPeopleAnalyticsService analyticsService,
            IDepartmentService departmentService,
            ICurrentUserService currentUser)
        {
            _employeeService = employeeService;
            _analyticsService = analyticsService;
            _departmentService = departmentService;
            _currentUser = currentUser;
        }

        private int? CurrentUserId => _currentUser.UserId;

        // ═══════════════════════════════════════════════════════════
        // DANH SÁCH NHÂN VIÊN (phân trang, lọc, sắp xếp, Clean Architecture)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index(
            string? keyword, int? departmentId, int? positionId, string? status,
            string? sortBy, bool sortDesc = true, int page = 1, int pageSize = 20)
        {
            var filter = new EmployeeFilterDto
            {
                Keyword = keyword,
                DepartmentId = departmentId,
                PositionId = positionId,
                Status = status,
                SortBy = sortBy ?? "HireDate",
                SortDesc = sortDesc,
                Page = page,
                PageSize = pageSize
            };

            var pagedList = await _employeeService.GetPagedAsync(filter);
            var counts = await _employeeService.GetStatusCountsAsync();

            ViewBag.TotalActive = counts.TotalActive;
            ViewBag.TotalProbation = counts.TotalProbation;
            ViewBag.TotalOnLeave = counts.TotalOnLeave;
            ViewBag.TotalResigned = counts.TotalResigned;
            ViewBag.Filter = filter;

            var dropdowns = await _employeeService.GetDropdownDataAsync(departmentId, positionId);
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;

            return View(pagedList);
        }

        // ═══════════════════════════════════════════════════════════
        // CHI TIẾT NHÂN VIÊN (HỒ SƠ 360°)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Details(int id, string? tab)
        {
            var dto = await _employeeService.GetDetailAsync(id);
            if (dto == null) return NotFound();

            ViewBag.ActiveTab = tab ?? "info";
            return View(dto);
        }

        // ═══════════════════════════════════════════════════════════
        // TẠO MỚI
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create()
        {
            var dropdowns = await _employeeService.GetDropdownDataAsync();
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;

            return View(new EmployeeCreateEditViewModel
            {
                EmployeeCode = $"EMP{VietnamTime.Now:yyMMddHHmm}",
                HireDate = VietnamTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create(EmployeeCreateEditViewModel model)
        {
            if (model.CreateUserAccount && !string.IsNullOrWhiteSpace(model.DefaultPassword))
            {
                string? loiMatKhau = MatKhauHelper.KiemTraDoManh(model.DefaultPassword);
                if (loiMatKhau != null)
                    ModelState.AddModelError(nameof(model.DefaultPassword), loiMatKhau);
            }

            if (ModelState.IsValid)
            {
                var (success, error, empId) = await _employeeService.CreateAsync(model, CurrentUserId);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Thêm mới nhân sự {model.FullName} thành công!";
                    return RedirectToAction(nameof(Details), new { id = empId });
                }
                ModelState.AddModelError("", error ?? "Lỗi không xác định.");
            }

            var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;
            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // CHỈNH SỬA
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Edit(int id)
        {
            var emp = await _employeeService.GetByIdAsync(id);
            if (emp == null) return NotFound();

            var detailDto = await _employeeService.GetDetailAsync(id);

            var model = new EmployeeCreateEditViewModel
            {
                Id = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                DateOfBirth = emp.DateOfBirth,
                Gender = emp.Gender,
                Email = emp.Email,
                Phone = emp.Phone,
                IdentityCard = emp.IdentityCard,
                DepartmentId = emp.DepartmentId,
                PositionId = emp.PositionId,
                Status = emp.Status,
                HireDate = emp.HireDate,
                AvatarUrl = emp.AvatarUrl,
                EmergencyContactName = emp.EmployeeDetail?.EmergencyContactName,
                EmergencyContactPhone = emp.EmployeeDetail?.EmergencyContactPhone,
                BankAccountNumber = emp.EmployeeDetail?.BankAccountNumber,
                BankName = emp.EmployeeDetail?.BankName,
                BankBranch = emp.EmployeeDetail?.BankBranch,
                CurrentAddress = emp.EmployeeDetail?.CurrentAddress,
                PermanentAddress = emp.EmployeeDetail?.PermanentAddress,
                TaxIdentificationNumber = emp.EmployeeDetail?.TaxIdentificationNumber,
                SocialInsuranceNumber = emp.EmployeeDetail?.SocialInsuranceNumber,
                EducationLevel = emp.EmployeeDetail?.EducationLevel,
                HasUserAccount = (detailDto?.UserAccount != null),
                Username = detailDto?.UserAccount?.Username
            };

            var dropdowns = await _employeeService.GetDropdownDataAsync(emp.DepartmentId, emp.PositionId);
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Edit(int id, EmployeeCreateEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var (success, error) = await _employeeService.UpdateAsync(id, model, CurrentUserId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật hồ sơ nhân sự thành công!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                ModelState.AddModelError("", error ?? "Lỗi không xác định.");
            }

            var dropdowns = await _employeeService.GetDropdownDataAsync(model.DepartmentId, model.PositionId);
            ViewBag.Departments = dropdowns.Departments;
            ViewBag.Positions = dropdowns.Positions;
            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // CẤP TÀI KHOẢN AN TOÀN
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> CapTaiKhoanNhanh(int employeeId)
        {
            bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                       || Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            var (success, error, username, tempPassword) = await _employeeService.ProvisionUserAccountAsync(
                employeeId, CurrentUserId, _currentUser.IpAddress);

            if (!success)
            {
                if (isAjax) return BadRequest(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";

            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    username,
                    temporaryPassword = tempPassword,
                    mustChangePasswordOnFirstLogin = true,
                    message = "Đã sinh mật khẩu tạm thời an toàn. Mật khẩu này chỉ hiển thị duy nhất 1 lần."
                });
            }

            TempData["SuccessMessage"] = $"Đã cấp tài khoản '{username}' thành công (bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên).";
            return RedirectToAction(nameof(Details), new { id = employeeId });
        }

        // ═══════════════════════════════════════════════════════════
        // IMPORT / EXPORT EXCEL
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> ExportExcel(string? keyword, int? departmentId, int? positionId, string? status)
        {
            var filter = new EmployeeFilterDto
            {
                Keyword = keyword,
                DepartmentId = departmentId,
                PositionId = positionId,
                Status = status,
                Page = 1,
                PageSize = 100000 // Xuất toàn bộ dữ liệu lọc
            };

            var bytes = await _employeeService.ExportToExcelAsync(filter);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DanhSachNhanSu_{VietnamTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public IActionResult DownloadImportTemplate()
        {
            var bytes = _employeeService.GenerateImportTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Import_NhanSu.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file Excel để nhập dữ liệu.";
                return RedirectToAction(nameof(Index));
            }

            if (!excelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ chấp nhận file định dạng Excel .xlsx.";
                return RedirectToAction(nameof(Index));
            }

            using var stream = excelFile.OpenReadStream();
            var importResult = await _employeeService.ImportFromExcelAsync(stream, CurrentUserId);

            if (importResult.Success)
            {
                TempData["SuccessMessage"] = $"Nhập thành công {importResult.ImportedCount}/{importResult.TotalRows} nhân sự từ file Excel!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Nhập được {importResult.ImportedCount}/{importResult.TotalRows} nhân sự. Có {importResult.ErrorCount} lỗi cần kiểm tra.";
                TempData["ImportErrors"] = System.Text.Json.JsonSerializer.Serialize(importResult.Errors.Take(10));
            }

            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // CẢNH BÁO NHÂN SỰ THÔNG MINH
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> CanhBao()
        {
            var alerts = await _employeeService.GetSmartAlertsAsync();
            return View(alerts);
        }

        // ═══════════════════════════════════════════════════════════
        // IN SƠ YẾU LÝ LỊCH / HỒ SƠ NHÂN SỰ
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> InSoYeuLyLich(int id)
        {
            var model = await _employeeService.GetPrintableResumeAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // UPLOAD AVATAR
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> UploadAvatar(int employeeId, IFormFile avatarFile)
        {
            if (avatarFile == null || avatarFile.Length == 0)
                return Json(new { success = false, error = "Không có file." });

            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(avatarFile.FileName).ToLower();
            if (!allowedExts.Contains(ext))
                return Json(new { success = false, error = "Chỉ chấp nhận file ảnh JPG, PNG hoặc WebP." });

            if (avatarFile.Length > 2 * 1024 * 1024)
                return Json(new { success = false, error = "File ảnh không được vượt quá 2MB." });

            using var stream = avatarFile.OpenReadStream();
            var url = await _employeeService.SaveAvatarAsync(employeeId, stream, avatarFile.FileName);

            if (url == null) return Json(new { success = false, error = "Không tìm thấy nhân viên." });
            return Json(new { success = true, url });
        }

        // ═══════════════════════════════════════════════════════════
        // QUẢN LÝ NGƯỜI THÂN
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> AddRelative(EmployeeRelative model)
        {
            if (ModelState.IsValid)
            {
                await _employeeService.AddRelativeAsync(model);
                TempData["SuccessMessage"] = "Thêm thông tin người thân thành công!";
            }
            return RedirectToAction(nameof(Details), new { id = model.EmployeeId, tab = "dependants" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> RemoveRelative(int id, int employeeId)
        {
            await _employeeService.RemoveRelativeAsync(id);
            TempData["SuccessMessage"] = "Đã xóa người thân.";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "dependants" });
        }

        // ═══════════════════════════════════════════════════════════
        // QUẢN LÝ KỸ NĂNG & CHỨNG CHỈ
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> AddSkill(EmployeeSkill model)
        {
            if (ModelState.IsValid)
            {
                await _employeeService.AddSkillAsync(model);
                TempData["SuccessMessage"] = "Thêm kỹ năng thành công!";
            }
            return RedirectToAction(nameof(Details), new { id = model.EmployeeId, tab = "skills" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> RemoveSkill(int id, int employeeId)
        {
            await _employeeService.RemoveSkillAsync(id);
            TempData["SuccessMessage"] = "Đã xóa kỹ năng.";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "skills" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> AddCertificate(EmployeeCertificate model)
        {
            if (ModelState.IsValid)
            {
                await _employeeService.AddCertificateAsync(model);
                TempData["SuccessMessage"] = "Thêm chứng chỉ thành công!";
            }
            return RedirectToAction(nameof(Details), new { id = model.EmployeeId, tab = "skills" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> RemoveCertificate(int id, int employeeId)
        {
            await _employeeService.RemoveCertificateAsync(id);
            TempData["SuccessMessage"] = "Đã xóa chứng chỉ.";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "skills" });
        }

        // ═══════════════════════════════════════════════════════════
        // QUẢN LÝ TÀI LIỆU ĐÍNH KÈM
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> UploadDocument(int employeeId, string documentName, string documentType, IFormFile documentFile)
        {
            if (documentFile == null || documentFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file.";
                return RedirectToAction(nameof(Details), new { id = employeeId, tab = "documents" });
            }

            if (documentFile.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File không được vượt quá 10MB.";
                return RedirectToAction(nameof(Details), new { id = employeeId, tab = "documents" });
            }

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents", employeeId.ToString());
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(documentFile.FileName);
            var safeName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, safeName);

            using (var fs = new FileStream(filePath, FileMode.Create))
                await documentFile.CopyToAsync(fs);

            var doc = new EmployeeDocument
            {
                EmployeeId = employeeId,
                DocumentName = documentName,
                DocumentType = documentType,
                FileUrl = $"/uploads/documents/{employeeId}/{safeName}",
                FileSize = documentFile.Length,
                ContentType = documentFile.ContentType,
                UploadedBy = CurrentUserId
            };

            await _employeeService.AddDocumentAsync(doc);
            TempData["SuccessMessage"] = $"Upload tài liệu '{documentName}' thành công!";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "documents" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> RemoveDocument(int id, int employeeId)
        {
            await _employeeService.RemoveDocumentAsync(id);
            TempData["SuccessMessage"] = "Đã xóa tài liệu.";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "documents" });
        }

        // ═══════════════════════════════════════════════════════════
        // DASHBOARD HR
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Dashboard()
        {
            var dashboard = await _employeeService.GetDashboardAsync();
            return View(dashboard);
        }

        // ═══════════════════════════════════════════════════════════
        // EXECUTIVE PEOPLE ANALYTICS & BÁO CÁO ĐIỀU HÀNH
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> BaoCao(int? year, int? quarter, int? branchId, int? departmentId)
        {
            var filter = new PeopleAnalyticsFilterDto
            {
                Year = year ?? VietnamTime.Today.Year,
                Quarter = quarter,
                BranchId = branchId,
                DepartmentId = departmentId
            };

            var analytics = await _analyticsService.GetExecutiveAnalyticsAsync(filter);
            ViewBag.Departments = await _departmentService.GetFlatListAsync();
            ViewBag.Branches = await _departmentService.GetBranchesAsync();

            return View(analytics);
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> XuatBaoCaoExcel(int? year, int? quarter, int? branchId, int? departmentId)
        {
            var filter = new PeopleAnalyticsFilterDto
            {
                Year = year ?? VietnamTime.Today.Year,
                Quarter = quarter,
                BranchId = branchId,
                DepartmentId = departmentId
            };

            var analytics = await _analyticsService.GetExecutiveAnalyticsAsync(filter);
            var fileBytes = _analyticsService.ExportExecutiveReportExcel(analytics);
            string fileName = $"Executive_People_Analytics_{filter.Year}_{(filter.Quarter.HasValue ? "Q" + filter.Quarter.Value : "All")}_{VietnamTime.Now:yyyyMMddHHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
