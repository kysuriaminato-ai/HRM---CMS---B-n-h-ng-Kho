using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    /// <summary>Triển khai dịch vụ quản lý nhân viên — Phiên 5</summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EmployeeService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // === DANH SÁCH & TÌM KIẾM (Projection, tránh N+1) ===
        public async Task<PaginatedList<EmployeeListItemDto>> GetPagedAsync(EmployeeFilterDto filter)
        {
            var query = _context.Employees
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .AsNoTracking()
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(e =>
                    e.FullName.ToLower().Contains(kw) ||
                    e.EmployeeCode.ToLower().Contains(kw) ||
                    e.Email.ToLower().Contains(kw) ||
                    (e.Phone != null && e.Phone.Contains(kw)));
            }

            // Lọc theo phòng ban
            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                query = query.Where(e => e.DepartmentId == filter.DepartmentId);

            // Lọc theo chức vụ
            if (filter.PositionId.HasValue && filter.PositionId > 0)
                query = query.Where(e => e.PositionId == filter.PositionId);

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(e => e.Status == filter.Status);

            // Sắp xếp
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDesc ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
                "code" => filter.SortDesc ? query.OrderByDescending(e => e.EmployeeCode) : query.OrderBy(e => e.EmployeeCode),
                "department" => filter.SortDesc ? query.OrderByDescending(e => e.Department.Name) : query.OrderBy(e => e.Department.Name),
                "status" => filter.SortDesc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
                _ => filter.SortDesc ? query.OrderByDescending(e => e.HireDate) : query.OrderBy(e => e.HireDate)
            };

            // Projection sang DTO
            var projected = query.Select(e => new EmployeeListItemDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Gender = e.Gender,
                Email = e.Email,
                Phone = e.Phone,
                AvatarUrl = e.AvatarUrl,
                Status = e.Status,
                HireDate = e.HireDate,
                DepartmentName = e.Department.Name,
                PositionName = e.Position.Name,
                BranchName = e.Department.Branch.Name
            });

            return await PaginatedList<EmployeeListItemDto>.CreateAsync(
                projected, filter.Page, filter.PageSize);
        }

        // === CHI TIẾT ===
        public async Task<EmployeeDetailDto?> GetDetailAsync(int id)
        {
            var emp = await _context.Employees
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (emp == null) return null;

            var dto = new EmployeeDetailDto
            {
                Employee = emp,
                Detail = emp.EmployeeDetail,
                Contracts = await _context.Contracts
                    .Where(c => c.EmployeeId == id)
                    .OrderByDescending(c => c.StartDate)
                    .AsNoTracking().ToListAsync(),
                Relatives = await _context.EmployeeRelatives
                    .Where(r => r.EmployeeId == id)
                    .OrderBy(r => r.Relationship)
                    .AsNoTracking().ToListAsync(),
                Skills = await _context.EmployeeSkills
                    .Where(s => s.EmployeeId == id)
                    .OrderBy(s => s.SkillName)
                    .AsNoTracking().ToListAsync(),
                Certificates = await _context.EmployeeCertificates
                    .Where(c => c.EmployeeId == id)
                    .OrderByDescending(c => c.IssuedDate)
                    .AsNoTracking().ToListAsync(),
                Documents = await _context.EmployeeDocuments
                    .Where(d => d.EmployeeId == id)
                    .OrderByDescending(d => d.CreatedAt)
                    .AsNoTracking().ToListAsync(),
                ChangeHistories = await _context.EmployeeChangeHistories
                    .Include(h => h.ChangedByUser)
                    .Where(h => h.EmployeeId == id)
                    .OrderByDescending(h => h.EffectiveDate)
                    .AsNoTracking().ToListAsync(),
                RecentAttendance = await _context.AttendanceDailies
                    .Include(a => a.Shift)
                    .Where(a => a.EmployeeId == id)
                    .OrderByDescending(a => a.WorkDate)
                    .Take(7)
                    .AsNoTracking().ToListAsync(),
                UserAccount = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.EmployeeId == id)
            };

            dto.ActiveContract = dto.Contracts
                .FirstOrDefault(c => c.Status == "Active");

            return dto;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        // === TẠO MỚI ===
        public async Task<(bool Success, string? Error, int EmployeeId)> CreateAsync(
            EmployeeCreateEditViewModel model, int? currentUserId)
        {
            // Kiểm tra trùng lặp
            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode))
                return (false, "Mã nhân viên đã tồn tại.", 0);

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
                return (false, "Email đã được sử dụng.", 0);

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
                CreatedAt = VietnamTime.Now
            };

            _context.Employees.Add(emp);
            await _context.SaveChangesAsync();

            // Tạo EmployeeDetail
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

            // Cấp tài khoản nếu yêu cầu
            if (model.CreateUserAccount)
            {
                string username = string.IsNullOrWhiteSpace(model.Username)
                    ? emp.EmployeeCode.ToLower()
                    : model.Username.Trim().ToLower();

                if (!await _context.Users.AnyAsync(u => u.Username == username))
                {
                    string salt = MatKhauHelper.GenerateSalt();
                    var newUser = new User
                    {
                        EmployeeId = emp.Id,
                        Username = username,
                        PasswordHash = MatKhauHelper.HashPassword(model.DefaultPassword, salt),
                        PasswordSalt = salt,
                        IsActive = true,
                        CreatedAt = VietnamTime.Now
                    };
                    _context.Users.Add(newUser);
                }
            }

            await _context.SaveChangesAsync();
            return (true, null, emp.Id);
        }

        // === CẬP NHẬT ===
        public async Task<(bool Success, string? Error)> UpdateAsync(
            int id, EmployeeCreateEditViewModel model, int? currentUserId)
        {
            var emp = await _context.Employees
                .Include(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (emp == null) return (false, "Không tìm thấy nhân viên.");

            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode && e.Id != id))
                return (false, "Mã nhân viên đã được dùng bởi người khác.");

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email && e.Id != id))
                return (false, "Email đã được dùng bởi người khác.");

            // Ghi lịch sử thay đổi trước khi cập nhật
            await TrackChanges(emp, model, currentUserId);

            // Cập nhật thông tin cơ bản
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

            // Cập nhật EmployeeDetail
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
            return (true, null);
        }

        // === XÓA MỀM ===
        public async Task<(bool Success, string? Error)> SoftDeleteAsync(int id, int? currentUserId)
        {
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (emp == null) return (false, "Không tìm thấy nhân viên.");

            emp.Status = "Resigned";
            // Ghi lịch sử
            _context.EmployeeChangeHistories.Add(new EmployeeChangeHistory
            {
                EmployeeId = id,
                ChangeType = "Status",
                OldValue = emp.Status,
                NewValue = "Resigned",
                EffectiveDate = VietnamTime.Today,
                Reason = "Nghỉ việc",
                ChangedBy = currentUserId,
                CreatedAt = VietnamTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // === HỒ SƠ MỞ RỘNG ===
        public async Task<List<EmployeeRelative>> GetRelativesAsync(int employeeId)
            => await _context.EmployeeRelatives.Where(r => r.EmployeeId == employeeId)
                .OrderBy(r => r.Relationship).AsNoTracking().ToListAsync();

        public async Task AddRelativeAsync(EmployeeRelative relative)
        {
            relative.CreatedAt = VietnamTime.Now;
            _context.EmployeeRelatives.Add(relative);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRelativeAsync(int id)
        {
            var entity = await _context.EmployeeRelatives.FindAsync(id);
            if (entity != null) { _context.EmployeeRelatives.Remove(entity); await _context.SaveChangesAsync(); }
        }

        public async Task<List<EmployeeSkill>> GetSkillsAsync(int employeeId)
            => await _context.EmployeeSkills.Where(s => s.EmployeeId == employeeId)
                .OrderBy(s => s.SkillName).AsNoTracking().ToListAsync();

        public async Task AddSkillAsync(EmployeeSkill skill)
        {
            skill.CreatedAt = VietnamTime.Now;
            _context.EmployeeSkills.Add(skill);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveSkillAsync(int id)
        {
            var entity = await _context.EmployeeSkills.FindAsync(id);
            if (entity != null) { _context.EmployeeSkills.Remove(entity); await _context.SaveChangesAsync(); }
        }

        public async Task<List<EmployeeCertificate>> GetCertificatesAsync(int employeeId)
            => await _context.EmployeeCertificates.Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.IssuedDate).AsNoTracking().ToListAsync();

        public async Task AddCertificateAsync(EmployeeCertificate cert)
        {
            cert.CreatedAt = VietnamTime.Now;
            _context.EmployeeCertificates.Add(cert);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCertificateAsync(int id)
        {
            var entity = await _context.EmployeeCertificates.FindAsync(id);
            if (entity != null) { _context.EmployeeCertificates.Remove(entity); await _context.SaveChangesAsync(); }
        }

        public async Task<List<EmployeeDocument>> GetDocumentsAsync(int employeeId)
            => await _context.EmployeeDocuments.Where(d => d.EmployeeId == employeeId)
                .OrderByDescending(d => d.CreatedAt).AsNoTracking().ToListAsync();

        public async Task AddDocumentAsync(EmployeeDocument doc)
        {
            doc.CreatedAt = VietnamTime.Now;
            _context.EmployeeDocuments.Add(doc);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveDocumentAsync(int id)
        {
            var entity = await _context.EmployeeDocuments.FindAsync(id);
            if (entity != null)
            {
                // Xóa file vật lý
                if (!string.IsNullOrEmpty(entity.FileUrl))
                {
                    var path = Path.Combine(_env.WebRootPath, entity.FileUrl.TrimStart('/'));
                    if (File.Exists(path)) File.Delete(path);
                }
                _context.EmployeeDocuments.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        // === LỊCH SỬ ===
        public async Task<List<EmployeeChangeHistory>> GetChangeHistoryAsync(int employeeId)
            => await _context.EmployeeChangeHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.EmployeeId == employeeId)
                .OrderByDescending(h => h.EffectiveDate)
                .AsNoTracking().ToListAsync();

        // === DASHBOARD ===
        public async Task<HrDashboardDto> GetDashboardAsync()
        {
            var today = VietnamTime.Today;
            var allEmployees = await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking().ToListAsync();

            var warningDate = today.AddDays(30);
            var expiringContracts = await _context.Contracts
                .Include(c => c.Employee).ThenInclude(e => e.Department)
                .Where(c => c.Status == "Active" && c.EndDate != null
                    && c.EndDate >= today && c.EndDate <= warningDate)
                .OrderBy(c => c.EndDate)
                .AsNoTracking().ToListAsync();

            var recentHires = await _context.Employees
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .Where(e => e.Status == "Active")
                .OrderByDescending(e => e.HireDate)
                .Take(5)
                .Select(e => new EmployeeListItemDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    AvatarUrl = e.AvatarUrl,
                    HireDate = e.HireDate,
                    DepartmentName = e.Department.Name,
                    PositionName = e.Position.Name,
                    BranchName = e.Department.Branch.Name,
                    Status = e.Status
                })
                .AsNoTracking().ToListAsync();

            return new HrDashboardDto
            {
                TotalEmployees = allEmployees.Count,
                ActiveEmployees = allEmployees.Count(e => e.Status == "Active"),
                ProbationEmployees = allEmployees.Count(e => e.Status == "Probation"),
                OnLeaveEmployees = allEmployees.Count(e => e.Status == "OnLeave"),
                ResignedEmployees = allEmployees.Count(e => e.Status == "Resigned"),
                NewHiresThisMonth = allEmployees.Count(e => e.HireDate.Month == today.Month && e.HireDate.Year == today.Year),
                ExpiringContracts30Days = expiringContracts.Count,
                ByDepartment = allEmployees
                    .Where(e => e.Status == "Active")
                    .GroupBy(e => e.Department?.Name ?? "Chưa phân bổ")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByStatus = allEmployees
                    .GroupBy(e => e.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                MaleCount = allEmployees.Count(e => e.Gender == "Nam"),
                FemaleCount = allEmployees.Count(e => e.Gender == "Nữ"),
                RecentHires = recentHires,
                ExpiringContractList = expiringContracts.Select(c => new ExpiringContractDto
                {
                    ContractId = c.Id,
                    ContractNumber = c.ContractNumber,
                    EmployeeName = c.Employee.FullName,
                    EmployeeCode = c.Employee.EmployeeCode,
                    DepartmentName = c.Employee.Department?.Name ?? "",
                    EndDate = c.EndDate,
                    DaysLeft = c.EndDate.HasValue ? (int)(c.EndDate.Value - today).TotalDays : 0
                }).ToList()
            };
        }

        // === UPLOAD AVATAR ===
        public async Task<string?> SaveAvatarAsync(int employeeId, Stream fileStream, string fileName)
        {
            var emp = await _context.Employees.FindAsync(employeeId);
            if (emp == null) return null;

            // Tạo thư mục uploads/avatars
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsDir);

            // Đặt tên file duy nhất
            var ext = Path.GetExtension(fileName).ToLower();
            var safeName = $"avatar_{employeeId}_{VietnamTime.Now:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(uploadsDir, safeName);

            // Lưu file
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            // Xóa avatar cũ nếu có
            if (!string.IsNullOrEmpty(emp.AvatarUrl))
            {
                var oldPath = Path.Combine(_env.WebRootPath, emp.AvatarUrl.TrimStart('/'));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            // Cập nhật đường dẫn
            emp.AvatarUrl = $"/uploads/avatars/{safeName}";
            await _context.SaveChangesAsync();

            return emp.AvatarUrl;
        }

        // === GHI LỊCH SỬ THAY ĐỔI ===
        private async Task TrackChanges(Employee emp, EmployeeCreateEditViewModel model, int? currentUserId)
        {
            var today = VietnamTime.Today;
            var changes = new List<EmployeeChangeHistory>();

            // Phòng ban thay đổi
            if (emp.DepartmentId != model.DepartmentId)
            {
                var oldDept = await _context.Departments.FindAsync(emp.DepartmentId);
                var newDept = await _context.Departments.FindAsync(model.DepartmentId);
                changes.Add(new EmployeeChangeHistory
                {
                    EmployeeId = emp.Id,
                    ChangeType = "Department",
                    OldValue = oldDept?.Name ?? emp.DepartmentId.ToString(),
                    NewValue = newDept?.Name ?? model.DepartmentId.ToString(),
                    EffectiveDate = today,
                    ChangedBy = currentUserId,
                    CreatedAt = VietnamTime.Now
                });
            }

            // Chức vụ thay đổi
            if (emp.PositionId != model.PositionId)
            {
                var oldPos = await _context.Positions.FindAsync(emp.PositionId);
                var newPos = await _context.Positions.FindAsync(model.PositionId);
                changes.Add(new EmployeeChangeHistory
                {
                    EmployeeId = emp.Id,
                    ChangeType = "Position",
                    OldValue = oldPos?.Name ?? emp.PositionId.ToString(),
                    NewValue = newPos?.Name ?? model.PositionId.ToString(),
                    EffectiveDate = today,
                    ChangedBy = currentUserId,
                    CreatedAt = VietnamTime.Now
                });
            }

            // Trạng thái thay đổi
            if (emp.Status != model.Status)
            {
                changes.Add(new EmployeeChangeHistory
                {
                    EmployeeId = emp.Id,
                    ChangeType = "Status",
                    OldValue = emp.Status,
                    NewValue = model.Status,
                    EffectiveDate = today,
                    ChangedBy = currentUserId,
                    CreatedAt = VietnamTime.Now
                });
            }

            if (changes.Count > 0)
                _context.EmployeeChangeHistories.AddRange(changes);
        }
    }
}
