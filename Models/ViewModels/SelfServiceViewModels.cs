using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ERP_HRM_CMS.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace ERP_HRM_CMS.Models.ViewModels
{
    // ═══════════════════════════════════════════════════════════════
    // DASHBOARD TỔNG QUAN CỔNG TỰ PHỤC VỤ NHÂN VIÊN (ESS PORTAL)
    // ═══════════════════════════════════════════════════════════════

    public class SelfServiceDashboardViewModel
    {
        public EmployeeProfileSummaryDto Profile { get; set; } = new();
        public TodayCheckInStatusDto TodayCheckIn { get; set; } = new();
        public AttendanceMonthSummaryDto AttendanceSummary { get; set; } = new();
        public LeaveBalanceSummaryDto LeaveBalance { get; set; } = new();
        public List<PersonalLeaveRequestDto> LeaveRequests { get; set; } = new();
        public List<PersonalSalarySlipSummaryDto> SalarySlips { get; set; } = new();
        public List<PersonalAssetDto> Assets { get; set; } = new();
        public List<PersonalContractDto> Contracts { get; set; } = new();
        public List<PersonalTrainingDto> Trainings { get; set; } = new();
        public List<PersonalKpiDto> Kpis { get; set; } = new();

        public bool IsPayslipUnlocked { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string ActiveTab { get; set; } = "overview";
    }

    public class EmployeeProfileSummaryDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public int SeniorityYears { get; set; }
        public string Status { get; set; } = "Active";

        // Chi tiết cá nhân
        public string? CitizenId { get; set; }
        public string? TaxCode { get; set; }
        public string? SocialInsuranceNumber { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? CurrentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 1. WIDGET CHẤM CÔNG TRỰC TUYẾN (WEB CHECK-IN / CHECK-OUT)
    // ═══════════════════════════════════════════════════════════

    public class TodayCheckInStatusDto
    {
        public bool HasShiftToday { get; set; } = true;
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } = "Ca hành chính";
        public TimeSpan ShiftStartTime { get; set; } = new TimeSpan(8, 0, 0);
        public TimeSpan ShiftEndTime { get; set; } = new TimeSpan(17, 30, 0);
        public int GracePeriodMinutes { get; set; } = 15;

        public bool HasCheckedIn { get; set; }
        public TimeSpan? CheckInTime { get; set; }

        public bool HasCheckedOut { get; set; }
        public TimeSpan? CheckOutTime { get; set; }

        public string StatusText { get; set; } = "Chưa chấm công";
        public string BadgeClass { get; set; } = "badge-neutral";
        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
        public decimal WorkHours { get; set; }
        public decimal OvertimeHours { get; set; }

        public string? ClientIp { get; set; }
        public string? DeviceName { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 2. LỊCH CHẤM CÔNG & THỐNG KÊ THÁNG
    // ═══════════════════════════════════════════════════════════════

    public class PersonalDayAttendanceDto
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public string DayOfWeekName { get; set; } = string.Empty; // T2, T3, ..., CN
        public bool IsWeekend { get; set; }
        public bool IsFuture { get; set; }

        public TimeSpan? FirstInTime { get; set; }
        public TimeSpan? LastOutTime { get; set; }

        public string StatusCode { get; set; } = "-"; // X, P, M, OT, KL, -
        public string StatusText { get; set; } = "Nghỉ";
        public string BadgeClass { get; set; } = "badge-neutral";

        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
        public decimal WorkHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public string ShiftName { get; set; } = "Ca hành chính";

        /// <summary>Có thể gửi giải trình (quên chấm công hoặc đi muộn trên ngày làm việc quá khứ)</summary>
        public bool CanAdjust { get; set; }
    }

    public class AttendanceMonthSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal StandardDays { get; set; } = 22;
        public decimal ActualWorkDays { get; set; }
        public decimal PaidLeaveDays { get; set; }
        public int LateCount { get; set; }
        public int EarlyCount { get; set; }
        public int TotalLateMinutes { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal TotalWorkHours { get; set; }

        public List<PersonalDayAttendanceDto> Days { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════
    // 3. QUỸ PHÉP & ĐƠN TỪ CÁ NHÂN
    // ═══════════════════════════════════════════════════════════════

    public class LeaveBalanceSummaryDto
    {
        public int Year { get; set; }
        public decimal StandardDays { get; set; } = 12;
        public decimal SeniorityBonusDays { get; set; }
        public decimal CarriedOverDays { get; set; }
        public decimal TotalDays => StandardDays + SeniorityBonusDays + CarriedOverDays;
        public decimal UsedDays { get; set; }
        public decimal RemainingDays => Math.Max(0, TotalDays - UsedDays);
        public decimal UsedPercent => TotalDays > 0 ? Math.Min(100, Math.Round((UsedDays / TotalDays) * 100, 1)) : 0;
    }

    public class PersonalLeaveRequestDto
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string RequestType { get; set; } = "ANNUAL_LEAVE";
        public string RequestTypeName { get; set; } = "Nghỉ phép năm";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public LeaveRequestStatus Status { get; set; }
        public string StatusName { get; set; } = "Chờ duyệt";
        public string StatusBadgeClass { get; set; } = "badge-warning";
        public DateTime CreatedAt { get; set; }

        public string? DepartmentHeadName { get; set; }
        public DateTime? DepartmentHeadApprovedAt { get; set; }
        public string? DepartmentHeadNote { get; set; }

        public string? DirectorName { get; set; }
        public DateTime? DirectorApprovedAt { get; set; }
        public string? DirectorNote { get; set; }

        /// <summary>Chỉ được hủy khi đang chờ Trưởng phòng duyệt</summary>
        public bool CanCancel => Status == LeaveRequestStatus.PendingApproval;
    }

    public class PersonalLeaveRequestCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn loại đơn")]
        public string RequestType { get; set; } = "ANNUAL_LEAVE";

        [Required(ErrorMessage = "Vui lòng chọn từ ngày")]
        public DateTime FromDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn đến ngày")]
        public DateTime ToDate { get; set; } = DateTime.Today;

        public decimal TotalDays { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng nhập lý do")]
        [MaxLength(500, ErrorMessage = "Lý do tối đa 500 ký tự")]
        public string Reason { get; set; } = string.Empty;

        public IFormFile? AttachmentFile { get; set; }
    }

    public class PersonalAdjustmentCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn ngày cần giải trình")]
        public DateTime WorkDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn loại giải trình")]
        public string AdjustmentType { get; set; } = "MissedCheckIn";

        public TimeSpan? ActualInTime { get; set; }
        public TimeSpan? ActualOutTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do giải trình")]
        [MaxLength(500, ErrorMessage = "Lý do tối đa 500 ký tự")]
        public string Reason { get; set; } = string.Empty;

        public IFormFile? AttachmentFile { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 4. TRA CỨU PHIẾU LƯƠNG BẢO MẬT (SECURE PAYSLIP VIEWER)
    // ═══════════════════════════════════════════════════════════════

    public class PersonalSalarySlipSummaryDto
    {
        public int Id { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public int StandardDays { get; set; }
        public decimal ActualWorkDays { get; set; }
        public decimal NetSalary { get; set; }
        public SalarySlipStatus Status { get; set; }
        public string StatusName { get; set; } = "Đã phát hành";
        public DateTime CreatedAt { get; set; }
    }

    public class PersonalSalarySlipDetailDto
    {
        public int Id { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }

        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;

        // Ngày công
        public int StandardDays { get; set; }
        public decimal ActualWorkDays { get; set; }
        public decimal PaidLeaveDays { get; set; }
        public decimal UnpaidLeaveDays { get; set; }
        public decimal OvertimeHours { get; set; }

        // Thu nhập
        public decimal BasicSalary { get; set; }
        public decimal InsuranceSalary { get; set; }
        public decimal StandardWorkSalary { get; set; }
        public decimal OvertimeSalary { get; set; }
        public decimal PositionAllowance { get; set; }
        public decimal LunchAllowance { get; set; }
        public decimal OtherAllowance { get; set; }
        public decimal TotalAllowance => PositionAllowance + LunchAllowance + OtherAllowance;
        public decimal Bonus { get; set; }
        public decimal GrossSalary { get; set; }

        // Khấu trừ
        public decimal LatePenalty { get; set; }
        public decimal SocialInsurance { get; set; }
        public decimal HealthInsurance { get; set; }
        public decimal UnemploymentInsurance { get; set; }
        public decimal TotalInsurance { get; set; }

        // Giảm trừ & Thuế TNCN
        public int DependentCount { get; set; }
        public decimal PersonalDeduction { get; set; }
        public decimal DependentDeduction { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal PersonalIncomeTax { get; set; }

        // Thực lĩnh
        public decimal NetSalary { get; set; }
        public string Status { get; set; } = "Sent";
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VerifyPasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string Password { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════
    // 5. TÀI SẢN, HỢP ĐỒNG, ĐÀO TẠO & KPI
    // ═══════════════════════════════════════════════════════════════

    public class PersonalAssetDto
    {
        public int Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public DateTime AssignedDate { get; set; }
        public string? ConditionOnAssign { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class PersonalContractDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal BasicSalary { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class PersonalTrainingDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? TrainerName { get; set; }
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Planned";
    }

    public class PersonalKpiDto
    {
        public int Id { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public decimal TotalScore { get; set; }
        public string? Grade { get; set; }
        public string? OverallComment { get; set; }
        public string Status { get; set; } = "Draft";
    }

    public class PersonalContactUpdateDto
    {
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string? CurrentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    public class PersonalDependentRegisterDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người phụ thuộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn mối quan hệ")]
        public string Relationship { get; set; } = "CON";

        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu giảm trừ")]
        public DateTime FromDate { get; set; } = DateTime.Today;

        public string? IdCardNumber { get; set; }
        public string? TaxCode { get; set; }
    }
}

