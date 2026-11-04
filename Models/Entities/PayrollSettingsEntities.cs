using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    /// <summary>
    /// Loại phụ cấp dùng trong bảng lương. Mỗi loại có cờ miễn thuế và hạn mức miễn thuế riêng.
    /// Mã "LUNCH" được hiển thị ở cột phụ cấp ăn trưa của phiếu lương; các loại khác cộng vào phụ cấp khác.
    /// </summary>
    [Table("PayrollAllowanceTypes")]
    public class PayrollAllowanceType
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Số tiền mặc định mỗi kỳ (đồng).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultAmount { get; set; } = 0;

        /// <summary>Có được miễn thuế TNCN đến mức hạn mức hay không.</summary>
        public bool IsTaxExempt { get; set; } = false;

        /// <summary>Hạn mức miễn thuế mỗi tháng (đồng). Null = không giới hạn khi được miễn thuế.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TaxExemptLimit { get; set; }

        /// <summary>Nguồn văn bản hoặc căn cứ chính sách.</summary>
        [MaxLength(500)]
        public string? SourceNote { get; set; }

        /// <summary>true = TODO, cần xác nhận cờ miễn thuế hoặc hạn mức trước khi dùng thật.</summary>
        public bool NeedsConfirmation { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Người phụ thuộc của nhân viên, dùng để tính giảm trừ gia cảnh theo từng kỳ.
    /// Người phụ thuộc được tính giảm trừ trong các tháng từ FromDate đến ToDate (ToDate null = còn hiệu lực).
    /// </summary>
    [Table("EmployeeDependents")]
    public class EmployeeDependent
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Quan hệ với người nộp thuế, ví dụ CON, BO_DE, ME_DE, VO_CHONG.</summary>
        [Required, MaxLength(50)]
        public string Relationship { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        /// <summary>Ngày bắt đầu được tính giảm trừ.</summary>
        public DateTime FromDate { get; set; }

        /// <summary>Ngày kết thúc được tính giảm trừ. Null = còn hiệu lực.</summary>
        public DateTime? ToDate { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }
}
