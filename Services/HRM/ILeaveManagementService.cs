using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface ILeaveManagementService
    {
        // === QUỸ PHÉP NĂM ===
        Task<PaginatedList<LeaveBalanceListItemDto>> GetLeaveBalancesPagedAsync(LeaveBalanceFilterDto filter);
        Task<EmployeeLeaveBalance?> GetEmployeeLeaveBalanceAsync(int employeeId, int year);
        decimal CalculateSeniorityBonus(DateTime hireDate, int targetYear);
        Task<(bool Success, string? Error, int Id)> InitializeOrRecalculateBalanceAsync(int employeeId, int year, int? currentUserId);
        Task<(bool Success, string? Error, int ProcessedCount)> RecalculateAllBalancesAsync(int year, int? currentUserId);

        // === ĐƠN TỪ ĐA CẤP ===
        Task<PaginatedList<LeaveRequestListItemDto>> GetLeaveRequestsPagedAsync(LeaveRequestFilterDto filter);
        Task<LeaveRequest?> GetLeaveRequestByIdAsync(int id);
        Task<LeaveRequestSummaryKpiDto> GetSummaryKpisAsync();
        Task<(bool Success, string? Error, int Id)> CreateLeaveRequestAsync(LeaveRequest request, int currentUserId);
        Task<(bool Success, string? Error)> ApproveLeaveRequestAsync(int id, string level, string? note, int approverUserId);
        Task<(bool Success, string? Error)> RejectLeaveRequestAsync(int id, string reason, int rejectorUserId);
        Task<(int SuccessCount, int FailCount, List<string> Errors)> BatchApproveAsync(List<int> ids, string level, string? note, int approverUserId);
        Task<List<LeaveType>> GetLeaveTypesAsync();

        // === ĐƠN GIẢI TRÌNH CHẤM CÔNG ===
        Task<PaginatedList<AttendanceAdjustmentRequest>> GetAdjustmentRequestsPagedAsync(AdjustmentFilterDto filter);
        Task<(bool Success, string? Error, int Id)> CreateAdjustmentRequestAsync(AttendanceAdjustmentRequest request, int currentUserId);
        Task<(bool Success, string? Error)> ApproveAdjustmentRequestAsync(int id, int approvedByUserId, string? note);
        Task<(bool Success, string? Error)> RejectAdjustmentRequestAsync(int id, string reason, int rejectorUserId);
    }

    public class LeaveBalanceFilterDto
    {
        public int Year { get; set; } = DateTime.Today.Year;
        public int? DepartmentId { get; set; }
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class LeaveBalanceListItemDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public int CompletedYearsOfService { get; set; }
        public int Year { get; set; }
        public decimal StandardDays { get; set; }
        public decimal SeniorityBonusDays { get; set; }
        public decimal CarriedOverDays { get; set; }
        public decimal TotalEntitledDays => StandardDays + SeniorityBonusDays + CarriedOverDays;
        public decimal UsedDays { get; set; }
        public decimal RemainingDays { get; set; }
    }

    public class LeaveRequestFilterDto
    {
        public string? Status { get; set; }
        public string? Type { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class LeaveRequestListItemDto
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string RequestTypeName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DepartmentHeadName { get; set; }
        public DateTime? DepartmentHeadApprovedAt { get; set; }
        public string? DirectorName { get; set; }
        public DateTime? DirectorApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeaveRequestSummaryKpiDto
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int DeptApprovedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public class AdjustmentFilterDto
    {
        public string? Status { get; set; }
        public string? AdjustmentType { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? WorkDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

