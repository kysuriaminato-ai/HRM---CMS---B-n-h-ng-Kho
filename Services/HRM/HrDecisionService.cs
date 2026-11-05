using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    public class HrDecisionService : IHrDecisionService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IDataScopeService? _dataScope;

        public HrDecisionService(
            AppDbContext context,
            INotificationService notificationService,
            IDataScopeService? dataScope = null)
        {
            _context = context;
            _notificationService = notificationService;
            _dataScope = dataScope;
        }

        public async Task<PaginatedList<HrDecisionListItemDto>> GetPagedAsync(HrDecisionFilterDto filter)
        {
            var query = _context.HrDecisions
                .Include(d => d.Employee)
                    .ThenInclude(e => e.Department)
                .Include(d => d.OldDepartment)
                .Include(d => d.NewDepartment)
                .Include(d => d.OldPosition)
                .Include(d => d.NewPosition)
                .Include(d => d.ApprovedBy)
                .AsNoTracking()
                .AsQueryable();

            if (_dataScope != null)
            {
                var scopedEmps = _dataScope.ApplyEmployeeScope(_context.Employees.AsNoTracking());
                var scopedEmpIds = scopedEmps.Select(e => e.Id);
                query = query.Where(d => scopedEmpIds.Contains(d.EmployeeId));
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(d => d.DecisionNumber.Contains(kw) ||
                                         d.Title.Contains(kw) ||
                                         d.Employee.FullName.Contains(kw) ||
                                         d.Employee.EmployeeCode.Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(filter.DecisionType))
                query = query.Where(d => d.DecisionType == filter.DecisionType);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(d => d.Status == filter.Status);

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                query = query.Where(d => d.Employee.DepartmentId == filter.DepartmentId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(d => d.EffectiveDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(d => d.EffectiveDate <= filter.ToDate.Value);

            var projected = query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new HrDecisionListItemDto
                {
                    Id = d.Id,
                    DecisionNumber = d.DecisionNumber,
                    EmployeeId = d.EmployeeId,
                    EmployeeCode = d.Employee.EmployeeCode,
                    FullName = d.Employee.FullName,
                    CurrentDepartmentName = d.Employee.Department.Name,
                    DecisionType = d.DecisionType,
                    Title = d.Title,
                    EffectiveDate = d.EffectiveDate,
                    SignDate = d.SignDate,
                    OldDepartmentName = d.OldDepartment != null ? d.OldDepartment.Name : null,
                    NewDepartmentName = d.NewDepartment != null ? d.NewDepartment.Name : null,
                    OldPositionName = d.OldPosition != null ? d.OldPosition.Name : null,
                    NewPositionName = d.NewPosition != null ? d.NewPosition.Name : null,
                    OldSalary = d.OldSalary,
                    NewSalary = d.NewSalary,
                    Amount = d.Amount,
                    Status = d.Status,
                    SignerName = d.SignerName,
                    SignerTitle = d.SignerTitle,
                    ApprovedByName = d.ApprovedBy != null ? d.ApprovedBy.Username : null,
                    ApprovedAt = d.ApprovedAt,
                    Reason = d.Reason
                });

            return await PaginatedList<HrDecisionListItemDto>.CreateAsync(projected, filter.Page, filter.PageSize);
        }

        public async Task<HrDecision?> GetByIdAsync(int id)
        {
            return await _context.HrDecisions
                .Include(d => d.Employee)
                    .ThenInclude(e => e.Department)
                .Include(d => d.Employee)
                    .ThenInclude(e => e.Position)
                .Include(d => d.OldDepartment)
                .Include(d => d.NewDepartment)
                .Include(d => d.OldPosition)
                .Include(d => d.NewPosition)
                .Include(d => d.ApprovedBy)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<HrDecisionPrintModel?> GetPrintModelAsync(int id)
        {
            var d = await GetByIdAsync(id);
            if (d == null) return null;

            var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync();

            return new HrDecisionPrintModel
            {
                Id = d.Id,
                DecisionNumber = d.DecisionNumber,
                CompanyName = comp?.Name ?? "CÔNG TY CỔ PHẦN CÔNG NGHỆ & THƯƠNG MẠI VIỆT NAM",
                SignLocation = "Hà Nội",
                SignDate = d.SignDate,
                Title = d.Title,
                DecisionType = d.DecisionType,
                EmployeeCode = d.Employee.EmployeeCode,
                EmployeeName = d.Employee.FullName,
                DepartmentName = d.Employee.Department?.Name ?? "",
                PositionName = d.Employee.Position?.Name ?? "",
                EffectiveDate = d.EffectiveDate,
                Reason = d.Reason,
                OldDepartmentName = d.OldDepartment?.Name,
                NewDepartmentName = d.NewDepartment?.Name,
                OldPositionName = d.OldPosition?.Name,
                NewPositionName = d.NewPosition?.Name,
                OldSalary = d.OldSalary,
                NewSalary = d.NewSalary,
                Amount = d.Amount,
                SignerName = d.SignerName ?? "Nguyễn Văn Giám Đốc",
                SignerTitle = d.SignerTitle ?? "Tổng Giám đốc"
            };
        }

        public async Task<(bool Success, string? Error, int Id)> CreateAsync(HrDecision decision, int? currentUserId)
        {
            if (await _context.HrDecisions.AnyAsync(d => d.DecisionNumber == decision.DecisionNumber))
                return (false, "Số quyết định này đã tồn tại trên hệ thống.", 0);

            var emp = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Id == decision.EmployeeId);

            if (emp == null)
                return (false, "Không tìm thấy nhân viên được chỉ định.", 0);

            decision.OldDepartmentId ??= emp.DepartmentId;
            decision.OldPositionId ??= emp.PositionId;
            decision.CreatedAt = VietnamTime.Now;
            decision.Status = "Issued"; // Mặc định ban hành, chờ phê duyệt hiệu lực

            _context.HrDecisions.Add(decision);
            await _context.SaveChangesAsync();

            return (true, null, decision.Id);
        }

        public async Task<(bool Success, string? Error)> ApproveDecisionAsync(int decisionId, int approvedByUserId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var decision = await _context.HrDecisions
                    .Include(d => d.OldDepartment)
                    .Include(d => d.NewDepartment)
                    .Include(d => d.OldPosition)
                    .Include(d => d.NewPosition)
                    .FirstOrDefaultAsync(d => d.Id == decisionId);

                if (decision == null)
                    return (false, "Không tìm thấy quyết định nhân sự.");

                if (decision.Status == "Approved")
                    return (false, "Quyết định này đã được phê duyệt trước đó.");

                if (decision.Status == "Cancelled")
                    return (false, "Không thể duyệt quyết định đã bị hủy.");

                var employee = await _context.Employees
                    .Include(e => e.Contracts)
                    .FirstOrDefaultAsync(e => e.Id == decision.EmployeeId);

                if (employee == null)
                    return (false, "Không tìm thấy nhân sự thụ hưởng quyết định.");

                // Cập nhật trạng thái Quyết định
                decision.Status = "Approved";
                decision.ApprovedById = approvedByUserId;
                decision.ApprovedAt = VietnamTime.Now;

                var changeHistories = new List<EmployeeChangeHistory>();

                switch (decision.DecisionType)
                {
                    case "Appointment": // Bổ nhiệm chức vụ mới
                        if (decision.NewPositionId.HasValue && decision.NewPositionId > 0)
                        {
                            var oldPosName = decision.OldPosition?.Name ?? employee.PositionId.ToString();
                            var newPos = await _context.Positions.FindAsync(decision.NewPositionId.Value);

                            employee.PositionId = decision.NewPositionId.Value;

                            changeHistories.Add(new EmployeeChangeHistory
                            {
                                EmployeeId = employee.Id,
                                ChangeType = "Position",
                                OldValue = oldPosName,
                                NewValue = newPos?.Name ?? decision.NewPositionId.Value.ToString(),
                                EffectiveDate = decision.EffectiveDate,
                                Reason = decision.Reason ?? decision.Title,
                                ChangedBy = approvedByUserId,
                                CreatedAt = VietnamTime.Now
                            });
                        }
                        break;

                    case "Transfer": // Điều chuyển phòng ban
                        if (decision.NewDepartmentId.HasValue && decision.NewDepartmentId > 0)
                        {
                            var oldDeptName = decision.OldDepartment?.Name ?? employee.DepartmentId.ToString();
                            var newDept = await _context.Departments.FindAsync(decision.NewDepartmentId.Value);

                            employee.DepartmentId = decision.NewDepartmentId.Value;

                            changeHistories.Add(new EmployeeChangeHistory
                            {
                                EmployeeId = employee.Id,
                                ChangeType = "Department",
                                OldValue = oldDeptName,
                                NewValue = newDept?.Name ?? decision.NewDepartmentId.Value.ToString(),
                                EffectiveDate = decision.EffectiveDate,
                                Reason = decision.Reason ?? decision.Title,
                                ChangedBy = approvedByUserId,
                                CreatedAt = VietnamTime.Now
                            });

                            if (decision.NewPositionId.HasValue && decision.NewPositionId > 0)
                            {
                                var oldPosName = decision.OldPosition?.Name ?? employee.PositionId.ToString();
                                var newPos = await _context.Positions.FindAsync(decision.NewPositionId.Value);
                                employee.PositionId = decision.NewPositionId.Value;

                                changeHistories.Add(new EmployeeChangeHistory
                                {
                                    EmployeeId = employee.Id,
                                    ChangeType = "Position",
                                    OldValue = oldPosName,
                                    NewValue = newPos?.Name ?? decision.NewPositionId.Value.ToString(),
                                    EffectiveDate = decision.EffectiveDate,
                                    Reason = decision.Reason ?? decision.Title,
                                    ChangedBy = approvedByUserId,
                                    CreatedAt = VietnamTime.Now
                                });
                            }
                        }
                        break;

                    case "SalaryAdjustment": // Điều chỉnh lương
                        if (decision.NewSalary.HasValue)
                        {
                            var activeContract = employee.Contracts.FirstOrDefault(c => c.Status == "Active");
                            if (activeContract != null)
                            {
                                activeContract.BasicSalary = decision.NewSalary.Value;
                            }

                            changeHistories.Add(new EmployeeChangeHistory
                            {
                                EmployeeId = employee.Id,
                                ChangeType = "Salary",
                                OldValue = decision.OldSalary.HasValue ? $"{decision.OldSalary.Value:N0} đ" : null,
                                NewValue = $"{decision.NewSalary.Value:N0} đ",
                                EffectiveDate = decision.EffectiveDate,
                                Reason = decision.Reason ?? decision.Title,
                                ChangedBy = approvedByUserId,
                                CreatedAt = VietnamTime.Now
                            });
                        }
                        break;

                    case "Reward": // Khen thưởng
                        changeHistories.Add(new EmployeeChangeHistory
                        {
                            EmployeeId = employee.Id,
                            ChangeType = "Reward",
                            OldValue = null,
                            NewValue = $"Khen thưởng: {decision.Amount:N0} đ - {decision.Title}",
                            EffectiveDate = decision.EffectiveDate,
                            Reason = decision.Reason,
                            ChangedBy = approvedByUserId,
                            CreatedAt = VietnamTime.Now
                        });
                        break;

                    case "Discipline": // Kỷ luật
                        changeHistories.Add(new EmployeeChangeHistory
                        {
                            EmployeeId = employee.Id,
                            ChangeType = "Discipline",
                            OldValue = null,
                            NewValue = $"Kỷ luật: {decision.Amount:N0} đ - {decision.Title}",
                            EffectiveDate = decision.EffectiveDate,
                            Reason = decision.Reason,
                            ChangedBy = approvedByUserId,
                            CreatedAt = VietnamTime.Now
                        });
                        break;

                    case "Termination": // Chấm dứt HĐLĐ / Thôi việc
                        employee.Status = "Resigned";

                        // Chấm dứt các hợp đồng đang hiệu lực
                        foreach (var contract in employee.Contracts.Where(c => c.Status == "Active"))
                        {
                            contract.Status = "Terminated";
                            contract.EndDate = decision.EffectiveDate;
                        }

                        // Thu hồi quyền truy cập hệ thống của nhân viên (khóa user account)
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);
                        if (user != null)
                        {
                            user.IsActive = false;
                            user.SecurityStamp = Guid.NewGuid();
                        }

                        changeHistories.Add(new EmployeeChangeHistory
                        {
                            EmployeeId = employee.Id,
                            ChangeType = "Status",
                            OldValue = "Active",
                            NewValue = "Resigned",
                            EffectiveDate = decision.EffectiveDate,
                            Reason = decision.Reason ?? "Thôi việc theo Quyết định " + decision.DecisionNumber,
                            ChangedBy = approvedByUserId,
                            CreatedAt = VietnamTime.Now
                        });
                        break;
                }

                if (changeHistories.Count > 0)
                {
                    _context.EmployeeChangeHistories.AddRange(changeHistories);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Gửi thông báo SignalR Realtime
                try
                {
                    var userTarget = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);
                    if (userTarget != null)
                    {
                        await _notificationService.SendToUserAsync(
                            userTarget.Id,
                            $"Quyết định nhân sự #{decision.DecisionNumber} đã được phê duyệt",
                            $"Quyết định '{decision.Title}' có hiệu lực từ ngày {decision.EffectiveDate:dd/MM/yyyy}.",
                            $"/QuyetDinhNhanSu/Details/{decision.Id}",
                            "HR_DECISION");
                    }

                    await _notificationService.SendToRoleAsync(
                        "HR_MANAGER",
                        $"Phê duyệt Quyết định nhân sự #{decision.DecisionNumber}",
                        $"Quyết định '{decision.Title}' áp dụng cho {employee.FullName} ({employee.EmployeeCode}) đã có hiệu lực.",
                        $"/QuyetDinhNhanSu/Details/{decision.Id}",
                        "HR_DECISION");
                }
                catch
                {
                    // Lỗi thông báo không cản trở transaction đã commit
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Lỗi khi phê duyệt quyết định: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Error)> CancelDecisionAsync(int decisionId, int cancelledByUserId, string? reason)
        {
            var decision = await _context.HrDecisions.FindAsync(decisionId);
            if (decision == null) return (false, "Không tìm thấy quyết định.");

            if (decision.Status == "Approved")
                return (false, "Không thể hủy quyết định đã được phê duyệt.");

            decision.Status = "Cancelled";
            decision.Reason = string.IsNullOrWhiteSpace(decision.Reason)
                ? $"[Đã hủy bởi User {cancelledByUserId}]: {reason}"
                : $"{decision.Reason} - [Đã hủy bởi User {cancelledByUserId}]: {reason}";
            decision.UpdatedAt = VietnamTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<HrDecisionDropdownsDto> GetDropdownsAsync()
        {
            var emps = await _context.Employees.AsNoTracking()
                .OrderBy(e => e.FullName)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.EmployeeCode} - {e.FullName} ({e.Department.Name})"
                })
                .ToListAsync();

            var depts = await _context.Departments.AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToListAsync();

            var pos = await _context.Positions.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync();

            return new HrDecisionDropdownsDto
            {
                Employees = emps,
                Departments = depts,
                Positions = pos
            };
        }
    }
}
