using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    public class LeaveManagementService : ILeaveManagementService
    {
        private readonly AppDbContext _context;
        private readonly IDataScopeService? _dataScope;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService? _currentUser;

        public LeaveManagementService(
            AppDbContext context,
            INotificationService notificationService,
            IDataScopeService? dataScope = null,
            ICurrentUserService? currentUser = null)
        {
            _context = context;
            _notificationService = notificationService;
            _dataScope = dataScope;
            _currentUser = currentUser;
        }

        // ═══════════════════════════════════════════════════════════
        // 1. QUẢN LÝ QUỸ PHÉP NĂM (LEAVE BALANCES)
        // ═══════════════════════════════════════════════════════════

        public decimal CalculateSeniorityBonus(DateTime hireDate, int targetYear)
        {
            var targetDate = new DateTime(targetYear, 12, 31);
            if (hireDate > targetDate) return 0;

            int completedYears = (int)((targetDate - hireDate.Date).TotalDays / 365.25);
            if (completedYears < 5) return 0;

            // Cứ mỗi 5 năm làm việc được cộng 1 ngày phép (Điều 114 BLLĐ 2019)
            return Math.Floor((decimal)completedYears / 5.0m);
        }

        public async Task<PaginatedList<LeaveBalanceListItemDto>> GetLeaveBalancesPagedAsync(LeaveBalanceFilterDto filter)
        {
            var empQuery = _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Status == "Active")
                .AsNoTracking()
                .AsQueryable();

            if (_dataScope != null)
                empQuery = _dataScope.ApplyEmployeeScope(empQuery);

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                empQuery = empQuery.Where(e => e.DepartmentId == filter.DepartmentId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                empQuery = empQuery.Where(e => e.EmployeeCode.ToLower().Contains(kw) ||
                                               e.FullName.ToLower().Contains(kw));
            }

            var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync();
            var empIds = employees.Select(e => e.Id).ToList();

            var existingBalances = await _context.EmployeeLeaveBalances
                .Where(b => b.Year == filter.Year && empIds.Contains(b.EmployeeId))
                .AsNoTracking()
                .ToListAsync();

            var list = new List<LeaveBalanceListItemDto>();
            foreach (var emp in employees)
            {
                var b = existingBalances.FirstOrDefault(x => x.EmployeeId == emp.Id);
                decimal standard = b?.StandardDays ?? 12.0m;
                decimal seniority = b?.SeniorityBonusDays ?? CalculateSeniorityBonus(emp.HireDate, filter.Year);
                decimal carried = b?.CarriedOverDays ?? 0.0m;
                decimal used = b?.UsedDays ?? 0.0m;
                decimal remaining = b?.RemainingDays ?? (standard + seniority + carried - used);

                int completedYears = Math.Max(0, (int)((new DateTime(filter.Year, 12, 31) - emp.HireDate.Date).TotalDays / 365.25));

                list.Add(new LeaveBalanceListItemDto
                {
                    Id = b?.Id ?? 0,
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    DepartmentName = emp.Department?.Name ?? "",
                    HireDate = emp.HireDate,
                    CompletedYearsOfService = completedYears,
                    Year = filter.Year,
                    StandardDays = standard,
                    SeniorityBonusDays = seniority,
                    CarriedOverDays = carried,
                    UsedDays = used,
                    RemainingDays = remaining
                });
            }

            int count = list.Count;
            var pagedItems = list.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();
            return new PaginatedList<LeaveBalanceListItemDto>(pagedItems, count, filter.Page, filter.PageSize);
        }

        public async Task<EmployeeLeaveBalance?> GetEmployeeLeaveBalanceAsync(int employeeId, int year)
        {
            return await _context.EmployeeLeaveBalances
                .Include(b => b.LeaveType)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);
        }

        public async Task<(bool Success, string? Error, int Id)> InitializeOrRecalculateBalanceAsync(
            int employeeId, int year, int? currentUserId)
        {
            var emp = await _context.Employees.FindAsync(employeeId);
            if (emp == null) return (false, "Không tìm thấy nhân viên.", 0);

            var leaveType = await _context.LeaveTypes.FirstOrDefaultAsync(t => t.Code == "ANNUAL_LEAVE")
                            ?? await _context.LeaveTypes.FirstOrDefaultAsync();

            if (leaveType == null)
            {
                leaveType = new LeaveType
                {
                    Code = "ANNUAL_LEAVE",
                    Name = "Nghỉ phép năm",
                    MaxDaysPerYear = 12,
                    IsPaid = true,
                    AllowHalfDay = true
                };
                _context.LeaveTypes.Add(leaveType);
                await _context.SaveChangesAsync();
            }

            decimal standard = 12.0m;
            decimal seniority = CalculateSeniorityBonus(emp.HireDate, year);

            // Phép tồn năm trước
            decimal carriedOver = 0;
            var prevBalance = await _context.EmployeeLeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year - 1);
            if (prevBalance != null && prevBalance.RemainingDays > 0)
            {
                carriedOver = prevBalance.RemainingDays;
            }

            // Số ngày phép đã dùng trong năm
            decimal used = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId && l.FromDate.Year == year &&
                            l.Status == LeaveRequestStatus.DirectorApproved && l.RequestType == "ANNUAL_LEAVE")
                .SumAsync(l => (decimal?)l.TotalDays) ?? 0.0m;

            decimal remaining = Math.Max(0, standard + seniority + carriedOver - used);

            var existing = await _context.EmployeeLeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

            if (existing == null)
            {
                existing = new EmployeeLeaveBalance
                {
                    EmployeeId = employeeId,
                    Year = year,
                    LeaveTypeId = leaveType.Id,
                    StandardDays = standard,
                    SeniorityBonusDays = seniority,
                    CarriedOverDays = carriedOver,
                    UsedDays = used,
                    RemainingDays = remaining,
                    ExpiredCarryOverDate = new DateTime(year, 3, 31),
                    CreatedAt = VietnamTime.Now
                };
                _context.EmployeeLeaveBalances.Add(existing);
            }
            else
            {
                existing.StandardDays = standard;
                existing.SeniorityBonusDays = seniority;
                existing.CarriedOverDays = carriedOver;
                existing.UsedDays = used;
                existing.RemainingDays = remaining;
                existing.UpdatedAt = VietnamTime.Now;
            }

            await _context.SaveChangesAsync();
            return (true, null, existing.Id);
        }

        public async Task<(bool Success, string? Error, int ProcessedCount)> RecalculateAllBalancesAsync(
            int year, int? currentUserId)
        {
            var employees = await _context.Employees
                .Where(e => e.Status == "Active")
                .ToListAsync();

            int count = 0;
            foreach (var emp in employees)
            {
                var (ok, _, _) = await InitializeOrRecalculateBalanceAsync(emp.Id, year, currentUserId);
                if (ok) count++;
            }

            return (true, null, count);
        }

        // ═══════════════════════════════════════════════════════════
        // 2. QUẢN LÝ ĐƠN TỪ ĐA CẤP (LEAVE REQUESTS)
        // ═══════════════════════════════════════════════════════════

        public async Task<PaginatedList<LeaveRequestListItemDto>> GetLeaveRequestsPagedAsync(LeaveRequestFilterDto filter)
        {
            var baseQuery = _context.LeaveRequests.AsNoTracking().AsQueryable();

            if (_dataScope != null)
                baseQuery = _dataScope.ApplyLeaveRequestScope(baseQuery);

            var query = baseQuery
                .Include(l => l.Employee)
                    .ThenInclude(e => e.Department)
                .Include(l => l.DepartmentHead)
                .Include(l => l.Director)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Status) &&
                WorkflowStatusCodes.TryParse(WorkflowStatusCodes.LeaveRequest, filter.Status, out var statusEnum))
            {
                query = query.Where(l => l.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
                query = query.Where(l => l.RequestType == filter.Type);

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                query = query.Where(l => l.Employee.DepartmentId == filter.DepartmentId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(l => l.FromDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(l => l.ToDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(l => l.RequestCode.ToLower().Contains(kw) ||
                                         l.Employee.EmployeeCode.ToLower().Contains(kw) ||
                                         l.Employee.FullName.ToLower().Contains(kw));
            }

            var paged = await PaginatedList<LeaveRequest>.CreateAsync(
                query.OrderByDescending(l => l.CreatedAt), filter.Page, filter.PageSize);

            var dtos = paged.Select(l => new LeaveRequestListItemDto
            {
                Id = l.Id,
                RequestCode = l.RequestCode,
                EmployeeId = l.EmployeeId,
                EmployeeCode = l.Employee.EmployeeCode,
                FullName = l.Employee.FullName,
                DepartmentName = l.Employee.Department?.Name ?? "",
                RequestType = l.RequestType,
                RequestTypeName = GetRequestTypeName(l.RequestType),
                FromDate = l.FromDate,
                ToDate = l.ToDate,
                TotalDays = l.TotalDays,
                Reason = l.Reason,
                AttachmentUrl = l.AttachmentUrl,
                Status = l.Status.ToString(),
                DepartmentHeadName = l.DepartmentHead?.Username,
                DepartmentHeadApprovedAt = l.DepartmentHeadApprovedAt,
                DirectorName = l.Director?.Username,
                DirectorApprovedAt = l.DirectorApprovedAt,
                CreatedAt = l.CreatedAt
            }).ToList();

            return new PaginatedList<LeaveRequestListItemDto>(dtos, paged.TotalCount, paged.PageIndex, filter.PageSize);
        }

        public async Task<LeaveRequest?> GetLeaveRequestByIdAsync(int id)
        {
            return await _context.LeaveRequests
                .Include(l => l.Employee)
                    .ThenInclude(e => e.Department)
                .Include(l => l.DepartmentHead)
                .Include(l => l.Director)
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<LeaveRequestSummaryKpiDto> GetSummaryKpisAsync()
        {
            var scoped = _context.LeaveRequests.AsNoTracking().AsQueryable();
            if (_dataScope != null)
                scoped = _dataScope.ApplyLeaveRequestScope(scoped);

            return new LeaveRequestSummaryKpiDto
            {
                TotalCount = await scoped.CountAsync(),
                PendingCount = await scoped.CountAsync(l => l.Status == LeaveRequestStatus.PendingApproval),
                DeptApprovedCount = await scoped.CountAsync(l => l.Status == LeaveRequestStatus.DepartmentApproved),
                ApprovedCount = await scoped.CountAsync(l => l.Status == LeaveRequestStatus.DirectorApproved),
                RejectedCount = await scoped.CountAsync(l => l.Status == LeaveRequestStatus.Rejected)
            };
        }

        public async Task<(bool Success, string? Error, int Id)> CreateLeaveRequestAsync(
            LeaveRequest request, int currentUserId)
        {
            if (request.ToDate < request.FromDate)
                return (false, "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", 0);

            // 1. Kiểm tra khóa kỳ công
            bool isLocked = await _context.AttendanceLocks.AnyAsync(l =>
                l.Year == request.FromDate.Year && l.Month == request.FromDate.Month && l.IsLocked);
            if (isLocked)
                return (false, $"Kỳ chấm công tháng {request.FromDate.Month}/{request.FromDate.Year} đã bị khóa. Không thể tạo đơn.", 0);

            // 2. Kiểm tra trùng lịch nghỉ
            bool overlaps = await _context.LeaveRequests.AnyAsync(l =>
                l.EmployeeId == request.EmployeeId &&
                l.Status != LeaveRequestStatus.Rejected &&
                request.FromDate.Date <= l.ToDate.Date && request.ToDate.Date >= l.FromDate.Date);

            if (overlaps)
                return (false, "Thời gian xin nghỉ bị trùng lặp với đơn từ khác đã gửi trước đó.", 0);

            // 3. Kiểm tra chứng từ đính kèm nếu LeaveType yêu cầu
            LeaveType? leaveType = null;
            if (request.LeaveTypeId.HasValue)
            {
                leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId.Value);
                if (leaveType != null && leaveType.RequiresAttachment && string.IsNullOrWhiteSpace(request.AttachmentUrl))
                {
                    return (false, $"Loại nghỉ '{leaveType.Name}' yêu cầu bắt buộc phải đính kèm chứng từ/giấy phép.", 0);
                }
            }

            // 4. Kiểm tra số dư Quỹ phép năm nếu là nghỉ phép năm
            if (request.RequestType == "ANNUAL_LEAVE")
            {
                int year = request.FromDate.Year;
                var balance = await _context.EmployeeLeaveBalances
                    .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId && b.Year == year);

                if (balance == null)
                {
                    // Tự động khởi tạo quỹ phép nếu chưa có
                    await InitializeOrRecalculateBalanceAsync(request.EmployeeId, year, currentUserId);
                    balance = await _context.EmployeeLeaveBalances
                        .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId && b.Year == year);
                }

                if (balance != null && balance.RemainingDays < request.TotalDays)
                {
                    return (false, $"Số dư phép năm còn lại ({balance.RemainingDays:N1} ngày) không đủ để đăng ký ({request.TotalDays:N1} ngày).", 0);
                }
            }

            // Sinh mã đơn từ
            string prefix = $"LR-{VietnamTime.Now:yyMM}";
            int countThisMonth = await _context.LeaveRequests.CountAsync(l => l.RequestCode.StartsWith(prefix));
            request.RequestCode = $"{prefix}-{(countThisMonth + 1):D3}";

            if (request.TotalDays <= 0)
            {
                var span = request.ToDate.Date - request.FromDate.Date;
                request.TotalDays = Math.Max(1, (decimal)span.TotalDays + 1);
            }

            request.Status = LeaveRequestStatus.PendingApproval;
            request.CreatedByUserId = currentUserId;
            request.CreatedAt = VietnamTime.Now;

            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            // Gửi thông báo đến HR/Quản lý
            var emp = await _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == request.EmployeeId);
            await _notificationService.SendToRoleAsync("HR_MANAGER",
                $"Đơn từ mới cần duyệt: {request.RequestCode}",
                $"{emp?.FullName} ({emp?.Department?.Name}) vừa nộp đơn {GetRequestTypeName(request.RequestType)}. Thời gian: {request.FromDate:dd/MM/yyyy} - {request.ToDate:dd/MM/yyyy}.",
                $"/DonTu/Index?status=CHO_DUYET",
                "LEAVE_REQUEST");

            return (true, null, request.Id);
        }

        public async Task<(bool Success, string? Error)> ApproveLeaveRequestAsync(
            int id, string level, string? note, int approverUserId)
        {
            string normalizedLevel = (level ?? string.Empty).Trim().ToUpperInvariant();
            if (normalizedLevel != "TP" && normalizedLevel != "GD")
                return (false, "Cấp phê duyệt không hợp lệ (chỉ chấp nhận TP hoặc GD).");

            var req = await _context.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (req == null) return (false, "Không tìm thấy đơn từ.");

            if (req.Status == LeaveRequestStatus.DirectorApproved)
                return (false, "Đơn từ này đã được phê duyệt chính thức trước đó.");

            if (req.Status == LeaveRequestStatus.Rejected)
                return (false, "Không thể duyệt đơn đã bị từ chối.");

            // Kiểm tra chống tự duyệt đơn của chính mình
            int? linkedEmployeeUserId = await _context.Users.AsNoTracking()
                .Where(u => u.EmployeeId == req.EmployeeId)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();

            var approvalCheck = LeaveApprovalPolicy.ValidateApprover(
                currentUserId: approverUserId,
                currentUserEmployeeId: _currentUser?.EmployeeId,
                isSuperAdmin: _currentUser?.IsSuperAdmin ?? false,
                requestEmployeeId: req.EmployeeId,
                requestCreatedByUserId: req.CreatedByUserId,
                requestEmployeeLinkedUserId: linkedEmployeeUserId);

            if (approvalCheck.IsFailure)
                return (false, approvalCheck.ErrorMessage);

            // Kiểm tra khóa kỳ công khi duyệt cấp cuối (GD)
            if (normalizedLevel == "GD")
            {
                bool isLocked = await _context.AttendanceLocks.AnyAsync(l =>
                    l.Year == req.FromDate.Year && l.Month == req.FromDate.Month && l.IsLocked);
                if (isLocked)
                    return (false, $"Kỳ chấm công tháng {req.FromDate.Month}/{req.FromDate.Year} đã bị khóa. Không thể duyệt đơn lùi ngày.");
            }

            if (normalizedLevel == "TP")
            {
                req.Status = LeaveRequestStatus.DepartmentApproved;
                req.DepartmentHeadId = approverUserId;
                req.DepartmentHeadApprovedAt = VietnamTime.Now;
                req.DepartmentHeadNote = note;

                await _notificationService.SendToRoleAsync("HR_MANAGER",
                    $"Đơn từ {req.RequestCode} đã qua duyệt Trưởng bộ phận",
                    $"Trưởng phòng đã xác nhận đơn của {req.Employee.FullName}. Chờ Ban Giám Đốc/HR phê duyệt.",
                    $"/DonTu/Index?status=TRUONG_PHONG_DUYET",
                    "LEAVE_REQUEST");
            }
            else // GD / HR duyệt chính thức
            {
                req.Status = LeaveRequestStatus.DirectorApproved;
                req.DirectorId = approverUserId;
                req.DirectorApprovedAt = VietnamTime.Now;
                req.DirectorNote = note;

                // 1. Tự động trừ quỹ phép năm nếu là ANNUAL_LEAVE
                if (req.RequestType == "ANNUAL_LEAVE")
                {
                    int year = req.FromDate.Year;
                    var balance = await _context.EmployeeLeaveBalances
                        .FirstOrDefaultAsync(b => b.EmployeeId == req.EmployeeId && b.Year == year);

                    if (balance == null)
                    {
                        await InitializeOrRecalculateBalanceAsync(req.EmployeeId, year, approverUserId);
                        balance = await _context.EmployeeLeaveBalances
                            .FirstOrDefaultAsync(b => b.EmployeeId == req.EmployeeId && b.Year == year);
                    }

                    if (balance != null)
                    {
                        balance.UsedDays += req.TotalDays;
                        balance.RemainingDays = Math.Max(0, balance.StandardDays + balance.SeniorityBonusDays + balance.CarriedOverDays - balance.UsedDays);
                        balance.UpdatedAt = VietnamTime.Now;
                    }
                }

                // 2. Tự động đồng bộ sang bảng chấm công ngày AttendanceDailies
                await SyncAttendanceOnApprovalAsync(req);

                // 3. Gửi thông báo realtime cho nhân viên
                if (linkedEmployeeUserId.HasValue)
                {
                    await _notificationService.SendToUserAsync(linkedEmployeeUserId.Value,
                        $"Đơn {req.RequestCode} đã được phê duyệt chính thức!",
                        $"Ban Giám Đốc đã duyệt đơn nghỉ {GetRequestTypeName(req.RequestType)}. Hệ thống đã tự động trừ quỹ phép và cập nhật bảng công.",
                        "/DonTu/Index",
                        "LEAVE_REQUEST");
                }
            }

            // Ghi AuditLog nếu SuperAdmin duyệt đơn của chính mình
            if (approvalCheck.GetRequiredValue().IsSelfApprovalOverrideBySuperAdmin)
            {
                var user = await _context.Users.FindAsync(approverUserId);
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = approverUserId,
                    Username = user?.Username ?? "SuperAdmin",
                    Action = "SELF_APPROVE_BY_SUPERADMIN",
                    EntityName = "LeaveRequest",
                    RecordId = req.Id.ToString(),
                    NewValues = $"SuperAdmin phê duyệt đơn từ {req.RequestCode} của chính mình ở cấp {normalizedLevel}. Ghi chú: {note}",
                    CreatedAt = VietnamTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RejectLeaveRequestAsync(
            int id, string reason, int rejectorUserId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Bắt buộc phải nhập lý do từ chối đơn.");

            var req = await _context.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (req == null) return (false, "Không tìm thấy đơn từ.");

            int? linkedEmployeeUserId = await _context.Users.AsNoTracking()
                .Where(u => u.EmployeeId == req.EmployeeId)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();

            var approvalCheck = LeaveApprovalPolicy.ValidateApprover(
                currentUserId: rejectorUserId,
                currentUserEmployeeId: _currentUser?.EmployeeId,
                isSuperAdmin: _currentUser?.IsSuperAdmin ?? false,
                requestEmployeeId: req.EmployeeId,
                requestCreatedByUserId: req.CreatedByUserId,
                requestEmployeeLinkedUserId: linkedEmployeeUserId);

            if (approvalCheck.IsFailure)
                return (false, approvalCheck.ErrorMessage);

            req.Status = LeaveRequestStatus.Rejected;
            req.DirectorId = rejectorUserId;
            req.DirectorApprovedAt = VietnamTime.Now;
            req.DirectorNote = reason;

            if (linkedEmployeeUserId.HasValue)
            {
                await _notificationService.SendToUserAsync(linkedEmployeeUserId.Value,
                    $"Đơn {req.RequestCode} đã bị từ chối",
                    $"Lý do: {reason}. Vui lòng liên hệ quản lý trực tiếp.",
                    "/DonTu/Index",
                    "LEAVE_REQUEST");
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(int SuccessCount, int FailCount, List<string> Errors)> BatchApproveAsync(
            List<int> ids, string level, string? note, int approverUserId)
        {
            int success = 0;
            int fail = 0;
            var errors = new List<string>();

            foreach (var id in ids)
            {
                var (ok, err) = await ApproveLeaveRequestAsync(id, level, note, approverUserId);
                if (ok)
                {
                    success++;
                }
                else
                {
                    fail++;
                    errors.Add($"Đơn #{id}: {err}");
                }
            }

            return (success, fail, errors);
        }

        public async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            return await _context.LeaveTypes
                .Where(t => t.IsActive)
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 3. ĐƠN GIẢI TRÌNH CHẤM CÔNG (ATTENDANCE ADJUSTMENTS)
        // ═══════════════════════════════════════════════════════════

        public async Task<PaginatedList<AttendanceAdjustmentRequest>> GetAdjustmentRequestsPagedAsync(AdjustmentFilterDto filter)
        {
            var query = _context.AttendanceAdjustmentRequests
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .Include(a => a.Shift)
                .Include(a => a.ApprovedBy)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(a => a.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.AdjustmentType))
                query = query.Where(a => a.AdjustmentType == filter.AdjustmentType);

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
                query = query.Where(a => a.Employee.DepartmentId == filter.DepartmentId.Value);

            if (filter.WorkDate.HasValue)
                query = query.Where(a => a.WorkDate.Date == filter.WorkDate.Value.Date);

            return await PaginatedList<AttendanceAdjustmentRequest>.CreateAsync(
                query.OrderByDescending(a => a.CreatedAt), filter.Page, filter.PageSize);
        }

        public async Task<(bool Success, string? Error, int Id)> CreateAdjustmentRequestAsync(
            AttendanceAdjustmentRequest request, int currentUserId)
        {
            // Kiểm tra khóa kỳ
            bool isLocked = await _context.AttendanceLocks.AnyAsync(l =>
                l.Year == request.WorkDate.Year && l.Month == request.WorkDate.Month && l.IsLocked);
            if (isLocked)
                return (false, $"Kỳ chấm công tháng {request.WorkDate.Month}/{request.WorkDate.Year} đã bị khóa. Không thể tạo đơn giải trình.", 0);

            string prefix = $"AR-{VietnamTime.Now:yyMM}";
            int count = await _context.AttendanceAdjustmentRequests.CountAsync(a => a.RequestCode.StartsWith(prefix));
            request.RequestCode = $"{prefix}-{(count + 1):D3}";
            request.Status = "Pending";
            request.CreatedAt = VietnamTime.Now;

            _context.AttendanceAdjustmentRequests.Add(request);
            await _context.SaveChangesAsync();

            var emp = await _context.Employees.FindAsync(request.EmployeeId);
            await _notificationService.SendToRoleAsync("HR_MANAGER",
                $"Đơn giải trình chấm công mới: {request.RequestCode}",
                $"{emp?.FullName} giải trình ngày {request.WorkDate:dd/MM/yyyy}. Lý do: {request.Reason}",
                "/DonTu/GiaiTrinh",
                "ATTENDANCE_ADJUSTMENT");

            return (true, null, request.Id);
        }

        public async Task<(bool Success, string? Error)> ApproveAdjustmentRequestAsync(
            int id, int approvedByUserId, string? note)
        {
            var req = await _context.AttendanceAdjustmentRequests
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (req == null) return (false, "Không tìm thấy đơn giải trình.");

            if (req.Status == "Approved")
                return (false, "Đơn giải trình này đã được phê duyệt.");

            // Kiểm tra khóa kỳ
            bool isLocked = await _context.AttendanceLocks.AnyAsync(l =>
                l.Year == req.WorkDate.Year && l.Month == req.WorkDate.Month && l.IsLocked);
            if (isLocked)
                return (false, $"Kỳ chấm công tháng {req.WorkDate.Month}/{req.WorkDate.Year} đã bị khóa.");

            req.Status = "Approved";
            req.ApprovedById = approvedByUserId;
            req.ApprovedAt = VietnamTime.Now;

            var defaultShift = (req.ShiftId.HasValue ? await _context.Shifts.FindAsync(req.ShiftId.Value) : null)
                               ?? await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive);

            // Tự động bổ sung/chỉnh sửa ngày công trong AttendanceDailies
            var att = await _context.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == req.EmployeeId && a.WorkDate.Date == req.WorkDate.Date);

            var inTime = req.ActualInTime ?? defaultShift?.StartTime ?? new TimeSpan(8, 0, 0);
            var outTime = req.ActualOutTime ?? defaultShift?.EndTime ?? new TimeSpan(17, 30, 0);
            decimal workHours = defaultShift?.WorkHours ?? 8.0m;

            if (att == null)
            {
                att = new AttendanceDaily
                {
                    EmployeeId = req.EmployeeId,
                    ShiftId = defaultShift?.Id ?? 1,
                    WorkDate = req.WorkDate.Date,
                    FirstInTime = inTime,
                    LastOutTime = outTime,
                    LateMinutes = 0,
                    EarlyMinutes = 0,
                    WorkHourCount = workHours,
                    AttendanceStatus = "Present",
                    Note = $"[Giải trình đã duyệt: {req.RequestCode}] {req.Reason}",
                    CreatedAt = VietnamTime.Now
                };
                _context.AttendanceDailies.Add(att);
            }
            else
            {
                att.FirstInTime = inTime;
                att.LastOutTime = outTime;
                att.LateMinutes = 0;
                att.EarlyMinutes = 0;
                att.WorkHourCount = workHours;
                att.AttendanceStatus = "Present";
                att.Note = $"[Giải trình đã duyệt: {req.RequestCode}] {req.Reason}";
            }

            // Gửi thông báo cho nhân viên
            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == req.EmployeeId);
            if (linkedUser != null)
            {
                await _notificationService.SendToUserAsync(linkedUser.Id,
                    $"Đơn giải trình {req.RequestCode} đã được phê duyệt",
                    $"Ngày công {req.WorkDate:dd/MM/yyyy} đã được chuẩn hóa lại giờ vào/ra.",
                    "/DonTu/GiaiTrinh",
                    "ATTENDANCE_ADJUSTMENT");
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RejectAdjustmentRequestAsync(
            int id, string reason, int rejectorUserId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Bắt buộc phải nhập lý do từ chối giải trình.");

            var req = await _context.AttendanceAdjustmentRequests.FindAsync(id);
            if (req == null) return (false, "Không tìm thấy đơn giải trình.");

            req.Status = "Rejected";
            req.ApprovedById = rejectorUserId;
            req.ApprovedAt = VietnamTime.Now;

            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == req.EmployeeId);
            if (linkedUser != null)
            {
                await _notificationService.SendToUserAsync(linkedUser.Id,
                    $"Đơn giải trình {req.RequestCode} đã bị từ chối",
                    $"Lý do: {reason}.",
                    "/DonTu/GiaiTrinh",
                    "ATTENDANCE_ADJUSTMENT");
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════

        private async Task SyncAttendanceOnApprovalAsync(LeaveRequest req)
        {
            var defaultShift = await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive) ??
                               new Shift { Id = 1, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 30, 0), WorkHours = 8.0m };

            for (var d = req.FromDate.Date; d <= req.ToDate.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Sunday) continue;

                var att = await _context.AttendanceDailies
                    .FirstOrDefaultAsync(a => a.EmployeeId == req.EmployeeId && a.WorkDate == d);

                if (att == null)
                {
                    att = new AttendanceDaily
                    {
                        EmployeeId = req.EmployeeId,
                        ShiftId = defaultShift.Id,
                        WorkDate = d,
                        CreatedAt = VietnamTime.Now
                    };
                    _context.AttendanceDailies.Add(att);
                }

                if (req.RequestType == "ANNUAL_LEAVE" || req.RequestType == "SICK_LEAVE")
                {
                    att.AttendanceStatus = "Leave";
                    att.WorkHourCount = req.TotalDays < 1.0m ? 4.0m : 8.0m;
                    att.LateMinutes = 0;
                    att.EarlyMinutes = 0;
                    att.Note = $"[Phép] {GetRequestTypeName(req.RequestType)} ({req.RequestCode}): {req.Reason}";
                }
                else if (req.RequestType == "UNPAID_LEAVE")
                {
                    att.AttendanceStatus = "Leave";
                    att.WorkHourCount = 0;
                    att.LateMinutes = 0;
                    att.EarlyMinutes = 0;
                    att.Note = $"[Nghỉ không lương] ({req.RequestCode}): {req.Reason}";
                }
                else if (req.RequestType == "BUSINESS_TRIP")
                {
                    att.AttendanceStatus = "BusinessTrip";
                    att.WorkHourCount = 8.0m;
                    att.LateMinutes = 0;
                    att.Note = $"[Công tác] ({req.RequestCode}): {req.Reason}";
                }
                else if (req.RequestType == "LATE_EARLY")
                {
                    att.LateMinutes = 0;
                    att.EarlyMinutes = 0;
                    att.AttendanceStatus = "Present";
                    att.Note = $"[Đi muộn/về sớm có phép] ({req.RequestCode})";
                }
                else if (req.RequestType == "OVERTIME")
                {
                    att.OvertimeHours += Math.Max(2.0m, req.TotalDays * 4.0m);
                    att.Note = $"[Đăng ký OT đã duyệt] ({req.RequestCode})";
                }
            }
        }

        private static string GetRequestTypeName(string type) => type switch
        {
            "ANNUAL_LEAVE" => "Nghỉ phép năm",
            "SICK_LEAVE" => "Nghỉ ốm / Khám bệnh",
            "LATE_EARLY" => "Đi muộn / Về sớm",
            "BUSINESS_TRIP" => "Đi công tác ngoại tỉnh",
            "UNPAID_LEAVE" => "Nghỉ không hưởng lương",
            "OVERTIME" => "Đăng ký làm thêm giờ (OT)",
            _ => type
        };
    }
}

