using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    // ═══════════════════════════════════════════════════════════════
    // LOẠI NGHỈ PHÉP
    // ═══════════════════════════════════════════════════════════════
    [Table("LeaveTypes")]
    public class LeaveType : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Số ngày tối đa mỗi năm (0 = không giới hạn)</summary>
        public int MaxDaysPerYear { get; set; } = 12;

        /// <summary>Có trả lương không</summary>
        public bool IsPaid { get; set; } = true;

        /// <summary>Cho phép nghỉ nửa ngày</summary>
        public bool AllowHalfDay { get; set; } = false;

        /// <summary>Yêu cầu đính kèm chứng từ</summary>
        public bool RequiresAttachment { get; set; } = false;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }

    // ═══════════════════════════════════════════════════════════════
    // TIN TUYỂN DỤNG
    // ═══════════════════════════════════════════════════════════════
    [Table("JobPostings")]
    public class JobPosting : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Đường dẫn thân thiện chuẩn SEO (ví dụ: truong-phong-kinh-doanh)</summary>
        [MaxLength(350)]
        public string Slug { get; set; } = string.Empty;

        public int DepartmentId { get; set; }
        public int PositionId { get; set; }

        /// <summary>Số lượng cần tuyển</summary>
        public int Vacancies { get; set; } = 1;

        public string? Description { get; set; }
        public string? Requirements { get; set; }

        /// <summary>Mức lương hiển thị (ví dụ "8-12 triệu")</summary>
        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        [MaxLength(200)]
        public string? WorkLocation { get; set; }

        /// <summary>Draft, Open, Closed, Cancelled</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;
        [ForeignKey("PositionId")]
        public virtual Position Position { get; set; } = null!;
        public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    }

    // ═══════════════════════════════════════════════════════════════
    // ỨNG VIÊN
    // ═══════════════════════════════════════════════════════════════
    [Table("Candidates")]
    public class Candidate : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? ResumeUrl { get; set; }

        [MaxLength(500)]
        public string? CoverLetter { get; set; }

        /// <summary>Applied, Screening, Interview, Offered, Hired, Rejected</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Applied";

        public DateTime AppliedDate { get; set; } = VietnamTime.Now;

        [MaxLength(500)]
        public string? Note { get; set; }

        /// <summary>Điểm đánh giá (0-100)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? EvaluationScore { get; set; }

        public int JobPostingId { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("JobPostingId")]
        public virtual JobPosting JobPosting { get; set; } = null!;

        public virtual ICollection<InterviewSchedule> InterviewSchedules { get; set; } = new List<InterviewSchedule>();
    }

    // ═══════════════════════════════════════════════════════════════
    // ĐÀO TẠO
    // ═══════════════════════════════════════════════════════════════
    [Table("Trainings")]
    public class Training : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TrainerName { get; set; }

        [MaxLength(300)]
        public string? Location { get; set; }

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; } = 0;

        /// <summary>Planned, InProgress, Completed, Cancelled</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Planned";

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        public virtual ICollection<TrainingParticipant> Participants { get; set; } = new List<TrainingParticipant>();
    }

    /// <summary>Bảng trung gian: nhân viên tham gia đào tạo</summary>
    [Table("TrainingParticipants")]
    public class TrainingParticipant
    {
        public int TrainingId { get; set; }
        public int EmployeeId { get; set; }

        [MaxLength(50)]
        public string? Result { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Score { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        // Navigation
        [ForeignKey("TrainingId")]
        public virtual Training Training { get; set; } = null!;
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    // ═══════════════════════════════════════════════════════════════
    // ĐÁNH GIÁ KPI
    // ═══════════════════════════════════════════════════════════════
    [Table("KpiReviews")]
    public class KpiReview : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public int ReviewerId { get; set; }

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalScore { get; set; } = 0;

        /// <summary>A, B, C, D, E</summary>
        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(500)]
        public string? OverallComment { get; set; }

        /// <summary>Draft, Submitted, Approved</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        // IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
        [ForeignKey("ReviewerId")]
        public virtual User Reviewer { get; set; } = null!;
        public virtual ICollection<KpiCriteria> Criteria { get; set; } = new List<KpiCriteria>();
    }

    /// <summary>Tiêu chí KPI chi tiết</summary>
    [Table("KpiCriteria")]
    public class KpiCriteria
    {
        [Key]
        public int Id { get; set; }

        public int KpiReviewId { get; set; }

        [Required, MaxLength(200)]
        public string CriteriaName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Weight { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; } = 0;

        [MaxLength(500)]
        public string? Comment { get; set; }

        // Navigation
        [ForeignKey("KpiReviewId")]
        public virtual KpiReview KpiReview { get; set; } = null!;
    }
}
