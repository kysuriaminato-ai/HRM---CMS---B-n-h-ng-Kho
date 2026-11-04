using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP_HRM_CMS.Models.Enums;

namespace ERP_HRM_CMS.Models.Entities
{
    // ==========================================
    // 1. QUẢN LÝ ĐƠN TỪ TRỰC TUYẾN (LEAVE REQUESTS)
    // ==========================================
    [Table("LeaveRequests")]
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RequestCode { get; set; } = string.Empty; // LR-2610-001

        public int EmployeeId { get; set; }

        [Required, MaxLength(50)]
        public string RequestType { get; set; } = "ANNUAL_LEAVE"; 
        // Types: ANNUAL_LEAVE (Nghỉ phép năm), SICK_LEAVE (Nghỉ ốm), LATE_EARLY (Đi muộn/về sớm), 
        // BUSINESS_TRIP (Công tác), UNPAID_LEAVE (Nghỉ không lương), OVERTIME (Làm thêm giờ)

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalDays { get; set; } = 1.0m;

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        [Required, MaxLength(50)]
        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.PendingApproval; 
        // Statuses: CHO_DUYET (Chờ duyệt), TRUONG_PHONG_DUYET (Trưởng phòng duyệt), GIAM_DOC_DUYET (Hoàn tất/Giám đốc duyệt), TU_CHOI (Bị từ chối)

        public int? DepartmentHeadId { get; set; }
        public DateTime? DepartmentHeadApprovedAt { get; set; }
        [MaxLength(500)]
        public string? DepartmentHeadNote { get; set; }

        public int? DirectorId { get; set; }
        public DateTime? DirectorApprovedAt { get; set; }
        [MaxLength(500)]
        public string? DirectorNote { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("DepartmentHeadId")]
        public virtual User? DepartmentHead { get; set; }

        [ForeignKey("DirectorId")]
        public virtual User? Director { get; set; }
    }

    // ==========================================
    // 2. KỲ LƯƠNG & BẢNG LƯƠNG (PAYROLL ENGINE)
    // ==========================================
    [Table("PayrollPeriods")]
    public class PayrollPeriod
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty; // Bảng lương Tháng 10/2026

        public int Month { get; set; }
        public int Year { get; set; }

        public int StandardWorkDays { get; set; } = 22; // Số ngày công chuẩn tháng

        [Required, MaxLength(50)]
        public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Draft; 
        // Statuses: DRAFT (Dự thảo), CALCULATED (Đã tính toán), LOCKED (Đã chốt lương), PAID (Đã giải ngân)

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGrossSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalNetSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInsurance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPIT { get; set; } = 0; // Thuế TNCN

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEmployerInsurance { get; set; } = 0; // BHXH, BHYT, BHTN, TNLĐ-BNN phần người sử dụng lao động

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;
        public DateTime? LockedAt { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        public virtual ICollection<SalarySlip> SalarySlips { get; set; } = new List<SalarySlip>();
    }

    [Table("SalarySlips")]
    public class SalarySlip
    {
        [Key]
        public int Id { get; set; }

        public int PayrollPeriodId { get; set; }
        public int EmployeeId { get; set; }

        // Tiền lương cơ bản & Ngày công
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; } = 0; // Lương hợp đồng

        [Column(TypeName = "decimal(18,2)")]
        public decimal InsuranceSalary { get; set; } = 0; // Lương đóng bảo hiểm

        public int StandardDays { get; set; } = 22;

        [Column(TypeName = "decimal(5,2)")]
        public decimal ActualWorkDays { get; set; } = 0; // Số ngày công thực tế chấm được

        [Column(TypeName = "decimal(5,2)")]
        public decimal PaidLeaveDays { get; set; } = 0; // Số ngày nghỉ phép hưởng lương (đã duyệt)

        [Column(TypeName = "decimal(5,2)")]
        public decimal UnpaidLeaveDays { get; set; } = 0; // Nghỉ không lương

        [Column(TypeName = "decimal(5,2)")]
        public decimal OvertimeHours { get; set; } = 0; // Số giờ làm thêm (OT)

        // Các khoản thu nhập
        [Column(TypeName = "decimal(18,2)")]
        public decimal StandardWorkSalary { get; set; } = 0; // Lương tính theo ngày công thực tế + phép

        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeSalary { get; set; } = 0; // Tiền OT (150%)

        [Column(TypeName = "decimal(18,2)")]
        public decimal PositionAllowance { get; set; } = 0; // Phụ cấp chức vụ

        [Column(TypeName = "decimal(18,2)")]
        public decimal LunchAllowance { get; set; } = 0; // Phụ cấp ăn trưa

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherAllowance { get; set; } = 0; // Phụ cấp xăng xe, điện thoại

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bonus { get; set; } = 0; // Thưởng hiệu suất / KPI

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossSalary { get; set; } = 0; // Tổng thu nhập trước thuế & BH

        // Các khoản khấu trừ
        [Column(TypeName = "decimal(18,2)")]
        public decimal LatePenalty { get; set; } = 0; // Phạt đi muộn không phép

        [Column(TypeName = "decimal(18,2)")]
        public decimal SocialInsurance { get; set; } = 0; // BHXH 8%

        [Column(TypeName = "decimal(18,2)")]
        public decimal HealthInsurance { get; set; } = 0; // BHYT 1.5%

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnemploymentInsurance { get; set; } = 0; // BHTN 1%

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInsurance { get; set; } = 0; // Tổng BH 10.5%

        // Phần người sử dụng lao động đóng (không trừ vào lương nhân viên, dùng cho bảng kê trích nộp)
        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployerSocialInsurance { get; set; } = 0; // BHXH: hưu trí tử tuất 14% + ốm đau thai sản 3%

        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployerHealthInsurance { get; set; } = 0; // BHYT 3%

        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployerUnemploymentInsurance { get; set; } = 0; // BHTN 1%

        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployerAccidentInsurance { get; set; } = 0; // TNLĐ-BNN 0,5%

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEmployerInsurance { get; set; } = 0; // Tổng phần người sử dụng lao động

        // Giảm trừ thuế TNCN
        public int DependentCount { get; set; } = 0; // Số người phụ thuộc được giảm trừ trong kỳ

        [Column(TypeName = "decimal(18,2)")]
        public decimal PersonalDeduction { get; set; } = 0; // Giảm trừ gia cảnh bản thân

        [Column(TypeName = "decimal(18,2)")]
        public decimal DependentDeduction { get; set; } = 0; // Giảm trừ người phụ thuộc (đã nhân số người)

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxExemptAllowance { get; set; } = 0; // Phụ cấp được miễn thuế (theo hạn mức)

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableIncome { get; set; } = 0; // Thu nhập tính thuế TNCN

        [Column(TypeName = "decimal(18,2)")]
        public decimal PersonalIncomeTax { get; set; } = 0; // Thuế TNCN lũy tiến

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; } = 0; // Thực lĩnh

        [Required, MaxLength(50)]
        public SalarySlipStatus Status { get; set; } = SalarySlipStatus.Draft; // DRAFT, SENT (Đã gửi cho NV), CONFIRMED (NV đã xác nhận)

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("PayrollPeriodId")]
        public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }
}
