using ERP_HRM_CMS.Helpers;
using System;

namespace ERP_HRM_CMS.Models.Entities
{
    /// <summary>
    /// Trạng thái đã đọc của từng người dùng đối với thông báo chung (theo vai trò hoặc toàn hệ thống).
    /// Thông báo cá nhân vẫn dùng cột Notification.IsRead.
    /// </summary>
    public class NotificationRead
    {
        public long NotificationId { get; set; }
        public int UserId { get; set; }
        public DateTime ReadAt { get; set; } = VietnamTime.Now;

        public Notification? Notification { get; set; }
    }
}
