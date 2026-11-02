using System.ComponentModel.DataAnnotations;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Models.ViewModels
{
    // --- 1. VIEWMODELS CHO NHÂN VIÊN ---
    public class EmployeeListViewModel
    {
        public List<Employee> Employees { get; set; } = new();
        public string? Keyword { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public string? Status { get; set; }
        public int TotalActive { get; set; }
        public int TotalOnLeave { get; set; }
        public int TotalResigned { get; set; }
    }

    public class EmployeeCreateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên")]
        [Display(Name = "Mã nhân viên")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public string? Gender { get; set; } = "Nam";

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email công ty")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Số CCCD / CMND")]
        public string? IdentityCard { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng ban")]
        [Display(Name = "Phòng ban")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chức danh")]
        [Display(Name = "Chức vụ")]
        public int PositionId { get; set; }

        [Display(Name = "Trạng thái làm việc")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Ngày vào làm")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.Today;

        [Display(Name = "Đường dẫn ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        // Thông tin mở rộng (EmployeeDetails)
        [Display(Name = "Tên người liên hệ khẩn cấp")]
        public string? EmergencyContactName { get; set; }

        [Display(Name = "SĐT người liên hệ khẩn cấp")]
        public string? EmergencyContactPhone { get; set; }

        [Display(Name = "Số tài khoản ngân hàng")]
        public string? BankAccountNumber { get; set; }

        [Display(Name = "Tên ngân hàng")]
        public string? BankName { get; set; }

        [Display(Name = "Chi nhánh ngân hàng")]
        public string? BankBranch { get; set; }

        [Display(Name = "Địa chỉ hiện tại")]
        public string? CurrentAddress { get; set; }

        [Display(Name = "Địa chỉ thường trú")]
        public string? PermanentAddress { get; set; }

        [Display(Name = "Mã số thuế cá nhân")]
        public string? TaxIdentificationNumber { get; set; }

        [Display(Name = "Số sổ bảo hiểm xã hội")]
        public string? SocialInsuranceNumber { get; set; }

        [Display(Name = "Trình độ học vấn")]
        public string? EducationLevel { get; set; }

        // Tùy chọn cấp tài khoản User đăng nhập
        [Display(Name = "Cấp tài khoản đăng nhập hệ thống ngay")]
        public bool CreateUserAccount { get; set; } = false;

        [Display(Name = "Tên tài khoản (để trống sẽ lấy theo Mã NV)")]
        public string? Username { get; set; }

        [Display(Name = "Mật khẩu khởi tạo (tối thiểu 8 ký tự, gồm chữ và số)")]
        public string DefaultPassword { get; set; } = string.Empty;

        public bool HasUserAccount { get; set; }
    }

    // --- 2. VIEWMODELS CHO HỢP ĐỒNG ---
    public class ContractListViewModel
    {
        public List<Contract> Contracts { get; set; } = new();
        public List<Contract> ExpiringContracts { get; set; } = new(); // Sắp hết hạn trong 30 ngày
        public string? Keyword { get; set; }
        public string? Status { get; set; }
    }

    // --- 3. VIEWMODELS CHO CHẤM CÔNG ---
    public class AttendanceDailyIndexViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public int? DepartmentId { get; set; }
        public List<AttendanceDaily> AttendanceList { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();
    }

    public class MonthlyTimesheetViewModel
    {
        public int Year { get; set; } = DateTime.Today.Year;
        public int Month { get; set; } = DateTime.Today.Month;
        public int? DepartmentId { get; set; }
        public List<MonthlyEmployeeRowDto> Rows { get; set; } = new();
        public int DaysInMonth { get; set; }
    }

    public class MonthlyEmployeeRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public Dictionary<int, string> DailyStatuses { get; set; } = new(); // Ngày trong tháng -> Mã trạng thái (P: Present, L: Late, A: Absent...)
        public decimal TotalWorkHours { get; set; }
        public int TotalLateMinutes { get; set; }
        public int PresentDaysCount { get; set; }
    }

    public class QuickCheckInDto
    {
        public int EmployeeId { get; set; }
        public int ShiftId { get; set; }
        public string ActionType { get; set; } = "CheckIn"; // CheckIn / CheckOut
    }

    // --- 4. VIEWMODELS CHO HEATMAP CHẤM CÔNG & PHÂN TÍCH NHÂN SỰ ---
    public class AttendanceHeatmapViewModel
    {
        public int Year { get; set; } = DateTime.Today.Year;
        public int Month { get; set; } = DateTime.Today.Month;
        public int? DepartmentId { get; set; }
        public int DaysInMonth { get; set; }
        public List<EmployeeHeatmapRowDto> EmployeeRows { get; set; } = new();

        // Thống kê tổng quan tháng
        public int TotalPresentDays { get; set; }
        public int TotalLateDays { get; set; }
        public int TotalAbsentDays { get; set; }
        public int TotalLeaveDays { get; set; }
        public decimal OnTimePercentage { get; set; }
    }

    public class EmployeeHeatmapRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public Dictionary<int, HeatmapCellDto> DayCells { get; set; } = new(); // 1..DaysInMonth
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public decimal TotalOT { get; set; }
    }

    public class HeatmapCellDto
    {
        public int Day { get; set; }
        public string Status { get; set; } = "Off"; // Present, Late, Early, Absent, Leave, BusinessTrip, Off
        public string Tooltip { get; set; } = string.Empty;
        public string ColorClass { get; set; } = "bg-secondary";
        public string IconClass { get; set; } = "";
        public decimal WorkHours { get; set; }
        public decimal OvertimeHours { get; set; }
    }

    public class HrAnalyticsViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int ResignedEmployees { get; set; }
        public decimal TurnoverRate { get; set; } // % biến động nhân sự
        public int NewHiresThisYear { get; set; }

        // Cơ cấu độ tuổi
        public int AgeUnder25 { get; set; }
        public int Age25To35 { get; set; }
        public int Age36To45 { get; set; }
        public int AgeOver45 { get; set; }

        // Cơ cấu thâm niên
        public int SeniorityUnder1Year { get; set; }
        public int Seniority1To3Years { get; set; }
        public int Seniority3To5Years { get; set; }
        public int SeniorityOver5Years { get; set; }

        // Cơ cấu giới tính
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }

        // Cơ cấu phòng ban
        public Dictionary<string, int> DepartmentDistribution { get; set; } = new();
    }
}
