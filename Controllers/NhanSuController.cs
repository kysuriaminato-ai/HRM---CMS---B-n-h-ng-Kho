using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using System.Security.Claims;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class NhanSuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;

        public NhanSuController(
            AppDbContext context,
            IEmployeeService employeeService,
            IDepartmentService departmentService)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
        }

        private int? CurrentUserId => int.TryParse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int uid) ? uid
            : HttpContext.Session.GetInt32("UserId");

        // ═══════════════════════════════════════════════════════════
        // DANH SÁCH NHÂN VIÊN (có phân trang, lọc, sắp xếp)
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

            // Thống kê nhanh
            ViewBag.TotalActive = await _context.Employees.CountAsync(e => e.Status == "Active");
            ViewBag.TotalProbation = await _context.Employees.CountAsync(e => e.Status == "Probation");
            ViewBag.TotalOnLeave = await _context.Employees.CountAsync(e => e.Status == "OnLeave");
            ViewBag.TotalResigned = await _context.Employees.CountAsync(e => e.Status == "Resigned");

            // Giữ lại filter
            ViewBag.Filter = filter;

            await LoadDropdowns(departmentId, positionId);
            return View(pagedList);
        }

        // ═══════════════════════════════════════════════════════════
        // CHI TIẾT NHÂN VIÊN (trang tab: Thông tin, Hợp đồng, Lịch sử, Tài liệu)
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
            await LoadDropdowns();
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
            // Kiểm tra mật khẩu khi cấp tài khoản
            if (model.CreateUserAccount)
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

            await LoadDropdowns(model.DepartmentId, model.PositionId);
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

            var userAcc = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);

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
                HasUserAccount = (userAcc != null),
                Username = userAcc?.Username
            };

            await LoadDropdowns(emp.DepartmentId, emp.PositionId);
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

            await LoadDropdowns(model.DepartmentId, model.PositionId);
            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // UPLOAD AVATAR (AJAX)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> UploadAvatar(int employeeId, IFormFile avatarFile)
        {
            if (avatarFile == null || avatarFile.Length == 0)
                return Json(new { success = false, error = "Không có file." });

            // Kiểm tra loại file
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(avatarFile.FileName).ToLower();
            if (!allowedExts.Contains(ext))
                return Json(new { success = false, error = "Chỉ chấp nhận file ảnh JPG, PNG hoặc WebP." });

            // Giới hạn 2MB
            if (avatarFile.Length > 2 * 1024 * 1024)
                return Json(new { success = false, error = "File ảnh không được vượt quá 2MB." });

            using var stream = avatarFile.OpenReadStream();
            var url = await _employeeService.SaveAvatarAsync(employeeId, stream, avatarFile.FileName);

            if (url == null) return Json(new { success = false, error = "Không tìm thấy nhân viên." });
            return Json(new { success = true, url });
        }

        // ═══════════════════════════════════════════════════════════
        // QUẢN LÝ NGƯỜI THÂN (AJAX)
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> AddRelative(EmployeeRelative model)
        {
            if (ModelState.IsValid)
            {
                await _employeeService.AddRelativeAsync(model);
                TempData["SuccessMessage"] = "Thêm người thân thành công!";
            }
            return RedirectToAction(nameof(Details), new { id = model.EmployeeId, tab = "info" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> RemoveRelative(int id, int employeeId)
        {
            await _employeeService.RemoveRelativeAsync(id);
            TempData["SuccessMessage"] = "Đã xóa người thân.";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "info" });
        }

        // ═══════════════════════════════════════════════════════════
        // QUẢN LÝ KỸ NĂNG
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
            return RedirectToAction(nameof(Details), new { id = model.EmployeeId, tab = "info" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> RemoveSkill(int id, int employeeId)
        {
            await _employeeService.RemoveSkillAsync(id);
            TempData["SuccessMessage"] = "Đã xóa kỹ năng.";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "info" });
        }

        // ═══════════════════════════════════════════════════════════
        // QUẢN LÝ CHỨNG CHỈ
        // ═══════════════════════════════════════════════════════════
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
            return RedirectToAction(nameof(Details), new { id = model.EmployeeId, tab = "info" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> RemoveCertificate(int id, int employeeId)
        {
            await _employeeService.RemoveCertificateAsync(id);
            TempData["SuccessMessage"] = "Đã xóa chứng chỉ.";
            return RedirectToAction(nameof(Details), new { id = employeeId, tab = "info" });
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

            // Giới hạn 10MB
            if (documentFile.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File không được vượt quá 10MB.";
                return RedirectToAction(nameof(Details), new { id = employeeId, tab = "documents" });
            }

            // Lưu file
            var uploadsDir = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents", employeeId.ToString());
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
        // CẤP TÀI KHOẢN NHANH
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> CapTaiKhoanNhanh(int employeeId, string rawPassword)
        {
            var emp = await _context.Employees.FindAsync(employeeId);
            if (emp == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.EmployeeId == employeeId))
            {
                TempData["ErrorMessage"] = "Nhân viên này đã có tài khoản người dùng!";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }

            string? loiMatKhau = MatKhauHelper.KiemTraDoManh(rawPassword);
            if (loiMatKhau != null)
            {
                TempData["ErrorMessage"] = loiMatKhau;
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }

            string username = emp.EmployeeCode.ToLower();
            string salt = MatKhauHelper.GenerateSalt();

            var user = new User
            {
                EmployeeId = emp.Id,
                Username = username,
                PasswordHash = MatKhauHelper.HashPassword(rawPassword, salt),
                PasswordSalt = salt,
                IsActive = true,
                CreatedAt = VietnamTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cấp tài khoản '{username}' thành công!";
            return RedirectToAction(nameof(Details), new { id = employeeId });
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
        // BÁO CÁO (giữ lại action cũ)
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public IActionResult BaoCao()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER: Load dropdown
        // ═══════════════════════════════════════════════════════════
        private async Task LoadDropdowns(int? selectedDept = null, int? selectedPos = null)
        {
            ViewBag.Departments = new SelectList(
                await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(),
                "Id", "Name", selectedDept);
            ViewBag.Positions = new SelectList(
                await _context.Positions.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(),
                "Id", "Name", selectedPos);
        }
    }
}
