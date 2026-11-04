using System.ComponentModel.DataAnnotations;

namespace ERP_HRM_CMS.Models.Entities
{
    /// <summary>
    /// Bộ đếm đăng nhập sai theo cặp (tên đăng nhập, địa chỉ IP). Lưu trong database để
    /// không bị đặt lại khi khởi động lại ứng dụng.
    /// </summary>
    public class LoginAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Số lần sai kể từ lần khóa gần nhất (hoặc từ lúc bắt đầu đếm).</summary>
        public int FailedCount { get; set; }

        /// <summary>Số lần đã bị khóa. Quyết định thời gian khóa lần tiếp theo (tăng dần, có giới hạn trên).</summary>
        public int LockoutLevel { get; set; }

        /// <summary>Thời điểm hết khóa (UTC). Null nếu không bị khóa.</summary>
        public DateTime? LockedUntilUtc { get; set; }

        /// <summary>Lần sai gần nhất (UTC). Dùng để đặt lại bộ đếm sau một khoảng thời gian không sai.</summary>
        public DateTime LastFailedUtc { get; set; }
    }
}
