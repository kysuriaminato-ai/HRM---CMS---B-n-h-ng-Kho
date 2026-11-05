using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IShiftScheduleService
    {
        Task<List<Shift>> GetAllShiftsAsync();
        Task<Shift?> GetShiftByIdAsync(int id);
        Task<(bool Success, string? Error, int Id)> CreateShiftAsync(Shift shift, int? currentUserId);
        Task<(bool Success, string? Error)> UpdateShiftAsync(int id, Shift shift, int? currentUserId);
        Task<(bool Success, string? Error)> DeleteShiftAsync(int id);
        Task<WeeklyRosterDto> GetWeeklyRosterAsync(DateTime weekStartDate, int? departmentId, int? branchId);
        Task<(bool Success, string? Error)> AssignShiftAsync(int employeeId, DateTime date, int shiftId, int? currentUserId);
        Task<(bool Success, string? Error, int Count)> BatchAssignShiftAsync(List<int> employeeIds, DateTime fromDate, DateTime toDate, int shiftId, bool skipWeekends, int? currentUserId);
        Task<(bool Success, string? Error, int CopiedCount)> CopyWeekScheduleAsync(DateTime sourceWeekStart, DateTime targetWeekStart, int? departmentId, int? branchId, int? currentUserId);
        Task<List<SelectListItem>> GetShiftSelectListAsync(int? selectedId = null);
    }

    public class WeeklyRosterDto
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public List<DateTime> DaysOfWeek { get; set; } = new();
        public List<RosterEmployeeRowDto> EmployeeRows { get; set; } = new();
        public List<Shift> AvailableShifts { get; set; } = new();
    }

    public class RosterEmployeeRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public Dictionary<int, RosterCellDto> DailyShifts { get; set; } = new(); // Key: Day 1-7
    }

    public class RosterCellDto
    {
        public DateTime Date { get; set; }
        public int DayIndex { get; set; } // 1 (Mon) - 7 (Sun)
        public int? ShiftId { get; set; }
        public string? ShiftCode { get; set; }
        public string? ShiftName { get; set; }
        public string? ShiftTime { get; set; }
        public bool IsOvernight { get; set; }
        public bool IsWeekend { get; set; }
        public string ColorClass { get; set; } = "shift-default";
    }
}

