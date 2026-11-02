using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? RecordId { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string? AffectedColumns { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public long Id { get; set; }

        public int? ReceiverUserId { get; set; }

        [MaxLength(50)]
        public string? TargetRole { get; set; } // e.g. SUPERADMIN, SALE_DIRECTOR, WAREHOUSE_STAFF, HR_MANAGER

        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        [MaxLength(50)]
        public string Type { get; set; } = "SYSTEM"; // ORDER, STOCK_ALERT, HRM, SYSTEM

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
