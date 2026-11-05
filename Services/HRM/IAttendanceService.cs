using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IAttendanceService
    {
        Task<PaginatedList<AttendanceDaily>> GetDailyAttendancePagedAsync(AttendanceDailyFilterDto filter);
        Task<(bool Success, string? Error)> QuickCheckInAsync(QuickCheckInDto dto, int? currentUserId);
        Task<MonthlyTimesheetViewModel> GetMonthlyTimesheetAsync(int year, int month, int? departmentId, int? branchId);
        Task<DayAttendanceDetailDto?> GetDayAttendanceDetailAsync(int employeeId, DateTime date);
        Task<byte[]> ExportMonthlyTimesheetToExcelAsync(int year, int month, int? departmentId, int? branchId);
        Task<AttendanceImportResultDto> ImportAttendanceLogAsync(Stream fileStream, string fileName, int? defaultShiftId, int? currentUserId);

        // Khóa kỳ công
        Task<bool> IsPeriodLockedAsync(int year, int month, int? branchId = null, int? departmentId = null);
        Task<List<AttendanceLockDto>> GetAttendanceLocksAsync(int year);
        Task<(bool Success, string? Error, int LockId)> LockPeriodAsync(int year, int month, int? branchId, int? departmentId, int lockedByUserId, string? note);
        Task<(bool Success, string? Error)> UnlockPeriodAsync(int lockId, int unlockedByUserId, string reason);
    }

    public class AttendanceDailyFilterDto
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public string? Status { get; set; }
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
    }

    public class DayAttendanceDetailDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public int? ShiftId { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public string ShiftHours { get; set; } = string.Empty;
        public TimeSpan? FirstInTime { get; set; }
        public TimeSpan? LastOutTime { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
        public decimal WorkHourCount { get; set; }
        public decimal OvertimeHours { get; set; }
        public string AttendanceStatus { get; set; } = "Present";
        public string? Note { get; set; }
        public string? RelatedLeaveInfo { get; set; }
        public string? RelatedAdjustmentInfo { get; set; }
    }

    public class AttendanceImportResultDto
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
    }

    public class AttendanceLockDto
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsLocked { get; set; }
        public DateTime LockedAt { get; set; }
        public string LockedByName { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public string? UnlockedByName { get; set; }
        public string? UnlockReason { get; set; }
    }
}

