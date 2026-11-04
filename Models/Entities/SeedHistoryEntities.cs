using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP_HRM_CMS.Helpers;

namespace ERP_HRM_CMS.Models.Entities
{
    /// <summary>
    /// Lịch sử gán quyền mặc định cho vai trò. Mỗi cặp (vai trò, quyền) chỉ được xét MỘT lần:
    /// quyền mà quản trị viên đã gỡ sẽ không bị gán lại khi ứng dụng khởi động.
    /// </summary>
    [Table("SeedHistories")]
    public class SeedHistory
    {
        [Key, MaxLength(200)]
        public string SeedKey { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; } = VietnamTime.Now;
    }
}
