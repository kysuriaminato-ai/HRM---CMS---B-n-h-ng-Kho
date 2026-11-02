using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string Status { get; set; } = "CHO_DUYET"; 
        // Statuses: CHO_DUYET (Chờ duyệt), TRUONG_PHONG_DUYET (Trưởng phòng duyệt), GIAM_DOC_DUYET (Hoàn tất/Giám đốc duyệt), TU_CHOI (Bị từ chối)

        public int? DepartmentHeadId { get; set; }
        public DateTime? DepartmentHeadApprovedAt { get; set; }
        [MaxLength(500)]
        public string? DepartmentHeadNote { get; set; }

        public int? DirectorId { get; set; }
        public DateTime? DirectorApprovedAt { get; set; }
        [MaxLength(500)]
        public string? DirectorNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

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
        public string Status { get; set; } = "DRAFT"; 
        // Statuses: DRAFT (Dự thảo), CALCULATED (Đã tính toán), LOCKED (Đã chốt lương), PAID (Đã giải ngân)

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGrossSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalNetSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInsurance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPIT { get; set; } = 0; // Thuế TNCN

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
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

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableIncome { get; set; } = 0; // Thu nhập tính thuế TNCN

        [Column(TypeName = "decimal(18,2)")]
        public decimal PersonalIncomeTax { get; set; } = 0; // Thuế TNCN lũy tiến

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; } = 0; // Thực lĩnh

        [Required, MaxLength(50)]
        public string Status { get; set; } = "DRAFT"; // DRAFT, SENT (Đã gửi cho NV), CONFIRMED (NV đã xác nhận)

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PayrollPeriodId")]
        public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }
}
