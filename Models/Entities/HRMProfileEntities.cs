using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    // ==========================================
    // PHIÊN 5 — HỒ SƠ NHÂN VIÊN MỞ RỘNG
    // ==========================================

    /// <summary>Người thân / liên hệ khẩn cấp của nhân viên</summary>
    [Table("EmployeeRelatives")]
    public class EmployeeRelative
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Quan hệ: Cha, Mẹ, Vợ, Chồng, Con, Anh/Chị/Em, Khác</summary>
        [Required, MaxLength(50)]
        public string Relationship { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        /// <summary>Là người phụ thuộc giảm trừ thuế TNCN</summary>
        public bool IsDependant { get; set; } = false;

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    /// <summary>Kỹ năng của nhân viên</summary>
    [Table("EmployeeSkills")]
    public class EmployeeSkill
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required, MaxLength(200)]
        public string SkillName { get; set; } = string.Empty;

        /// <summary>Cấp độ: Beginner, Intermediate, Advanced, Expert</summary>
        [Required, MaxLength(50)]
        public string Level { get; set; } = "Intermediate";

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    /// <summary>Chứng chỉ / bằng cấp của nhân viên</summary>
    [Table("EmployeeCertificates")]
    public class EmployeeCertificate
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required, MaxLength(300)]
        public string CertificateName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? IssuedBy { get; set; }

        public DateTime? IssuedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    /// <summary>Tài liệu đính kèm hồ sơ nhân viên (CCCD, hợp đồng scan, bằng...)</summary>
    [Table("EmployeeDocuments")]
    public class EmployeeDocument
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required, MaxLength(200)]
        public string DocumentName { get; set; } = string.Empty;

        /// <summary>Loại tài liệu: CCCD, BangCap, HopDong, GiayKhamSucKhoe, Khac</summary>
        [Required, MaxLength(50)]
        public string DocumentType { get; set; } = "Khac";

        [Required, MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>Kích thước file (bytes)</summary>
        public long FileSize { get; set; }

        [MaxLength(50)]
        public string? ContentType { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int? UploadedBy { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("UploadedBy")]
        public virtual User? Uploader { get; set; }
    }

    /// <summary>Lịch sử thay đổi: phòng ban, chức vụ, lương, trạng thái</summary>
    [Table("EmployeeChangeHistories")]
    public class EmployeeChangeHistory
    {
        [Key]
        public long Id { get; set; }

        public int EmployeeId { get; set; }

        /// <summary>Loại thay đổi: Department, Position, Salary, Status</summary>
        [Required, MaxLength(50)]
        public string ChangeType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OldValue { get; set; }

        [MaxLength(500)]
        public string? NewValue { get; set; }

        /// <summary>Ngày có hiệu lực</summary>
        public DateTime EffectiveDate { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>Người thực hiện thay đổi</summary>
        public int? ChangedBy { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("ChangedBy")]
        public virtual User? ChangedByUser { get; set; }
    }
}
