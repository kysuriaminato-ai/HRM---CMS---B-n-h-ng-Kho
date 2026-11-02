using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services
{
    public interface INotificationService
    {
        Task<Notification> SendToUserAsync(int userId, string title, string content, string? linkUrl = null, string type = "SYSTEM");
        Task<Notification> SendToRoleAsync(string roleName, string title, string content, string? linkUrl = null, string type = "SYSTEM");
        Task<Notification> BroadcastAsync(string title, string content, string? linkUrl = null, string type = "SYSTEM");
        Task<List<Notification>> GetUserRecentNotificationsAsync(int? userId, string? roleName, int take = 10);
        Task<int> GetUnreadCountAsync(int? userId, string? roleName);
        Task<bool> MarkAsReadAsync(long notificationId, int? userId, string? roleName);
        Task<bool> MarkAllAsReadAsync(int? userId, string? roleName);
    }
}
