using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    // ═══════════════════════════════════════════════════════════════
    // 1. QUỸ PHÉP NĂM NHÂN VIÊN (LEAVE BALANCE)
    // ═══════════════════════════════════════════════════════════════
    [Table("EmployeeLeaveBalances")]
    public class EmployeeLeaveBalance : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public int LeaveTypeId { get; set; }

        /// <summary>Số ngày phép tiêu chuẩn trong năm (mặc định 12 ngày theo BLLĐ)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal StandardDays { get; set; } = 12.00m;

        /// <summary>Phép thâm niên (cộng 1 ngày cho mỗi 5 năm làm việc theo Đ114 BLLĐ)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal SeniorityBonusDays { get; set; } = 0.00m;

        /// <summary>Phép tồn năm trước chuyển sang</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal CarriedOverDays { get; set; } = 0.00m;

        /// <summary>Số ngày phép đã sử dụng trong năm</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal UsedDays { get; set; } = 0.00m;

        /// <summary>Số ngày phép còn lại (Standard + Seniority + CarriedOver - Used)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal RemainingDays { get; set; } = 12.00m;

        /// <summary>Hạn chót sử dụng ngày phép tồn (ví dụ 31/03 hàng năm)</summary>
        public DateTime? ExpiredCarryOverDate { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("LeaveTypeId")]
        public virtual LeaveType LeaveType { get; set; } = null!;
    }

    // ═══════════════════════════════════════════════════════════════
    // 2. KHÓA KỲ CHẤM CÔNG (ATTENDANCE LOCK)
    // ═══════════════════════════════════════════════════════════════
    [Table("AttendanceLocks")]
    public class AttendanceLock : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        /// <summary>Khóa theo chi nhánh cụ thể hoặc NULL nếu khóa toàn công ty</summary>
        public int? BranchId { get; set; }

        /// <summary>Khóa theo phòng ban cụ thể hoặc NULL nếu khóa toàn chi nhánh</summary>
        public int? DepartmentId { get; set; }

        public bool IsLocked { get; set; } = true;
        public DateTime LockedAt { get; set; } = VietnamTime.Now;
        public int LockedById { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime? UnlockedAt { get; set; }
        public int? UnlockedById { get; set; }

        [MaxLength(500)]
        public string? UnlockReason { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [ForeignKey("LockedById")]
        public virtual User LockedBy { get; set; } = null!;

        [ForeignKey("UnlockedById")]
        public virtual User? UnlockedBy { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 3. ĐƠN GIẢI TRÌNH CHẤM CÔNG (ATTENDANCE ADJUSTMENT REQUEST)
    // ═══════════════════════════════════════════════════════════════
    [Table("AttendanceAdjustmentRequests")]
    public class AttendanceAdjustmentRequest : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RequestCode { get; set; } = string.Empty; // AR-2611-001

        public int EmployeeId { get; set; }
        public DateTime WorkDate { get; set; }
        public int? ShiftId { get; set; }

        /// <summary>MissedCheckIn (Quên vào), MissedCheckOut (Quên ra), Both (Quên cả hai), SystemError (Lỗi máy), BusinessTrip (Công tác)</summary>
        [Required, MaxLength(50)]
        public string AdjustmentType { get; set; } = "MissedCheckIn";

        public TimeSpan? ActualInTime { get; set; }
        public TimeSpan? ActualOutTime { get; set; }

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        /// <summary>Pending, Approved, Rejected</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? ApprovalNote { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 4. QUYẾT ĐỊNH NHÂN SỰ ĐIỆN TỬ (HR DECISION)
    // ═══════════════════════════════════════════════════════════════
    [Table("HrDecisions")]
    public class HrDecision : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string DecisionNumber { get; set; } = string.Empty; // QD-2026-001

        public int EmployeeId { get; set; }

        /// <summary>Appointment (Bổ nhiệm), Transfer (Điều chuyển), SalaryAdjustment (Điều chỉnh lương), Reward (Khen thưởng), Discipline (Kỷ luật), Termination (Chấm dứt HĐLĐ)</summary>
        [Required, MaxLength(50)]
        public string DecisionType { get; set; } = "Appointment";

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public DateTime EffectiveDate { get; set; }
        public DateTime SignDate { get; set; } = VietnamTime.Today;

        public int? OldDepartmentId { get; set; }
        public int? NewDepartmentId { get; set; }
        public int? OldPositionId { get; set; }
        public int? NewPositionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NewSalary { get; set; }

        /// <summary>Số tiền thưởng hoặc tiền phạt (tự động cộng/trừ vào kỳ tính lương)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        [MaxLength(1000)]
        public string? Reason { get; set; }

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        /// <summary>Draft, Issued, Cancelled</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Issued";

        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(150)]
        public string? SignerName { get; set; }

        [MaxLength(150)]
        public string? SignerTitle { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("OldDepartmentId")]
        public virtual Department? OldDepartment { get; set; }

        [ForeignKey("NewDepartmentId")]
        public virtual Department? NewDepartment { get; set; }

        [ForeignKey("OldPositionId")]
        public virtual Position? OldPosition { get; set; }

        [ForeignKey("NewPositionId")]
        public virtual Position? NewPosition { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 5. QUẢN LÝ TÀI SẢN DOANH NGHIỆP & CẤP PHÁT (COMPANY ASSET)
    // ═══════════════════════════════════════════════════════════════
    [Table("CompanyAssets")]
    public class CompanyAsset : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string AssetCode { get; set; } = string.Empty; // AST-LAP-001

        [Required, MaxLength(200)]
        public string AssetName { get; set; } = string.Empty;

        /// <summary>Laptop, Monitor, AccessCard, Uniform, Mobile, Other</summary>
        [Required, MaxLength(50)]
        public string AssetType { get; set; } = "Laptop";

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [MaxLength(1000)]
        public string? Specification { get; set; }

        public DateTime? PurchaseDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; } = 0;

        public DateTime? WarrantyExpiryDate { get; set; }

        /// <summary>Available (Trong kho), Assigned (Đang cấp phát), UnderRepair (Đang sửa), Liquidated (Đã thanh lý), Lost (Báo mất)</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Available";

        public int? BranchId { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<EmployeeAssetAssignment> Assignments { get; set; } = new List<EmployeeAssetAssignment>();
    }

    [Table("EmployeeAssetAssignments")]
    public class EmployeeAssetAssignment : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int AssetId { get; set; }
        public int EmployeeId { get; set; }

        public DateTime AssignedDate { get; set; } = VietnamTime.Today;

        [MaxLength(300)]
        public string? ConditionOnAssign { get; set; }

        [MaxLength(500)]
        public string? HandoverDocumentUrl { get; set; }

        public DateTime? ReturnedDate { get; set; }

        [MaxLength(300)]
        public string? ConditionOnReturn { get; set; }

        [MaxLength(500)]
        public string? ReturnHandoverDocumentUrl { get; set; }

        /// <summary>Active (Đang dùng), Returned (Đã trả), Lost (Mất), Damaged (Hư hỏng)</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Active";

        public int? AssignedById { get; set; }
        public int? ReturnedById { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("AssetId")]
        public virtual CompanyAsset Asset { get; set; } = null!;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("AssignedById")]
        public virtual User? AssignedBy { get; set; }

        [ForeignKey("ReturnedById")]
        public virtual User? ReturnedBy { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 6. QUY TRÌNH HỘI NHẬP & BÀN GIAO NGHỈ VIỆC (ONBOARDING / OFFBOARDING)
    // ═══════════════════════════════════════════════════════════════
    [Table("OnboardingChecklists")]
    public class OnboardingChecklist : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        /// <summary>Onboarding (Hội nhập nhân viên mới), Offboarding (Thủ tục nghỉ việc / bàn giao)</summary>
        [Required, MaxLength(50)]
        public string Type { get; set; } = "Onboarding";

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = VietnamTime.Today;
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        /// <summary>Pending, InProgress, Completed, Cancelled</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "InProgress";

        public int? AssignedHrId { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("AssignedHrId")]
        public virtual User? AssignedHr { get; set; }

        public virtual ICollection<OnboardingTask> Tasks { get; set; } = new List<OnboardingTask>();
    }

    [Table("OnboardingTasks")]
    public class OnboardingTask : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int ChecklistId { get; set; }

        [Required, MaxLength(250)]
        public string TaskName { get; set; } = string.Empty;

        /// <summary>HR, IT, DepartmentHead, Employee, Accountant</summary>
        [Required, MaxLength(50)]
        public string DepartmentRole { get; set; } = "HR";

        public int? AssignedToUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        /// <summary>Pending, InProgress, Completed, Skipped</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string? Note { get; set; }

        public int OrderIndex { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("ChecklistId")]
        public virtual OnboardingChecklist Checklist { get; set; } = null!;

        [ForeignKey("AssignedToUserId")]
        public virtual User? AssignedToUser { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // 7. LỊCH PHỎNG VẤN ỨNG VIÊN (INTERVIEW SCHEDULE)
    // ═══════════════════════════════════════════════════════════════
    [Table("InterviewSchedules")]
    public class InterviewSchedule : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int CandidateId { get; set; }
        public int RoundNumber { get; set; } = 1;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime ScheduledTime { get; set; }
        public int DurationMinutes { get; set; } = 45;

        /// <summary>Online, Offline</summary>
        [Required, MaxLength(50)]
        public string InterviewType { get; set; } = "Offline";

        [MaxLength(500)]
        public string? LocationOrMeetingLink { get; set; }

        public int? InterviewerEmployeeId { get; set; }
        public int? InterviewerUserId { get; set; }

        /// <summary>Scheduled, Completed, Cancelled, NoShow</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Scheduled";

        /// <summary>Điểm đánh giá thang điểm 0 - 100</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Score { get; set; }

        public string? Feedback { get; set; }

        /// <summary>Passed, Failed, Pending, Consider</summary>
        [Required, MaxLength(50)]
        public string Result { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("CandidateId")]
        public virtual Candidate Candidate { get; set; } = null!;

        [ForeignKey("InterviewerEmployeeId")]
        public virtual Employee? InterviewerEmployee { get; set; }

        [ForeignKey("InterviewerUserId")]
        public virtual User? InterviewerUser { get; set; }
    }
}

