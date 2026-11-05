using ClosedXML.Excel;
using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDataScopeService? _dataScope;

        public EmployeeService(
            AppDbContext context,
            IWebHostEnvironment env,
            IDataScopeService? dataScope = null)
        {
            _context = context;
            _env = env;
            _dataScope = dataScope;
        }

        // === DANH SÁCH & TÌM KIẾM (Projection, tránh N+1, áp dụng IDataScopeService) ===
        public async Task<PaginatedList<EmployeeListItemDto>> GetPagedAsync(EmployeeFilterDto filter)
        {
            var baseQuery = _context.Employees.AsNoTracking();
            var query = (_dataScope != null ? _dataScope.ApplyEmployeeScope(baseQuery) : baseQuery)
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(e =>
                    e.FullName.Contains(kw) ||
                    e.EmployeeCode.Contains(kw) ||
                    e.Email.Contains(kw) ||
                    (e.Phone != null && e.Phone.Contains(kw)));
            }

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                query = query.Where(e => e.DepartmentId == filter.DepartmentId);

            if (filter.PositionId.HasValue && filter.PositionId > 0)
                query = query.Where(e => e.PositionId == filter.PositionId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(e => e.Status == filter.Status);

            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDesc ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
                "code" => filter.SortDesc ? query.OrderByDescending(e => e.EmployeeCode) : query.OrderBy(e => e.EmployeeCode),
                "department" => filter.SortDesc ? query.OrderByDescending(e => e.Department.Name) : query.OrderBy(e => e.Department.Name),
                "status" => filter.SortDesc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
                _ => filter.SortDesc ? query.OrderByDescending(e => e.HireDate) : query.OrderBy(e => e.HireDate)
            };

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

            return await PaginatedList<EmployeeListItemDto>.CreateAsync(projected, filter.Page, filter.PageSize);
        }

        // === CHI TIẾT HỒ SƠ 360° ===
        public async Task<EmployeeDetailDto?> GetDetailAsync(int id)
        {
            var baseQuery = _context.Employees.AsNoTracking();
            var scopedQuery = _dataScope != null ? _dataScope.ApplyEmployeeScope(baseQuery) : baseQuery;
            var emp = await scopedQuery
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
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
                Decisions = await _context.HrDecisions
                    .Include(d => d.OldDepartment)
                    .Include(d => d.NewDepartment)
                    .Include(d => d.OldPosition)
                    .Include(d => d.NewPosition)
                    .Where(d => d.EmployeeId == id)
                    .OrderByDescending(d => d.EffectiveDate)
                    .AsNoTracking().ToListAsync(),
                Assets = await _context.EmployeeAssetAssignments
                    .Include(asg => asg.Asset)
                    .Where(asg => asg.EmployeeId == id)
                    .OrderByDescending(asg => asg.AssignedDate)
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

            dto.ActiveContract = dto.Contracts.FirstOrDefault(c => c.Status == "Active");
            return dto;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<EmployeeStatusCountsDto> GetStatusCountsAsync()
        {
            var baseQuery = _context.Employees.AsNoTracking();
            var scopedEmps = _dataScope != null ? _dataScope.ApplyEmployeeScope(baseQuery) : baseQuery;

            return new EmployeeStatusCountsDto
            {
                TotalActive = await scopedEmps.CountAsync(e => e.Status == "Active"),
                TotalProbation = await scopedEmps.CountAsync(e => e.Status == "Probation"),
                TotalOnLeave = await scopedEmps.CountAsync(e => e.Status == "OnLeave"),
                TotalResigned = await scopedEmps.CountAsync(e => e.Status == "Resigned")
            };
        }

        public async Task<EmployeeDropdownDto> GetDropdownDataAsync(int? selectedDeptId = null, int? selectedPosId = null)
        {
            var depts = await _context.Departments.AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            var pos = await _context.Positions.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var emps = await _context.Employees.AsNoTracking()
                .Where(e => e.Status == "Active")
                .Include(e => e.Department)
                .OrderBy(e => e.FullName)
                .Select(e => new { e.Id, Name = $"{e.EmployeeCode} - {e.FullName} ({e.Department.Name})" })
                .ToListAsync();

            return new EmployeeDropdownDto
            {
                Departments = new SelectList(depts, "Id", "Name", selectedDeptId),
                Positions = new SelectList(pos, "Id", "Name", selectedPosId),
                Employees = new SelectList(emps, "Id", "Name")
            };
        }

        // === CRUD ===
        public async Task<(bool Success, string? Error, int EmployeeId)> CreateAsync(
            EmployeeCreateEditViewModel model, int? currentUserId)
        {
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
                CreatedBy = currentUserId?.ToString(),
                CreatedAt = VietnamTime.Now
            };

            _context.Employees.Add(emp);
            await _context.SaveChangesAsync();

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

            if (model.CreateUserAccount && !string.IsNullOrWhiteSpace(model.DefaultPassword))
            {
                string salt = MatKhauHelper.GenerateSalt();
                var user = new User
                {
                    EmployeeId = emp.Id,
                    Username = emp.EmployeeCode.ToLower(),
                    PasswordHash = MatKhauHelper.HashPassword(model.DefaultPassword, salt),
                    PasswordSalt = salt,
                    IsActive = true,
                    MustChangePasswordOnFirstLogin = true,
                    SecurityStamp = Guid.NewGuid(),
                    CreatedAt = VietnamTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var empRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "EMPLOYEE");
                if (empRole != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = empRole.Id,
                        AssignedAt = VietnamTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return (true, null, emp.Id);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(
            int id, EmployeeCreateEditViewModel model, int? currentUserId)
        {
            var emp = await _context.Employees
                .Include(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (emp == null) return (false, "Không tìm thấy nhân viên.");

            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode && e.Id != id))
                return (false, "Mã nhân viên đã được dùng.");

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email && e.Id != id))
                return (false, "Email đã được dùng.");

            await TrackChanges(emp, model, currentUserId);

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
            if (!string.IsNullOrWhiteSpace(model.AvatarUrl))
                emp.AvatarUrl = model.AvatarUrl;

            emp.UpdatedAt = VietnamTime.Now;
            emp.UpdatedBy = currentUserId?.ToString();

            if (emp.EmployeeDetail == null)
            {
                emp.EmployeeDetail = new EmployeeDetail { EmployeeId = emp.Id };
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

        public async Task<(bool Success, string? Error)> SoftDeleteAsync(int id, int? currentUserId)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return (false, "Không tìm thấy nhân viên.");

            emp.IsDeleted = true;
            emp.Status = "Resigned";
            emp.UpdatedAt = VietnamTime.Now;
            emp.UpdatedBy = currentUserId?.ToString();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
            if (user != null)
            {
                user.IsActive = false;
                user.SecurityStamp = Guid.NewGuid();
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error, string? Username, string? TempPassword)> ProvisionUserAccountAsync(
            int employeeId, int? currentUserId, string? clientIp)
        {
            var emp = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (emp == null)
                return (false, "Không tìm thấy nhân viên.", null, null);

            if (await _context.Users.AnyAsync(u => u.EmployeeId == employeeId))
                return (false, "Nhân viên này đã được cấp tài khoản người dùng.", null, null);

            string baseUsername = emp.EmployeeCode.Trim().ToLowerInvariant();
            string username = baseUsername;
            int suffix = 1;
            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}_{suffix++}";
            }

            string tempPassword = MatKhauHelper.GenerateTemporaryPassword(14);
            string salt = MatKhauHelper.GenerateSalt();

            var user = new User
            {
                EmployeeId = emp.Id,
                Username = username,
                PasswordHash = MatKhauHelper.HashPassword(tempPassword, salt),
                PasswordSalt = salt,
                IsActive = true,
                MustChangePasswordOnFirstLogin = true,
                SecurityStamp = Guid.NewGuid(),
                CreatedAt = VietnamTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "EMPLOYEE" && r.IsActive);
            if (employeeRole != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = employeeRole.Id,
                    AssignedAt = VietnamTime.Now
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUserId,
                Username = currentUserId?.ToString() ?? "System",
                Action = "PROVISION_TEMP_ACCOUNT",
                EntityName = "User",
                RecordId = user.Id.ToString(),
                NewValues = $"Cấp tài khoản nhanh '{username}' cho nhân viên {emp.EmployeeCode}",
                IpAddress = clientIp,
                CreatedAt = VietnamTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, null, username, tempPassword);
        }

        // === HỒ SƠ MỞ RỘNG ===
        public async Task<List<EmployeeRelative>> GetRelativesAsync(int employeeId)
            => await _context.EmployeeRelatives.Where(r => r.EmployeeId == employeeId).AsNoTracking().ToListAsync();

        public async Task AddRelativeAsync(EmployeeRelative relative)
        {
            relative.CreatedAt = VietnamTime.Now;
            _context.EmployeeRelatives.Add(relative);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRelativeAsync(int id)
        {
            var r = await _context.EmployeeRelatives.FindAsync(id);
            if (r != null)
            {
                _context.EmployeeRelatives.Remove(r);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<EmployeeSkill>> GetSkillsAsync(int employeeId)
            => await _context.EmployeeSkills.Where(s => s.EmployeeId == employeeId).AsNoTracking().ToListAsync();

        public async Task AddSkillAsync(EmployeeSkill skill)
        {
            skill.CreatedAt = VietnamTime.Now;
            _context.EmployeeSkills.Add(skill);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveSkillAsync(int id)
        {
            var s = await _context.EmployeeSkills.FindAsync(id);
            if (s != null)
            {
                _context.EmployeeSkills.Remove(s);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<EmployeeCertificate>> GetCertificatesAsync(int employeeId)
            => await _context.EmployeeCertificates.Where(c => c.EmployeeId == employeeId).AsNoTracking().ToListAsync();

        public async Task AddCertificateAsync(EmployeeCertificate cert)
        {
            cert.CreatedAt = VietnamTime.Now;
            _context.EmployeeCertificates.Add(cert);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCertificateAsync(int id)
        {
            var c = await _context.EmployeeCertificates.FindAsync(id);
            if (c != null)
            {
                _context.EmployeeCertificates.Remove(c);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<EmployeeDocument>> GetDocumentsAsync(int employeeId)
            => await _context.EmployeeDocuments.Where(d => d.EmployeeId == employeeId).AsNoTracking().ToListAsync();

        public async Task AddDocumentAsync(EmployeeDocument doc)
        {
            doc.CreatedAt = VietnamTime.Now;
            _context.EmployeeDocuments.Add(doc);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveDocumentAsync(int id)
        {
            var d = await _context.EmployeeDocuments.FindAsync(id);
            if (d != null)
            {
                _context.EmployeeDocuments.Remove(d);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<EmployeeChangeHistory>> GetChangeHistoryAsync(int employeeId)
            => await _context.EmployeeChangeHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.EmployeeId == employeeId)
                .OrderByDescending(h => h.EffectiveDate)
                .AsNoTracking().ToListAsync();

        // === DASHBOARD & CẢNH BÁO THÔNG MINH ===
        public async Task<HrDashboardDto> GetDashboardAsync()
        {
            var today = VietnamTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var in30Days = today.AddDays(30);

            var baseQuery = _context.Employees.AsNoTracking();
            var emps = _dataScope != null ? _dataScope.ApplyEmployeeScope(baseQuery) : baseQuery;

            var total = await emps.CountAsync();
            var active = await emps.CountAsync(e => e.Status == "Active");
            var probation = await emps.CountAsync(e => e.Status == "Probation");
            var onLeave = await emps.CountAsync(e => e.Status == "OnLeave");
            var resigned = await emps.CountAsync(e => e.Status == "Resigned");
            var newHires = await emps.CountAsync(e => e.HireDate >= startOfMonth);

            var byDept = await emps
                .GroupBy(e => e.Department.Name)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Dept, x => x.Count);

            var byStatus = await emps
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var male = await emps.CountAsync(e => e.Gender == "Nam");
            var female = await emps.CountAsync(e => e.Gender == "Nữ");

            var recentHires = await emps
                .OrderByDescending(e => e.HireDate)
                .Take(5)
                .Select(e => new EmployeeListItemDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.Department.Name,
                    PositionName = e.Position.Name,
                    HireDate = e.HireDate,
                    Status = e.Status,
                    AvatarUrl = e.AvatarUrl
                })
                .ToListAsync();

            var expiringContracts = await _context.Contracts
                .Include(c => c.Employee).ThenInclude(e => e.Department)
                .Where(c => c.Status == "Active" && c.EndDate.HasValue && c.EndDate.Value >= today && c.EndDate.Value <= in30Days)
                .OrderBy(c => c.EndDate)
                .Take(10)
                .Select(c => new ExpiringContractDto
                {
                    ContractId = c.Id,
                    ContractNumber = c.ContractNumber,
                    EmployeeName = c.Employee.FullName,
                    EmployeeCode = c.Employee.EmployeeCode,
                    DepartmentName = c.Employee.Department.Name,
                    EndDate = c.EndDate,
                    DaysLeft = (int)(c.EndDate!.Value.Date - today).TotalDays
                })
                .ToListAsync();

            return new HrDashboardDto
            {
                TotalEmployees = total,
                ActiveEmployees = active,
                ProbationEmployees = probation,
                OnLeaveEmployees = onLeave,
                ResignedEmployees = resigned,
                NewHiresThisMonth = newHires,
                ExpiringContracts30Days = expiringContracts.Count,
                ByDepartment = byDept,
                ByStatus = byStatus,
                MaleCount = male,
                FemaleCount = female,
                RecentHires = recentHires,
                ExpiringContractList = expiringContracts
            };
        }

        public async Task<HrSmartAlertsDto> GetSmartAlertsAsync()
        {
            var today = VietnamTime.Today;
            var in30Days = today.AddDays(30);

            var baseQuery = _context.Employees.AsNoTracking();
            var scopedEmps = _dataScope != null ? _dataScope.ApplyEmployeeScope(baseQuery) : baseQuery;
            var scopedEmpIds = scopedEmps.Select(e => e.Id);

            // 1. Hợp đồng sắp hết hạn (30, 15, 7 ngày)
            var expiringContracts = await _context.Contracts
                .Include(c => c.Employee).ThenInclude(e => e.Department)
                .AsNoTracking()
                .Where(c => scopedEmpIds.Contains(c.EmployeeId) && c.Status == "Active" && c.EndDate.HasValue && c.EndDate.Value >= today && c.EndDate.Value <= in30Days)
                .OrderBy(c => c.EndDate)
                .Select(c => new ExpiringContractDto
                {
                    ContractId = c.Id,
                    ContractNumber = c.ContractNumber,
                    EmployeeName = c.Employee.FullName,
                    EmployeeCode = c.Employee.EmployeeCode,
                    DepartmentName = c.Employee.Department.Name,
                    EndDate = c.EndDate,
                    DaysLeft = (int)(c.EndDate!.Value.Date - today).TotalDays,
                    UrgencyLevel = (c.EndDate!.Value.Date - today).TotalDays <= 7 ? "Critical" :
                                   (c.EndDate!.Value.Date - today).TotalDays <= 15 ? "Warning" : "Normal"
                })
                .ToListAsync();

            // 2. Thử việc sắp hết hạn (Mặc định 60 ngày thử việc)
            var probations = await scopedEmps
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.Status == "Probation")
                .AsNoTracking()
                .ToListAsync();

            var endingProbations = probations
                .Select(e =>
                {
                    var expectedEnd = e.HireDate.AddDays(60);
                    var daysLeft = (int)(expectedEnd.Date - today).TotalDays;
                    return new ProbationEndingDto
                    {
                        EmployeeId = e.Id,
                        EmployeeCode = e.EmployeeCode,
                        FullName = e.FullName,
                        DepartmentName = e.Department?.Name ?? "",
                        PositionName = e.Position?.Name ?? "",
                        HireDate = e.HireDate,
                        ExpectedProbationEndDate = expectedEnd,
                        DaysLeft = daysLeft
                    };
                })
                .Where(p => p.DaysLeft <= 15)
                .OrderBy(p => p.DaysLeft)
                .ToList();

            // 3. Chứng chỉ sắp hết hạn trong 30 ngày
            var expiringCerts = await _context.EmployeeCertificates
                .Include(c => c.Employee)
                .AsNoTracking()
                .Where(c => scopedEmpIds.Contains(c.EmployeeId) && c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= in30Days)
                .OrderBy(c => c.ExpiryDate)
                .Select(c => new ExpiringCertificateDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeCode = c.Employee.EmployeeCode,
                    FullName = c.Employee.FullName,
                    CertificateName = c.CertificateName,
                    ExpiryDate = c.ExpiryDate!.Value,
                    DaysLeft = (int)(c.ExpiryDate!.Value.Date - today).TotalDays
                })
                .ToListAsync();

            // 4. Sinh nhật trong tháng
            var currentMonth = today.Month;
            var birthdays = await scopedEmps
                .Include(e => e.Department)
                .Where(e => e.DateOfBirth.HasValue && e.DateOfBirth.Value.Month == currentMonth && e.Status != "Resigned")
                .OrderBy(e => e.DateOfBirth!.Value.Day)
                .Select(e => new UpcomingBirthdayDto
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.Department.Name,
                    DateOfBirth = e.DateOfBirth!.Value,
                    Day = e.DateOfBirth!.Value.Day,
                    Age = today.Year - e.DateOfBirth!.Value.Year,
                    AvatarUrl = e.AvatarUrl
                })
                .ToListAsync();

            return new HrSmartAlertsDto
            {
                ExpiringContracts = expiringContracts,
                EndingProbations = endingProbations,
                ExpiringCertificates = expiringCerts,
                BirthdaysInMonth = birthdays
            };
        }

        // === IMPORT & EXPORT EXCEL (ClosedXML) ===
        public async Task<byte[]> ExportToExcelAsync(EmployeeFilterDto filter)
        {
            var baseQuery = _context.Employees.AsNoTracking();
            var query = (_dataScope != null ? _dataScope.ApplyEmployeeScope(baseQuery) : baseQuery)
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(e => e.FullName.Contains(kw) || e.EmployeeCode.Contains(kw) || e.Email.Contains(kw));
            }
            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                query = query.Where(e => e.DepartmentId == filter.DepartmentId);
            if (filter.PositionId.HasValue && filter.PositionId > 0)
                query = query.Where(e => e.PositionId == filter.PositionId);
            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(e => e.Status == filter.Status);

            var list = await query.OrderBy(e => e.EmployeeCode).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("DanhSachNhanSu");

            // Header Style
            string[] headers = new[]
            {
                "STT", "Mã NV", "Họ và tên", "Giới tính", "Ngày sinh", "Email",
                "Số điện thoại", "Số CCCD", "Chi nhánh", "Phòng ban", "Chức vụ", "Trạng thái", "Ngày vào làm"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5"); // Indigo Primary
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            int row = 2;
            foreach (var item in list)
            {
                worksheet.Cell(row, 1).Value = row - 1;
                worksheet.Cell(row, 2).Value = item.EmployeeCode;
                worksheet.Cell(row, 3).Value = item.FullName;
                worksheet.Cell(row, 4).Value = item.Gender ?? "";
                worksheet.Cell(row, 5).Value = item.DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cell(row, 6).Value = item.Email;
                worksheet.Cell(row, 7).Value = item.Phone ?? "";
                worksheet.Cell(row, 8).Value = item.IdentityCard ?? "";
                worksheet.Cell(row, 9).Value = item.Department?.Branch?.Name ?? "";
                worksheet.Cell(row, 10).Value = item.Department?.Name ?? "";
                worksheet.Cell(row, 11).Value = item.Position?.Name ?? "";
                worksheet.Cell(row, 12).Value = item.Status;
                worksheet.Cell(row, 13).Value = item.HireDate.ToString("dd/MM/yyyy");

                // Zebra striping
                if (row % 2 == 1)
                {
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Template_Import_NhanSu");

            string[] headers = new[]
            {
                "Mã nhân viên (*)", "Họ và tên (*)", "Giới tính (Nam/Nữ)", "Ngày sinh (dd/MM/yyyy)",
                "Email (*)", "Số điện thoại", "Số CCCD", "Mã phòng ban (*)", "Mã chức vụ (*)",
                "Trạng thái (Active/Probation)", "Ngày vào làm (dd/MM/yyyy)"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#312E81"); // Deep Indigo
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Dòng mẫu 1
            ws.Cell(2, 1).Value = "NV26001";
            ws.Cell(2, 2).Value = "Trần Minh Quân";
            ws.Cell(2, 3).Value = "Nam";
            ws.Cell(2, 4).Value = "15/08/1995";
            ws.Cell(2, 5).Value = "quan.tm@company.com";
            ws.Cell(2, 6).Value = "0901234567";
            ws.Cell(2, 7).Value = "001295012345";
            ws.Cell(2, 8).Value = "CNTT";
            ws.Cell(2, 9).Value = "DEV";
            ws.Cell(2, 10).Value = "Active";
            ws.Cell(2, 11).Value = "01/01/2026";

            // Dòng mẫu 2
            ws.Cell(3, 1).Value = "NV26002";
            ws.Cell(3, 2).Value = "Lê Thị Mai";
            ws.Cell(3, 3).Value = "Nữ";
            ws.Cell(3, 4).Value = "22/11/1998";
            ws.Cell(3, 5).Value = "mai.lt@company.com";
            ws.Cell(3, 6).Value = "0912345678";
            ws.Cell(3, 7).Value = "001298056789";
            ws.Cell(3, 8).Value = "NS";
            ws.Cell(3, 9).Value = "HR_SPEC";
            ws.Cell(3, 10).Value = "Probation";
            ws.Cell(3, 11).Value = "15/01/2026";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<EmployeeImportResultDto> ImportFromExcelAsync(Stream fileStream, int? currentUserId)
        {
            var result = new EmployeeImportResultDto();

            using var workbook = new XLWorkbook(fileStream);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                result.Errors.Add(new RowValidationErrorDto { RowIndex = 0, ErrorMessage = "File Excel không chứa bất kỳ sheet nào." });
                return result;
            }

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();
            if (rows == null || rows.Count == 0)
            {
                result.Errors.Add(new RowValidationErrorDto { RowIndex = 0, ErrorMessage = "File Excel không có dữ liệu hàng để nhập." });
                return result;
            }

            result.TotalRows = rows.Count;

            var existingCodes = (await _context.Employees.Select(e => e.EmployeeCode.ToUpper()).ToListAsync()).ToHashSet();
            var existingEmails = (await _context.Employees.Select(e => e.Email.ToLower()).ToListAsync()).ToHashSet();
            var departments = await _context.Departments.ToDictionaryAsync(d => d.Code.ToUpper(), d => d.Id);
            var positions = await _context.Positions.ToDictionaryAsync(p => p.Code.ToUpper(), p => p.Id);

            var fileCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fileEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var employeesToInsert = new List<Employee>();

            int rowIndex = 1;
            foreach (var r in rows)
            {
                rowIndex++;
                string code = r.Cell(1).GetString().Trim().ToUpper();
                string fullName = r.Cell(2).GetString().Trim();
                string gender = r.Cell(3).GetString().Trim();
                string dobStr = r.Cell(4).GetString().Trim();
                string email = r.Cell(5).GetString().Trim().ToLower();
                string phone = r.Cell(6).GetString().Trim();
                string cccd = r.Cell(7).GetString().Trim();
                string deptCode = r.Cell(8).GetString().Trim().ToUpper();
                string posCode = r.Cell(9).GetString().Trim().ToUpper();
                string status = r.Cell(10).GetString().Trim();
                string hireDateStr = r.Cell(11).GetString().Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new RowValidationErrorDto { RowIndex = rowIndex, Field = "EmployeeCode", ErrorMessage = "Mã nhân viên bắt buộc nhập." });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    result.Errors.Add(new RowValidationErrorDto { RowIndex = rowIndex, EmployeeCode = code, Field = "FullName", ErrorMessage = "Họ tên bắt buộc nhập." });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    result.Errors.Add(new RowValidationErrorDto { RowIndex = rowIndex, EmployeeCode = code, Field = "Email", ErrorMessage = "Email bắt buộc nhập." });
                    continue;
                }

                if (existingCodes.Contains(code) || fileCodes.Contains(code))
                {
                    result.Errors.Add(new RowValidationErrorDto { RowIndex = rowIndex, EmployeeCode = code, Field = "EmployeeCode", ErrorMessage = $"Mã nhân viên '{code}' đã tồn tại." });
                    continue;
                }

                if (existingEmails.Contains(email) || fileEmails.Contains(email))
                {
                    result.Errors.Add(new RowValidationErrorDto { RowIndex = rowIndex, EmployeeCode = code, Field = "Email", ErrorMessage = $"Email '{email}' đã tồn tại." });
                    continue;
                }

                if (!departments.TryGetValue(deptCode, out int deptId))
                {
                    result.Errors.Add(new RowValidationErrorDto { RowIndex = rowIndex, EmployeeCode = code, Field = "DepartmentCode", ErrorMessage = $"Không tìm thấy phòng ban có mã '{deptCode}'." });
                    continue;
                }

                if (!positions.TryGetValue(posCode, out int posId))
                {
                    result.Errors.Add(new RowValidationErrorDto { RowIndex = rowIndex, EmployeeCode = code, Field = "PositionCode", ErrorMessage = $"Không tìm thấy chức vụ có mã '{posCode}'." });
                    continue;
                }

                DateTime? dob = null;
                if (!string.IsNullOrWhiteSpace(dobStr) && DateTime.TryParse(dobStr, out var parsedDob))
                {
                    dob = parsedDob;
                }

                DateTime hireDate = VietnamTime.Today;
                if (!string.IsNullOrWhiteSpace(hireDateStr) && DateTime.TryParse(hireDateStr, out var parsedHireDate))
                {
                    hireDate = parsedHireDate;
                }

                fileCodes.Add(code);
                fileEmails.Add(email);

                var emp = new Employee
                {
                    EmployeeCode = code,
                    FullName = fullName,
                    Gender = string.IsNullOrWhiteSpace(gender) ? "Khác" : gender,
                    DateOfBirth = dob,
                    Email = email,
                    Phone = phone,
                    IdentityCard = cccd,
                    DepartmentId = deptId,
                    PositionId = posId,
                    Status = string.IsNullOrWhiteSpace(status) ? "Active" : status,
                    HireDate = hireDate,
                    CreatedAt = VietnamTime.Now,
                    CreatedBy = currentUserId?.ToString()
                };

                employeesToInsert.Add(emp);
            }

            if (employeesToInsert.Count > 0)
            {
                _context.Employees.AddRange(employeesToInsert);
                await _context.SaveChangesAsync();

                // Tạo kèm EmployeeDetail trống
                var details = employeesToInsert.Select(e => new EmployeeDetail { EmployeeId = e.Id });
                _context.EmployeeDetails.AddRange(details);
                await _context.SaveChangesAsync();

                result.ImportedCount = employeesToInsert.Count;
            }

            result.Success = result.ErrorCount == 0;
            return result;
        }

        // === IN ẤN CHUẨN HÀNH CHÍNH (SƠ YẾU LÝ LỊCH) ===
        public async Task<EmployeePrintModel?> GetPrintableResumeAsync(int employeeId)
        {
            var emp = await _context.Employees
                .Include(e => e.Department).ThenInclude(d => d.Branch)
                .Include(e => e.Position)
                .Include(e => e.EmployeeDetail)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (emp == null) return null;

            return new EmployeePrintModel
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                DateOfBirth = emp.DateOfBirth,
                Gender = emp.Gender,
                Email = emp.Email,
                Phone = emp.Phone,
                IdentityCard = emp.IdentityCard,
                AvatarUrl = emp.AvatarUrl,
                DepartmentName = emp.Department?.Name ?? "",
                PositionName = emp.Position?.Name ?? "",
                BranchName = emp.Department?.Branch?.Name ?? "",
                HireDate = emp.HireDate,
                Status = emp.Status,
                Detail = emp.EmployeeDetail,
                Contracts = await _context.Contracts.Where(c => c.EmployeeId == employeeId).AsNoTracking().ToListAsync(),
                Relatives = await _context.EmployeeRelatives.Where(r => r.EmployeeId == employeeId).AsNoTracking().ToListAsync(),
                Skills = await _context.EmployeeSkills.Where(s => s.EmployeeId == employeeId).AsNoTracking().ToListAsync(),
                Certificates = await _context.EmployeeCertificates.Where(c => c.EmployeeId == employeeId).AsNoTracking().ToListAsync(),
                ChangeHistories = await _context.EmployeeChangeHistories.Where(h => h.EmployeeId == employeeId).OrderByDescending(h => h.EffectiveDate).AsNoTracking().ToListAsync(),
                Decisions = await _context.HrDecisions.Where(d => d.EmployeeId == employeeId).OrderByDescending(d => d.EffectiveDate).AsNoTracking().ToListAsync(),
                Assets = await _context.EmployeeAssetAssignments.Include(a => a.Asset).Where(a => a.EmployeeId == employeeId).AsNoTracking().ToListAsync()
            };
        }

        // === UPLOAD AVATAR ===
        public async Task<string?> SaveAvatarAsync(int employeeId, Stream fileStream, string fileName)
        {
            var emp = await _context.Employees.FindAsync(employeeId);
            if (emp == null) return null;

            var ext = Path.GetExtension(fileName).ToLower();
            var newFileName = $"avatar_{employeeId}_{DateTime.UtcNow.Ticks}{ext}";
            var avatarFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");

            if (!Directory.Exists(avatarFolder))
                Directory.CreateDirectory(avatarFolder);

            var filePath = Path.Combine(avatarFolder, newFileName);
            using (var outputStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(outputStream);
            }

            var avatarUrl = $"/uploads/avatars/{newFileName}";
            emp.AvatarUrl = avatarUrl;
            emp.UpdatedAt = VietnamTime.Now;

            await _context.SaveChangesAsync();
            return avatarUrl;
        }

        // === GHI LỊCH SỬ THAY ĐỔI ===
        private async Task TrackChanges(Employee emp, EmployeeCreateEditViewModel model, int? currentUserId)
        {
            var today = VietnamTime.Today;
            var changes = new List<EmployeeChangeHistory>();

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
