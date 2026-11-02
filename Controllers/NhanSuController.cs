using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class NhanSuController : Controller
    {
        private readonly AppDbContext _context;

        public NhanSuController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index(string? keyword, int? departmentId, int? positionId, string? status)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(e => e.FullName.ToLower().Contains(keyword) ||
                                         e.EmployeeCode.ToLower().Contains(keyword) ||
                                         e.Email.ToLower().Contains(keyword) ||
                                         (e.Phone != null && e.Phone.Contains(keyword)));
            }

            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            if (positionId.HasValue && positionId.Value > 0)
                query = query.Where(e => e.PositionId == positionId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(e => e.Status == status);

            var employees = await query.OrderByDescending(e => e.HireDate).ToListAsync();

            var vm = new EmployeeListViewModel
            {
                Employees = employees,
                Keyword = keyword,
                DepartmentId = departmentId,
                PositionId = positionId,
                Status = status,
                TotalActive = await _context.Employees.CountAsync(e => e.Status == "Active"),
                TotalOnLeave = await _context.Employees.CountAsync(e => e.Status == "OnLeave"),
                TotalResigned = await _context.Employees.CountAsync(e => e.Status == "Resigned")
            };

            await LoadDropdowns();
            return View(vm);
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Details(int id)
        {
            var emp = await _context.Employees
                .Include(e => e.Department)
                    .ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .Include(e => e.Contracts.OrderByDescending(c => c.StartDate))
                .FirstOrDefaultAsync(e => e.Id == id);

            if (emp == null) return NotFound();

            var userAcc = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
            ViewBag.UserAccount = userAcc;

            // Lấy 7 ngày chấm công gần nhất
            ViewBag.RecentAttendance = await _context.AttendanceDailies
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == id)
                .OrderByDescending(a => a.WorkDate)
                .Take(7)
                .ToListAsync();

            return View(emp);
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new EmployeeCreateEditViewModel
            {
                EmployeeCode = $"EMP{DateTime.Now:yyMMddHHmm}",
                HireDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create(EmployeeCreateEditViewModel model)
        {
            // Kiểm tra trùng lặp mã nhân viên hoặc Email
            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode))
                ModelState.AddModelError("EmployeeCode", "Mã nhân viên này đã tồn tại.");

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
                ModelState.AddModelError("Email", "Email công ty này đã được sử dụng.");

            // Không dùng mật khẩu mặc định yếu: khi cấp tài khoản, mật khẩu phải đủ mạnh
            if (model.CreateUserAccount)
            {
                string? loiMatKhau = MatKhauHelper.KiemTraDoManh(model.DefaultPassword);
                if (loiMatKhau != null)
                    ModelState.AddModelError(nameof(model.DefaultPassword), loiMatKhau);
            }

            if (ModelState.IsValid)
            {
                var emp = new Employee
                {
                    EmployeeCode = model.EmployeeCode.Trim().ToUpper(),
                    FullName = model.FullName.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Email = model.Email.Trim().ToLower(),
                    Phone = model.Phone,
                    IdentityCard = model.IdentityCard,
                    DepartmentId = model.DepartmentId,
                    PositionId = model.PositionId,
                    Status = model.Status,
                    HireDate = model.HireDate,
                    AvatarUrl = model.AvatarUrl,
                    CreatedAt = DateTime.Now
                };

                _context.Employees.Add(emp);
                await _context.SaveChangesAsync();

                // Tạo kèm EmployeeDetails
                var detail = new EmployeeDetail
                {
                    EmployeeId = emp.Id,
                    EmergencyContactName = model.EmergencyContactName,
                    EmergencyContactPhone = model.EmergencyContactPhone,
                    BankAccountNumber = model.BankAccountNumber,
                    BankName = model.BankName,
                    BankBranch = model.BankBranch,
                    CurrentAddress = model.CurrentAddress,
                    PermanentAddress = model.PermanentAddress,
                    TaxIdentificationNumber = model.TaxIdentificationNumber,
                    SocialInsuranceNumber = model.SocialInsuranceNumber,
                    EducationLevel = model.EducationLevel
                };
                _context.EmployeeDetails.Add(detail);

                // Cấp tài khoản đăng nhập nếu có yêu cầu
                if (model.CreateUserAccount)
                {
                    string username = string.IsNullOrWhiteSpace(model.Username)
                        ? emp.EmployeeCode.ToLower()
                        : model.Username.Trim().ToLower();

                    if (!await _context.Users.AnyAsync(u => u.Username == username))
                    {
                        string salt = MatKhauHelper.GenerateSalt();
                        string rawPass = model.DefaultPassword; // đã kiểm tra độ mạnh ở trên
                        string passHash = MatKhauHelper.HashPassword(rawPass, salt);

                        var newUser = new User
                        {
                            EmployeeId = emp.Id,
                            Username = username,
                            PasswordHash = passHash,
                            PasswordSalt = salt,
                            IsActive = true,
                            IsSuperAdmin = false,
                            CreatedAt = DateTime.Now
                        };
                        _context.Users.Add(newUser);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thêm mới nhân sự {emp.FullName} thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model.DepartmentId, model.PositionId);
            return View(model);
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Edit(int id)
        {
            var emp = await _context.Employees
                .Include(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(e => e.Id == id);

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

            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode && e.Id != id))
                ModelState.AddModelError("EmployeeCode", "Mã nhân viên đã được dùng bởi người khác.");

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email && e.Id != id))
                ModelState.AddModelError("Email", "Email đã được dùng bởi người khác.");

            if (ModelState.IsValid)
            {
                var emp = await _context.Employees
                    .Include(e => e.EmployeeDetail)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (emp == null) return NotFound();

                emp.EmployeeCode = model.EmployeeCode.Trim().ToUpper();
                emp.FullName = model.FullName.Trim();
                emp.DateOfBirth = model.DateOfBirth;
                emp.Gender = model.Gender;
                emp.Email = model.Email.Trim().ToLower();
                emp.Phone = model.Phone;
                emp.IdentityCard = model.IdentityCard;
                emp.DepartmentId = model.DepartmentId;
                emp.PositionId = model.PositionId;
                emp.Status = model.Status;
                emp.HireDate = model.HireDate;
                emp.AvatarUrl = model.AvatarUrl;

                if (emp.EmployeeDetail == null)
                {
                    emp.EmployeeDetail = new EmployeeDetail { EmployeeId = id };
                    _context.EmployeeDetails.Add(emp.EmployeeDetail);
                }

                emp.EmployeeDetail.EmergencyContactName = model.EmergencyContactName;
                emp.EmployeeDetail.EmergencyContactPhone = model.EmergencyContactPhone;
                emp.EmployeeDetail.BankAccountNumber = model.BankAccountNumber;
                emp.EmployeeDetail.BankName = model.BankName;
                emp.EmployeeDetail.BankBranch = model.BankBranch;
                emp.EmployeeDetail.CurrentAddress = model.CurrentAddress;
                emp.EmployeeDetail.PermanentAddress = model.PermanentAddress;
                emp.EmployeeDetail.TaxIdentificationNumber = model.TaxIdentificationNumber;
                emp.EmployeeDetail.SocialInsuranceNumber = model.SocialInsuranceNumber;
                emp.EmployeeDetail.EducationLevel = model.EducationLevel;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật hồ sơ nhân sự thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model.DepartmentId, model.PositionId);
            return View(model);
        }

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

            string username = emp.EmployeeCode.ToLower();
            string salt = MatKhauHelper.GenerateSalt();
            string? loiMatKhau = MatKhauHelper.KiemTraDoManh(rawPassword);
            if (loiMatKhau != null)
            {
                TempData["ErrorMessage"] = loiMatKhau;
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }
            string pass = rawPassword;

            var user = new User
            {
                EmployeeId = emp.Id,
                Username = username,
                PasswordHash = MatKhauHelper.HashPassword(pass, salt),
                PasswordSalt = salt,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cấp tài khoản '{username}' với mật khẩu mặc định: {pass}";
            return RedirectToAction(nameof(Details), new { id = employeeId });
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> BaoCao()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .ToListAsync();

            var now = DateTime.Today;
            int total = employees.Count;
            int active = employees.Count(e => e.Status == "Active");
            int resigned = employees.Count(e => e.Status == "Resigned");

            decimal turnoverRate = (total > 0 && resigned > 0) ? Math.Round((decimal)resigned / total * 100m, 1) : 0m;
            int newHires = employees.Count(e => e.HireDate.Year == now.Year);

            // Cơ cấu độ tuổi
            int ageUnder25 = 0, age25To35 = 0, age36To45 = 0, ageOver45 = 0;
            foreach (var e in employees)
            {
                if (e.DateOfBirth.HasValue)
                {
                    int age = now.Year - e.DateOfBirth.Value.Year;
                    if (now < e.DateOfBirth.Value.AddYears(age)) age--;

                    if (age < 25) ageUnder25++;
                    else if (age <= 35) age25To35++;
                    else if (age <= 45) age36To45++;
                    else ageOver45++;
                }
                else
                {
                    age25To35++;
                }
            }

            // Cơ cấu thâm niên
            int senUnder1 = 0, sen1To3 = 0, sen3To5 = 0, senOver5 = 0;
            foreach (var e in employees)
            {
                var tenure = (now - e.HireDate).TotalDays / 365.25;
                if (tenure < 1) senUnder1++;
                else if (tenure <= 3) sen1To3++;
                else if (tenure <= 5) sen3To5++;
                else senOver5++;
            }

            // Cơ cấu giới tính
            int male = employees.Count(e => e.Gender == "Nam" || string.IsNullOrEmpty(e.Gender));
            int female = employees.Count(e => e.Gender == "Nữ");

            // Phân bổ phòng ban
            var deptDist = employees
                .GroupBy(e => e.Department?.Name ?? "Chưa phân bổ")
                .ToDictionary(g => g.Key, g => g.Count());

            var vm = new HrAnalyticsViewModel
            {
                TotalEmployees = total,
                ActiveEmployees = active,
                ResignedEmployees = resigned,
                TurnoverRate = turnoverRate,
                NewHiresThisYear = newHires,
                AgeUnder25 = ageUnder25,
                Age25To35 = age25To35,
                Age36To45 = age36To45,
                AgeOver45 = ageOver45,
                SeniorityUnder1Year = senUnder1,
                Seniority1To3Years = sen1To3,
                Seniority3To5Years = sen3To5,
                SeniorityOver5Years = senOver5,
                MaleCount = male,
                FemaleCount = female,
                DepartmentDistribution = deptDist
            };

            return View(vm);
        }

        private async Task LoadDropdowns(int? selectedDept = null, int? selectedPos = null)
        {
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", selectedDept);
            ViewBag.Positions = new SelectList(await _context.Positions.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", selectedPos);
        }
    }
}